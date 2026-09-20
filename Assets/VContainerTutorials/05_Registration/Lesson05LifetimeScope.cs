using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson05
{
    public sealed class Lesson05LifetimeScope : LifetimeScope
    {
        GameSettings settings;

        protected override void Awake()
        {
            // 真实项目里 settings 是 [SerializeField] 拖进来的 .asset；
            // 这里为了不用手动创建资源，运行时造一个内存实例。
            settings = ScriptableObject.CreateInstance<GameSettings>();
            settings.cameraSettings = new CameraSettings { MoveSpeed = 12f, ZoomMax = 25f };
            settings.actorSettings = new ActorSettings { MoveSpeed = 0.8f, FlyingTime = 1.5f };

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // ① 一个实现，多种身份：接口们 + 自身
            builder.Register<ServiceA>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            // ② 需要"值"而不是"依赖"的类，用 WithParameter 按类型给常量
            builder.Register<HttpConfig>(Lifetime.Singleton)
                .WithParameter<string>("https://example.com")
                .WithParameter<int>(30);

            // ③ 开放泛型：任意闭合类型都自动可用
            builder.Register(typeof(GenericRepository<>), Lifetime.Singleton);

            // ④ RegisterInstance：把已经存在的对象交给容器
            //    注意：Lifecycle 恒为 Singleton，且容器不接管它的释放。
            builder.RegisterInstance(new AppInfo("1.19.0"));
            builder.RegisterInstance(settings.cameraSettings);
            builder.RegisterInstance(settings.actorSettings);

            builder.RegisterEntryPoint<InterfaceRegistrationDemo>();
            builder.RegisterEntryPoint<ValueAndInstanceDemo>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // RegisterInstance 注册的对象容器不管，所以要自己收尾。
            if (settings != null)
            {
                Destroy(settings);
            }
        }
    }
}
