# 第 04 课：向 MonoBehaviour 注入依赖

> 官方对照：`resolving/gameobject-injection`、`registering/register-monobehaviour`

## 本课目标

理解 VContainer 对 `MonoBehaviour` 的**态度**，并掌握**三种触发注入的方式**。

这是新手最容易卡住的一课——因为"我明明写了 `[Inject]`，为什么没被调用？"

---

## 1. 为什么 `MonoBehaviour` 不能用构造函数注入

`MonoBehaviour` 是 Unity 用**原生层反序列化**创建的：`Instantiate(prefab)` 时，
Unity 需要一个**无参构造函数**来构造 C# 对象，然后再把序列化字段填进去。

```csharp
public class BadView : MonoBehaviour
{
    public BadView(ScoreService score)   // ❌ Unity 不会调用它，它甚至可能是 null
    {
    }
}
```

所以对 `MonoBehaviour`：

- 构造函数注入 ❌
- 方法注入 `[Inject]` ✅
- 字段注入 `[Inject]` ✅
- 属性注入 `[Inject]` ✅

---

## 2. 最重要的一句话

> **给 `MonoBehaviour` 打 `[Inject]` 不等于注入会发生。`[Inject]` 只是一个"标记"，需要有人来触发。**

具体来说，`[Inject]` 标记的内容会在**以下三种情况之一**发生时被执行：

| # | 触发方式 | 适用场景 |
| --- | --- | --- |
| ① | `builder.RegisterComponent*(...)` 系列 | 场景中/由容器创建的对象，希望容器管起来 |
| ② | `container.InjectGameObject(go)` | 运行时自己 `new GameObject()` 出来的对象 |
| ③ | `LifetimeScope` Inspector 里的 `Auto Inject Game Objects` | 手动拖进去的零散对象 |

---

## 3. 官方为什么"不自动注入所有 MonoBehaviour"

VContainer 的作者在文档里给了明确理由，值得一读：

1. **Unity 没有提供"监听所有 GameObject 创建"的官方钩子**。既然做不到 100% 自动，就不要做"一半自动"——半自动最容易产生"为什么这个注入了那个没有"的困惑。
2. **推荐"注入 MonoBehaviour"，而不是"注入到 MonoBehaviour 里"**。
   - 也就是说：让 **View 组件作为依赖被别人拿到**，而不是让 View 组件去依赖一堆业务对象。
3. **运行时快速变化的数据不要用 `[Inject]`**。比如"当前血量"这种每帧都在变的值，应该作为**方法参数**传进去，而不是当成依赖注入。
   - `[Inject]` 只适合"生命周期稳定、创建后不变"的引用。
4. **保住 Prefab 的可移植性**。如果 Prefab 依赖外部注入才能工作，它就不再是"拖出来就能跑"的了。

---

## 4. 三种触发方式详解

### 4.1 `RegisterComponent*` 家族

```csharp
// (a) 已经拿到的组件实例（比如 [SerializeField] 拖进来的）
builder.RegisterComponent(scoreView);

// (b) 从当前场景里找
builder.RegisterComponentInHierarchy<PlayerView>();

// (c) 用 Prefab 实例化
builder.RegisterComponentInNewPrefab(prefab, Lifetime.Scoped);

// (d) 新建 GameObject 并挂上组件
builder.RegisterComponentOnNewGameObject<AutoCreatedView>(Lifetime.Scoped, "AutoCreatedView");
```

**关键差异（源码 `ContainerBuilderUnityExtensions`）**：

| 方法 | 默认 Lifetime | 是否"被动创建"（没人 Resolve 也会注入） |
| --- | --- | --- |
| `RegisterComponent(instance)` | `Singleton` | ✅ 会（内部挂了 build callback 强制 Resolve） |
| `RegisterComponentInHierarchy<T>()` | 注册时按 `Singleton` 存，**语义上等价于"这一层的 Scoped"** | ✅ 会 |
| `RegisterComponentInNewPrefab(...)` | 你指定 | ❌ 不会，**被 Resolve 时才实例化** |
| `RegisterComponentOnNewGameObject<T>(...)` | 你指定 | ❌ 不会，**被 Resolve 时才创建 GameObject** |

