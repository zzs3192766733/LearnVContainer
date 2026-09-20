# 第 07 课：EntryPoint —— 不用 MonoBehaviour 也能拿到 Start / Update

> 官方对照：`integrations/entrypoint`

## 本课目标

搞懂 VContainer 的"纯 C# 入口点"机制：

1. 有哪些时机接口，分别在什么时候被调用
2. `RegisterEntryPoint<T>` 到底做了什么
3. `UseEntryPoints` / 异常处理 / 异步启动 / `CancellationToken`

---

## 1. 核心理念：把控制流从 MonoBehaviour 里搬出来

Unity 里 `MonoBehaviour` 是"入口点"——因为 Unity 只会驱动它。
问题是：**MonoBehaviour 同时又是"视图组件"**，于是事件处理、控制流、领域逻辑、渲染全都挤在一个类里。

VContainer 的做法：用**自己实现的 `PlayerLoopSystem`** 去驱动纯 C# 类。
只要你的类实现了某个"标记接口"，它就会被排进 Unity 的 PlayerLoop。

```csharp
public class FooController : IStartable
{
    void IStartable.Start() { /* ... */ }
}

builder.RegisterEntryPoint<FooController>();
```

结果：这个类**不是 MonoBehaviour**，没有 `.cs` 挂在场景里，但它的 `Start()` 会被调用。

好处：

| | MonoBehaviour | EntryPoint（纯 C#） |
| --- | --- | --- |
| 生命周期 | 受 Unity 控制，随时可能被销毁 | 受容器/作用域控制，可预期 |
| 依赖 | 只能字段/方法注入 | 构造函数注入，`readonly` 保证 |
| 单元测试 | 需要实例化 GameObject | 直接 `new` 就能测 |
| 职责 | 容易把逻辑和渲染混在一起 | 只有逻辑 |

> 官方强调：**把副作用入口统一收敛到标记接口**是推荐做法。
> MonoBehaviour 自己用 `Start`/`Update` 没问题——它本来就该干"表现层"的活。

---

## 2. 全部可用接口与时机

| 接口 | 回调 | 触发时机 |
| --- | --- | --- |
| `IInitializable` | `Initialize()` | 容器构建完成后**立即**（同步，不经过 PlayerLoop） |
| `IPostInitializable` | `PostInitialize()` | `IInitializable` 全部执行完之后（同步） |
| `IStartable` | `Start()` | 接近 `MonoBehaviour.Start()` |
| `IAsyncStartable` | `StartAsync(CancellationToken)` | 与 `IStartable` 同时机，但是异步的 |
| `IPostStartable` | `PostStart()` | `MonoBehaviour.Start()` 之后 |
| `IFixedTickable` | `FixedTick()` | 接近 `MonoBehaviour.FixedUpdate()` |
| `IPostFixedTickable` | `PostFixedTick()` | `FixedUpdate` 之后 |
| `ITickable` | `Tick()` | 接近 `MonoBehaviour.Update()` |
| `IPostTickable` | `PostTick()` | `Update` 之后 |
| `ILateTickable` | `LateTick()` | 接近 `MonoBehaviour.LateUpdate()` |
| `IPostLateTickable` | `PostLateTick()` | `LateUpdate` 之后 |
| `IDisposable` | `Dispose()` | 所属作用域（`LifetimeScope`）销毁时 |

### 关键细节（源码 `EntryPointDispatcher.Dispatch`）

- `IInitializable` / `IPostInitializable` 是**同步直接调用**的，发生在容器构建回调里。
  → 所以它们的执行**早于任何 `Start()`**，适合做"容器就绪后的自检/预热"。
- 其余接口通过 `PlayerLoopHelper.Dispatch(timing, loopItem)` 注册到 PlayerLoop。
- **`ITickable` 这类是每帧调用**，所以本课示例里只打印第一次，避免刷屏。
- 因为用的是 `PlayerLoopSystem`，**任何时候注册都能生效**——甚至在运行时新建的子作用域里注册也行。

