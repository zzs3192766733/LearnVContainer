using VContainer.Unity;

namespace VContainerTutorials.Lesson01
{
    /// <summary>
    /// 程序的"入口点"。
    ///
    /// 它是纯 C# 类（不是 MonoBehaviour），但实现了 IStartable，
    /// 于是 VContainer 会把它挂到 Unity 的 PlayerLoop 上，
    /// 在接近 MonoBehaviour.Start() 的时机调用 Start()。
    ///
    /// 这就是官方文档里说的 IoC：让"控制流"从 MonoBehaviour 里搬出来。
    /// </summary>
    public sealed class HelloWorldApp : IStartable
    {
        // readonly = 依赖一旦注入就不再改变。官方推荐写法。
        readonly GreetingService greetingService;

        /// <summary>
        /// 构造函数就是"依赖声明"。
        /// VContainer 不需要任何特性就能识别它（规则：参数最多的那个构造函数）。
        /// </summary>
        public HelloWorldApp(GreetingService greetingService)
        {
            this.greetingService = greetingService;
        }

        void IStartable.Start()
        {
            greetingService.SayHello("VContainer");
        }
    }
}