也就是说 (c) (d) 是**懒加载**的。本课示例里，我们让入口点的构造函数依赖 `AutoCreatedView`，容器为了构造入口点就会去把它创建出来——这就是"按需创建"。

> ⚠️ `RegisterComponentInHierarchy<T>()` 的查找范围是**"这个 LifetimeScope 所在的场景"的根物体及其所有子物体**
> （源码 `FindComponentProvider` 用的是 `scene.GetRootGameObjects()` + `GetComponentInChildren`）。
> 它**不会**去别的场景找，也不会去 `DontDestroyOnLoad` 场景找。

### 4.2 `IObjectResolver.InjectGameObject`

```csharp
var go = new GameObject("Runtime");
go.AddComponent<SomeView>();
container.InjectGameObject(go);       // ← 注入 go 及其所有后代上的所有 MonoBehaviour
```

特点：

- **递归**：会遍历所有子物体（源码 `InjectGameObject` 是递归实现）。
- **忽略 `enabled` / `activeSelf`**：不管物体是否激活、组件是否启用，都会注入。
- **不注册**：只是为了注入，容器之后并不知道这个对象的存在（无法 `Resolve<SomeView>()` 拿到它）。
- 想省事的话，官方建议给常用模式写**扩展方法**。

### 4.3 Inspector 里的 Auto Inject Game Objects

`LifetimeScope` 组件在 Inspector 上有一个 `Auto Inject Game Objects` 列表。
把物体拖进去，容器构建完成后会自动对它们调用 `InjectGameObject`。

这是最"Unity 味"的方式，适合零散的编辑器摆放对象。

---

## 5. 本课示例的运行结果

示例里有三个对象：

| 对象 | 触发方式 | 预期输出 |
| --- | --- | --- |
| `PlayerView`（场景中已存在） | `RegisterComponentInHierarchy<PlayerView>()` | 属性/方法注入都执行 |
| `AutoCreatedView`（容器新建） | `RegisterComponentOnNewGameObject<AutoCreatedView>(Scoped, "AutoCreatedView")`，由入口点的构造函数拉起来 | 字段注入 + `[Inject]` 方法注入执行 |
| `LateInjected`（运行时新建） | `container.InjectGameObject(go)` | 注入执行 |

Console：

```
[Lesson04] PlayerView 'PlayerView (Scene Object)' 属性注入完成, Score = 100
[Lesson04] PlayerView 'PlayerView (Scene Object)' 的 [Inject] 方法被调用, Score = 100
[Lesson04] AutoCreatedView 由容器创建：name=AutoCreatedView, Score=100（字段注入）
[Lesson04] AutoCreatedView.Initialize() 被调用（方法注入，晚于字段注入）
[Lesson04] 手动 InjectGameObject：PlayerView 'LateInjected (InjectGameObject)' ...
```

注意 `AutoCreatedView` 的 GameObject 名字正好是我们传给 `RegisterComponentOnNewGameObject` 的名字，而且它被挂在了 `Lesson04LifetimeScope` 的 `transform` 下面（因为用了 `.UnderTransform(transform)`）。

### 5.1 追加内容（动态 Prefab）的运行结果

本课后来又补了"动态创建 Prefab"的例子（详见第 9 节），它多出四个对象：

| 对象 | 触发方式 | 预期输出 |
| --- | --- | --- |
| `EnemyPrefabView#2` | `resolver.Instantiate(prefab, parent)` | 注入执行；`Awake 时已注入 = True` |
| `EnemyPrefabView#3` | `Object.Instantiate` 后补 `resolver.InjectGameObject(raw)` | 补注入后 `Score = 100`，但 `Awake 时已注入 = False` |
| `EnemyPrefabView#4` | 注入的 `Func<EnemyPrefabView>` 工厂 | 注入执行；`Awake 时已注入 = True` |
| `EnemyPrefabView#5` | `Object.Instantiate`（**反例**） | `Score = null（没有被注入！）` |

