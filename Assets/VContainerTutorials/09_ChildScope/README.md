# 第 09 课：子作用域与场景层级

> 官方对照：`scoping/generate-child-with-code-first`、`scoping/generate-child-via-scene`、`scoping/project-root-lifetimescope`

## 本课目标

1. 什么时候需要**开子作用域**，它能解决什么问题
2. 三种创建方式的区别：`CreateScope` / `CreateChild` / `CreateChildFromPrefab`
3. **Additive 场景**的父子关系：`EnqueueParent` / `Enqueue`
4. **项目根 LifetimeScope**：`VContainerSettings`
5. 释放（Dispose）是怎么传播的

---

## 1. 为什么需要子作用域

游戏里经常出现这样的结构：

```
整个游戏（跨场景）
├── 主菜单
├── 关卡 1  ← 进入时创建，退出时全部释放
│    ├── 关卡 1 的 UI
│    └── 关卡 1 的敌人
└── 关卡 2  ← 进入时创建，退出时全部释放
```

如果全都注册在一个容器里：

- "关卡数据"会全局可见 → 关卡 2 可能用到关卡 1 的残留状态
- 退出关卡时无法精确释放那批对象
- `Singleton` 越来越多，最终变成全局变量地狱

**子作用域**就是为此存在的：

- 子作用域**能解析父作用域的一切**（向上查找）
- 父作用域**看不到**子作用域的注册
- `Lifetime.Scoped` 的实例在**每个作用域各一份**
- 子作用域 `Dispose` 时，它创建的那批对象会一起释放

---

## 2. 三种创建方式

### 2.1 `IObjectResolver.CreateScope` —— 纯容器层

```csharp
using (var child = container.CreateScope(builder =>
       {
           builder.RegisterInstance(new StageInfo("关卡 A"));
       }))
{
    var session = child.Resolve<LevelSession>();
}
// 离开 using：child.Dispose()
```

特点：

- **不创建任何 GameObject**，纯粹是容器层面的作用域
- 返回 `IScopedObjectResolver`（`IDisposable`）
- 适合"短生命周期的逻辑分组"，比如一次结算、一次战斗
- ⚠️ 必须自己 `Dispose`（用 `using` 最省心）

### 2.2 `LifetimeScope.CreateChild` —— Unity 层

```csharp
var childScope = currentScope.CreateChild(builder =>
{
    builder.RegisterInstance(new StageInfo("关卡 B"));
    builder.RegisterEntryPoint<StageWatcher>();
}, "LevelB_Scope");
```

特点（源码 `LifetimeScope.CreateChild<TScope>`）：

- **新建一个 GameObject** 并 `AddComponent<TScope>()`
- 该 GameObject 会**自动挂到父 LifetimeScope 的 `transform` 下面**
- `Parent` 会被设为当前作用域
- 返回 `LifetimeScope`，可以 `Dispose()`（顺便销毁 GameObject）

还有基于 Prefab 的版本：

```csharp
// 用 Prefab 里预先配置好的 LifetimeScope
var childScope = currentScope.CreateChildFromPrefab(lifetimeScopePrefab, builder =>
{
    builder.RegisterInstance(someExtraAsset);
    builder.RegisterEntryPoint<ExtraEntryPoint>();
});
```

`CreateChildFromPrefab` 会保留 Prefab 上序列化的引用（`[SerializeField]` 拖的资产），
并用 `ObjectResolverUnityExtensions.PrefabDirtyScope` 保证**不会把 Prefab 资源改脏**。

### 2.3 `LifetimeScope.Create` —— 纯代码创建根作用域

```csharp
using (var scope = LifetimeScope.Create(builder =>
       {
           builder.Register<LevelSession>(Lifetime.Scoped);
       }, "CodeFirst_Scope"))
{
    var session = scope.Container.Resolve<LevelSession>();
}
```

它等价于"用代码 new 了一个 GameObject 并挂上 LifetimeScope"，
主要用于**测试**和**无场景的工具型代码**。

### 2.4 `IInstaller`：把注册逻辑抽出去复用

```csharp
public class FooInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.Register<ExtraType>(Lifetime.Scoped);
    }
}

var child = currentScope.CreateChild(new FooInstaller());
```

`IInstaller` 就是"一段可复用的注册代码"。`CreateChild` / `Enqueue` / `LifetimeScope.Create`
都有接受 `IInstaller` 的重载。

---

## 3. 子作用域的解析规则

| 操作 | 结果 |
| --- | --- |
| 在子作用域里解析**只注册在父作用域**的类型 | ✅ 成功（向上查找，见 `ScopedContainer.TryFindRegistration`） |
| 在父作用域里解析**只注册在子作用域**的类型 | ❌ 失败（父作用域根本不知道） |
| 解析 `Lifetime.Singleton`（注册在父） | 拿到父作用域那个实例 |
| 解析 `Lifetime.Scoped`（注册在父） | **在子作用域里新建一份**（每个作用域一份） |
| 解析 `Lifetime.Transient` | 永远是新的 |
| 解析集合（`IReadOnlyList<T>`） | **合并**父作用域 + 当前作用域的注册 |

