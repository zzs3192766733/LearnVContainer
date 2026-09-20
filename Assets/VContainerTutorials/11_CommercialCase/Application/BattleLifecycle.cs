using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Application
{
    /// <summary>
    /// 战斗引导：异步读档 → 进入战斗。
    ///
    /// 演示第 07 课的 IAsyncStartable：
    ///   - 它在容器构建后被自动启动（不需要任何 MonoBehaviour）
    ///   - 它拿到的 CancellationToken 会在所属作用域销毁时被取消
    ///   - async 入口点**不会阻塞**后续 PlayerLoop，所以 Tick 可能比它还早开始跑
    /// </summary>
    public sealed class BattleBootstrapper : IAsyncStartable
    {
        readonly IBattleConfig config;
        readonly ISaveRepository save;
        readonly IEventBus bus;
        readonly BattleSession session;

        public BattleBootstrapper(IBattleConfig config, ISaveRepository save, IEventBus bus, BattleSession session)
        {
            this.config = config;
            this.save = save;
            this.bus = bus;
            this.session = session;
        }

        public async Task StartAsync(CancellationToken cancellation)
        {
            Debug.Log("[Lesson11] 【引导】开始读取存档与关卡数据 ...");

            var bestScore = save.LoadBestScore();

            // 真实项目里这里通常是 Addressables.LoadAssetAsync / 表格加载
            await Task.Yield();

            if (cancellation.IsCancellationRequested)
            {
                Debug.Log("[Lesson11] 【引导】被取消（作用域已销毁）");
                return;
            }

            session.Begin(config);
            bus.Publish(new BattleStartedEvent(config.Level, bestScore));

            Debug.Log($"[Lesson11] 【引导】进入战斗：Level = {config.Level}，" +
                      $"目标分数 = {config.TargetScore}，历史最高 = {bestScore}");
        }
    }

    /// <summary>
    /// 战斗收尾：判定胜负 → 写存档 → 释放整个战斗作用域。
    ///
    /// 这是本案例的闭环：一次子作用域从"创建"到"自我销毁"的完整生命周期。
    /// </summary>
    public sealed class BattleScopeEnder : ITickable
    {
        readonly BattleSession session;
        readonly IBattleConfig config;
        readonly ISaveRepository save;
        readonly IEventBus bus;
        readonly LifetimeScope scope;

        bool settled;
        bool disposeRequested;

        /// <summary>
        /// 注入 LifetimeScope —— VContainer 在构建每个作用域时会自动把
        /// 「当前这个 LifetimeScope」注册进去（源码 InstallTo），
        /// 所以这里拿到的就是「本阶段所在的那个子作用域」。
        /// </summary>
        public BattleScopeEnder(
            BattleSession session,
            IBattleConfig config,
            ISaveRepository save,
            IEventBus bus,
            LifetimeScope scope)
        {
            this.session = session;
            this.config = config;
            this.save = save;
            this.bus = bus;
            this.scope = scope;
        }

        public void Tick()
        {
            // 第二步：先用一帧把事件发出去让表现层渲染完，再销毁容器。
            if (disposeRequested)
            {
                Debug.Log("[Lesson11] 【收尾】Dispose 战斗子作用域（等价于退出关卡，所有 Scoped 对象一起释放）");
                scope.Dispose();
                return;
            }

            if (settled || !session.IsFighting)
            {
                return;
            }

            var victory = session.PlayerHealth > 0f;
            if (session.Score < config.TargetScore && victory)
            {
                return;
            }

            settled = true;
            session.Finish();

            var bestScore = save.LoadBestScore();
            if (session.Score > bestScore)
            {
                bestScore = session.Score;
                save.SaveBestScore(bestScore);
            }

            bus.Publish(new BattleFinishedEvent(victory, session.Score, bestScore, session.KilledCount));
            disposeRequested = true;
        }
    }
}
