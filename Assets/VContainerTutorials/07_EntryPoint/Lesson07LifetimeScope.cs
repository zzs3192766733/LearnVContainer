using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson07
{
    public sealed class Lesson07LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 批量注册入口点。等价于：
            //   builder.RegisterEntryPoint<LifecycleTracker>();
            //   builder.RegisterEntryPoint<AsyncBootstrapper>();
            //   builder.RegisterEntryPoint<ThrowingEntryPoint>();
            //   builder.RegisterEntryPointExceptionHandler(...);
            builder.UseEntryPoints(Lifetime.Singleton, entryPoints =>
            {
                entryPoints.Add<LifecycleTracker>();
                entryPoints.Add<AsyncBootstrapper>();
                entryPoints.Add<ThrowingEntryPoint>();

                // 入口点里未捕获的异常都会走到这里。
                // 注意：一旦注册了它，默认的 Debug.LogException 就不会再触发
                //（容器解析时拿到的是最后注册的那个 handler）。
                entryPoints.OnException(ex =>
                {
                    Debug.LogWarning("[Lesson07] 捕获到入口点未处理异常：" + ex.Message);
                });
            });
        }
    }
}
