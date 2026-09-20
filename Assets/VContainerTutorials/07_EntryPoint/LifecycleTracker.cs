using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials.Lesson07
{
    /// <summary>
    /// 实现了 VContainer 提供的全部 PlayerLoop 时机接口，用来观察它们的执行顺序。
    ///
    /// 注意：Tick 类是**每帧**调用的，所以这里只打印每种回调的第一次。
    /// </summary>
    public sealed class LifecycleTracker :
        IInitializable,
        IPostInitializable,
        IStartable,
        IPostStartable,
        IFixedTickable,
        IPostFixedTickable,
        ITickable,
        IPostTickable,
        ILateTickable,
        IPostLateTickable,
        IDisposable
    {
        readonly HashSet<string> reported = new HashSet<string>();

        void ReportOnce(string name)
        {
            if (reported.Add(name))
            {
                Debug.Log($"[Lesson07] {name}   frame={Time.frameCount}");
            }
        }

        // ---- 同步：容器构建回调里直接调用，最早 ----
        public void Initialize() => ReportOnce("IInitializable.Initialize");
        public void PostInitialize() => ReportOnce("IPostInitializable.PostInitialize");

        // ---- 驱动：注册到 PlayerLoop ----
        public void Start() => ReportOnce("IStartable.Start");
        public void PostStart() => ReportOnce("IPostStartable.PostStart");

        public void FixedTick() => ReportOnce("IFixedTickable.FixedTick");
        public void PostFixedTick() => ReportOnce("IPostFixedTickable.PostFixedTick");

        public void Tick() => ReportOnce("ITickable.Tick");
        public void PostTick() => ReportOnce("IPostTickable.PostTick");

        public void LateTick() => ReportOnce("ILateTickable.LateTick");
        public void PostLateTick() => ReportOnce("IPostLateTickable.PostLateTick");

        // ---- 释放：所属作用域销毁时 ----
        public void Dispose()
        {
            Debug.Log("[Lesson07] IDisposable.Dispose  容器 / 作用域销毁时被调用");
        }
    }
}
