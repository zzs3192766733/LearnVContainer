using UnityEngine;
using VContainer;
using VContainer.Unity;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace VContainerTutorials.Lesson10
{
    /// <summary>会被缓存复用的服务。</summary>
    public sealed class CachedService
    {
        public int Value = 42;
    }

    /// <summary>每次解析都要新建的服务。</summary>
    public sealed class FreshService
    {
        public int Value;

        public FreshService()
        {
            Value = 42;
        }
    }

    /// <summary>
    /// 标了 [InjectIgnore] 的类型不会走 Source Generator（仍然走反射，功能不受影响）。
    /// 只在开启了 Source Generator 时有实际差别；这里作为一个示例点。
    /// </summary>
    [InjectIgnore]
    public sealed class ReflectionOnlyService
    {
        public int Value = 42;
    }

    /// <summary>
    /// 粗略测量解析开销，建立"Resolve 到底贵不贵"的直觉。
    /// </summary>
    public sealed class PerformanceProbe : IStartable
    {
        const int Iterations = 100000;

        readonly IObjectResolver container;

        public PerformanceProbe(IObjectResolver container)
        {
            this.container = container;
        }

        void IStartable.Start()
        {
            Debug.Log("[Lesson10] ---- 解析开销实测（Editor / Mono，仅供参考量级）----");

            Measure("Singleton（已缓存，不新建对象）", () =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    container.Resolve<CachedService>();
                }
            });

            Measure("Transient（每次都要 new）      ", () =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    container.Resolve<FreshService>();
                }
            });

            Measure("Transient + 标了 [InjectIgnore] ", () =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    container.Resolve<ReflectionOnlyService>();
                }
            });

            Debug.Log("[Lesson10] 结论：Singleton 的解析基本等价于一次字典查找；" +
                      "Transient 的主要成本在 new 本身。真正贵的是**第一次**创建某类型时构建注入器，" +
                      "而那只发生一次。");
        }

        static void Measure(string label, System.Action action)
        {
            // 预热一次，避免 JIT 干扰
            action();

            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            var perCallUs = stopwatch.Elapsed.TotalMilliseconds * 1000.0 / Iterations;
            Debug.Log($"[Lesson10] {label}: {Iterations} 次耗时 {stopwatch.Elapsed.TotalMilliseconds:F2} ms" +
                      $"（约 {perCallUs:F4} us/次）");
        }
    }
}
