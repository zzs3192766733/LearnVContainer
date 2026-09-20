# 第 02 课：Lifetime 生命周期 —— Transient / Singleton / Scoped

> 官方对照：`scoping/lifetime-overview`

## 本课目标

搞清楚三个 `Lifetime` 到底在什么时候"复用"、什么时候"新建"，以及**父子作用域**对它们的影响。

这是 VContainer（乃至所有 DI 容器）**最容易搞混、也最容易埋 bug** 的地方。

---

## 1. 三种 Lifetime 的定义

```csharp
public enum Lifetime
{
    Transient,   // 每次 Resolve 都 new 一个新实例
    Singleton,   // 整个容器的根作用域里只有一个实例
    Scoped,      // 每个「注册它的那个作用域」各有一个实例
}
```

> 源码位置：`Runtime/Container.cs`。注意枚举顺序里 `Transient = 0`，
> 也就是说 `default(Lifetime)` 是 `Transient`，**但 VContainer 的所有 Register 重载都要求你显式传**，所以不用担心漏写。

### 一句话记忆

| Lifetime | 记忆口诀 | 典型用途 |
| --- | --- | --- |
| `Transient` | **每次都要新的** | 一次性的命令对象、纯计算工具、ViewModel |
| `Singleton` | **全局就一个** | 存档、音频、时间、网络、事件总线 |
| `Scoped` | **一层一个** | 一局战斗、一个关卡、一个 Prefab 作用域的状态 |

`Scoped` 是 VContainer 相对 Zenject 更强调的概念。它的语义是：
**"以当前作用域为界，单例"**。

- 如果这个作用域是根作用域 → `Scoped` 表现得**和 `Singleton` 一模一样**；
- 如果开了子作用域 → 每个子作用域各自持有一份。

---

## 2. 示例代码

### 用来观察"实例身份"的基类

`SampleServices.cs`

```csharp
public abstract class SerialNumberedService
{
    static int nextSerial;
    protected SerialNumberedService() => Serial = ++nextSerial;
    public int Serial { get; }
}

public sealed class TransientSample : SerialNumberedService { }
public sealed class SingletonSample : SerialNumberedService { }
public sealed class ScopedSample    : SerialNumberedService { }

public sealed class DisposableSample : SerialNumberedService, IDisposable
{
    public void Dispose()
        => Debug.Log($"[Lesson02] DisposableSample #{Serial}.Dispose() （作用域销毁时回调）");
}
```

每个实例在构造时拿一个**自增序号**，这样日志里就能看出"是不是新建的"。

### 组合根

`Lesson02LifetimeScope.cs`

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.Register<TransientSample>(Lifetime.Transient);
    builder.Register<SingletonSample>(Lifetime.Singleton);
    builder.Register<ScopedSample>(Lifetime.Scoped);

    builder.RegisterEntryPoint<LifetimeDemo>();
}
```

### 实验代码

`LifetimeDemo.cs` 的可运行部分（节选）：

```csharp
void IStartable.Start()
{
    // (A) 同一个作用域内，连续 Resolve 两次
    var t1 = container.Resolve<TransientSample>();   var t2 = container.Resolve<TransientSample>();
    var s1 = container.Resolve<SingletonSample>();   var s2 = container.Resolve<SingletonSample>();
    var c1 = container.Resolve<ScopedSample>();      var c2 = container.Resolve<ScopedSample>();

    // (B) 开一个子作用域，重复上面的实验
    using (var child = container.CreateScope())
    {
        var ct1 = child.Resolve<TransientSample>();
        var cs1 = child.Resolve<SingletonSample>();
        var cc1 = child.Resolve<ScopedSample>();
        ...
    }
}
```

`container.CreateScope()` 返回 `IScopedObjectResolver`，它本身也是 `IDisposable`。

---

## 3. 运行结果与解读

Console 里会看到类似：

```
========== Lesson02 : Lifetime 生命周期 ==========
【父作用域内，连续 Resolve 两次】
  Transient : #1 / #2   -> 不同实例
  Singleton : #3 / #3   -> 同一实例
  Scoped    : #4 / #4   -> 同一实例

【子作用域内，连续 Resolve 两次】
  Transient : #5 / #6   -> 不同实例
  Singleton : #3 / #3   -> 同一实例
  Scoped    : #7 / #7   -> 同一实例

【父 vs 子】
  Singleton : 父 #3 vs 子 #3 -> 同一个实例（Singleton 全局唯一）
  Scoped    : 父 #4 vs 子 #7 -> 不同实例（每个作用域各一份）
```

逐条解读：

| 现象 | 原因 |
| --- | --- |
| Transient 每两次都不同 | 每次 `Resolve` 都调 `registration.SpawnInstance()` |
| Singleton 子作用域也是 #3 | 子容器发现注册不在自己的 registry 里，就**向上委派给父** |
| Scoped 父子不同 | registration 虽在父作用域，但实例是**在"发起 Resolve 的那个作用域"里缓存**的 |

### 关键源码（`Container.cs` / `ScopedContainer.ResolveCore`）

```csharp
case Lifetime.Singleton:
    if (Parent is null)
        return Root.Resolve(registration);              // 根作用域：全局唯一
    if (!registry.Exists(registration.ImplementationType, registration.Key))
        return Parent.Resolve(registration);            // 子作用域没自己注册 → 找父级
    return CreateTrackedInstance(registration);         // 子作用域自己注册了 → 自己缓存

