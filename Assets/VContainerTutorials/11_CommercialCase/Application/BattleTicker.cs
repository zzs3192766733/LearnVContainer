using VContainer.Unity;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Application
{
    /// <summary>
    /// 战斗主循环：生成敌人、敌人反击、把数据推给 HUD。
    ///
    /// 它实现了 ITickable —— 也就是说这一切都发生在纯 C# 类里，
    /// 场景里没有任何 MonoBehaviour 参与战斗推进（第 07 课的核心思想）。
    /// </summary>
    public sealed class BattleTicker : ITickable
    {
        readonly BattleSession session;
        readonly IBattleConfig config;
        readonly IEventBus bus;
        readonly IRandomSource random;
        readonly IClock clock;
        readonly IBattleHud hud;

        float spawnTimer;
        float attackTimer;

        public BattleTicker(
            BattleSession session,
            IBattleConfig config,
            IEventBus bus,
            IRandomSource random,
            IClock clock,
            IBattleHud hud)
        {
            this.session = session;
            this.config = config;
            this.bus = bus;
            this.random = random;
            this.clock = clock;
            this.hud = hud;
        }

        public void Tick()
        {
            if (!session.IsFighting)
            {
                return;
            }

            var dt = clock.DeltaTime;

            TickSpawn(dt);
            TickEnemyAttack(dt);
            PushToHud();
        }

        void TickSpawn(float dt)
        {
            if (session.AliveCount >= config.MaxEnemiesAlive)
            {
                return;
            }

            spawnTimer += dt;
            if (spawnTimer < config.SpawnInterval)
            {
                return;
            }

            spawnTimer = 0f;

            // 血量带一点随机浮动，避免每次都是同一个数字（IRandomSource 让它可以被测试替换）
            var health = config.EnemyBaseHealth * random.Range(0.8f, 1.2f);
            var enemy = session.SpawnEnemy(health);
            bus.Publish(new EnemySpawnedEvent(enemy.Id, enemy.MaxHealth));
        }

        void TickEnemyAttack(float dt)
        {
            if (session.AliveCount <= 0)
            {
                attackTimer = 0f;
                return;
            }

            attackTimer += dt;
            if (attackTimer < config.EnemyAttackInterval)
            {
                return;
            }

            attackTimer = 0f;

            var damage = config.EnemyAttackDamage * session.AliveCount;
            session.ApplyPlayerDamage(damage);
        }

        void PushToHud()
        {
            // BattleHudView 内部会做"值没变就不刷新"的判断，
            // 所以这里每帧推是安全的 —— 但真实项目里更推荐"只在变化时推"。
            hud.SetScore(session.Score, config.TargetScore);
            hud.SetEnemiesAlive(session.AliveCount);
            hud.SetPlayerHealth(session.PlayerHealth, session.PlayerMaxHealth);
        }
    }
}
