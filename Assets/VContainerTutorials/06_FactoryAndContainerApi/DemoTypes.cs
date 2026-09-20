using UnityEngine;

namespace VContainerTutorials.Lesson06
{
    /// <summary>只有"运行时参数"的产物。</summary>
    public sealed class Bullet
    {
        public readonly float Speed;

        public Bullet(float speed)
        {
            Speed = speed;
        }

        public override string ToString() => $"Bullet(speed={Speed})";
    }

    /// <summary>容器里的依赖。</summary>
    public sealed class EnemyConfig
    {
        public int BaseHealth = 100;
    }

    /// <summary>既要容器依赖、又要运行时参数的产物。</summary>
    public sealed class Enemy
    {
        public readonly int Id;
        public readonly int Health;

        public Enemy(EnemyConfig config, int id)
        {
            Id = id;
            Health = config.BaseHealth;
        }

        public override string ToString() => $"Enemy#{Id}(hp={Health})";
    }

    /// <summary>官方更推荐的写法：用工厂类而不是 lambda。工厂类本身可以被容器管理。</summary>
    public sealed class EnemyFactory
    {
        readonly EnemyConfig config;
        int nextId;

        public EnemyFactory(EnemyConfig config)
        {
            this.config = config;
        }

        public Enemy Create() => new Enemy(config, ++nextId);
    }

    /// <summary>用"委托注册"创建的产物。</summary>
    public sealed class WeaponBag
    {
        public readonly string[] Weapons;

        public WeaponBag(string[] weapons)
        {
            Weapons = weapons;
        }

        public override string ToString() => "WeaponBag[" + string.Join(", ", Weapons) + "]";
    }

    public sealed class WeaponConfig
    {
        public string[] WeaponNames = { "Sword", "Bow" };
    }
}
