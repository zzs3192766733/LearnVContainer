# 第 08 课：Key 与集合注入

> 官方对照：`registering/register-with-keys`、`registering/register-collection`

## 本课目标

1. **同一个接口有多个实现**时，怎么区分？（`.Keyed()` + `[Key]`）
2. 怎么**一次性拿到所有实现**？（`IEnumerable<T>` / `IReadOnlyList<T>`）
3. 这两种做法的**副作用**和官方推荐姿势。

---

## 1. 用 Key 区分多个实现

```csharp
public enum WeaponType { Primary, Secondary, Special }

builder.Register<IWeapon, Sword>(Lifetime.Singleton).Keyed(WeaponType.Primary);
builder.Register<IWeapon, Bow>(Lifetime.Singleton).Keyed(WeaponType.Secondary);
builder.Register<IWeapon, MagicStaff>(Lifetime.Singleton).Keyed(WeaponType.Special);
```

**Key 可以是任意类型**：枚举、字符串、整数……（内部会走 `Equals`/`GetHashCode` 比较）

```csharp
builder.Register<IEnemy, Goblin>(Lifetime.Singleton).Keyed("goblin");
builder.Register<ILevel, Level1>(Lifetime.Singleton).Keyed(1);
```

### 消费方式 A：构造/方法参数上的 `[Key]`

```csharp
public KeyedWeaponUser(
    [Key(WeaponType.Primary)]   IWeapon primary,
    [Key(WeaponType.Secondary)] IWeapon secondary,
    [Key(WeaponType.Special)]   IWeapon special)
```

`[Key]` 可以标在**构造函数参数、方法参数、字段、属性**上：

```csharp
public class WeaponHolder
{
    [Inject]
    [Key(WeaponType.Primary)]
    public IWeapon PrimaryWeapon { get; set; }

    [Inject]
    [Key("orc")]
    IEnemy orcEnemy;      // 字段上也必须同时写 [Inject]
}
```

⚠️ 字段/属性上**必须同时写 `[Inject]` 和 `[Key]`**。只写 `[Key]` 不会触发注入。

### 消费方式 B：容器 API

```csharp
var primary = container.Resolve<IWeapon>(WeaponType.Primary);
var goblin  = container.Resolve<IEnemy>("goblin");
var level1  = container.Resolve<ILevel>(1);

if (container.TryResolve<IWeapon>(out var special, WeaponType.Special))
{
    // 拿到了
}
```

`TryResolve` 在 Key 不存在时**返回 false，不抛异常**。

### ⚠️ 用了 Key 之后，不带 Key 就解析不到了

```csharp
container.Resolve<IWeapon>();   // ❌ VContainerException: No such registration of type: IWeapon
```

原理（源码 `Registry.AddToBuildBuffer`）：带 Key 的注册只会写入 `(IWeapon, Primary)` 这种
"类型 + Key"的键；`(IWeapon, null)` 这个键**不会**被写入。
所以**同一个接口要么"全带 Key"，要么"全不带 Key"**，混用会很难受。

---

## 2. 集合注入：一次拿到所有实现

只要**同一个服务类型被注册了多次**，VContainer 就会自动生成一个"集合注册"，
你可以用 `IEnumerable<T>` 或 `IReadOnlyList<T>` 注入：

```csharp
builder.Register<IWeapon, Sword>(Lifetime.Singleton).Keyed(WeaponType.Primary);
builder.Register<IWeapon, Bow>(Lifetime.Singleton).Keyed(WeaponType.Secondary);
builder.Register<IWeapon, MagicStaff>(Lifetime.Singleton).Keyed(WeaponType.Special);
```

```csharp
public WeaponCatalog(IReadOnlyList<IWeapon> all, IEnumerable<IWeapon> alsoAll)
{
    Debug.Log($"共 {all.Count} 把武器");
}
```

输出：

```
[Lesson08] 集合注入：共 3 把武器
[Lesson08]   - 长剑 (atk 12)
[Lesson08]   - 弓 (atk 8)
[Lesson08]   - 法杖 (atk 20)
```

### 实现原理（源码 `Registry.AddToBuildBuffer`）

注册表除了 `(服务类型, Key)` 这个键，还会额外维护一个 `(服务类型, AnyKey)`：
第二次注册同一个服务类型时，容器就构造一个 `CollectionInstanceProvider`，
并把它同时注册到 `IEnumerable<T>` 和 `IReadOnlyList<T>` 两个键上。

- 集合本身是 `Lifetime.Transient`（每次解析新建一个数组），**但里面每个元素的 Lifetime 保持不变**。
- 集合的**顺序 = 注册顺序**。
- 集合**不只是"重复注册"的副产品**：它也是 VContainer 内部驱动 `ITickable` 等标记接口的机制。

