namespace VContainerTutorials.Lesson11.Domain
{
    // ==================================================================
    // 这一组接口就是 Domain 与外界之间的「端口」（Ports）。
    //
    // 依赖倒置的关键：Domain 定义接口，Infrastructure 去实现它。
    // 所以 Domain 永远不需要知道 Unity、PlayerPrefs、Addressables 的存在。
    // ==================================================================

    /// <summary>时间抽象：让战斗逻辑不依赖 Time.deltaTime，从而能在测试里逐帧推进。</summary>
    public interface IClock
    {
        float DeltaTime { get; }
        float ElapsedTime { get; }
    }

    /// <summary>随机数抽象：测试时可以换成一个"可预测"的实现。</summary>
    public interface IRandomSource
    {
        float Range(float min, float max);
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// 战斗数值配置。
    /// 实现可以是 ScriptableObject（正式项目）、表格数据、或者测试里的一个匿名类。
    /// </summary>
    public interface IBattleConfig
    {
        int Level { get; }
        int TargetScore { get; }
        float SpawnInterval { get; }
        int MaxEnemiesAlive { get; }
        float EnemyBaseHealth { get; }
        int EnemyReward { get; }
        float WeaponDamage { get; }
        float WeaponCooldown { get; }
        float EnemyAttackInterval { get; }
        float EnemyAttackDamage { get; }
        float PlayerMaxHealth { get; }
    }

    /// <summary>存档抽象。正式实现可以是 PlayerPrefs / JSON 文件 / 云存档。</summary>
    public interface ISaveRepository
    {
        int LoadBestScore();
        void SaveBestScore(int score);
    }

    /// <summary>
    /// 表现层出口：Application 只往这里"推值"，
    /// 完全不知道对面是 TMP_Text、Slider 还是 Debug.Log。
    /// </summary>
    public interface IBattleHud
    {
        void SetStatus(string text);
        void SetScore(int score, int target);
        void SetBestScore(int bestScore);
        void SetEnemiesAlive(int count);
        void SetPlayerHealth(float health, float max);
    }
}