---

## 3. `RegisterEntryPoint<T>` 做了什么

```csharp
builder.RegisterEntryPoint<T>(Lifetime lifetime = Lifetime.Singleton);
```

源码：

```csharp
public static RegistrationBuilder RegisterEntryPoint<T>(
    this IContainerBuilder builder,
    Lifetime lifetime = Lifetime.Singleton)
{
    EntryPointsBuilder.EnsureDispatcherRegistered(builder);
    return builder.Register<T>(lifetime).AsImplementedInterfaces();
}
```

所以它 = **`Register<T>(lt).AsImplementedInterfaces()` + 注册 `EntryPointDispatcher`**。

`EnsureDispatcherRegistered` 做三件事：

```csharp
containerBuilder.Register<EntryPointDispatcher>(Lifetime.Scoped);
// 如果没有注册异常处理器，就默认用 Debug.LogException
containerBuilder.RegisterEntryPointExceptionHandler(UnityEngine.Debug.LogException);
// 容器构建后立刻派发一次
containerBuilder.RegisterBuildCallback(container =>
    container.Resolve<EntryPointDispatcher>().Dispatch());
```

> **注意**：如果你自己调用了 `RegisterEntryPointExceptionHandler(...)`，它注册的是**后一个**
> `EntryPointExceptionHandler`；容器在解析时会拿到"最后一个"注册，也就是你的那个。

---

## 4. 批量注册：`UseEntryPoints`

```csharp
builder.UseEntryPoints(Lifetime.Singleton, entryPoints =>
{
    entryPoints.Add<LifecycleTracker>();
    entryPoints.Add<AsyncBootstrapper>().AsSelf();   // Add 返回 RegistrationBuilder，可以继续链式
    entryPoints.OnException(ex => Debug.LogException(ex));
});
```

等价于：

```csharp
builder.RegisterEntryPoint<LifecycleTracker>();
builder.RegisterEntryPoint<AsyncBootstrapper>().AsSelf();
builder.RegisterEntryPointExceptionHandler(ex => Debug.LogException(ex));
```

> `Add<T>()` 内部是 `Register<T>(lifetime).AsImplementedInterfaces()`。

---

## 5. 异常处理

`Start()` / `Tick()` 里抛出的异常，**在外部是捕获不到的**（它在 PlayerLoop 里跑）。
VContainer 的行为：

- 默认：`UnityEngine.Debug.LogException`
- 或者：`builder.RegisterEntryPointExceptionHandler(ex => { ... })`

```csharp
builder.RegisterEntryPointExceptionHandler(ex =>
{
    Debug.LogWarning("入口点异常：" + ex.Message);
});
```

源码 `StartableLoopItem.MoveNext`：

```csharp
foreach (var x in entries)
{
    try { x.Start(); }
    catch (Exception ex)
    {
        if (exceptionHandler == null) throw;
        exceptionHandler.Publish(ex);   // ← 交给你
    }
}
```

⚠️ 注意：**一旦你注册了 handler，默认的 `Debug.LogException` 就被"顶掉"了**
（因为解析时拿到的是后注册的那个）。

---

## 6. 异步入口点 `IAsyncStartable`

```csharp
public class FooController : IAsyncStartable
{
    public async UniTask StartAsync(CancellationToken cancellation)
    {
        await LoadSomethingAsync(cancellation);
    }
}
```

**返回类型的三种情况**（源码 `IAsyncStartable.cs`）：

| 条件 | `StartAsync` 返回类型 |
| --- | --- |
| 装了 `com.cysharp.unitask` | `Cysharp.Threading.Tasks.UniTask` |
| Unity 2023.1+（无 UniTask） | `UnityEngine.Awaitable` |
| 其它（含本项目的 2022.3） | `System.Threading.Tasks.Task` |

本项目是 Unity 2022.3 且没有装 UniTask，所以示例里 `StartAsync` 返回的是 `Task`。

### 三个必须知道的点