> 编号从 `#2` 开始：作为"伪 Prefab 模板"的那个对象本身也是 `EnemyPrefabView` 的一个实例，它占了 `#1`。

Console（追加部分）：

```
[Lesson04] EnemyPrefabView#2 的 [Inject] 方法注入被调用，Score = 100
[Lesson04] EnemyPrefabView#2 [① resolver.Instantiate(prefab, parent)] Score = 100, Awake 时已注入 = True
[Lesson04] EnemyPrefabView#3 补注入【之前】: Score 是 null ? -> True
[Lesson04] EnemyPrefabView#3 的 [Inject] 方法注入被调用，Score = 100
[Lesson04] EnemyPrefabView#3 [② Object.Instantiate + InjectGameObject] Score = 100, Awake 时已注入 = False
[Lesson04] EnemyPrefabView#4 的 [Inject] 方法注入被调用，Score = 100
[Lesson04] EnemyPrefabView#4 [③ 注入 Func<EnemyPrefabView> 工厂] Score = 100, Awake 时已注入 = True
[Lesson04] EnemyPrefabView#5 [④ 反例 Unity 原生 Instantiate（没注入）] Score = null（没有被注入！）, Awake 时已注入 = False
```

**这两列就是本节的全部看点**：`Score` 说明"有没有被注入"，`Awake 时已注入` 说明"注入发生在 `Awake` 之前还是之后"。

---

## 6. 顺手认识两个链式 API

```csharp
builder.RegisterComponentOnNewGameObject<YourView>(Lifetime.Scoped, "MyView")
       .UnderTransform(parentTransform)   // 挂到指定父物体下
       .DontDestroyOnLoad();              // 设为跨场景常驻
```

`UnderTransform` 还有 `Func<Transform>` / `Func<IObjectResolver, Transform>` 重载，可以**在运行时**决定父物体：

```csharp
builder.RegisterComponentOnNewGameObject<YourView>(Lifetime.Scoped)
       .UnderTransform(() => currentStageRoot);
```

批量写法的糖：

```csharp
builder.UseComponents(components =>
{
    components.AddInstance(playerView);
    components.AddInHierarchy<EnemyView>();
    components.AddOnNewGameObject<HudView>(Lifetime.Scoped, "Hud");
    components.AddInNewPrefab(enemyPrefab, Lifetime.Scoped);
});
```

---

## 7. ⚠️ 一个很重要的坑：Scoped 的 MonoBehaviour 不会跟着作用域销毁

官方专门警告过：

> 如果**场景还活着**，只有 `LifetimeScope` 被销毁，那么以 `Lifetime.Scoped` 注册的 **MonoBehaviour 不会被自动销毁**。

因为容器释放的是 C# 对象引用，它没有权力去 `Destroy` 一个 GameObject。

**解决办法：**

1. 把 View 设为 `LifetimeScope` 所在 GameObject 的**子物体**（本课就是这么做的，用了 `.UnderTransform(transform)`）。
2. 或者让 View 自己实现 `IDisposable`，在 `Dispose()` 里 `Destroy(gameObject)`。

---

## 8. 常见坑

