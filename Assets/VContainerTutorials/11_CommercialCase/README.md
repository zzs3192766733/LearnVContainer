# 第 11 课：商业级综合案例 —— 分层战斗关卡

> 这是毕业实战。前 10 课讲的是「零件」，这一课把它们组装成一台**能直接搬进真实项目**的机器。

## 本课目标

1. 看到一个**分层架构**（Domain / Application / Infrastructure / Presentation / Composition）在 Unity + VContainer 下长什么样
2. 理解**根作用域 + 子作用域**如何映射到「整个游戏」和「一局战斗」
3. 看到 `IInstaller`、`IEventBus`、`RegisterFactory`、`IAsyncStartable`、`ITickable`、`IDisposable` 在真实场景里的位置
4. 建立「哪些代码该放哪一层」的判断力

---

## 1. 怎么跑

和前面一样：场景里放一个空物体 → 挂上 **`Lesson11LifetimeScope`** → Play。

> `Lesson11LifetimeScope` 位于 `Composition` 程序集（有 asmdef），
> 而 `TutorialLauncher.cs` 在 `Assembly-CSharp` 里，Unity 会自动让预定义程序集引用
> `autoReferenced` 的 asmdef，所以 `TutorialLauncher` 的下拉框里也能直接选到本课。

一次 Play 会自动完成：

```
根作用域构建 → 创建 HUD → 开战斗子作用域 → 异步读档 → 开打 →
打够目标分数 → 结算 → 写存档 → **子作用域自我销毁** → 整场结束
```

Console 里会看到完整的生命周期日志（`[Lesson11]` 前缀）。

---

## 2. 架构全景

```
┌──────────────────────────────────────────────────────────────────────┐
│  Composition（组合根）                                                │
│    GameRootLifetimeScope      基础设施注册 + 根容器                    │
│    Lesson11LifetimeScope      挂载入口 = 根容器 + 开一个战斗子作用域    │
│    BattleInstaller            「一局战斗」的全部注册（IInstaller）      │
└───────────────────────────┬──────────────────────────────────────────┘
                            │ 只在这一层知道"哪个接口用哪个实现"
        ┌───────────────────┼───────────────────┬──────────────────┐
        ▼                   ▼                   ▼                  ▼
┌───────────────┐  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐
│ Presentation  │  │  Application   │  │ Infrastructure  │  │    Domain    │
│ BattleHudView │  │ BattleSession  │  │ EventBus        │  │ IEventBus    │
│ BattlePresenter│ │ CombatService  │  │ UnityClock      │  │ IBattleConfig│
│               │  │ AutoCombatDriver│ │ UnityRandomSrc  │  │ ISaveRepository│
│               │  │ BattleTicker   │  │ PlayerPrefsSave │  │ IBattleHud   │
│               │  │ BattleBootstrapper│ EventBus        │  │ EnemyModel   │
│               │  │ BattleScopeEnder│ │ BattleConfigAsset│ │ BattleStateMachine│
└───────┬───────┘  └───────┬────────┘  └────────┬────────┘  └──────┬───────┘
        │                  │                    │                  │
        └──────────────────┴────────────────────┴──────────────────┘
                  所有箭头最终都指向 Domain（依赖倒置）
```

### 依赖方向（由 asmdef 强制保证）

| 程序集 | 引用 | 说明 |
| --- | --- | --- |
| `…Lesson11.Domain` | （无，`noEngineReferences: true`） | **纯 C#**，连 UnityEngine 都不引用 |
| `…Lesson11.Infrastructure` | Domain, VContainer | 实现 Domain 的端口 |
| `…Lesson11.Application` | Domain, VContainer | 用例与流程编排 |
| `…Lesson11.Presentation` | Domain, Application, VContainer | View + Presenter |
| `…Lesson11.Composition` | 以上全部 + VContainer | 唯一知道"具体实现"的地方 |

> **依赖不会反向流动**：Domain 里写不出 `using UnityEngine;`，
> Application 里写不出 `using …Presentation;`。这不是靠自觉，是编译期就拦住了。
> 这就是商业项目里 asmdef 最大的价值 —— 把架构约束变成编译器错误。

---

## 3. 分层职责：每层只干一件事

### Domain（领域层）—— 规则与抽象，零依赖

| 文件 | 内容 |
| --- | --- |
| `BattleEvents.cs` | `IEventBus` 抽象 + 7 个 `readonly struct` 领域事件 |
| `BattleModels.cs` | `EnemyModel`、`WeaponModel`、`BattleStateMachine` |
| `DomainPorts.cs` | `IClock`、`IRandomSource`、`IBattleConfig`、`ISaveRepository`、`IBattleHud` |

