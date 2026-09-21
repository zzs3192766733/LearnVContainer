using VContainer;
using VContainer.Unity;

namespace Tutorial_01
{
    public class SayHelloLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<DebugLogger>(Lifetime.Singleton).As<ILogger>();
            builder.Register<SayHelloService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<SayHelloApp>();
        }
    }
}