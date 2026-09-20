# 第 10 课：诊断、优化与最佳实践（毕业课）

> 官方对照：`diagnostics/diagnostics-window`、`optimization/source-generator`、`optimization/codegen`、`comparing/comparing-to-zenject`

## 本课目标

1. 用**诊断窗口**把依赖图可视化，快速排查"谁依赖谁"
2. 用 **Source Generator** 把运行时反射换成编译期生成代码
3. 建立正确的**性能直觉**
4. 收拢成一份**最佳实践清单**，并回答总纲里留的 5 个问题

---

## 1. 诊断窗口（Diagnostics Window）

### 开启步骤

1. 菜单 **`Assets → Create → VContainer → VContainer Settings`**，生成 `VContainerSettings` 资源。
   （它会自动被加入 `Project Settings → Player → Preload Assets`）
2. 在 `VContainerSettings` 上勾选 **Enable Diagnostics**。
3. 菜单 **`Window → VContainer Diagnostics`** 打开窗口。

### 三个面板

| 面板 | 内容 |
| --- | --- |
| 中间 | **依赖树**：容器里所有注册对象及其依赖关系；点一行看详情 |
| 下方 | 这一行**是在哪里被注册的**；点蓝色链接可跳转到代码位置 |
| 右侧 | 容器里那个类型的**实例内容**（字段/属性的 `ToString()` 列表） |

> 官方建议：**重写你的 `ToString()`**，否则右侧面板只能显示 `TypeName`，看不出有用信息。

### 性能警告（官方原文）

> ⚠️ 启用 Enable Diagnostics 会**显著降低性能**，并且**大幅增加 GC 分配**。
> 建议只在开发期开启。

### API 方式读取诊断信息

```csharp
using VContainer.Diagnostics;

// 所有容器
foreach (var info in DiagnositcsContext.GetDiagnosticsInfos())
{
    Debug.Log(info);
}

// 或从某个容器拿
var infos = container.Diagnostics?.GetDiagnosticsInfos();
```

> 注意类名是 `DiagnositcsContext`（官方源码里的拼写就是这样，少了一个 `s` 的位置），
> 命名空间是 `VContainer.Diagnostics`。

---

## 2. Source Generator：把反射换成生成代码

### 默认情况

VContainer 默认用 **反射**（`ReflectionInjector`）在运行时构建注入逻辑。
反射只在**构建容器时**和**第一次创建某类型时**发生（结果会缓存在 `InjectorCache` / `TypeAnalyzer.Cache`），
所以运行时开销被隔离在启动阶段——这也是 VContainer 快的原因之一。

### 开启 Source Generator

1. 从 <https://github.com/hadashiA/VContainer/releases> 下载 `VContainer.SourceGenerator.dll`
2. 放进项目 `Assets/` 下的任意位置
3. 选中它，在 Inspector 底部的 **Asset Labels** 里添加标签 **`RoslynAnalyzer`**
4. 在 Inspector 的 **Select platforms for plugin** 里：
   - 取消勾选 **Any Platform**
   - 在 Include Platforms 里取消勾选 **Editor** 和 **Standalone**

> 前置要求：**Unity 2021.3+**；VContainer **1.13.0** 起从 `Mono.Cecil` 的 IL weaving 迁移到了 Roslyn Source Generator。

### 哪些类会被生成

**同时满足**：

1. 类所在程序集引用了 `VContainer` 的 asmdef
2. 类上标了 `[Inject]`，**或**该类型出现在任意 `Register*` 调用里
3. 类上**没有**标 `[InjectIgnore]`

被纳入生成的类会自动打上 `[Preserve]`，因此**不会被 IL2CPP 剥离**。

你可以手动控制：

```csharp
[Inject]           // 显式要求为这个类生成注入代码
public class Foo { }

[InjectIgnore]     // 显式排除（仍然可用，只是走反射）
public class Bar { }
```

### 限制（不加速，但仍然可用）

| 类型 | 是否支持 |
| --- | --- |
| 嵌套类 | ❌ |
| `struct` | ❌ |
| 访问级别低于 `internal` 的类型（如 `private`） | ❌ |

### 已废弃：`VContainer.EnableCodeGen`

旧版本通过在 asmdef 里引用 `VContainer.EnableCodeGen` 触发 IL 织入，**官方已标记 Deprecated**，请使用 Source Generator。

---

## 3. 性能直觉

### 3.1 本课实测（Editor / Mono，只代表量级）

`PerformanceProbe` 会跑两组对比：

```
[Lesson10] ---- 解析开销实测（Editor / Mono，仅供参考量级）----
[Lesson10] Singleton 解析 100000 次耗时 xx.xx ms（约 0.xxxx us/次）
[Lesson10] Transient 解析 100000 次耗时 xx.xx ms（约 0.xxxx us/次）
```