判断标准：**这段逻辑能不能在没有任何 Unity 环境的纯 NUnit 测试里跑？** 能，就属于 Domain。

```csharp
// EnemyModel 没有任何框架依赖，直接 new 就能测
var enemy = new EnemyModel(1, 100f);
enemy.ApplyDamage(30f);
Assert.AreEqual(70f, enemy.Health);
```

### Application（应用层）—— 用例与流程

| 类 | 职责 |
| --- | --- |
| `BattleSession` | 一局的全部运行时状态（`Scoped`） |
| `CombatService` | **规则**：开火会发生什么 |
| `AutoCombatDriver` | **节奏**：什么时候开火（`ITickable`） |
| `BattleTicker` | 战斗主循环：刷怪、敌人反击、推 HUD（`ITickable`） |
| `BattleBootstrapper` | 异步读档 → 进入战斗（`IAsyncStartable`） |
| `BattleScopeEnder` | 判定胜负 → 写存档 → 释放作用域（`ITickable`） |

这里有一个非常值得抄的拆分：

```csharp
// 节奏（什么时候做）和规则（做什么）分开
public sealed class AutoCombatDriver : ITickable   // 只有冷却计时
{
    public void Tick()
    {
        cooldown -= clock.DeltaTime;
        if (cooldown <= 0f && combat.FireAtNearest())   // ← 规则委托给 CombatService
            cooldown = session.Weapon.Cooldown;
    }
}
```

将来要加「手动开火」，只需要再写一个 `ManualCombatDriver`，`CombatService` 一行都不用改。

### Infrastructure（基础设施层）—— 对接外部世界

`EventBus`（自研事件总线）、`UnityClock`、`UnityRandomSource`、`PlayerPrefsSaveRepository`、`BattleConfigAsset`。

**换实现只需要改组合根的一行**：

```csharp
// 换成云存档？只动这一行
builder.Register<CloudSaveRepository>(Lifetime.Singleton).As<ISaveRepository>();
```

### Presentation（表现层）—— 只负责显示

`BattleHudView`（MonoBehaviour，实现 `IBattleHud`）+ `BattlePresenter`（把事件翻译成界面变化）。

两个细节是真实项目的常规操作：

1. **脏值检查**：`BattleTicker` 每帧都推数据，`BattleHudView` 只在值真变了才刷新。
2. **订阅即 IDisposable**：`BattlePresenter` 把所有订阅收集起来，`Dispose` 时统一退订，杜绝悬挂回调。

### Composition（组合根）—— 唯一知道全部真相的地方

```csharp
// BattleInstaller：一局战斗的完整注册
builder.RegisterInstance(config).As<IBattleConfig>();
builder.RegisterComponent(hud).As<IBattleHud>();
builder.Register<BattleSession>(Lifetime.Scoped);
builder.Register<CombatService>(Lifetime.Scoped);
builder.Register<AutoCombatDriver>(Lifetime.Scoped);
builder.RegisterEntryPoint<BattleBootstrapper>(Lifetime.Scoped);
builder.RegisterEntryPoint<BattlePresenter>(Lifetime.Scoped);
builder.RegisterEntryPoint<BattleTicker>(Lifetime.Scoped);
builder.RegisterEntryPoint<BattleScopeEnder>(Lifetime.Scoped);
```

> **为什么用 `IInstaller` 而不是再写一个 `BattleLifetimeScope`？**
> `CreateChild` 出来的 `LifetimeScope` 实例拿不到 Inspector 上拖的引用，
> 而 Installer 是纯 C# 对象，可以把 `config` / `hud` 通过构造函数带进去。
> 并且 Installer 还能被 `LifetimeScope.Enqueue(...)` 塞给下一个加载的场景，复用性更好。

---

## 4. 一次战斗的完整时序

