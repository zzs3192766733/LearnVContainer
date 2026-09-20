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
| 用 `UnityEngine.Object.Instantiate(prefab)` 而不是 `container.Instantiate(prefab)` | 新对象上的 `[Inject]` 没执行 | 换成 `container.Instantiate(prefab)` |
| 同一个**具体类型**以 `Lifetime.Singleton` 注册了两次 | 构建时抛 `VContainerException: Conflict implementation type` | 同一个具体类型 + Singleton 在同一作用域只能注册一次（其它生命周期是"后者生效"，详见第 08 课） |

---

## 9. 练习题

1. 把 `RegisterComponentInHierarchy<PlayerView>()` 换成 `RegisterComponentOnNewGameObject<PlayerView>(Lifetime.Scoped, "PlayerView")`，观察 `Awake` 里手动创建的那个对象还会不会被注入。
2. 把 `AutoCreatedView` 的 Lifetime 改成 `Singleton`，再跑一次，观察创建时机有没有变化。
3. 在 `LateInjected` 那个 GameObject 下再挂一个子物体并给它也加上 `[Inject]`，验证 `InjectGameObject` 的递归行为。
4. 把 `.UnderTransform(transform)` 删掉，观察 `AutoCreatedView` 出现在场景的什么位置，以及销毁 `LifetimeScope` 后它会不会留下。
