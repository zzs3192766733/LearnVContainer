using System;
using System.Collections.Generic;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Application
{
    /// <summary>
    /// 一局战斗的完整运行时状态。
    ///
    /// 它注册成 Lifetime.Scoped，所以天然就是「一局一份」：
    ///   - 进入关卡 → 子作用域创建 → 新的 BattleSession
    ///   - 退出关卡 → 子作用域 Dispose → BattleSession 一起被释放
    ///
    /// 这正是第 02 课讲的 Scoped 在真实项目里的用法。
    /// </summary>
    public sealed class BattleSession : IDisposable
    {
        readonly List<EnemyModel> enemies = new List<EnemyModel>(32);

        int nextEnemyId = 1;
        int aliveCount;

        public BattleStateMachine State { get; } = new BattleStateMachine();

        public int Score { get; private set; }
        public int KilledCount { get; private set; }
        public int AliveCount => aliveCount;

        public float PlayerMaxHealth { get; private set; }
        public float PlayerHealth { get; private set; }

        public WeaponModel Weapon { get; private set; }

        public bool IsFighting => State.Phase == BattlePhase.Fighting;
        public bool IsFinished => State.Phase == BattlePhase.Finished;

        /// <summary>由 BattleBootstrapper 在异步加载完成后调用。</summary>
        public void Begin(IBattleConfig config)
        {
            PlayerMaxHealth = config.PlayerMaxHealth;
            PlayerHealth = config.PlayerMaxHealth;
            Weapon = new WeaponModel("自动步枪", config.WeaponDamage, config.WeaponCooldown);
            State.MoveTo(BattlePhase.Fighting);
        }

        public EnemyModel SpawnEnemy(float maxHealth)
        {
            RemoveDead();
            var enemy = new EnemyModel(nextEnemyId++, maxHealth);
            enemies.Add(enemy);
            aliveCount++;
            return enemy;
        }

        /// <summary>找一个还活着的目标（最简单的一版仇恨规则）。</summary>
        public EnemyModel FindFirstAlive()
        {
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return enemies[i];
                }
            }

            return null;
        }

        public void AddKill()
        {
            KilledCount++;
            if (aliveCount > 0)
            {
                aliveCount--;
            }
        }

        public void AddScore(int value)
        {
            Score += value;
        }

        /// <summary>玩家受伤，返回剩余血量。</summary>
        public float ApplyPlayerDamage(float damage)
        {
            PlayerHealth -= damage;
            if (PlayerHealth < 0f)
            {
                PlayerHealth = 0f;
            }

            return PlayerHealth;
        }

        public void Finish()
        {
            State.MoveTo(BattlePhase.Finished);
        }

        void RemoveDead()
        {
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                if (!enemies[i].IsAlive)
                {
                    enemies.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            enemies.Clear();
            aliveCount = 0;
        }
    }
}
