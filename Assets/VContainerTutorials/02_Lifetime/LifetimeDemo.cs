using System.Text;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson02
{
    /// <summary>
    /// 用 IObjectResolver 亲手 Resolve，观察三种 Lifetime 的实际行为。
    ///
    /// 平时写业务代码几乎不需要直接调 Resolve —— 这里是为了教学才这样写。
    /// </summary>
    public sealed class LifetimeDemo : IStartable
    {
        readonly IObjectResolver container;

        // 容器默认就把 IObjectResolver 注册好了，直接注入即可。
        public LifetimeDemo(IObjectResolver container)
        {
            this.container = container;
        }

        void IStartable.Start()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== Lesson02 : Lifetime 生命周期 ==========");

            sb.AppendLine("【父作用域内，连续 Resolve 同一类型两次】");
            AppendPair(sb, "Transient", container.Resolve<TransientSample>(), container.Resolve<TransientSample>());
            AppendPair(sb, "Singleton", container.Resolve<SingletonSample>(), container.Resolve<SingletonSample>());
            AppendPair(sb, "Scoped   ", container.Resolve<ScopedSample>(), container.Resolve<ScopedSample>());

            var parentSingleton = container.Resolve<SingletonSample>();
            var parentScoped = container.Resolve<ScopedSample>();

            sb.AppendLine();
            sb.AppendLine("【开一个子作用域，再做同样的实验】");

            // CreateScope() 返回 IScopedObjectResolver，它实现了 IDisposable。
            // 子作用域的注册表是"空 + 一个安装委托"，解析时会向上查找父作用域。
            using (var child = container.CreateScope(b =>
                   {
                       // 这个子作用域里额外注册一个只属于它的服务
                       b.Register<DisposableSample>(Lifetime.Scoped);
                   }))
            {
                AppendPair(sb, "Transient", child.Resolve<TransientSample>(), child.Resolve<TransientSample>());
                AppendPair(sb, "Singleton", child.Resolve<SingletonSample>(), child.Resolve<SingletonSample>());
                AppendPair(sb, "Scoped   ", child.Resolve<ScopedSample>(), child.Resolve<ScopedSample>());

                var childSingleton = child.Resolve<SingletonSample>();
                var childScoped = child.Resolve<ScopedSample>();

                sb.AppendLine();
                sb.AppendLine("【父 vs 子：是不是同一个实例】");
                sb.AppendLine($"  Singleton : 父 #{parentSingleton.Serial} vs 子 #{childSingleton.Serial} -> " +
                              (ReferenceEquals(parentSingleton, childSingleton)
                                  ? "同一个实例（Singleton 全局唯一）"
                                  : "不同实例"));
                sb.AppendLine($"  Scoped    : 父 #{parentScoped.Serial} vs 子 #{childScoped.Serial} -> " +
                              (ReferenceEquals(parentScoped, childScoped)
                                  ? "同一个实例"
                                  : "不同实例（每个作用域各持一份）"));

                Debug.Log(sb.ToString());

                // 单独演示 Dispose 时机：这个实例注册在子作用域，所以归子作用域管。
                var disposable = child.Resolve<DisposableSample>();
                Debug.Log($"[Lesson02] 在子作用域里创建了 DisposableSample #{disposable.Serial}，" +
                          "马上离开 using 块，观察它的 Dispose() 什么时候被调用");
            }

            Debug.Log("[Lesson02] 子作用域已 Dispose 完毕。");
        }

        static void AppendPair(StringBuilder sb, string label, SerialNumberedService a, SerialNumberedService b)
        {
            sb.AppendLine($"  {label} : #{a.Serial} / #{b.Serial}  -> " +
                          (ReferenceEquals(a, b) ? "同一实例" : "不同实例"));
        }
    }
}
