using System;
using UnityEngine;

namespace VContainerTutorials.Lesson05
{
    [Serializable]
    public class CameraSettings
    {
        public float MoveSpeed = 10f;
        public float ZoomMax = 20f;
    }

    [Serializable]
    public class ActorSettings
    {
        public float MoveSpeed = 0.5f;
        public float FlyingTime = 2f;
    }

    /// <summary>
    /// 官方推荐的配置载体：一个 ScriptableObject 资源，里面装着若干"纯数据"配置对象。
    ///
    /// 真实项目里的用法：
    ///   1. 菜单 Assets → Create → VContainerTutorials → Game Settings 生成 .asset；
    ///   2. 在 LifetimeScope 上写 [SerializeField] GameSettings settings; 把资源拖进去；
    ///   3. Configure 里 builder.RegisterInstance(settings.cameraSettings) 等。
    ///
    /// 本课程为了让示例开箱即用，改成运行时 CreateInstance 造一个内存实例。
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "VContainerTutorials/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        public CameraSettings cameraSettings;
        public ActorSettings actorSettings;
    }
}
