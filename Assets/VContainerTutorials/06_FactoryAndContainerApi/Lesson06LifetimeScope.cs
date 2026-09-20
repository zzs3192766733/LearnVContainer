using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson06
{
    public sealed class Lesson06LifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 普通依赖
            builder.Register<EnemyConfig>(Lifetime.Singleton);
            builder.Register<WeaponConfig>(Lifetime.Singleton);

            // ---------------------------------------------------------------
            // ① 只有运行时参数的工厂：没有 Lifetime 参数，
            //    因为它注册的其实就是"那个委托实例本身"，永远可复用。
            // ---------------------------------------------------------------
            builder.RegisterFactory<float, Bullet>(speed => new Bullet(speed));

            // ---------------------------------------------------------------
            // ② 需要容器依赖 + 运行时参数的工厂。
            //    外层 lambda 的返回类型是 Func<int, Enemy>，
            //    Lifecycle 控制的是"外层 lambda 多久重新求值一次"。
            // ---------------------------------------------------------------
            builder.RegisterFactory<int, Enemy>(container =>
            {
                // 这一层每个作用域只跑一次（Lifetime.Scoped）
                var config = container.Resolve<EnemyConfig>();
                Debug.Log("[Lesson06] 【外层 lambda 执行了】EnemyConfig 已解析，"
                          + "接下来返回的 Func<int, Enemy> 会在本次作用域内复用");
                return id => new Enemy(config, id);
            }, Lifetime.Scoped);

            // ---------------------------------------------------------------
            // ③ 推荐的 OOP 写法：工厂类由容器正常注册
            // ---------------------------------------------------------------
            builder.Register<EnemyFactory>(Lifetime.Singleton);

            // ---------------------------------------------------------------
            // ④ 委托注册：注册类型是 WeaponBag 本身，lambda 只执行一次
            // ---------------------------------------------------------------
            builder.Register<WeaponBag>(container =>
            {
                var config = container.Resolve<WeaponConfig>();
                Debug.Log("[Lesson06] WeaponBag 的委托被执行了（每个作用域只执行一次）");
                return new WeaponBag(config.WeaponNames);
            }, Lifetime.Scoped);

            // ---------------------------------------------------------------
            // ⑤ 容器回调
            // ---------------------------------------------------------------
            builder.RegisterBuildCallback(container =>
            {
                Debug.Log("[Lesson06] RegisterBuildCallback: 容器构建完成，可以在这里预热/自检");
            });

            builder.RegisterDisposeCallback(container =>
            {
                Debug.Log("[Lesson06] RegisterDisposeCallback: 容器即将销毁（停止 Play 或切换课程时触发）");
            });

            builder.RegisterEntryPoint<FactoryDemo>();
            builder.RegisterEntryPoint<ContainerApiDemo>();
        }
    }
}
