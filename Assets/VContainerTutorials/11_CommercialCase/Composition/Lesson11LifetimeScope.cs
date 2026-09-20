using UnityEngine;
using VContainer;
using VContainer.Unity;
using VContainerTutorials.Lesson11.Infrastructure;
using VContainerTutorials.Lesson11.Presentation;

namespace VContainerTutorials.Lesson11.Composition
{
    /// <summary>
    /// 本案例的挂载入口 = 根作用域 + 一个战斗子作用域。
    ///
    /// 真实项目里"进入战斗"通常由大厅按钮 → 场景加载流程触发；
    /// 这里为了开箱即用，在根作用域构建完成后立刻进入一场战斗。
    /// </summary>
    public sealed class Lesson11LifetimeScope : GameRootLifetimeScope
    {
        [SerializeField] BattleConfigAsset config;

        LifetimeScope battleScope;

        protected override void Configure(IContainerBuilder builder)
        {
            // 关键：先调用 base，保留父类（GameRootLifetimeScope）里的基础设施注册。
            // 忘了这一行，子作用域就会解析不到 IClock / IEventBus ... —— 新手最常踩的坑。
            base.Configure(builder);
        }

        protected override void Awake()
        {
            // base.Awake() 内部会 Configure + Build，
            // 走完这一行 Container 就已经可用了。
            base.Awake();

            if (config == null)
            {
                // 正式项目：config 是 [SerializeField] 拖进来的 .asset。
                // 这里为了不用你手动创建资源，运行时造一个内存实例（字段默认值即数值）。
                config = ScriptableObject.CreateInstance<BattleConfigAsset>();
            }

            // 表现层对象挂在根作用域下面 —— 这样根作用域销毁时它一定跟着走，
            // 不会出现"作用域没了，UI 还在"的经典泄漏（第 04 课的警告）。
            var hudObject = new GameObject("BattleHudView");
            hudObject.transform.SetParent(transform, false);
            var hud = hudObject.AddComponent<BattleHudView>();

            // 进入战斗 = 开一个子作用域，并把"这一局的注册"装进去。
            // 子作用域创建后，BattleBootstrapper 会立刻开始跑（IAsyncStartable）。
            battleScope = CreateChild(new BattleInstaller(config, hud), "BattleScope");

            Debug.Log($"[Lesson11] 战斗子作用域已就绪: {battleScope.name}" +
                      "（如果上面的战斗日志比这一行还早，说明入口点已经在子作用域里跑起来了）");
        }

        protected override void OnDestroy()
        {
            // 战斗正常结束时 BattleScopeEnder 会自己 Dispose 掉子作用域；
            // 这里兜底处理"中途停止 Play / 切换课程"的情况。
            if (battleScope != null)
            {
                battleScope.Dispose();
                battleScope = null;
                Debug.Log("[Lesson11] 根作用域销毁前，兜底释放战斗子作用域");
            }

            base.OnDestroy();
        }
    }
}
