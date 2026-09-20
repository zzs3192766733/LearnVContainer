using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials.Lesson07
{
    /// <summary>
    /// 异步入口点。
    ///
    /// 返回类型取决于项目环境（源码 IAsyncStartable.cs）：
    ///   - 装了 UniTask          -> Cysharp.Threading.Tasks.UniTask
    ///   - Unity 2023.1+         -> UnityEngine.Awaitable
    ///   - 其它（本项目 2022.3）  -> System.Threading.Tasks.Task
    ///
    /// 所以这里是 Task。如果你之后给项目加了 UniTask，把这个签名改回去即可。
    /// </summary>
    public sealed class AsyncBootstrapper : IAsyncStartable
    {
        public async Task StartAsync(CancellationToken cancellation)
        {
            Debug.Log($"[Lesson07] IAsyncStartable.StartAsync 开始   frame={Time.frameCount}");

            // 让出到下一帧继续
            await Task.Yield();

            // LifetimeScope 销毁时，这个 token 会被取消
            if (cancellation.IsCancellationRequested)
            {
                Debug.Log("[Lesson07] StartAsync 被取消（作用域已销毁）");
                return;
            }

            Debug.Log($"[Lesson07] IAsyncStartable.StartAsync await 之后恢复执行   frame={Time.frameCount}");
            Debug.Log("[Lesson07] ↑ 注意：这行日志出现时，ITickable.Tick 早就已经在跑了 —— " +
                      "async 入口点不会阻塞 PlayerLoop");
        }
    }
}
