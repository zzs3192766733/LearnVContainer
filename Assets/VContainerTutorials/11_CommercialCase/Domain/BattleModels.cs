using System;

namespace VContainerTutorials.Lesson11.Domain
{
    public enum BattlePhase
    {
        Preparing,
        Fighting,
        Finished,
    }

    /// <summary>武器数据。纯数据结构，不依赖任何框架。</summary>
    public readonly struct WeaponModel
    {
        public readonly string Name;
        public readonly float Damage;
        public readonly float Cooldown;

        public WeaponModel(string name, float damage, float cooldown)
        {
            Name = name;
            Damage = damage;
            Cooldown = cooldown;
        }
    }

    /// <summary>
    /// 敌人的领域模型。
    /// 注意它**完全没有** Unity / VContainer 的引用，所以可以在纯 NUnit 测试里直接 new。
    /// </summary>
    public sealed class EnemyModel
    {
        public int Id { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }

        public bool IsAlive => Health > 0f;

        public EnemyModel(int id, float maxHealth)
        {
            Id = id;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        /// <summary>返回本次实际造成的伤害（避免溢出）。</summary>
        public float ApplyDamage(float damage)
        {
            if (!IsAlive || damage <= 0f)
            {
                return 0f;
            }

            var dealt = damage > Health ? Health : damage;
            Health -= dealt;
            return dealt;
        }
    }

    /// <summary>
    /// 战斗阶段状态机。
    /// 只维护状态 + 抛事件，不关心是谁在驱动它。
    /// </summary>
    public sealed class BattleStateMachine
    {
        public BattlePhase Phase { get; private set; } = BattlePhase.Preparing;

        public event Action<BattlePhase> PhaseChanged;

        public void MoveTo(BattlePhase next)
        {
            if (Phase == next)
            {
                return;
            }

            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