结论：

| 情况 | 成本 |
| --- | --- |
| 解析已缓存的 `Singleton` | 基本就是**一次字典查找**，不产生新对象、不产生 GC |
| 解析 `Transient` | 成本主要在 `new` 本身 + 构造注入的反射/生成代码调用 |
| **第一次**解析某类型 | 需要构建注入器（反射分析），成本明显更高，但只发生一次 |

（官方基准数据：Resolve 场景下 VContainer 基本比 Zenject 快 5–10 倍，
"VContainer (CodeGen)" 开启代码生成后更快。测试环境 Unity 2019.x / IL2CPP Standalone macOS，每个用例 10,000 次迭代。）

### 3.2 真正的性能建议

| 建议 | 原因 |
| --- | --- |
| **不要在 `Update` / `Tick` 里 `Resolve`** | 就算单次很便宜，每帧乘以对象数也会累积；而且通常说明你该注入工厂而不是每次解析 |
| 用 `Lifetime.Singleton` / `Scoped` 复用对象 | 避免无意义的重复创建 |
| 需要"按需创建"时用工厂 + 对象池 | 见第 06 课；VContainer 本身不含内存池，官方建议自己实现并注入到工厂 |
| **不要开 Enable Diagnostics 发布** | 官方明确警告性能与 GC 显著劣化 |
| 启动耗时敏感的项目开启 Source Generator | 去掉运行时反射分析 |
| 大的 `Configure` 可考虑并行构建 | `VCONTAINER_PARALLEL_CONTAINER_BUILD` 宏会让 `ContainerBuilder.BuildRegistry` 用 `Parallel.For` |

---

## 4. 与 Zenject 对照

| Zenject | VContainer |
| --- | --- |
| `Bind<Service>().AsTransient()` | `Register<Service>(Lifetime.Transient)` |
| `Bind<Service>().AsCached()` | `Register<Service>(Lifetime.Scoped)` |
| `Bind<Service>().AsSingle()` | `Register<Service>(Lifetime.Singleton)` |
| `Bind<IService>().To<Service>().AsCached()` | `Register<IService, Service>(Lifetime.Scoped)` |
| `BindInterfacesTo<Service>().AsCached()` | `Register<Service>(Lifetime.Scoped).AsImplementedInterfaces()` |
| `BindInterfacesAndSelfTo<Foo>().AsCached()` | `Register<Foo>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf()` |
| `BindInstance(obj)` | `RegisterInstance(obj)` |
| `Bind<Foo>().WithId("foo")` | ❌ 不支持（用 `.Keyed()` 或 `.WithParameter()` 替代） |
| `FromComponentInHierarchy()` | `RegisterComponentInHierarchy<Foo>()` |
| `FromComponentInNewPrefab(prefab)` | `RegisterComponentInNewPrefab(prefab, Lifetime.Scoped)` |
| `FromNewComponentOnNewGameObject()` | `RegisterComponentOnNewGameObject<Foo>(Lifetime.Scoped, "Foo1")` |
| `PlaceholderFactory<T>` + `BindFactory<...>` | `RegisterFactory<...>(...)` |
| **Signal** | ❌ 不支持（官方建议用 MessagePipe / VitalRouter 等） |
| **Memory Pool** | ❌ 不支持（自己实现并注入） |
| `FromComponentInNewPrefabResource("Path")` | ❌ 不支持（用 `LoadAsync` 加载后 `RegisterInstance`） |

### VContainer 的设计取舍（官方说法）

- 性能好；**大部分反射和断言都隔离在容器构建阶段**。
- 实现容易阅读（代码量小）。
- **精心选择功能范围**：不把"面向数据的对象"注册进容器，也不主动注入所有 View 组件，
  以免 DI 声明变得过分复杂。
- 明确主张"**注入 MonoBehaviour**"而不是"**注入到 MonoBehaviour 里**"。
  - Zenject 在场景启动时会用反射扫描所有 GameObject（开销大），VContainer 不这么做。

---

## 5. 最佳实践清单（全 10 课总结）

### 组合根

- [ ] 每个场景/模块一个 `LifetimeScope`，`Configure` 里**只做注册**，不写业务逻辑。
- [ ] 全局服务放**根 LifetimeScope**（`VContainerSettings.RootLifetimeScope`）；
      关卡/战斗级状态放**子作用域**。
- [ ] 一个 `LifetimeScope` 不要注册成千上万条，按模块用 `IInstaller` 拆开。

### 注入

