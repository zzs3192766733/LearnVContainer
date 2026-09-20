# 第 06 课：工厂、委托注册与容器 API

> 官方对照：`registering/register-factory`、`registering/register-using-delegate`、`registering/register-callbacks`、`resolving/container-api`

## 本课目标

回答四个问题：

1. **运行时才知道参数的对象，怎么交给容器创建？** → `RegisterFactory`
2. **`builder.Register<T>(lambda)` 和 `RegisterFactory` 有什么区别？** → 执行时机完全不同
3. **怎么在容器构建完成 / 销毁前做点事？** → Build / Dispose 回调
4. **什么时候应该直接调 `Resolve`？** → 很少，但要认识这套 API

---

## 1. 问题：运行时参数 x 容器依赖

```csharp
public class Enemy
{
    public Enemy(EnemyConfig config, int id) { ... }
}
```

- `EnemyConfig` 是**依赖**（容器里有）
- `id` 是**运行时数据**（每创建一个敌人都不一样，不该进容器）

DI 的标准答案：**把"部分应用"做成工厂**。

VContainer 用 `Func<>` 来表达工厂。

### 1.1 只有运行时参数

```csharp
builder.RegisterFactory<float, Bullet>(speed => new Bullet(speed));
```

之后任何类都可以注入 `Func<float, Bullet>`：

```csharp
public class Turret
{
    readonly Func<float, Bullet> createBullet;
    public Turret(Func<float, Bullet> createBullet) => this.createBullet = createBullet;

    public void Fire() => var bullet = createBullet(12.5f);
}
```

这个重载没有 `Lifetime` 参数，因为它注册的其实是那个**委托实例本身**（`RegisterInstance`），
委托是无状态的，永远可以复用。

### 1.2 既要容器依赖，又要运行时参数

```csharp
builder.RegisterFactory<int, Enemy>(container =>
{
    // (A) 这一层在「作用域」里执行，container 是 IObjectResolver
    var config = container.Resolve<EnemyConfig>();
    // (B) 返回真正的工厂函数
    return id => new Enemy(config, id);
}, Lifetime.Scoped);
```

**这里的 `Lifetime` 描述的是"外层 `Func<IObjectResolver, Func<...>>` 多久重新求值一次"**，
也就是**内层 `Func<int, Enemy>` 多久换一个新的闭包**：

| Lifetime | 含义 |
| --- | --- |
| `Lifetime.Transient` | 每次解析 `Func<int, Enemy>` 都重新跑一遍外层 lambda |
| `Lifetime.Scoped` | 每个作用域只跑一次外层 lambda，闭包内的 `config` 只在作用域内解析一次 |
| `Lifetime.Singleton` | 全局只跑一次 |

用 `Scoped`/`Singleton` 可以避免每次创建对象都重新 `Resolve` 依赖。

⚠️ **重要**：VContainer **不会**接管工厂返回对象的生命周期。
如果 `Enemy` 实现了 `IDisposable`，需要你自己负责释放。
（这也是官方推荐"用工厂类而不是 lambda"的原因：工厂类本身由容器管理，实现 `IDisposable` 就会被自动释放。）

### 1.3 推荐的 OOP 写法：工厂类

```csharp
public class EnemyFactory
{
    readonly EnemyConfig config;
    int nextId;

    public EnemyFactory(EnemyConfig config) => this.config = config;

    public Enemy Create() => new Enemy(config, ++nextId);
}

builder.Register<EnemyFactory>(Lifetime.Singleton);
```

优点是：工厂自己也能有状态（比如自增 id）、能被测试、能实现 `IDisposable`。

---

## 2. 委托注册 vs 工厂注册

```csharp
builder.Register<WeaponBag>(container =>
{
    var config = container.Resolve<WeaponConfig>();
    return new WeaponBag(config.WeaponNames);
}, Lifetime.Scoped);
```

| | 委托注册 `Register<T>(Func<IObjectResolver, T>, Lifetime)` | 工厂注册 `RegisterFactory<...>` |
| --- | --- | --- |
| 注册进容器的类型 | **`T` 本身** | **`Func<...>`** |
| lambda 执行时机 | 该注册在所属作用域中**第一次被解析时**，且**只执行一次** | 外层 lambda 按你指定的 Lifetime 执行；内层 `Func` 每次调用都执行 |
| 用途 | 创建过程需要代码（读配置、拼装） | 运行时反复创建新对象 |

> 官方原话：*"These delegates will be executed only once during scope construction.
> If you want to create an instance at any time during runtime, please refer to Register Factory."*

---

## 3. 容器回调

```csharp
protected override void Configure(IContainerBuilder builder)
{
    // 容器构建完成时（此时所有注册已就绪，可以 Resolve）
    builder.RegisterBuildCallback(container =>
    {
        var service = container.Resolve<SomeService>();
        Debug.Log("容器构建完成");
    });

    // 容器被 Dispose 时（做收尾、清理、存档）
    builder.RegisterDisposeCallback(container =>
    {
        Debug.Log("容器即将销毁");
    });
}
```