> 回忆第 02 课的那段 `ResolveCore` 源码，这里就是它的实战场景。

---

## 4. 释放（Dispose）传播

```
LifetimeScope.OnDestroy()
      ↓ DisposeCore()
   Container.Dispose()
      ↓
   ├─ 释放 Lifetime.Singleton 实例（实现 IDisposable 的）
   ├─ 释放 Lifetime.Scoped 实例
   └─ 子作用域（如果是 CreateChild 出来的，会随父 GameObject 一起被销毁）
```

⚠️ **两个必须记住的点**：

1. **`Transient` 实例不会被释放**（容器没有持有它）。
2. **以 `Scoped` 注册的 MonoBehaviour 不会被自动销毁**（官方明确警告）。
   除非：把它的 GameObject 挂成 `LifetimeScope` 的子物体，或者自己实现 `IDisposable` 去 `Destroy`。
   （`CreateChild` 自动做了第一件事：子作用域 GameObject 是父作用域 GameObject 的子物体。）

---

## 5. Additive 场景的父子关系

`SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)` 加载进来的场景，
如果自带 `LifetimeScope`，它需要知道"谁是我的父作用域"。

### 5.1 `LifetimeScope.EnqueueParent(parent)`

```csharp
class SceneLoader
{
    readonly LifetimeScope parent;

    public SceneLoader(LifetimeScope lifetimeScope)   // 注入当前作用域
    {
        parent = lifetimeScope;
    }

    IEnumerator LoadSceneAsync()
    {
        // 这个 using 块里生成的所有 LifetimeScope 都会以 parent 为父级
        using (LifetimeScope.EnqueueParent(parent))
        {
            var loading = SceneManager.LoadSceneAsync("Stage1", LoadSceneMode.Additive);
            while (!loading.isDone) yield return null;
        }
    }

    // UniTask 写法
    async UniTask LoadSceneAsync()
    {
        using (LifetimeScope.EnqueueParent(parent))
        {
            await SceneManager.LoadSceneAsync("Stage1", LoadSceneMode.Additive);
        }
    }
}
```

⚠️ **一定要用 `using` 包住**。它内部是一个 `Stack`（`GlobalOverrideParents`），
`using` 结束会 `Pop`；忘记用 `using` 会导致"父级泄漏"到后续不相关的场景加载中。

### 5.2 `LifetimeScope.Enqueue(...)`：给还没加载的场景追加注册

```csharp
// 形式一：直接追加注册
using (LifetimeScope.Enqueue(builder =>
{
    builder.RegisterInstance(extraAsset);   // 会注册到下一个加载的场景里
}))
{
    await SceneManager.LoadSceneAsync("Stage1", LoadSceneMode.Additive);
}

// 形式二：追加一个 Installer
using (LifetimeScope.Enqueue(new FooInstaller()))
{
    // ...
}
```

### 5.3 两者叠加

```csharp
using (LifetimeScope.EnqueueParent(parent))
using (LifetimeScope.Enqueue(builder => builder.RegisterInstance(extraAsset)))
{
    await SceneManager.LoadSceneAsync("Stage1", LoadSceneMode.Additive);
}
```

这是官方"异步加载资源 + 加载场景"的推荐组合：

```csharp
var extraAsset = await Addressables.LoadAssetAsync<ExtraAsset>(key);
using (LifetimeScope.EnqueueParent(parentScope))
using (LifetimeScope.Enqueue(builder => builder.RegisterInstance(extraAsset)))
{
    await SceneManager.LoadSceneAsync("AdditiveScene");
}
```

### 5.4 Inspector 里预设父级

`LifetimeScope` 上有一个 `Parent Reference` 字段（`ParentReference`），
可以指定"父作用域的**类型**"。场景加载完成（`isLoaded`）时，
VContainer 会在当前场景里找这个类型；**找不到就抛错**
（`VContainerParentTypeReferenceNotFound`）。

它的实现是延迟重试：`LifetimeScope.Awake` 里 catch 到这个异常后会把
自己放进 `WaitingList`，等父作用域构建完成再唤醒。

### 5.5 `LifetimeScope.Find<T>()`

```csharp
var parent = LifetimeScope.Find<BaseLifetimeScope>();
```

> 能用注入拿到 `LifetimeScope` 就尽量注入（像 5.1 那样），少用全局查找。

---

## 6. 项目根 LifetimeScope（`VContainerSettings`）

如果你想让"整个游戏"有一个贯穿始终的根作用域：

