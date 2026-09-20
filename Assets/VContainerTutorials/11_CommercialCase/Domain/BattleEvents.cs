using System;

namespace VContainerTutorials.Lesson11.Domain
{
    /// <summary>
    /// 极简事件总线抽象。
    ///
    /// VContainer 官方明确不支持 Zenject 的 Signal，理由是需要中央消息模式的
    /// 项目风格差异太大。所以商业项目通常二选一：
    ///   1) 自己实现一个极小的 EventBus（本案例的做法）；
    ///   2) 或引入 MessagePipe / VitalRouter 这类专门的库。
    ///
    /// 关键点：接口放在 Domain，实现放在 Infrastructure。
    /// 这样 Application / Presentation 都只依赖抽象，测试时可以直接塞一个同步假实现。
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T evt);
        IDisposable Subscribe<T>(Action<T> handler);
    }

    // ------------------------------------------------------------------
    // 领域事件。全部用 readonly struct，避免每次发事件都产生 GC 分配。
    // ------------------------------------------------------------------

    public readonly struct BattleStartedEvent
    {
        public readonly int Level;
        public readonly int BestScore;

        public BattleStartedEvent(int level, int bestScore)
        {
            Level = level;
            BestScore = bestScore;
        }
    }

    public readonly struct EnemySpawnedEvent
    {
        public readonly int EnemyId;
        public readonly float MaxHealth;

        public EnemySpawnedEvent(int enemyId, float maxHealth)
        {
            EnemyId = enemyId;
            MaxHealth = maxHealth;
        }
    }

    public readonly struct EnemyDamagedEvent
    {
        public readonly int EnemyId;
        public readonly float Damage;
        public readonly float Health;

        public EnemyDamagedEvent(int enemyId, float damage, float health)
        {
            EnemyId = enemyId;
            Damage = damage;
            Health = health;
        }
    }

    public readonly struct EnemyKilledEvent
    {
        public readonly int EnemyId;
        public readonly int Reward;

        public EnemyKilledEvent(int enemyId, int reward)
        {
            EnemyId = enemyId;
            Reward = reward;
        }
    }

    public readonly struct ScoreChangedEvent
    {
        public readonly int Score;
        public readonly int Target;

        public ScoreChangedEvent(int score, int target)
        {
            Score = score;
            Target = target;
        }
    }

    public readonly struct BattleFinishedEvent
    {
        public readonly bool Victory;
        public readonly int Score;
        public readonly int BestScore;
        public readonly int KilledCount;

        public BattleFinishedEvent(bool victory, int score, int bestScore, int killedCount)
        {
            Victory = victory;
            Score = score;
            BestScore = bestScore;
            KilledCount = killedCount;
        }
    }
}
