# 第 03 课：注入的四种形态

> 官方对照：`resolving/constructor-injection`、`resolving/method-injection`、`resolving/property-field-injection`

## 本课目标

掌握 VContainer 支持的**全部注入方式**、它们的**执行顺序**，以及一个关键问题：

> 什么时候必须用 `[Inject]`，什么时候不需要？

---

## 1. 总览

| 方式 | 是否需要 `[Inject]` | 能否用于 `MonoBehaviour` | 官方推荐度 |
| --- | --- | --- | --- |
| **构造函数注入** | ❌ 不需要 | ❌ 不行（Unity 不允许） | ⭐⭐⭐⭐⭐ 首选 |
| **方法注入** | ✅ 需要 | ✅ 可以 | ⭐⭐⭐ |
| **字段注入** | ✅ 需要 | ✅ 可以 | ⭐⭐ |
| **属性注入** | ✅ 需要 | ✅ 可以 | ⭐⭐ |

**执行顺序**（源码 `ReflectionInjector.Inject`）：

```
构造（CreateInstance）
   ↓
字段注入（InjectFields）
   ↓
属性注入（InjectProperties）
   ↓
方法注入（InjectMethods）
```

每创建一个对象，这个流程都会完整跑一遍。

---

## 2. 构造函数注入（推荐）

```csharp
public sealed class ConstructorInjectionTarget
{
    readonly Logger logger;

    public ConstructorInjectionTarget(Logger logger)   // ← 唯一的"声明"
    {
        this.logger = logger;
    }
}
```

**规则（源码 `TypeAnalyzer.Analyze`）：**

1. 类里只有一个构造函数 → 直接用它。
2. 有多个构造函数，且**恰好有一个**标了 `[Inject]` → 用标了的那个。
3. 有多个构造函数，**没有一个**标 `[Inject]` → 用**参数最多**的那个。
4. 有多个构造函数，且**多于一个**标了 `[Inject]` → **抛异常**。

### 多构造函数的例子

```csharp
public sealed class MultiConstructorTarget
{
    public readonly string Used;

    public MultiConstructorTarget()                 // 参数 0 个
    {
        Used = "无参构造";
    }

    [Inject]                                        // ← 显式指定
    public MultiConstructorTarget(Logger logger)    // 参数 1 个
    {
        Used = "被 [Inject] 标记的构造函数";
    }
}
```

实测输出是 `被 [Inject] 标记的构造函数`。

> **为什么不干脆用参数最多的那个？**
> 因为参数最多的构造函数不一定是你想要的"契约"。
> 显式 `[Inject]` 让意图可读，也能避免"加了个可选参数就悄悄换了构造函数"这种事故。

### 官方为什么力推构造函数注入

1. **最简单**：不需要任何容器 API，甚至不需要 `using VContainer;`。
2. **依赖一目了然**：构造函数多长，你就知道这个类负了多少责任。构造函数长到 8 个参数，说明这个类该拆了。
3. **可以手动 `new`**：单元测试里直接 `new MyClass(new FakeLogger())` 就行，不需要容器。
4. **`readonly` 保证不可变**：依赖注入一次，之后永不改变。
5. **对象要么完全可用，要么根本不存在**：不存在"字段还没注入就去用"的半成品状态。

### 构造函数不支持可选依赖

```csharp
public MyClass(ILogger logger = null)   // ❌ 不要指望这样写能"可选"
```

如果 `ILogger` 没注册，容器构建时就会抛异常，而不是给你一个 `null`。
**这是有意为之**：DI 的价值就是"缺依赖 = 立刻失败"，而不是运行到一半才 `NullReferenceException`。

如果某天需要处理 Unity 的 **代码剥离（Code Stripping）** 问题（IL2CPP 构建后构造函数被裁掉），
官方给了两种解法：

1. 加 `link.xml`；
2. 给那**一个**构造函数加 `[Inject]`（有了显式引用，剥离器就不会删）。

---

## 3. 方法注入（`MonoBehaviour` 的救命稻草）

```csharp
public sealed class MethodInjectionTarget
{
    Logger logger;

    [Inject]
    public void Construct(Logger logger)   // 方法名随便起，Construct / Initialize / Setup 都行
    {
        this.logger = logger;
    }
}
```

要点：

- 方法**名字随便起**，访问级别也随便（`public` / `private` / `protected` / `internal` 都可以）。
- 一个类可以有**多个** `[Inject]` 方法，按声明顺序依次调用。
- **构造 → 字段 → 属性 → 方法**，所以方法注入时其它依赖已经就位了 —— 这是它比字段注入更适合"做初始化"的原因。

### 它在 `MonoBehaviour` 上才真正发光

