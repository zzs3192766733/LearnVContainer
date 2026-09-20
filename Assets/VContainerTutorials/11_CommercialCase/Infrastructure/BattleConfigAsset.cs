using UnityEngine;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Infrastructure
{
    /// <summary>
    /// IBattleConfig 的 ScriptableObject 实现。
    ///
    /// 正式项目流程：
    ///   1. 菜单 Assets → Create → VContainerTutorials → Lesson11 Battle Config 生成 .asset；
    ///   2. 在 Lesson11LifetimeScope 的 Inspector 上拖进去；
    ///   3. 策划直接改数值，不用动代码、不用重新编译。
    ///
    /// 本课程为了开箱即用，改成运行时 CreateInstance 一个内存实例（数值就是下面的默认值）。
    /// </summary>
    [CreateAssetMenu(fileName = "BattleConfig", menuName = "VContainerTutorials/Lesson11 Battle Config")]
    public sealed class BattleConfigAsset : ScriptableObject, IBattleConfig
    {
        [Header("关卡")]
        [SerializeField] int level = 1;
        [SerializeField] int targetScore = 100;

        [Header("敌人")]
        [SerializeField] float spawnInterval = 0.35f;
        [SerializeField] int maxEnemiesAlive = 6;
        [SerializeField] float enemyBaseHealth = 26f;
        [SerializeField] int enemyReward = 10;
        [SerializeField] float enemyAttackInterval = 1.2f;
        [SerializeField] float enemyAttackDamage = 1.5f;

        [Header("玩家")]
        [SerializeField] float playerMaxHealth = 100f;
        [SerializeField] float weaponDamage = 9f;
        [SerializeField] float weaponCooldown = 0.12f;

        public int Level => level;
        public int TargetScore => targetScore;
        public float SpawnInterval => spawnInterval;
        public int MaxEnemiesAlive => maxEnemiesAlive;
        public float EnemyBaseHealth => enemyBaseHealth;
        public int EnemyReward => enemyReward;
        public float WeaponDamage => weaponDamage;
        public float WeaponCooldown => weaponCooldown;
        public float EnemyAttackInterval => enemyAttackInterval;
        public float EnemyAttackDamage => enemyAttackDamage;
        public float PlayerMaxHealth => playerMaxHealth;
    }
}
