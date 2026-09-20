# VContainer 从零到实战（Unity 教程合集）

> 参考官方文档：<https://vcontainer.hadashikick.jp>
> 本教程基于 **VContainer 1.19.0** / Unity **2022.3.16f1** 编写，代码与结论都对照过 `1.19.0` 的真实源码。

这套教程的目标只有一个：**让你看懂 VContainer 到底在做什么，并且能立刻用起来。**

---

## 0. 这套教程长什么样

```
Assets/VContainerTutorials/
├── README.md                ← 你正在看的这份总纲
├── TutorialLauncher.cs      ← 「一键跑任意一课」的启动器
├── 01_HelloWorld/           ← 每一课 = 一个文件夹 + 一份 README.md + 配套 C# 代码
├── 02_Lifetime/
├── 03_Injection/
├── 04_MonoBehaviourInjection/
├── 05_Registration/
├── 06_FactoryAndContainerApi/
├── 07_EntryPoint/
├── 08_KeysAndCollections/
├── 09_ChildScope/
├── 10_DiagnosticsAndOptimization/
└── 11_CommercialCase/       ← 毕业课：分层架构 + 完整战斗关卡的商业级案例（含 asmdef）
```

**每课都包含：**

| 内容 | 说明 |
| --- | --- |
| `README.md` | 原理讲解 + 逐行代码分析 + 观察结果 + 常见坑 |
| `*.cs` | 可以直接运行的示例代码，全部带中文注释 |
| 输出 | 运行后在 Unity Console 里能看到 `[LessonXX]` 前缀的日志 |

---

## 1. 安装确认

VContainer 已经写进了 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "jp.hadashikick.vcontainer": "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.19.0"
  }
}
```

**第一次打开 Unity 时**，Package Manager 会自动从 GitHub 拉取这个包（需要联网 + 本机装有 git）。
拉取完成后，左侧 `Project` 窗口的 `Packages` 节点下会出现 `VContainer`。

> 如果拉取失败：
> 1. 确认本机 `git` 在 PATH 中（命令行执行 `git --version` 有输出）；
> 2. 或者改用 OpenUPM：先加 `scopedRegistries`（`https://package.openupm.com`，scope `jp.hadashikick.vcontainer`），再写 `"jp.hadashikick.vcontainer": "1.19.0"`；
> 3. 或者去 <https://github.com/hadashiA/VContainer/releases> 下载 `.unitypackage` 手动导入。

---

## 2. 怎么跑起来（重点，只需做一次）

### 方式 A：使用教程自带的启动器（推荐）

1. 打开任意场景（没有就 `File → New Scene` 建一个）。
2. 在 Hierarchy 里右键 → `Create Empty`，命名随意，比如 `TutorialLauncher`。
3. 选中它，`Add Component` → 搜索 **`Tutorial Launcher`**。
4. Inspector 里的 `Lesson` 下拉框选择你要学的课。
5. 点 Play，看 Console。

**运行时想换一课**：直接在 Inspector 里改 `Lesson` 下拉框（编辑器里会立刻重建容器）。

> `TutorialLauncher` 并不是什么"魔法"。它做的事就是：
> `new GameObject()` → `AddComponent<对应课程的 LifetimeScope>()`，
> 也就是**你在真实项目里手动"新建空物体 + 挂 LifetimeScope"所做的事**。
> 它只是帮你省掉 10 次手动搭建场景的功夫。

### 方式 B：手动搭（推荐在学完第 1 课后尝试）

1. 新建空物体。
2. `Add Component` → 搜索并挂上某一课的 `XxxLifetimeScope`（例如 `Lesson01LifetimeScope`）。
3. Play。

这两种方式的结果完全等价。真实项目里用的就是方式 B。

---

## 3. 学习路线

| # | 课程 | 你会学到 | 关键 API |
| --- | --- | --- | --- |
| 01 | [Hello World](01_HelloWorld/README.md) | 组合根、注册、构造函数注入、纯 C# 入口点 | `LifetimeScope` / `Configure` / `Register<T>` / `RegisterEntryPoint<T>` |
| 02 | [Lifetime 生命周期](02_Lifetime/README.md) | Transient / Singleton / Scoped 的真实差别、父子作用域查找 | `Lifetime` / `CreateScope` |
| 03 | [注入的四种形态](03_Injection/README.md) | 构造注入、方法注入、字段/属性注入、多构造函数规则 | `[Inject]` / `[Key]` |
| 04 | [MonoBehaviour 注入](04_MonoBehaviourInjection/README.md) | 为什么 MB 不能构造注入、三种触发注入的方式 | `RegisterComponent*` / `InjectGameObject` |
| 05 | [注册家族大全](05_Registration/README.md) | 接口注册、多接口、"注册为自身"、实例、参数、开放泛型、SO | `As` / `AsSelf` / `AsImplementedInterfaces` / `WithParameter` / `RegisterInstance` |
| 06 | [工厂与容器 API](06_FactoryAndContainerApi/README.md) | 运行时造对象、委托注册、Build/Dispose 回调、直接 Resolve | `RegisterFactory` / `Register(Func)` / `IObjectResolver` |
| 07 | [EntryPoint 与 PlayerLoop](07_EntryPoint/README.md) | 不用 MonoBehaviour 也能拿到 Start/Update、异步启动、异常处理 | `IStartable` / `ITickable` / `IAsyncStartable` / `UseEntryPoints` |
| 08 | [Key 与集合注入](08_KeysAndCollections/README.md) | 一个接口多个实现、批量拿到所有实现 | `.Keyed()` / `[Key]` / `IReadOnlyList<T>` |
| 09 | [子作用域与场景](09_ChildScope/README.md) | 运行时动态开作用域、Additive 场景父子关系、根 LifetimeScope | `CreateChild` / `EnqueueParent` / `VContainerSettings` |
| 10 | [诊断与优化](10_DiagnosticsAndOptimization/README.md) | 依赖图可视化、Source Generator、性能常识、最佳实践清单 | Diagnostics Window / `[InjectIgnore]` |
| 11 | [商业级综合案例](11_CommercialCase/README.md) | 五层架构 + asmdef 强制依赖方向、根/子作用域、事件总线、完整战斗关卡闭环 | 全部能力综合运用 |

