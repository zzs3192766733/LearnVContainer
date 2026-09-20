using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using VContainerTutorials.Lesson11.Application;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Presentation
{
    /// <summary>
    /// 表现层协调者（Presenter）。
    ///
    /// 它的职责只有一条：**把领域事件翻译成界面变化**。
    /// 它不包含任何战斗规则，所以战斗规则改了、UI 换了，两边互不影响。
    ///
    /// 订阅用 IDisposable 收集起来，在 Dispose（作用域销毁）时统一退订 ——
    /// 这样即使对象比事件总线活得短，也不会留下悬挂回调。
    /// </summary>
    public sealed class BattlePresenter : IStartable, IDisposable
    {
        readonly IEventBus bus;
        readonly IBattleHud hud;
        readonly BattleSession session;
        readonly List<IDisposable> subscriptions = new List<IDisposable>(8);

        public BattlePresenter(IEventBus bus, IBattleHud hud, BattleSession session)
        {
            this.bus = bus;
            this.hud = hud;
            this.session = session;
        }

        /// <summary>IStartable：容器构建完成后、第一帧之前执行一次。</summary>
        public void Start()
        {
            subscriptions.Add(bus.Subscribe<BattleStartedEvent>(OnBattleStarted));
            subscriptions.Add(bus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned));
            subscriptions.Add(bus.Subscribe<EnemyKilledEvent>(OnEnemyKilled));
            subscriptions.Add(bus.Subscribe<ScoreChangedEvent>(OnScoreChanged));
            subscriptions.Add(bus.Subscribe<BattleFinishedEvent>(OnBattleFinished));

            // 初始状态同步：事件是"增量"，但界面第一次渲染需要"全量"。
            // （分数不在这里设，交给 BattleTicker 每帧推送，避免出现一次 0/0 的中间态）
            hud.SetStatus("正在加载关卡数据 ...");
            hud.SetEnemiesAlive(session.AliveCount);
        }

        public void Dispose()
        {
            for (var i = 0; i < subscriptions.Count; i++)
            {
                subscriptions[i].Dispose();
            }

            subscriptions.Clear();
        }

        void OnBattleStarted(BattleStartedEvent e)
        {
            hud.SetBestScore(e.BestScore);
            hud.SetStatus($"战斗中（Level {e.Level}）");
        }

        void OnEnemySpawned(EnemySpawnedEvent e)
        {
            Debug.Log($"[Lesson11] 敌人 #{e.EnemyId} 出现（HP {e.MaxHealth:F0}）");
        }

        void OnEnemyKilled(EnemyKilledEvent e)
        {
            Debug.Log($"[Lesson11] 敌人 #{e.EnemyId} 被击杀（+{e.Reward} 分）");
        }

        void OnScoreChanged(ScoreChangedEvent e)
        {
            hud.SetScore(e.Score, e.Target);
        }

        void OnBattleFinished(BattleFinishedEvent e)
        {
            hud.SetStatus(e.Victory ? "胜利！" : "失败 ...");
            hud.SetBestScore(e.BestScore);

            Debug.Log($"[Lesson11] 【结算】结果 = {(e.Victory ? "胜利" : "失败")}，" +
                      $"得分 = {e.Score}，击杀 = {e.KilledCount}，历史最高 = {e.BestScore}");
        }
    }
}