| 坑 | 现象 | 解法 |
| --- | --- | --- |
| 只写了 `[Inject]`，没注册也没 InjectGameObject | 字段一直是 `null` | 用上面三种方式之一触发 |
| `RegisterComponentInHierarchy<T>()` 找不到对象 | 构建时报 `T is not in this scene` | 确认对象在**同一个场景**里，且是从根物体可达的 |
| `RegisterComponent(instance)` 传了 `null` | 构建时报错 | 检查 `[SerializeField]` 是否拖了引用 |
| 用 `UnityEngine.Object.Instantiate(prefab)` 而不是 `container.Instantiate(prefab)` | 新对象上的 `[Inject]` 没执行 | 换成 `container.Instantiate(prefab)`（详见第 9 节） |
| 同一个**具体类型**以 `Lifetime.Singleton` 注册了两次 | 构建时抛 `VContainerException: Conflict implementation type` | 同一个具体类型 + Singleton 在同一作用域只能注册一次（其它生命周期是"后者生效"，详见第 08 课） |

---

## 9. 追加：动态创建的 Prefab 怎么注入

> 这一节补上 §4.1(c) 没展开的部分：**运行时**创建 Prefab 实例，怎么让 `[Inject]` 生效。
>
> 本课示例里的"伪 Prefab"就是一个普通 `GameObject` + `EnemyPrefabView` 组件 ——
> 真实项目里它等价于你在 Inspector 上拖的那个 Prefab 资源（`[SerializeField] EnemyPrefabView`）。

### 9.1 前提：MonoBehaviour 的注入点必须显式标 `[Inject]`

因为 Unity 用无参构造函数反序列化组件，构造函数注入对 MonoBehaviour 用不上。
VContainer 对 MonoBehaviour 只认**显式标记**：

| 位置 | 要求 | 源码 |
| --- | --- | --- |
| 字段 | **`[Inject]` only** | `TypeAnalyzer.cs:292-296` |
| 属性 | **`[Inject]` only** | `TypeAnalyzer.cs:320-324` |
| 方法 | **`[Inject]` only** | `TypeAnalyzer.cs:267-271` |

而且 —— **这个 MonoBehaviour 自己不需要注册**。`Inject` 只负责把它的依赖填进去，
被依赖的服务（本课的 `ScoreService`）才需要注册。

### 9.2 四种写法对照

| # | 写法 | 会注入吗 | 适用场景 |
| --- | --- | --- | --- |
| ① | `resolver.Instantiate(prefab, parent)` | ✅ | **推荐**：由你负责创建 |
| ② | `Object.Instantiate(...)` + `resolver.InjectGameObject(go)` | ✅（事后补） | 对象是 Unity / 第三方代码创建的 |
| ③ | 注入 `Func<EnemyPrefabView>`（由 `RegisterFactory` 提供） | ✅ | 业务类不想碰 `IObjectResolver` |
| ④ | `Object.Instantiate(...)`（原生） | ❌ | 反例，用来对照 |

①③④ 的差别全在下面这两张表里：

```csharp
// EnemySpawner.cs（本课新增）
public EnemyPrefabView Spawn(Transform parent)                       // ①
    => resolver.Instantiate(prefab, parent);

public EnemyPrefabView CreateRaw(Transform parent)                   // ④ 反例 / ② 的前半段
{
    var go = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
    return go.GetComponent<EnemyPrefabView>();
}

public EnemyPrefabView Patch(EnemyPrefabView alreadyCreated)         // ② 后半段
{
    resolver.InjectGameObject(alreadyCreated.gameObject);
    return alreadyCreated;
}
```

```csharp
// Lesson04LifetimeScope.cs（本课新增）—— ③ 工厂
builder.RegisterFactory<EnemyPrefabView>(
    resolver => () => resolver.Instantiate(enemyPrefab, transform),
    Lifetime.Singleton);
```

```csharp
// PrefabSpawnDemo.cs —— ③ 的消费方式：完全不出现 IObjectResolver
public PrefabSpawnDemo(EnemySpawner spawner, Func<EnemyPrefabView> createEnemy, LifetimeScope scope)
...
createEnemy().Report("③ 注入 Func<EnemyPrefabView> 工厂");
```

**`resolver.Instantiate` 的重载**（`ObjectResolverUnityExtensions`，`Component` 和 `GameObject` 各有一套）：