```
① TutorialLauncher 创建 GameObject 并挂上 Lesson11LifetimeScope
        ↓
② LifetimeScope.Awake → Configure（GameRootLifetimeScope 的注册生效）
        ↓
③ builder.Build() → 根容器就绪
        ↓
④ Lesson11LifetimeScope.Awake 继续：
     创建 config（或使用拖进来的 .asset）
     创建 BattleHudView，挂到根作用域下面
        ↓
⑤ CreateChild(new BattleInstaller(config, hud), "BattleScope")
     ├─ 新建 GameObject（根作用域的子物体）
     ├─ BattleInstaller 装入「一局」的全部注册
     ├─ parent = 根作用域 ← 子作用域可以向上解析 IClock / IEventBus / ISaveRepository
     └─ Build → 子容器就绪 → Dispatch
        ↓
⑥ 子作用域 Dispatch（EntryPointDispatcher）
     ├─ BattleBootstrapper.StartAsync()   ← IAsyncStartable，同帧启动，await 之后下一帧继续
     └─ IStartable / ITickable 挂到 PlayerLoop
        ↓
⑦ BattlePresenter.Start()  → 订阅 6 个事件、初始化 HUD
        ↓
⑧ BattleBootstrapper：读档 → await → session.Begin(config) → 发布 BattleStartedEvent
        ↓
⑨ 每帧（BattleTicker + AutoCombatDriver）
     ├─ 刷怪：session.SpawnEnemy → 发布 EnemySpawnedEvent
     ├─ 开火：AutoCombatDriver → CombatService.FireAtNearest → EnemyDamagedEvent / EnemyKilledEvent
     ├─ 敌人反击：AliveCount > 0 时按间隔扣玩家血
     └─ 推 HUD：SetScore / SetEnemiesAlive / SetPlayerHealth
        ↓
⑩ BattleScopeEnder：分数达标（或玩家阵亡）
     ├─ session.Finish() → 发布 BattleFinishedEvent → Presenter 显示结算
     └─ 下一帧：scope.Dispose()
           ├─ EntryPointDispatcher 停止 PlayerLoop 驱动
           ├─ BattleSession / CombatService / BattlePresenter ... 依次 IDisposable
           └─ 销毁子作用域 GameObject
        ↓
⑪ 停止 Play 时：Lesson11LifetimeScope.OnDestroy 兜底释放 + 根容器 Dispose
```

> 第 ⑩ 步是**本案例最值得看的一段**：作用域出现了"自我销毁"的闭环。
> 真实项目里对应"退出关卡 / 战斗结算后回到大厅"，所有关卡级对象一次性干净释放。

---

## 5. 七个关键设计决策

| # | 决策 | 理由 |
| --- | --- | --- |
| 1 | Domain 用 `noEngineReferences` 的独立程序集 | 编译期保证领域逻辑不被 UnityEngine 污染，可纯 NUnit 测试 |
| 2 | 事件用 `readonly struct` | 发事件零 GC 分配（对应第 10 课的性能意识） |
| 3 | `IEventBus` 的接口在 Domain、实现在 Infrastructure | 测试时可换成同步假实现；将来换 MessagePipe 只改组合根 |
| 4 | `BattleSession` 注册为 `Scoped` | 「一局一份」由容器表达，不靠程序员自觉清空静态数据 |
| 5 | 入口点全部注册在**战斗子作用域** | 父作用域不放入口点，避免子作用域派发时把父级入口点重复驱动（第 09 课） |
| 6 | 数值全走 `IBattleConfig` | 策划改 SO 即可，不用改代码；测试可注入固定数值（消掉随机性） |
| 7 | `BattleScopeEnder` 用两帧完成销毁 | 当前帧先把 `BattleFinishedEvent` 发出去让表现层渲染，下一帧再 `Dispose` 容器，避免"在 Tick 中途销毁自己" |

---

## 6. 和前面 10 课的对应关系

| 本案例中的东西 | 出自 |
| --- | --- |
| `LifetimeScope.Configure`、`Register<T>(Lifetime)` | 01 / 05 |
| `Lifetime.Scoped` 表达「一局一份」、子作用域向上查找 | 02 |
| 全构造函数注入 + `readonly` 字段 | 03 |
| `RegisterComponent(hud).As<IBattleHud>()` | 04 |
| `As<IClock>()`、`RegisterInstance(config)` | 05 |
| `IBattleConfig` 用 SO 承载、`RegisterComponent` | 05 |
| `IInstaller` 复用注册 | 06 / 09 |
| `IAsyncStartable` + `CancellationToken` | 07 |
| 全部 `ITickable` 驱动、`IDisposable` 释放 | 07 |
| `IEventBus` 作为 Signal 的替代品 | 08（官方推荐自建）/ 05 |
| `CreateChild(installer)`、根/子作用域、`scope.Dispose()` | 09 |
| 脏值检查、零分配事件、不在 Tick 里创建对象 | 10 |
| asmdef 分层、依赖倒置 | 10（最佳实践）+ 本课新增 |

---

## 7. 换成真实项目的结构长什么样

把本案例搬进真项目时，只需要做三件事：

1. **把根作用域做成 Prefab + VContainerSettings**
   `Assets → Create → VContainer → VContainer Settings`，
   把挂 `GameRootLifetimeScope` 的 Prefab 拖到 `Root Lifetime Scope`。
   这样它跨场景常驻，`IEventBus` / `ISaveRepository` 就不会随场景重建。

2. **把 `BattleConfigAsset` 变成真资源**
   菜单 `Assets → Create → VContainerTutorials → Lesson11 Battle Config`，
   在 `Lesson11LifetimeScope` 的 Inspector 上把 `config` 拖进去。

