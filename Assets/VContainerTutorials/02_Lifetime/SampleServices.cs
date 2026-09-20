using System;
using UnityEngine;

namespace VContainerTutorials.Lesson02
{
    /// <summary>
    /// 所有示例服务的基类：构造时拿一个自增序号，
    /// 这样日志里就能一眼看出"这是新建的实例，还是复用的旧实例"。
    /// </summary>
    public abstract class SerialNumberedService
    {
        static int nextSerial;

        protected SerialNumberedService()
        {
            Serial = ++nextSerial;
        }

        public int Serial { get; }
    }

    public sealed class TransientSample : SerialNumberedService { }

    public sealed class SingletonSample : SerialNumberedService { }

    public sealed class ScopedSample : SerialNumberedService { }

    /// <summary>
    /// 用来观察"作用域销毁时会不会回调 Dispose"。
    /// </summary>
    public sealed class DisposableSample : SerialNumberedService, IDisposable
    {
        public void Dispose()
        {
            Debug.Log($"[Lesson02] DisposableSample #{Serial}.Dispose() 被调用了（作用域销毁时）");
        }
    }
}