两个回调的参数都是 `IObjectResolver`。

---

## 4. `IObjectResolver`（Container API）

容器本身也可以被注入 —— 它默认就是注册好的：

```csharp
public MyClass(IObjectResolver container) { }
```

| API | 说明 |
| --- | --- |
| `Resolve<T>(key = null)` | 解析；找不到就**抛异常** |
| `TryResolve<T>(out T, key = null)` | 安全解析，返回 `bool`，不抛异常 |
| `ResolveOrDefault<T>(defaultValue = default, key = null)` | 解析失败返回默认值 |
| `Inject(object)` | 对已有对象执行 `[Inject]` 注入 |
| `InjectGameObject(GameObject)` | 递归注入 GameObject 及其所有后代 |
| `Instantiate(prefab, ...)` | 替代 `UnityEngine.Object.Instantiate`，实例化后自动注入 |
| `CreateScope(Action<IContainerBuilder> = null)` | 开子作用域（第 09 课） |
| `ApplicationOrigin` | 创建这个容器的 `LifetimeScope`（可以用来拿到 scene / transform） |

### 什么时候该用 `Resolve`？

官方给出的态度很明确：

> 只有当**其它受支持的注入方式都不适用**，或者你在使用**会暴露 `IObjectResolver` 的回调**
> （比如 build callback、部分工厂函数）时，才需要显式 `Resolve`。
> 直接 `Resolve` 需要写更多代码，而且会**掩盖你的真实意图**。

换句话说：

- 大多数类应该老老实实写构造函数参数。
- `Resolve` 主要出现在：工厂内部、`RegisterBuildCallback` 里、以及工具/框架代码里。

---

## 5. 本课示例运行结果

```
[Lesson06] ---- 1. 只有运行时参数的工厂 ----
[Lesson06] createBullet(10) -> Bullet(speed=10)
[Lesson06] createBullet(20) -> Bullet(speed=20)
[Lesson06] ---- 2. 依赖 + 运行时参数的工厂 ----
[Lesson06] 【第一次调用】外层 lambda 执行了（Lifetime.Scoped -> 每个作用域只执行一次）
[Lesson06] createEnemy(1) -> Enemy#1(hp=100)
[Lesson06] createEnemy(2) -> Enemy#2(hp=100)
[Lesson06] 【第二次调用】外层 lambda 执行了   ← 不会出现！
[Lesson06] ---- 3. 工厂类 ----
[Lesson06] EnemyFactory.Create() -> Enemy#1(hp=120)
[Lesson06] ---- 4. 委托注册（只执行一次）----
[Lesson06] WeaponBag 的委托被执行了（每个作用域只会执行一次）
[Lesson06] weaponBag -> WeaponBag[Sword, Bow]
[Lesson06] ---- 5. 容器 API ----
[Lesson06] TryResolve<Bullet>()        -> False  （Bullet 只是工厂的产物，不是注册类型）
[Lesson06] TryResolve<Func<float,Bullet>>() -> True
[Lesson06] ResolveOrDefault<Bullet>()  -> (null)
[Lesson06] ---- 回调 ----
[Lesson06] RegisterBuildCallback: 容器构建完成
...
[Lesson06] RegisterDisposeCallback: 容器即将销毁
```

> `RegisterDisposeCallback` 的日志会在你**停止 Play 或切换课程**（销毁 LifetimeScope）时出现。

---

## 6. 常见坑

| 坑 | 说明 |
| --- | --- |
| 把运行时数据（id、坐标）塞进容器 | 会污染注册表，且无法支持"多个不同 id 的敌人" |
| 以为 `RegisterFactory` 返回的对象会被自动 `Dispose` | 不会，自己管 |
| 给 1.1 那种"只有运行时参数"的工厂传 `Lifetime` | 没有这个重载 —— 它注册的是委托实例本身 |
| 在 `Configure` 里就直接 `Resolve` | 此时容器还没构建完，拿不到 `IObjectResolver`；请用 `RegisterBuildCallback` |
| 用 `Register<T>(lambda)` 当"运行时工厂"用 | 它只会执行一次，第二次拿到的还是同一个对象 |
| 到处 `Resolve` | 依赖关系从代码里消失了，等于放弃了 DI 的最大好处 |

---

## 7. 练习题

1. 把 `RegisterFactory<int, Enemy>` 的 Lifetime 改成 `Transient`，观察外层 lambda 的执行次数变化。
2. 让 `Enemy` 实现 `IDisposable`，验证工厂产出的对象**不会**被容器释放。
3. 把 `WeaponBag` 的 Lifetime 改成 `Singleton`，观察委托执行次数。
4. 用 `RegisterBuildCallback` 在容器构建后立刻预热一批对象，并在 `RegisterDisposeCallback` 里清理。
5. 用 `container.Instantiate(prefab)` 替换 `Object.Instantiate(prefab)`，验证 Prefab 上的 `[Inject]` 会被执行。