1. **所有 `StartAsync` 都被安排在 `Startup` 时机同帧启动**，且**后续 PlayerLoop 阶段不会等它们**。
   每帧的 `Update` 会照常跑——所以你的 `Tick()` 可能在 `StartAsync` 还没 `await` 完时就已经在执行了。
   需要"等加载完再开始"的话，用标志位或 `UniTaskCompletionSource`。
2. **`CancellationToken` 在所属 `LifetimeScope` 被销毁时会取消**。
   `AsyncStartableLoopItem.Dispose()` 里调用了 `cts.Cancel()`。
3. **`async` + `try/catch` 是可行的**（这点比协程强）。异常也会交给 `RegisterEntryPointExceptionHandler`。

> 想在别的 PlayerLoop 时机跑异步代码，官方建议用 UniTask 自带的
> `await UniTask.Yield(PlayerLoopTiming.FixedUpdate)`，而不是让 VContainer 再提供异步接口。

---

## 7. 本课示例运行顺序

一个 `LifecycleTracker` 实现了**所有**接口，只打印每种回调的第一次：

```
[Lesson07] IInitializable.Initialize          frame=1
[Lesson07] IPostInitializable.PostInitialize  frame=1
[Lesson07] IAsyncStartable.StartAsync 开始...  frame=1
[Lesson07] IStartable.Start                   frame=1
[Lesson07] IPostStartable.PostStart           frame=1
[Lesson07] IAsyncStartable.StartAsync await 之后恢复  frame=2
[Lesson07] ITickable.Tick                     frame=1
[Lesson07] IPostTickable.PostTick             frame=1
[Lesson07] ILateTickable.LateTick             frame=1
[Lesson07] IPostLateTickable.PostLateTick     frame=1
[Lesson07] IFixedTickable.FixedTick           frame=？（取决于 FixedTimestep）
[Lesson07] IPostFixedTickable.PostFixedTick   frame=？
[Lesson07] 捕获到入口点未处理异常：这是故意的异常，用来演示 RegisterEntryPointExceptionHandler
...（停止 Play 或切换课程时）
[Lesson07] IDisposable.Dispose                容器 / 作用域销毁时被调用
```

可以看出：

- `Initialize` / `PostInitialize` 最早，而且和 `Start` **在同一帧**（因为 `Dispatch()` 是同步的）。
- `IAsyncStartable` 先"开始"，但 `await` 之后的代码跑到了下一帧。
- `Tick` / `LateTick` 每帧都调用（只是我们只打印一次）。

---

## 8. 常见坑

| 坑 | 说明 |
| --- | --- |
| 在 `Tick()` 里 `Resolve` 创建对象 | 每帧 GC。用第 06 课的工厂 + 对象池 |
| 忘记 `RegisterEntryPoint`，只写了 `Register<T>` | 实现接口但不会被执行 |
| 以为 `IAsyncStartable` 会"阻塞"后面的流程 | 不会，同帧并发启动 |
| `StartAsync` 里忘了处理 `CancellationToken` | 作用域销毁后异步任务还在跑，访问已释放对象 |
| 在 `Initialize()` 里做重活 | 它在容器构建回调里同步执行，会拖慢启动帧 |
| 多个 `[Inject]` 入口点互相依赖 | 会形成环，构建时抛 `Circular dependency detected!` |

---

## 9. 练习题

1. 把 `LifecycleTracker` 只保留 `ITickable`，改成每帧累加并在第 100 帧时打印一次。
2. 让 `AsyncBootstrapper` 在 `StartAsync` 里 `await Task.Delay(1000)`，观察 `Tick` 是不是早就开始跑了。
3. 把 `UseEntryPoints` 的 `Lifetime.Singleton` 改成 `Lifetime.Scoped`，再开一个子作用域注册同一批入口点，观察会不会出现两份。
4. 把 `ThrowingEntryPoint` 的异常改成在 `Tick()` 里抛，观察每帧都会走到异常处理器。
