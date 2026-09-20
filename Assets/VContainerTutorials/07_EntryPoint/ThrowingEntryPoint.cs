using System;
using VContainer.Unity;

namespace VContainerTutorials.Lesson07
{
    /// <summary>
    /// 故意抛异常的入口点，用来演示 RegisterEntryPointExceptionHandler。
    ///
    /// 入口点跑在 PlayerLoop 里，外部无法 try/catch 它，
    /// 所以必须通过 RegisterEntryPointExceptionHandler 统一接管。
    /// </summary>
    public sealed class ThrowingEntryPoint : IStartable
    {
        void IStartable.Start()
        {
            throw new InvalidOperationException("这是故意的异常，用来演示 RegisterEntryPointExceptionHandler");
        }
    }
}