| 重载 | 父节点 | 位置 |
| --- | --- | --- |
| `Instantiate(prefab)` | 无 | 用 prefab 自身的 transform |
| `Instantiate(prefab, parent, worldPositionStays = false)` | 指定 | — |
| `Instantiate(prefab, position, rotation)` | 见 §9.6 的说明 | 指定 |
| `Instantiate(prefab, position, rotation, parent)` | 指定 | 指定 |

> 这两个扩展方法在 **`VContainer.Unity`** 命名空间，别忘 `using`。
> 忘了的话 `resolver.Instantiate(...)` 会解析成 Unity 的 `Object.Instantiate`，行为就变成 ④ 了。

### 9.3 ⚠️ 坑 1：工厂要选对重载

同样是"注册工厂"，两个重载的语义完全不同：

```csharp
// ✅ 正确：能拿到 resolver，造出来的对象会被注入
builder.RegisterFactory<EnemyPrefabView>(
    resolver => () => resolver.Instantiate(enemyPrefab, transform),
    Lifetime.Singleton);
//   → RegisterFactory<T>(Func<IObjectResolver, Func<T>>, Lifetime)

// ❌ 错误：内部只是 RegisterInstance(factory)，闭包拿不到 resolver → 不会被注入
builder.RegisterFactory<EnemyPrefabView>(() => UnityEngine.Object.Instantiate(enemyPrefab));
//   → RegisterFactory<T>(Func<T>)
```

源码里后者的实现是：

```csharp
public static RegistrationBuilder RegisterFactory<T>(this IContainerBuilder builder, Func<T> factory)
    => builder.RegisterInstance(factory);      // ← 只是把一个委托对象注册进去，没有 resolver
```

**判断口诀：工厂 lambda 的参数里有没有 `IObjectResolver`。** 没有，就一定是注册实例那条路。

### 9.4 ⚠️ 坑 2：Prefab 引用别用 `RegisterInstance` 送进来

Prefab 是 Unity 资源，不是容器里的服务，所以需要想办法把它交给需要它的类。两种常见写法：

```csharp
// ❌ 埋雷写法
builder.RegisterInstance(enemyPrefab);
```

`RegisterInstance<TInterface>(TInterface instance)` 内部是
`builder.Register(new InstanceRegistrationBuilder(instance)).As(typeof(TInterface))`，
`TInterface` 会被推断成 **`EnemyPrefabView`**。后果：

- `Resolve<EnemyPrefabView>()` 返回的是 **Prefab 资源本身**，不是敌人实例
- 任何地方 `[Inject] EnemyPrefabView` 拿到的都是那个模板
- 它固定是 `Singleton`、**容器永不 Dispose 它**、**也不会对它做注入**
  （`ExistingInstanceProvider` 被容器的 Dispose 逻辑显式排除）

```csharp
// ✅ 推荐：作为构造参数注入，不占用任何类型
builder.Register<EnemySpawner>(Lifetime.Scoped)
       .WithParameter(enemyPrefab);
```

```csharp
public EnemySpawner(IObjectResolver resolver, EnemyPrefabView prefab)   // ← 这个 prefab 由 WithParameter 提供
```

原理是 `ResolveOrParameter` 会**先查 `Parameters`**，按类型匹配到就直接给值，
查不到才回落去 `Resolve`（`IObjectResolverExtensions.cs:41-66`）。
于是容器里**没有** `EnemyPrefabView` 这条注册，类型名干干净净。

另一种同样干净的做法是包一层专用类型再 `RegisterInstance`：

```csharp
public sealed class EnemyPrefabAsset { public readonly EnemyPrefabView Prefab; /* ... */ }
builder.RegisterInstance(new EnemyPrefabAsset(enemyPrefab));
```

### 9.5 ⚠️ 坑 3：`RegisterComponentInNewPrefab` 是懒创建，且只能绑一个

