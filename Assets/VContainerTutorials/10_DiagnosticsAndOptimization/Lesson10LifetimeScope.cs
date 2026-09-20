using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson10
{
    public sealed class Lesson10LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<CachedService>(Lifetime.Singleton);
            builder.Register<FreshService>(Lifetime.Transient);
            builder.Register<ReflectionOnlyService>(Lifetime.Transient);

            builder.RegisterEntryPoint<PerformanceProbe>();
        }
    }
}