`MonoBehaviour` **不允许自定义构造函数**（Unity 内部会调用它的构造函数来反序列化组件）。
所以对 `MonoBehaviour` 来说，方法注入就是"替代构造注入"的标准姿势：

```csharp
public class PlayerView : MonoBehaviour
{
    float speed;

    [Inject]
    public void Construct(GameSettings settings)
    {
        speed = settings.Speed;
    }
}
```

⚠️ **但是**，`[Inject]` 只是"标记"，**不会自动被调用**。
具体怎么触发，是第 04 课的主题。

---

## 4. 字段注入 / 属性注入

```csharp
public sealed class FieldPropertyInjectionTarget
{
    [Inject]
    Logger logger = null;             // 私有字段

    [Inject]
    public AppConfig Config { get; set; }   // 属性（需要有 set）
}
```

- 字段和属性**必须同时能写**（字段不能是 `readonly` / `const`，属性必须有 setter）。
- 它们比构造函数注入弱：编译器无法保证"注入过"，也无法保证顺序。
- **能用构造函数就用构造函数**，这两个留给"框架/Editor 帮你 new 对象、你插不进构造函数"的场景。

---

## 5. 继承：基类的注入点也会生效

```csharp
public abstract class BaseTarget
{
    [Inject] protected Logger BaseLogger = null;
}

public sealed class DerivedTarget : BaseTarget
{
    AppConfig config;

    [Inject]
    void Construct(AppConfig config) => this.config = config;
}
```

源码里 `TypeAnalyzer.Analyze` 会从当前类型**一路向上遍历到 `object`**，
所以基类的 `[Inject]` 字段/属性/方法都会被收集。

两个细节：

1. **构造函数只看"本类"**（`BindingFlags.DeclaredOnly`）。基类构造函数由 C# 的构造链负责。
2. **基类和方法被覆写时只注入一次**：如果子类 `override` 了基类的 `[Inject]` 方法，VContainer 用 `GetBaseDefinition()` 去重。

---

## 6. 带 Key 的注入（先用着，第 08 课详解）

同一个接口有多个实现时：

```csharp
public sealed class WeaponHolder
{
    public WeaponHolder(
        [Key(WeaponType.Primary)]   IWeapon primary,
        [Key(WeaponType.Secondary)] IWeapon secondary)
    { ... }
}
```

⚠️ `[Key]` **必须和注册端的 `.Keyed(key)` 配对使用**，否则解析时找不到。
字段/属性上用 Key 时，**必须同时写 `[Inject]` 和 `[Key]`**（只写 `[Key]` 不生效）。

---

## 7. 抑制 IDE 的"未使用"警告

如果你用 Rider，它会把只被容器调用的构造函数标成 "never used"。
VContainer 的 `InjectAttribute` 已经带了 `JetBrains.Annotations.MeansImplicitUse`，
所以**加了 `[Inject]` 的方法不会报警**；构造函数则可能被误报，可以这样处理：

```csharp
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public Dependency(GameContext game, StatusContext status) { ... }
```

---

## 8. 常见坑

| 坑 | 说明 |
| --- | --- |
| 给 `MonoBehaviour` 写构造函数注入 | 编译能过，但 Unity 会在反序列化时调用**无参**构造函数，字段是 `null` |
| 字段是 `readonly` 还想用字段注入 | 不行，反射也写不进去 |
| 忘了注册被注入的类型 | 构建容器时立刻抛 `VContainerException`，信息里会指出是哪个类的哪个参数 |
| 循环依赖 | 构建容器时抛 `Circular dependency detected!`，并打印完整依赖链 |
| 在 `[Inject]` 字段上做 `null` 判断当作"可选依赖" | 不要这么做，缺依赖就该构建失败 |

### 循环依赖长什么样

```csharp
class A { public A(B b) {} }
class B { public B(A a) {} }
```

```
VContainerException: Circular dependency detected!
    [1] B..ctor(a) --> B
    [2] A..ctor(b) --> A
```

**这是构建期检测**，不是运行期。VContainer 会遍历整张依赖图，提前把环找出来。
（例外：通过 `RegisterFactory` 声明的 `Func<>` 不参与环检测，因为那是运行时才调用的。）

---

## 9. 练习题

1. 把 `MethodInjectionTarget.Construct` 改成 `private`，还生效吗？（答案：生效）
2. 给 `FieldPropertyInjectionTarget` 加第二个 `[Inject]` 方法，观察两个方法的调用顺序是否与声明顺序一致。
3. 给 `MultiConstructorTarget` 再加一个标了 `[Inject]` 的构造函数，观察报错。
4. 制造一个 A↔B 的循环依赖，观察 `Circular dependency detected!` 的完整输出。
