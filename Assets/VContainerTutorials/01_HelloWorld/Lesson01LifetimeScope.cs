using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson01
{
    /// <summary>
    /// 组合根（Composition Root）。
    ///
    /// LifetimeScope 是 MonoBehaviour：
    ///   - Awake()   → 采集注册 → 构建容器（之后容器不可变）
    ///   - OnDestroy() → 释放容器 → 级联 Dispose 所有 Singleton / Scoped 实例
    ///
    /// 把它挂到场景里的任意 GameObject 上即可生效。
    /// </summary>
    public class Lesson01LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 1) 注册一个普通类型。
            //    Lifetime 参数是必填的 —— VContainer 强迫你显式表达生命周期意图。
            builder.Register<GreetingService>(Lifetime.Singleton);

            // 2) 注册一个"入口点"。
            //    等价于：builder.Register<HelloWorldApp>(Lifetime.Singleton).AsImplementedInterfaces()
            //    额外还会把 EntryPointDispatcher 注册进来，正是它负责把 IStartable /
            //    ITickable 等标记接口挂到 Unity 的 PlayerLoop 上。
            builder.RegisterEntryPoint<HelloWorldApp>();
        }
    }
}