> 建议顺序学习。01–04 是 80% 日常开发要用的内容，05–06 是"注册怎么写才优雅"，07–10 是进阶。

---

## 4. 五分钟核心概念速查

### 4.1 三个词

| 术语 | 一句话解释 | 在代码里长什么样 |
| --- | --- | --- |
| **Composition Root（组合根）** | 所有"谁依赖谁"的配置集中写在一个地方 | 继承 `LifetimeScope` 的 `Configure` 方法 |
| **Registration（注册）** | 告诉容器："接口 X 用实现 Y，生命周期是 Z" | `builder.Register<IX, Y>(Lifetime.Singleton)` |
| **Resolve（解析）** | 让容器把对象图构造出来并交给你 | 通常是**自动**发生的（构造注入），偶尔手动 `container.Resolve<T>()` |

### 4.2 最小闭环（背下来就够用了）

```csharp
// 1) 业务逻辑：纯 C#，不需要任何 VContainer 引用
public class GreetingService
{
    public void SayHello() => UnityEngine.Debug.Log("Hello!");
}

// 2) 使用者：构造函数声明"我需要什么"，容器会自动塞进来
public class App : VContainer.Unity.IStartable
{
    readonly GreetingService service;
    public App(GreetingService service) => this.service = service;   // ← 依赖注入
    void VContainer.Unity.IStartable.Start() => service.SayHello();  // ← 入口点
}

// 3) 组合根：把上面两个注册进容器
public class GameLifetimeScope : VContainer.Unity.LifetimeScope
{
    protected override void Configure(VContainer.IContainerBuilder builder)
    {
        builder.Register<GreetingService>(VContainer.Lifetime.Singleton);
        builder.RegisterEntryPoint<App>();
    }
}
```

### 4.3 心智模型

```
        [ Composition Root ]                 容器里的一张"配方表"
   LifetimeScope.Configure(builder)   ──►   TInterface ──► (TImpl, Lifetime)
                                                                 │
   new App(...)  ◄── 自动装配(Auto-wiring) ◄── Resolve<TInterface>()
```

- **容器只负责"造对象 + 塞依赖"**，它不负责你的游戏逻辑。
- 一个 `LifetimeScope`（MonoBehaviour）对应一个容器，它在 `Awake` 时构建、`OnDestroy` 时释放。
- 容器是**不可变**的：`Configure` 跑完之后不能再加注册（要加就开子作用域）。

---

## 5. 代码约定说明

- 所有示例代码命名空间为 `VContainerTutorials.Lesson0X`，互不干扰。
- 日志统一带 `[LessonXX]` 前缀，方便在 Console 里用搜索框过滤。
- 示例**刻意不使用 `UnityEngine.UI`**，避免额外引入 `com.unity.ugui` 依赖；所有演示都用 `Debug.Log` / `GameObject` 完成。
- 教程中出现的"实例 Id"用 `GetHashCode()` 或自增序号表示，用于判断是不是同一个对象。

---

## 6. 学完之后你应该能回答

1. `Lifetime.Singleton` 和 `Lifetime.Scoped` 在**没有子作用域**时表现一样吗？为什么？
2. 为什么 `MonoBehaviour` 上的 `[Inject]` 方法不会自动被调用？有哪三种方式让它被调用？
3. 同一个类型在一个 `LifetimeScope` 里注册两次会发生什么？
4. `builder.Register<IFoo>(c => new Foo(), Lifetime.Scoped)` 里的 lambda **什么时候执行**？执行几次？
5. 想让一个类在容器构建后、第一帧前就拿到 `Update` 回调，应该实现哪个接口？

答案都在各课 README 里，最后第 10 课会把它们串成一张"最佳实践清单"。

---

## 7. 官方文档对照索引

| 本教程 | 官方文档页面 |
| --- | --- |
| 01 | `getting-started/hello-world` |
| 02 | `scoping/lifetime-overview` |
| 03 | `resolving/constructor-injection`、`resolving/method-injection`、`resolving/property-field-injection` |
| 04 | `resolving/gameobject-injection`、`registering/register-monobehaviour` |
| 05 | `registering/register-type`、`registering/register-scriptable-object` |
| 06 | `registering/register-factory`、`registering/register-using-delegate`、`registering/register-callbacks`、`resolving/container-api` |
| 07 | `integrations/entrypoint` |
| 08 | `registering/register-with-keys`、`registering/register-collection` |
| 09 | `scoping/generate-child-with-code-first`、`scoping/generate-child-via-scene`、`scoping/project-root-lifetimescope` |
| 10 | `diagnostics/diagnostics-window`、`optimization/source-generator`、`comparing/comparing-to-zenject` |

> 注：官方文档部分页面基于较早版本，个别签名（例如 `LifetimeScope.Configure` 在 1.19.0 里是 `virtual` 而非 `abstract`）与本教程不一致时，**以本教程为准**（已对照 1.19.0 源码）。