- [ ] **优先构造函数注入 + `readonly` 字段**；构造函数超过 5–6 个参数就该考虑拆类了。
- [ ] `MonoBehaviour` 用**方法注入**；不要给它写自定义构造函数。
- [ ] 给 `MonoBehaviour` 打 `[Inject]` 后，**记得用三种方式之一触发**（第 04 课）。
- [ ] 不要在 `MonoBehaviour` 里注入"每帧变化的数据"——那些应该作为方法参数传。
- [ ] 缺依赖就让它**构建期报错**，不要用 `TryResolve` 兜底掩盖设计问题。

### 注册

- [ ] `Lifetime` 一定要想清楚：`Transient`（每次新的）/ `Singleton`（全局一个）/ `Scoped`（一层一个）。
- [ ] 需要具体类型也保留入口时加 `.AsSelf()`。
- [ ] 配置数据用**配置对象**（`[Serializable]` 类 / ScriptableObject 子对象）而不是裸 `string`/`int`。
- [ ] `RegisterInstance` 的对象**不受容器管理**（不 Dispose、不注入），只把它当"值"用。
- [ ] 别用 `WithId`-式的 Key 来表达设计意图，优先用**工厂 / 强类型方法**。

### 生命周期与释放

- [ ] 用 `IDisposable` 承接"作用域销毁时"的清理逻辑。
- [ ] **`Transient` 不会被释放**，`RegisterInstance` 的也不会。
- [ ] `Scoped` 的 MonoBehaviour 不会随作用域销毁 → 挂成子物体或自己 `Destroy`。
- [ ] 用 `using` 管理 `CreateScope` / `LifetimeScope.Create` / `Enqueue*`。

### 性能

- [ ] 不在 `Update`/`Tick` 里 `Resolve`；用工厂 + 池。
- [ ] 发布版本关掉 Diagnostics。
- [ ] 启动耗时敏感 → 开 Source Generator。

---

## 6. 回答总纲里留的 5 个问题

**Q1. `Lifetime.Singleton` 和 `Lifetime.Scoped` 在"没有子作用域"时表现一样吗？为什么？**

一样。因为 `Container.ResolveCore` 对 `Scoped` 的处理是
`return rootScope.Resolve(registration)`——根作用域就是唯一的那个作用域，
所以每个 `Scoped` 注册在根作用域里只有一份，与 `Singleton` 表现一致。
**只有出现子作用域时二者才分道扬镳。**

**Q2. 为什么 `MonoBehaviour` 上的 `[Inject]` 方法不会自动被调用？有哪三种方式让它被调用？**

因为 Unity 通过原生反序列化创建 MonoBehaviour，VContainer 无法感知"所有 GameObject 的创建"。
`[Inject]` 只是标记。三种触发方式：
① `builder.RegisterComponent*` 系列注册；② `container.InjectGameObject(go)`；
③ `LifetimeScope` Inspector 的 `Auto Inject Game Objects`。

**Q3. 同一个类型在一个 `LifetimeScope` 里注册两次会发生什么？**

- 具体类型 + `Lifetime.Singleton`：**构建时抛** `VContainerException: Conflict implementation type`。
- 其它 Lifetime：不报错。直接 `Resolve<T>()` 得到**最后注册的**；
  并且所有注册都能通过 `IReadOnlyList<T>` / `IEnumerable<T>` 拿到。
- 接口注册给**不同实现**：完全正常，就是集合注入的用法。

**Q4. `builder.Register<IFoo>(c => new Foo(), Lifetime.Scoped)` 里的 lambda 什么时候执行？执行几次？**

在**该注册于所属作用域中第一次被解析时**执行，并且**在同一个作用域内只执行一次**
（实例被缓存在该作用域的 `sharedInstances` 里）。
每个新的作用域会再执行一次。要在运行时反复创建对象，请用 `RegisterFactory`。

**Q5. 想让一个类在容器构建后、第一帧前就拿到 `Update` 回调，应该实现哪个接口？**

实现 `ITickable`（`Tick()` 挂在接近 `Update` 的时机），并用 `builder.RegisterEntryPoint<T>()`
或 `builder.UseEntryPoints(...)` 注册。
如果还想在"容器刚构建完"这一刻做初始化，再实现 `IInitializable`——它在容器构建回调里**同步**执行，
早于所有 PlayerLoop 回调。

---

## 7. 练习题

1. 打开诊断窗口，把本教程 10 课的 `LifetimeScope` 依次跑一遍，观察依赖树。
2. 给 `PerformanceProbe` 加一组"每帧 Resolve Transient 1000 次"的测试，用 Profiler 观察 GC Alloc。
3. 按第 2 节给项目装上 Source Generator，对比开启前后的启动耗时。
4. 把本课的最佳实践清单打印出来，逐条检查你正在做的项目。
