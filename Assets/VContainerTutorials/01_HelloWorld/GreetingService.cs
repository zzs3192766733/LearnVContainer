using UnityEngine;

namespace VContainerTutorials.Lesson01
{
    /// <summary>
    /// 一个纯 C# 业务类。
    ///
    /// 请注意：它**没有引用任何 VContainer 的类型**。
    /// 这正是依赖注入的核心好处 —— 被注入的一方对容器是无感知的，
    /// 因此它既可以被容器创建，也可以被你在单元测试里手动 new 出来。
    /// </summary>
    public class GreetingService
    {
        int callCount;

        public void SayHello(string who)
        {
            callCount++;
            Debug.Log($"[Lesson01] Hello, {who}! (第 {callCount} 次调用, 实例 Id = {GetHashCode()})");
        }
    }
}
