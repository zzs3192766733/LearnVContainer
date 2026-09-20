using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson03
{
    public sealed class Lesson03LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 被注入的依赖
            builder.Register<Logger>(Lifetime.Singleton);
            builder.Register<AppConfig>(Lifetime.Singleton);

            // 各种注入形态的"目标对象"。
            // 注意：只有入口点会被容器主动创建，
            // 其它类型都是"被需要时才创建"（懒加载）。
            builder.Register<ConstructorInjectionTarget>(Lifetime.Singleton);
            builder.Register<MethodInjectionTarget>(Lifetime.Singleton);
            builder.Register<FieldPropertyInjectionTarget>(Lifetime.Singleton);
            builder.Register<MultiConstructorTarget>(Lifetime.Singleton);
            builder.Register<DerivedTarget>(Lifetime.Singleton);

            // 入口点：它在构造时会把上面 5 个目标全部拉起来
            builder.RegisterEntryPoint<InjectionDemo>();
        }
    }
}