```csharp
builder.RegisterComponentInNewPrefab<EnemyView>(enemyPrefab, Lifetime.Scoped);
```

- **懒创建**：没人 `Resolve<EnemyView>()` 就不会建（和 §4.1 的表一致），
  需要靠别的类的构造函数依赖把它拉起来
- **一个注册绑一个固定的 Prefab 实例**，不能"每次调用都新建一个"
- 想要"按需反复创建"，用 §9.3 的工厂或者 ①

### 9.6 注入时机：为什么 `Awake()` 里读 `[Inject]` 字段是安全的

所有 VContainer 的实例化路径都是这个套路（`ObjectResolverUnityExtensions.cs:72-96`、
`PrefabComponentProvider.cs:31-58`、`NewGameObjectProvider.cs:35-47`）：

```
① 记住模板的 active 状态，然后 SetActive(false)
② 实例化  —— 因为模板是 inactive，克隆体也是 inactive → 克隆体的 Awake() 不会跑
③ 注入（字段 → 属性 → 方法）
④ finally 里恢复 SetActive(模板原状态) → 克隆体被激活 → 此时 Awake() 才执行
```

所以：

- **走 ①③ 创建的实例**，`Awake()` 里读 `[Inject]` 字段一定是安全的（本课的 `Awake 时已注入 = True`）
- **走 ④ 原生 `Object.Instantiate`**，`Awake()` 会在注入之前跑（`= False`）——这就是为什么反例的字段是 `null`
- **走 ②**，`Awake()` 也是提前跑的（`= False`），只能靠事后 `InjectGameObject` 把值补上

> 这也是"模板必须是 active 的"的原因：如果模板本身是 inactive，
> 第 ④ 步恢复时克隆体也会被设成 inactive，`Awake()` 永远不跑。

### 9.7 别忘了：动态创建的实例不在容器的生命周期管理内

`resolver.Instantiate(...)` 造出来的对象，容器**没有持有它的引用**，
所以它不会被 `Dispose`、也不会跟着作用域自动销毁。要自己负责：

- 把实例挂成 `LifetimeScope` 的**子物体**（本课就是这么做的：`parent = scope.transform`），
  这样 `LifetimeScope` 被销毁时它会跟着一起销毁
- 或者自己实现 `IDisposable` / 在合适时机 `Destroy`

---

## 10. 练习题

1. 把 `RegisterComponentInHierarchy<PlayerView>()` 换成 `RegisterComponentOnNewGameObject<PlayerView>(Lifetime.Scoped, "PlayerView")`，观察 `Awake` 里手动创建的那个对象还会不会被注入。
2. 把 `AutoCreatedView` 的 Lifetime 改成 `Singleton`，再跑一次，观察创建时机有没有变化。
3. 在 `LateInjected` 那个 GameObject 下再挂一个子物体并给它也加上 `[Inject]`，验证 `InjectGameObject` 的递归行为。
4. 把 `.UnderTransform(transform)` 删掉，观察 `AutoCreatedView` 出现在场景的什么位置，以及销毁 `LifetimeScope` 后它会不会留下。
5. 把 `EnemySpawner.Spawn()` 里的 `parent` 换成 `null`，再换成 `resolver.Instantiate(prefab, position, rotation)` 这个重载，观察新对象出现在场景的什么位置（提示：这个重载会看 `resolver.ApplicationOrigin`，见 §9.2 的表）。
6. 把工厂那行改成错误重载 `builder.RegisterFactory<EnemyPrefabView>(() => UnityEngine.Object.Instantiate(enemyPrefab));`，观察 ③ 的输出是不是变成了 `Score = null（没有被注入！）`。
7. 把 `WithParameter(enemyPrefab)` 删掉，看看构建容器时报什么错（提示：`No such registration of type: VContainerTutorials.Lesson04.EnemyPrefabView`）。想清楚：为什么"用 `WithParameter` 传 Prefab"和"把 Prefab 注册进容器"是两件不同的事？
