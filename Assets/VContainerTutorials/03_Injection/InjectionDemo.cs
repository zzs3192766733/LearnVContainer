using VContainer.Unity;

namespace VContainerTutorials.Lesson03
{
    /// <summary>
    /// 注意这个类自己也是"构造函数注入"的示例：
    /// 4 个依赖全部写在构造函数里，一眼就能看出它依赖了哪些东西。
    /// 构造函数越长 → 这个类扛的责任越多 → 越该考虑拆分。
    /// </summary>
    public sealed class InjectionDemo : IStartable
    {
        readonly ConstructorInjectionTarget constructorTarget;
        readonly MethodInjectionTarget methodTarget;
        readonly FieldPropertyInjectionTarget fieldPropertyTarget;
        readonly MultiConstructorTarget multiConstructorTarget;
        readonly DerivedTarget derivedTarget;

        public InjectionDemo(
            ConstructorInjectionTarget constructorTarget,
            MethodInjectionTarget methodTarget,
            FieldPropertyInjectionTarget fieldPropertyTarget,
            MultiConstructorTarget multiConstructorTarget,
            DerivedTarget derivedTarget)
        {
            this.constructorTarget = constructorTarget;
            this.methodTarget = methodTarget;
            this.fieldPropertyTarget = fieldPropertyTarget;
            this.multiConstructorTarget = multiConstructorTarget;
            this.derivedTarget = derivedTarget;
        }

        void IStartable.Start()
        {
            constructorTarget.Report();
            methodTarget.Report();
            fieldPropertyTarget.Report();
            multiConstructorTarget.Report();
            derivedTarget.ReportBase();
            derivedTarget.ReportDerived();
        }
    }
}