1. 创建一个 Prefab，挂上 `LifetimeScope`（写好它的 `Configure`）。
2. 菜单：**`Assets → Create → VContainer → VContainer Settings`**，生成 `VContainerSettings` 资源。
3. 把第 1 步的 Prefab 拖到 `VContainerSettings` 的 **Root Lifetime Scope** 栏位。

之后：

- 启动时会 `Instantiate` 这个 Prefab，并设为 `DontDestroyOnLoad`。
- 场景里**没有指定 Parent 的** `LifetimeScope` 会自动以它为父级
  （源码 `GetRuntimeParent()` 最后一步会去问 `VContainerSettings.Instance`）。
- `LifetimeScope.IsRoot` 为真的就是它。

> 小提示：`VContainerSettings` 会被自动加进 `Project Settings → Player → Preload Assets`。
> 如果它没被加载，去那里检查一下。

---

## 7. 注意事项

1. **不要把"创建子作用域"这件事写在父作用域的 EntryPoint 里**。
   创建子作用域会触发子作用域的 `EntryPointDispatcher.Dispatch()`，
   而集合解析会通过 `CollectionInstanceProvider.CollectFromParentScopes`
   **把父作用域的同类注册一并收集进来**。父子作用域注册了同一批标记接口时，
   容易造成"入口点被驱动两次"。
   实践建议：**父作用域放纯服务，入口点放在真正需要它的那一层**。
2. **`Scoped` 的 MonoBehaviour 不会随作用域销毁**（见第 4 节）。
3. **忘记 `Dispose` 子作用域**：`CreateScope` 出来的作用域不会自动释放。
4. **`CreateChild` 出来的子作用域** 是父 GameObject 的子物体，会随父 GameObject 销毁，
   但显式 `Dispose()` 更清晰。
5. **循环引用**：父作用域注册的类不要直接依赖子作用域的类型（父看不到子）。

---

## 8. 本课示例运行结果

```
[Lesson09] ===== A. 纯容器层的子作用域：IObjectResolver.CreateScope =====
[Lesson09] 创建 LevelSession #1
[Lesson09] 父作用域解析 LevelSession -> #1
[Lesson09] 创建 LevelSession #2
[Lesson09] 子作用域解析 LevelSession -> #2 / #2 （同一子作用域内同实例，与父不同）
[Lesson09] 子作用域私有的注册 StageInfo -> StageInfo(关卡 A（子作用域私有的注册）)
[Lesson09] 父作用域能解析到 StageInfo 吗？ -> False
[Lesson09] 释放 LevelSession #2
[Lesson09] 离开 using：子作用域被 Dispose

[Lesson09] ===== B. Unity 层的子作用域：LifetimeScope.CreateChild =====
[Lesson09] 子作用域的 StageWatcher.Start() 已触发，关卡 = StageInfo(关卡 B)
[Lesson09] 子作用域已创建，名字 = 'LevelB_Scope'，Parent = '[Lesson09_ChildScope]'
[Lesson09] 创建 LevelSession #3
[Lesson09] 子作用域里的 LevelSession -> #3
[Lesson09] 子作用域的 StageWatcher.Tick() 已触发

[Lesson09] ===== C. 纯代码创建一个根作用域：LifetimeScope.Create =====
[Lesson09] 纯代码作用域解析 StageInfo -> StageInfo(代码创建的根作用域)
[Lesson09] 它的 Parent 是？ -> (null，说明它是根作用域)
[Lesson09] 离开 using：代码创建的作用域被 Dispose，GameObject 也被销毁

...（停止 Play 或切换课程时）
[Lesson09] ---- 卸载：Dispose 子作用域 ----
[Lesson09] 子作用域的 StageWatcher.Dispose() 已触发
[Lesson09] 释放 LevelSession #3
[Lesson09] 释放 LevelSession #1
```

请重点观察：

- **#1 / #2 / #3 三个 `LevelSession`** 分别属于父作用域、A 的临时子作用域、B 的长期子作用域。
- 每个 `Dispose` 都和它所属的作用域一一对应。

---

## 9. 练习题

1. 把 A 段的 `CreateScope` 换成不用 `using` 的写法并且不 `Dispose`，观察 `LevelSession` 有没有被释放。
2. 在 B 段的安装委托里再加一条 `builder.Register<LevelSession>(Lifetime.Singleton)`，
   观察子作用域拿到的是不是自己新建的那一份（回忆第 02 课 `ResolveCore` 的 `registry.Exists` 分支）。
3. 在 B 段安装委托里不注册 `StageWatcher`，观察子作用域还会不会有 Tick 日志。
4. 用 `CreateChild<MyCustomScope>(...)` 传一个自己写的 `LifetimeScope` 子类，观察它的 `Configure` 会不会被调用。
5. 按第 6 节创建 `VContainerSettings` + 根 LifetimeScope Prefab，
   观察把 A 段的 `Parent` 打印出来会变成谁。
