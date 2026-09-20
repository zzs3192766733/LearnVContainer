using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson02
{
    public sealed class Lesson02LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 三种生命周期各注册一个，用来对比行为。
            builder.Register<TransientSample>(Lifetime.Transient);
            builder.Register<SingletonSample>(Lifetime.Singleton);
            builder.Register<ScopedSample>(Lifetime.Scoped);

            // DisposableSample 故意**不**在这里注册，
            // 而是等会儿在子作用域里注册，方便观察 Dispose 的归属。
            builder.RegisterEntryPoint<LifetimeDemo>();
        }
    }
}