case Lifetime.Scoped:
    return CreateTrackedInstance(registration);         // 永远在"当前作用域"缓存

default: // Transient
    return registration.SpawnInstance(this);            // 永远新建
}
```

> 这张代码就是本课的全部。看懂它，Lifetime 就不会再错了。

**注意 `Singleton` 的第二条分支**：如果子作用域**自己也注册**了同一个类型，那么子作用域会创建并缓存自己的实例，**不会**再用父级的。这就是官方文档说的"返回作用域最近的那个实例"。

---

## 4. 释放时机（Dispose）

```csharp
using (var child = container.CreateScope(b =>
       {
           b.Register<DisposableSample>(Lifetime.Scoped);
       }))
{
    var d = child.Resolve<DisposableSample>();   // 创建
    // ...
}   // ← 离开 using：child.Dispose() → 触发 DisposableSample.Dispose()
```

输出：

```
[Lesson02] DisposableSample #8.Dispose() （作用域销毁时回调）
```

规则：

- **`Transient` 实例不会被容器释放**（容器根本没持有它的引用）。
- **`Singleton` / `Scoped` 实例如果实现了 `IDisposable`，会在其所属作用域销毁时被 `Dispose`**。
- 通过 `RegisterInstance(obj)` 注册的实例**不会被自动 Dispose**（容器认为"我不管它"）。
- `LifetimeScope` 销毁（`OnDestroy`）时，它会 `Dispose` 自己的容器，从而级联释放上面这些对象。

> ⚠️ 官方特别提醒：如果**场景还活着**，只是 `LifetimeScope` 被销毁了，
> 那么以 `Lifetime.Scoped` 注册的 **MonoBehaviour 不会被自动销毁**。
> 想让它们跟着走，就把它们设为 `LifetimeScope` 所在 GameObject 的子物体（本教程第 04 课就是这么做的）。

---

## 5. 我该选哪个？

一个实用的决策流程：

```
这个对象需要"状态"吗？
├─ 不需要（纯计算 / 每次都想要干净的新对象）
│     → Transient
└─ 需要状态
      ├─ 这个状态要跨场景 / 跨整个游戏生命周期吗？
      │     → Singleton（注册在根 LifetimeScope）
      └─ 这个状态只在"某一层"里有效吗（一局、一个关卡、一个界面）？
            → Scoped，并把它注册在那层作用域里
```

### 常见反模式

| 反模式 | 后果 |
| --- | --- |
| 什么都是 `Singleton` | 内存永不释放、隐藏的全局状态、测试互相污染 |
| 把"当前关卡数据"注册成 `Singleton` | 换关卡时旧数据残留 |
| 把"每帧都要"的短命对象注册成 `Transient` 并在 `Update` 里反复 Resolve | 每帧 GC（应当用工厂 + 池，见第 06 课） |
| 在子作用域里重复注册了父作用域已有的 `Singleton` | 你以为在用全局的，其实用了局部的 |

---

## 6. 练习题

### 练习 1
把 `SingletonSample` 在**子作用域里也注册一次**（`child` 那个 builder 的安装委托）：

```csharp
using (var child = container.CreateScope(b =>
       {
           b.Register<SingletonSample>(Lifetime.Singleton);
       }))
{ ... }
```

**预测**：父 vs 子还是同一个实例吗？为什么？
（提示：看 `ResolveCore` 里 `registry.Exists(...)` 那一行。）

### 练习 2
在 `Start()` 里连续 `Resolve<TransientSample>()` 10000 次，用 `Stopwatch` 记时。
**观察**：分配了多少次对象？（用 Unity Profiler 看 GC Alloc）

### 练习 3
让 `SingletonSample` 实现 `IDisposable`，然后在子作用域里 Resolve 它，再 `Dispose` 子作用域。
**预测**：`Dispose()` 会被调用吗？
（答案：不会。因为实例是**父（根）作用域**创建并持有的，子作用域只是"向上借"，释放责任在根作用域。）

---

## 7. API 速查

| API | 说明 |
| --- | --- |
| `Lifetime.Transient` | 每次 Resolve 新建 |
| `Lifetime.Singleton` | 根作用域唯一（例：注册在父作用域的 Singleton，子作用域共享） |
| `Lifetime.Scoped` | 每个作用域一份 |
| `IObjectResolver.CreateScope(Action<IContainerBuilder> = null)` | 运行时开子作用域，返回 `IScopedObjectResolver`（可 Dispose） |
| `IScopedObjectResolver.Root` / `.Parent` | 根引用 / 父作用域引用 |
| `IDisposable` | Singleton / Scoped 实现它，可在作用域销毁时收到回调 |
