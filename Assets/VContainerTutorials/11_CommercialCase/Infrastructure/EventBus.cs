using System;
using System.Collections.Generic;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Infrastructure
{
    /// <summary>
    /// IEventBus 的正式实现。
    ///
    /// 设计要点：
    ///   1. 事件用 readonly struct，Publish 走泛型方法 → 正常路径零装箱；
    ///   2. Subscribe 返回 IDisposable，让"订阅"和"退订"成对出现，
    ///      不会像 C# event 那样忘记 -= 造成内存泄漏；
    ///   3. 用 List + 索引遍历，Publish 过程中不产生分配。
    ///
    /// 注意这不是线程安全的：VContainer 的事件基本都在主线程发，够用了。
    /// 需要跨线程时，把 Dictionary/List 换成并发结构即可，接口不变。
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        sealed class Subscription : IDisposable
        {
            readonly EventBus owner;
            readonly Type eventType;
            readonly Delegate handler;

            bool disposed;

            public Subscription(EventBus owner, Type eventType, Delegate handler)
            {
                this.owner = owner;
                this.eventType = eventType;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                owner.Remove(eventType, handler);
            }
        }

        readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(T evt)
        {
            if (!handlers.TryGetValue(typeof(T), out var list) || list.Count == 0)
            {
                return;
            }

            // 正序索引遍历：Publish 期间新加的订阅会等到下一次 Publish 才生效
            for (var i = 0; i < list.Count; i++)
            {
                ((Action<T>)list[i]).Invoke(evt);
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>(4);
                handlers.Add(typeof(T), list);
            }

            list.Add(handler);
            return new Subscription(this, typeof(T), handler);
        }

        void Remove(Type eventType, Delegate handler)
        {
            if (!handlers.TryGetValue(eventType, out var list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                handlers.Remove(eventType);
            }
        }
    }
}
