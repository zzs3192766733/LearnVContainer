# 第 01 课：Hello World —— 最小可用的 VContainer

> 官方对照：`getting-started/hello-world`

## 本课目标

跑通 VContainer 的**最小闭环**，理解三个词：**组合根（Composition Root）**、**注册（Register）**、**依赖注入（Injection）**。

---

## 1. 先看"不用 DI"会怎么写

假设你要在 Unity 里打印一句问候：

```csharp
public class BadApp : MonoBehaviour
{
    void Start()
    {
        var service = new GreetingService();   // ← 问题在这里
        service.SayHello("VContainer");
    }
}
```

这行 `new` 有三个毛病：

1. **紧耦合**：`BadApp` 的源码里写死了 `GreetingService` 这个具体类型，想换成 `FakeGreetingService` 必须改 `BadApp` 的代码。
2. **不能测试**：想在 EditMode 测试里跑 `BadApp`，它一定会去 `new` 一个真的服务。
3. **生命周期混乱**：谁创建、谁销毁、会不会重复创建，全靠人肉记忆。

DI（依赖注入）的核心主张只有一句：

> **一个类不应该自己去创建它的依赖，而应该"声明"它需要什么，由外部把依赖送进来。**

而 VContainer 就是那个"外部"。

---

## 2. 三个文件，三个角色

### 角色一：被依赖的服务（纯 C#）

`GreetingService.cs`

```csharp
public class GreetingService
{
    int callCount;

    public void SayHello(string who)
    {
        callCount++;
        Debug.Log($"[Lesson01] Hello, {who}! (第 {callCount} 次调用, 实例 Id = {GetHashCode()})");
    }
}
```

注意 **它完全没有 `using VContainer;`**。这就是 DI 最重要的好处：

> **依赖注入是被注入方"零成本"的**。业务类不需要继承任何基类、不需要 `[Inject]` 特性（构造函数注入时）、也不需要在构造函数里写任何容器代码。

### 角色二：使用者 / 入口点

`HelloWorldApp.cs`

```csharp
public class HelloWorldApp : IStartable            // ← 标记接口：我需要在 Start 时机被调用
{
    readonly GreetingService greetingService;

    public HelloWorldApp(GreetingService greetingService)   // ← 唯一的"DI 声明"
    {
        this.greetingService = greetingService;
    }

    void IStartable.Start()
    {
        greetingService.SayHello("VContainer");
    }
}
```

三件事值得注意：

| 点 | 说明 |
| --- | --- |
| `readonly` 字段 | 依赖一旦注入就不再变，编译器帮你保证。官方推荐写法。 |
| 构造函数就是契约 | VContainer **不需要** `[Inject]`。它会选"参数最多的那个构造函数"。 |
| `IStartable` | 来自 `VContainer.Unity`。实现它，`Start()` 就会被挂到 Unity 的 PlayerLoop 上——**即使这个类不是 MonoBehaviour**。 |

### 角色三：组合根

`Lesson01LifetimeScope.cs`

```csharp
public class Lesson01LifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GreetingService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<HelloWorldApp>();
    }
}
```

- `LifetimeScope` 是一个 **MonoBehaviour**。把它挂到 GameObject 上，它在 `Awake()` 时构建容器，在 `OnDestroy()` 时释放容器。
- `Configure(IContainerBuilder builder)` 就是**整个游戏里唯一描述"谁依赖谁"的地方**——这就是 Composition Root。
- `Lifetime` 参数是**必填**的。VContainer 刻意不给你默认值，强迫你想清楚生命周期（对比 Zenject 是可以省略的）。

---

## 3. 运行时会发生的完整时序

