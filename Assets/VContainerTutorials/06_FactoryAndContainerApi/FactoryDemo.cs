using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson06
{
    /// <summary>演示三种"运行时创建对象"的方式。</summary>
    public sealed class FactoryDemo : IStartable
    {
        readonly Func<float, Bullet> createBullet;
        readonly Func<int, Enemy> createEnemy;
        readonly EnemyFactory enemyFactory;
        readonly WeaponBag weaponBag;

        public FactoryDemo(
            Func<float, Bullet> createBullet,
            Func<int, Enemy> createEnemy,
            EnemyFactory enemyFactory,
            WeaponBag weaponBag)
        {
            this.createBullet = createBullet;
            this.createEnemy = createEnemy;
            this.enemyFactory = enemyFactory;
            this.weaponBag = weaponBag;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson06] ---- 1. 只有运行时参数的工厂 ----");
            Debug.Log("[Lesson06] createBullet(10) -> " + createBullet(10f));
            Debug.Log("[Lesson06] createBullet(20) -> " + createBullet(20f));

            Debug.Log("[Lesson06] ---- 2. 容器依赖 + 运行时参数的工厂 ----");
            Debug.Log("[Lesson06] createEnemy(1) -> " + createEnemy(1));
            Debug.Log("[Lesson06] createEnemy(2) -> " + createEnemy(2));
            Debug.Log("[Lesson06] （注意：上面两次之间没有再打印『外层 lambda 执行』——因为 Lifetime.Scoped 只求值一次）");

            Debug.Log("[Lesson06] ---- 3. 工厂类（推荐写法）----");
            Debug.Log("[Lesson06] enemyFactory.Create() -> " + enemyFactory.Create());
            Debug.Log("[Lesson06] enemyFactory.Create() -> " + enemyFactory.Create());

            Debug.Log("[Lesson06] ---- 4. 委托注册（每个作用域只执行一次）----");
            Debug.Log("[Lesson06] weaponBag -> " + weaponBag);
        }
    }

    /// <summary>演示容器 API：Resolve / TryResolve / ResolveOrDefault。</summary>
    public sealed class ContainerApiDemo : IStartable
    {
        readonly IObjectResolver container;

        public ContainerApiDemo(IObjectResolver container)
        {
            this.container = container;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson06] ---- 5. 容器 API ----");

            // Bullet 从来不是"被注册的类型"，它只是工厂的产物。
            Debug.Log("[Lesson06] TryResolve<Bullet>() -> " + container.TryResolve<Bullet>(out _));
            Debug.Log("[Lesson06] TryResolve<Func<float, Bullet>>() -> " + container.TryResolve<Func<float, Bullet>>(out _));
            Debug.Log("[Lesson06] ResolveOrDefault<Bullet>() -> " + (container.ResolveOrDefault<Bullet>() == null ? "(null)" : "非 null"));

            // 直接 Resolve 一个没注册的类型会抛异常 —— 这里演示 TryResolve 的"安全"含义。
            // container.Resolve<Bullet>();   // ← 取消注释会抛 VContainerException

            // ApplicationOrigin 就是创建这个容器的 LifetimeScope。
            var origin = container.ApplicationOrigin;
            Debug.Log("[Lesson06] ApplicationOrigin -> " + (origin == null ? "(null)" : origin.GetType().Name));
        }
    }
}
