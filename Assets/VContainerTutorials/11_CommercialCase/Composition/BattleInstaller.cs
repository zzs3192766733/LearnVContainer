using VContainer;
using VContainer.Unity;
using VContainerTutorials.Lesson11.Application;
using VContainerTutorials.Lesson11.Domain;
using VContainerTutorials.Lesson11.Infrastructure;
using VContainerTutorials.Lesson11.Presentation;

namespace VContainerTutorials.Lesson11.Composition
{
    /// <summary>
    /// 「一局战斗」的全部注册。
    ///
    /// 为什么用 IInstaller 而不是写一个 BattleLifetimeScope 子类？
    ///   - Installer 是纯 C# 对象，可以带构造参数（把 config / hud 传进去），
    ///     而 CreateChild 出来的 LifetimeScope 实例拿不到 Inspector 上拖的引用；
    ///   - Installer 很轻，将来可以把同一份注册 Enqueue 到下一个加载的场景里；
    ///   - 注册逻辑和「作用域的宿主形式」解耦（场景 / Prefab / 纯代码都能用）。
    /// </summary>
    public sealed class BattleInstaller : IInstaller
    {
        readonly BattleConfigAsset config;
        readonly BattleHudView hud;

        public BattleInstaller(BattleConfigAsset config, BattleHudView hud)
        {
            this.config = config;
            this.hud = hud;
        }

        public void Install(IContainerBuilder builder)
        {
            // ---- 配置 & 表现层出口 ----
            builder.RegisterInstance(config).As<IBattleConfig>();
            builder.RegisterComponent(hud).As<IBattleHud>();

            // ---- 一局一份的运行时状态（Scoped = 跟着战斗子作用域生灭）----
            builder.Register<BattleSession>(Lifetime.Scoped);
            builder.Register<CombatService>(Lifetime.Scoped);
            builder.Register<AutoCombatDriver>(Lifetime.Scoped);

            // ---- 入口点：全部限定在战斗作用域内 ----
            // 用 Scoped 而不是默认的 Singleton，是为了让语义和"一局一份"完全一致。
            builder.RegisterEntryPoint<BattleBootstrapper>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BattlePresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BattleTicker>(Lifetime.Scoped);
            builder.RegisterEntryPoint<BattleScopeEnder>(Lifetime.Scoped);
        }
    }
}