```
① GameObject 被激活
        ↓
② LifetimeScope.Awake()            （LifetimeScope.cs 中 [DefaultExecutionOrder(-5000)] 保证它最先执行）
        ↓
③ Configure(builder)               ← 你的注册代码在这里跑
        ↓
④ builder.Build() → 得到 Container ← 此刻容器是"只读"的，注册完毕
        ↓
⑤ EntryPointDispatcher.Dispatch()  ← 容器构建回调里触发
        ↓
⑥ 容器发现 HelloWorldApp 实现了 IStartable，于是构造它：
        new HelloWorldApp(容器.Resolve<GreetingService>())
        ↑ 自动装配（auto-wiring）：看到构造函数要 GreetingService，就去配方表里找
        ↓
⑦ 把 HelloWorldApp.Start() 挂到 PlayerLoop 的 Startup 时机
        ↓
⑧ 一帧内，Start() 被调用 → 打印 "Hello, VContainer!"
```

**关键认知**：`HelloWorldApp` 你从来没有 `new` 过，也没有在场景里挂过。它完全由容器创建和驱动。

---

## 4. 观察输出

Play 之后 Console 里应该出现：

```
[Lesson01] Hello, VContainer! (第 1 次调用, 实例 Id = 1234567)
```

再跑一次，因为是 `Lifetime.Singleton` 且每次重新 Play，`实例 Id` 会变；但**在一次 Play 内**它永远只有一个实例。

---

## 5. 亲手改一改（练习题）

### 练习 1：把 Singleton 改成 Transient

```csharp
builder.Register<GreetingService>(Lifetime.Transient);
```

再让 `HelloWorldApp` 也接收第二个 `GreetingService` 参数：

```csharp
public HelloWorldApp(GreetingService a, GreetingService b)
{
    Debug.Log($"a.Id={a.GetHashCode()}, b.Id={b.GetHashCode()}");
}
```

**预测**：两个 Id 一样还是不一样？为什么？
（答案见第 02 课）

### 练习 2：把 `RegisterEntryPoint` 换成普通注册

```csharp
builder.Register<HelloWorldApp>(Lifetime.Singleton);
```

**预测**：`Start()` 还会被调用吗？
（答案：不会。`RegisterEntryPoint<T>()` 等价于 `Register<T>(Lifetime.Singleton).AsImplementedInterfaces()` **外加把 `EntryPointDispatcher` 注册进来**。少了它，容器根本不知道要去驱动 `IStartable`。）

### 练习 3：故意漏掉一个注册

注释掉 `builder.Register<GreetingService>(...)`。

**预测**：会在什么时候报错？错误信息长什么样？
（答案：在构建容器时就会抛 `VContainerException`，类似
`Failed to resolve VContainerTutorials.Lesson01.HelloWorldApp : No such registration of type: GreetingService`。
**VContainer 不会拖到运行时某一帧才炸**，这是它相对反射型容器的优势之一。）

---

## 6. API 速查

| API | 位置 | 说明 |
| --- | --- | --- |
| `LifetimeScope` | `VContainer.Unity` | MonoBehaviour 形式的容器宿主；`Awake` 构建、`OnDestroy` 释放 |
| `protected override void Configure(IContainerBuilder)` | `LifetimeScope` | 组合根；1.19.0 中基类是 `virtual`（不是 `abstract`） |
| `builder.Register<T>(Lifetime)` | `VContainer` | 注册一个类型 |
| `builder.RegisterEntryPoint<T>(Lifetime = Singleton)` | `VContainer.Unity` | 注册入口点，等价于 `Register<T>(lt).AsImplementedInterfaces()` + 注册 `EntryPointDispatcher` |
| `Lifetime.Singleton / Transient / Scoped` | `VContainer` | 三种生命周期 |
| `IStartable.Start()` | `VContainer.Unity` | 接近 `MonoBehaviour.Start()` 的时机被调用 |

---

## 7. 常见坑

1. **忘了挂 `LifetimeScope` 到 GameObject**：`Configure` 永远不会执行，日志里只有 Unity 的常规输出。
2. **`Configure` 里写业务逻辑**：不要把"加载资源""开始游戏"写进 `Configure`。它只应该做注册。
3. **把 `LifetimeScope` 挂在经常被销毁的物体上**：容器会跟着销毁，`Dispose` 会级联触发。第 09 课会细讲。
4. **以为 `Lifetime` 可以不写**：不行，必须显式写。
