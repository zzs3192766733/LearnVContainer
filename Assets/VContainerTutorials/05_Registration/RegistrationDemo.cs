using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials.Lesson05
{
    /// <summary>
    /// 演示：一次注册、多种身份。
    /// 构造函数里 4 个参数，其实指向的是**同一个对象**。
    /// </summary>
    public sealed class InterfaceRegistrationDemo : IStartable
    {
        readonly IServiceA byInterface;
        readonly IInputPort inputPort;
        readonly IOutputPort outputPort;
        readonly ServiceA bySelf;

        public InterfaceRegistrationDemo(
            IServiceA byInterface,
            IInputPort inputPort,
            IOutputPort outputPort,
            ServiceA bySelf)
        {
            this.byInterface = byInterface;
            this.inputPort = inputPort;
            this.outputPort = outputPort;
            this.bySelf = bySelf;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson05] ---- 一个实现，多种身份 ----");
            Debug.Log("[Lesson05] Resolve<IServiceA>   -> " + byInterface.GetType().Name);
            Debug.Log("[Lesson05] Resolve<IInputPort>  -> " + inputPort.GetType().Name + ", Read() = " + inputPort.Read());
            Debug.Log("[Lesson05] Resolve<IOutputPort> -> " + outputPort.GetType().Name);
            Debug.Log("[Lesson05] Resolve<ServiceA>    -> " + bySelf.GetType().Name + "  （靠 .AsSelf()）");

            var same = ReferenceEquals(byInterface, bySelf)
                       && ReferenceEquals(byInterface, inputPort)
                       && ReferenceEquals(byInterface, outputPort);
            Debug.Log("[Lesson05] 它们是不是同一个实例？ " + same + "  （'多种身份'不会多造对象）");

            outputPort.Write("hello");
        }
    }

    /// <summary>
    /// 演示：WithParameter / 开放泛型 / RegisterInstance。
    /// 6 个构造函数参数确实有点长 —— 这也正是构造函数注入的可读性优势：
    /// 你一眼就能看出这个类依赖了几个东西。
    /// </summary>
    public sealed class ValueAndInstanceDemo : IStartable
    {
        readonly HttpConfig httpConfig;
        readonly GenericRepository<int> intRepository;
        readonly GenericRepository<string> stringRepository;
        readonly CameraSettings cameraSettings;
        readonly ActorSettings actorSettings;
        readonly AppInfo appInfo;

        public ValueAndInstanceDemo(
            HttpConfig httpConfig,
            GenericRepository<int> intRepository,
            GenericRepository<string> stringRepository,
            CameraSettings cameraSettings,
            ActorSettings actorSettings,
            AppInfo appInfo)
        {
            this.httpConfig = httpConfig;
            this.intRepository = intRepository;
            this.stringRepository = stringRepository;
            this.cameraSettings = cameraSettings;
            this.actorSettings = actorSettings;
            this.appInfo = appInfo;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson05] ---- 参数 / 开放泛型 / 实例 ----");
            Debug.Log("[Lesson05] WithParameter 注入的 HttpConfig -> " + httpConfig);
            Debug.Log("[Lesson05] 开放泛型 -> " + intRepository.Describe() + " / " + stringRepository.Describe());
            Debug.Log("[Lesson05] RegisterInstance 注入的 CameraSettings.MoveSpeed = " + cameraSettings.MoveSpeed);
            Debug.Log("[Lesson05] RegisterInstance 注入的 ActorSettings.FlyingTime = " + actorSettings.FlyingTime);
            Debug.Log("[Lesson05] RegisterInstance 注入的 AppInfo -> " + appInfo);
        }
    }
}