3. **把 `BattleHudView` 换成真 UI**
   给它的方法接上 TMP_Text / Slider / Image。`IBattleHud` 接口不用改，
   `BattlePresenter` 一行都不用改。

---

## 8. 商业项目还能往上加什么

| 需求 | 建议做法 | 用到的 VContainer 能力 |
| --- | --- | --- |
| 敌人从对象池取，而不是 `new` | 自研 `IEnemyPool`（VContainer 不内置内存池），用 `RegisterFactory` 暴露 `Func<Enemy>` | 06 |
| 战斗回放 / 可复现 | 把 `IRandomSource` 换成带种子的自研随机数 | 05 / 本课端口设计 |
| 手动操作 | 再加一个 `ManualCombatDriver`，复用 `CombatService` | 07 |
| 关卡配置从 Excel/表数据来 | 写一个新的 `IBattleConfig` 实现 | 05 |
| 多语言 / 本地化 | 加 `ILocalizer` 端口，HUD 只拿 key | 04 |
| 战斗录像上传 | `RegisterBuildCallback` / `RegisterDisposeCallback` 打点 | 06 |
| 大项目启动优化 | 开 Source Generator；`Configure` 拆成多个 Installer | 10 |

---

## 9. 常见坑（本案例刻意规避的）

| 坑 | 本案例的处理 |
| --- | --- |
| 子类 override `Configure` 忘了 `base.Configure(builder)` | `Lesson11LifetimeScope.Configure` 里显式调用并加了注释 |
| 父作用域放 EntryPoint，导致子作用域重复驱动 | `GameRootLifetimeScope` 里一个入口点都没有 |
| 用 `UnityEngine.Object.Instantiate` 创建带 `[Inject]` 的对象 | 本案例没有运行时 Prefab，需要时请用 `container.Instantiate` |
| 订阅事件不取消 → 悬挂回调 / 内存泄漏 | `BattlePresenter` 用 `IDisposable` 收集订阅 |
| 每个 `SetScore` 都刷 UI | `BattleHudView` 全部做了脏值检查 |
| 在 `Tick` 中途 `Dispose` 自己所在的作用域 | `BattleScopeEnder` 分成"结算帧"和"销毁帧"两步 |
| `Scoped` 的 MonoBehaviour 不随作用域销毁 | HUD 被挂成根作用域 GameObject 的子物体 |
| `RegisterInstance` 的对象不会被 `Dispose` | `BattleConfigAsset` 由根作用域的 `OnDestroy` 兜底 `Destroy` |

> ⚠️ 如果你只是想把本课当参考代码，却不想引入 asmdef：
> 直接删掉 `Domain/` 等 5 个目录下的 `.asmdef` 文件即可，
> 代码会自动并入 `Assembly-CSharp` 正常编译（只是失去"编译期架构约束"这个好处）。

---

## 10. 练习题

1. **改玩法**：把"自动开火"改成"每 3 秒一发大招"（改 `AutoCombatDriver.weaponCooldown` 的来源即可），体会节奏与规则分离的好处。
2. **加失败条件**：现在玩家血量为 0 时 `BattleScopeEnder` 也能正确判定失败，请加上"HUD 显示失败原因"。
3. **换存档实现**：写一个 `JsonSaveRepository` 并在 `GameRootLifetimeScope` 里替换掉 `PlayerPrefsSaveRepository`，验证 Application / Domain 一行都不用改。
4. **复现战斗**：写一个固定的 `IRandomSource`（每次返回 1.0），跑两次战斗看结果是否完全一致。
5. **重开一局**：在 `Lesson11LifetimeScope` 里加一个 `Restart()`，战斗子作用域销毁后再 `CreateChild` 一次，观察第二轮是否从干净状态开始（没有上一局的分数残留）。
6. **加对象池**：把 `session.SpawnEnemy` 换成从 `IEnemyPool` 取对象，用 `RegisterFactory` 暴露 `Func<EnemyModel>`。
7. **加一个测试**：为 `EnemyModel.ApplyDamage` 写一个纯 NUnit 测试（不需要 Unity 环境），体会 Domain 层零依赖的价值。

---

## 11. 一句话总结

> **DI 不是"让代码能跑起来"的工具，而是"让架构约束能被编译器强制执行"的工具。**
> 这一课里，`IEventBus`、`IBattleConfig`、`ISaveRepository` 这些接口本身没有一行实现，
> 但它们决定了整个项目**哪些代码可以被替换、哪些依赖可以被测试、哪一层不许碰哪一层**。
> 这才是 VContainer 在商业项目里真正的价值。