### 子作用域会合并父作用域的集合

源码 `CollectionInstanceProvider.CollectFromParentScopes`：在子作用域里解析集合时，
会把父作用域的同类注册**一并收集**进来。其中：

- `Lifetime.Singleton` 的元素会**从注册它的那个作用域**解析（保证是同一个实例）；
- 其它元素在当前作用域解析。

---

## 3. ⚠️ 重复注册的真实行为

这是很多教程写错的地方。**以 1.19.0 源码为准：**

| 场景 | 行为 |
| --- | --- |
| 同一个**具体类型** + `Lifetime.Singleton` 注册两次 | **构建时抛异常** `VContainerException: Conflict implementation type`（源码 `CollectionInstanceProvider.Add` 专门拦这一种） |
| 同一个**具体类型** + 其它 Lifetime 注册两次 | 不报错。直接 `Resolve<T>()` 拿到**最后注册的那个**；所有注册都能通过 `IReadOnlyList<T>` 拿到 |
| 同一个**接口**注册给**不同实现** | 正常，就是集合注册的经典用法 |

所以官方文档里"Singleton 同一类型不能重复注册"的说法是准确的，
但它**只是 Singleton 的限制**，不要把它推广到所有情况。

---

## 4. 官方对 Key 的态度（值得一读）

文档在 `register-with-keys` 页面的 Note 里写得很直接：

> - 不要因为有了 Key 就觉得"什么都能 DI"，**不推荐注入细粒度的值**。
> - 一般做法是：**先创建工厂（Factory）或提供者（Provider），再对工厂做 DI**。
> - 只有在这两种情况才考虑用 Key：
>   1. 明确属于不必要的抽象；
>   2. 由于某些原因无法用工厂 / 提供者方式实现。

为什么？因为 Key 是**字符串/数字味的魔法值**：

```csharp
[Key("goblin")] IEnemy e      // ← 这个 "goblin" 写错一个字母，编译期完全发现不了
```

而工厂是这样：

```csharp
public interface IEnemyProvider { IEnemy Get(EnemyKind kind); }

builder.Register<IEnemyProvider, EnemyProvider>(Lifetime.Singleton);
```

调用方写 `provider.Get(EnemyKind.Goblin)`，**类型安全、可测试、可追踪**。
所以：**能用强类型 API（方法/工厂）表达的选择，就别用 Key。**

---

## 5. 本课示例运行结果

```
[Lesson08] ---- 1. 构造参数上的 [Key] ----
[Lesson08] 主武器 = 长剑 (atk 12)
[Lesson08] 副武器 = 弓 (atk 8)
[Lesson08] 特殊武器 = 法杖 (atk 20)
[Lesson08] ---- 2. 容器 API 带 Key 解析 ----
[Lesson08] container.Resolve<IWeapon>(WeaponType.Primary) -> 长剑，与注入的是同一实例？ True
[Lesson08] container.TryResolve<IWeapon>(out _, "missing") -> False（不抛异常）
[Lesson08] ---- 3. 集合注入 ----
[Lesson08] IReadOnlyList<IWeapon> 共 3 把武器
[Lesson08]   - 长剑 (atk 12)
[Lesson08]   - 弓 (atk 8)
[Lesson08]   - 法杖 (atk 20)
[Lesson08] IEnumerable<IWeapon> 也是同一批（3 把）
```

---

## 6. 常见坑

| 坑 | 说明 |
| --- | --- |
| 注册用了 Key，解析没用 | `Resolve<IWeapon>()` 直接抛异常 |
| 字段/属性上只写 `[Key]` 忘了 `[Inject]` | 注入不会发生 |
| Key 写错 | 运行/构建期才报错，编译器帮不了你 |
| 以为集合注入的顺序是随机的 | 是**注册顺序** |
| 以为集合是单例 | 集合本身是 Transient，每次解析都新建数组（元素按各自 Lifetime 复用） |
| 在子作用域里 Resolve 集合 | 会把父作用域的同类注册也收集进来 |

---

## 7. 练习题

1. 把 `[Key(WeaponType.Primary)]` 改成 `[Key("primary")]`，同时把注册改成 `.Keyed("primary")`，验证字符串 Key。
2. 故意把 `[Key]` 的枚举值写成一个没注册过的值，观察报错信息。
3. 把三个武器都改成 `Lifetime.Transient`，观察集合每次解析出来的实例不同。
4. 把一个武器的注册从 `.Keyed(...)` 改成不带 Key，观察 `Resolve<IWeapon>()` 是否变得可用（提示：这是"混用"的坑）。
5. 把 `KeyedWeaponUser` 改成注入 `IReadOnlyList<IWeapon>`，用代码自己挑武器——对比一下两种设计哪个更不容易出错。
