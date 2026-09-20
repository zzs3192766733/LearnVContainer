using VContainer.Unity;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Application
{
    /// <summary>
    /// 开火用例。
    ///
    /// 这是「应用层动作」：输入层 / UI / AI 都可以调用它，
    /// 而它自己不关心是谁调用的，也不关心打中了要显示什么。
    /// 它只做三件事：找到目标 → 结算伤害 → 发事件。
    /// </summary>
    public sealed class CombatService
    {
        readonly BattleSession session;
        readonly IEventBus bus;
        readonly IBattleConfig config;

        public CombatService(BattleSession session, IEventBus bus, IBattleConfig config)
        {
            this.session = session;
            this.bus = bus;
            this.config = config;
        }

        /// <summary>返回是否真的开了一枪（没有目标时为 false）。</summary>
        public bool FireAtNearest()
        {
            if (!session.IsFighting)
            {
                return false;
            }

            var enemy = session.FindFirstAlive();
            if (enemy == null)
            {
                return false;
            }

            var dealt = enemy.ApplyDamage(session.Weapon.Damage);
            bus.Publish(new EnemyDamagedEvent(enemy.Id, dealt, enemy.Health));

            if (!enemy.IsAlive)
            {
                session.AddKill();
                session.AddScore(config.EnemyReward);
                bus.Publish(new EnemyKilledEvent(enemy.Id, config.EnemyReward));
                bus.Publish(new ScoreChangedEvent(session.Score, config.TargetScore));
            }

            return true;
        }
    }

    /// <summary>
    /// 自动开火驱动：把「按冷却开火」这条控制流从 MonoBehaviour 里搬出来。
    ///
    /// 注意它和 CombatService 的分工：
    ///   - AutoCombatDriver = 什么时候开火（节奏）
    ///   - CombatService    = 开火会发生什么（规则）
    /// 拆开之后，将来要做「手动开火」只需要再写一个 Driver，规则完全复用。
    /// </summary>
    public sealed class AutoCombatDriver : ITickable
    {
        readonly CombatService combat;
        readonly BattleSession session;
        readonly IClock clock;

        float cooldown;

        public AutoCombatDriver(CombatService combat, BattleSession session, IClock clock)
        {
            this.combat = combat;
            this.session = session;
            this.clock = clock;
        }

        public void Tick()
        {
            if (!session.IsFighting)
            {
                return;
            }

            cooldown -= clock.DeltaTime;
            if (cooldown > 0f)
            {
                return;
            }

            if (combat.FireAtNearest())
            {
                cooldown = session.Weapon.Cooldown;
            }
        }
    }
}
