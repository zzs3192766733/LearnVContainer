using System;
using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials.Lesson09
{
    /// <summary>
    /// 只注册在**子作用域**里的入口点。
    ///
    /// 它证明了：子作用域自己也能驱动 EntryPoint，
    /// 而且它会随子作用域的释放而被 Dispose。
    /// </summary>
    public sealed class StageWatcher : IStartable, ITickable, IDisposable
    {
        readonly StageInfo stage;
        bool tickLogged;

        public StageWatcher(StageInfo stage)
        {
            this.stage = stage;
        }

        public void Start()
        {
            Debug.Log($"[Lesson09] 子作用域的 StageWatcher.Start() 已触发，关卡 = {stage}");
        }

        public void Tick()
        {
            if (tickLogged)
            {
                return;
            }

            tickLogged = true;
            Debug.Log("[Lesson09] 子作用域的 StageWatcher.Tick() 已触发（子作用域的 EntryPoint 同样会被驱动）");
        }

        public void Dispose()
        {
            Debug.Log("[Lesson09] 子作用域的 StageWatcher.Dispose() 已触发");
        }
    }
}
