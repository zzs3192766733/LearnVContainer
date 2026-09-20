using VContainer;
using VContainer.Unity;
using VContainerTutorials.Lesson11.Domain;
using VContainerTutorials.Lesson11.Infrastructure;

namespace VContainerTutorials.Lesson11.Composition
{
    /// <summary>
    /// 项目根作用域：只放「比任何一局战斗都活得久」的东西。
    ///
    /// 正式项目里它应该：
    ///   1. 做成 Prefab，挂上这个组件；
    ///   2. 在 VContainerSettings 的 Root Lifetime Scope 栏位引用它（见第 09 课）；
    ///   3. 加上 DontDestroyOnLoad（VContainerSettings 会自动做）。
    ///
    /// 本案例把它当作场景里的根作用域使用，效果等价。
    /// </summary>
    public class GameRootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 依赖倒置的落点：把 Infrastructure 的实现绑定到 Domain 的接口上。
            // 整个项目里，"哪个接口用哪个实现"只在组合根出现一次。
            builder.Register<UnityClock>(Lifetime.Singleton).As<IClock>();
            builder.Register<UnityRandomSource>(Lifetime.Singleton).As<IRandomSource>();
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();
            builder.Register<PlayerPrefsSaveRepository>(Lifetime.Singleton).As<ISaveRepository>();

            // 注意：这里**不放任何 EntryPoint**。
            // 入口点应该注册在真正需要它的那一层作用域里，
            // 否则子作用域派发时会被一并收集，造成重复驱动（见第 09 课注意事项）。
        }
    }
}
