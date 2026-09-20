using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Infrastructure
{
    /// <summary>
    /// Domain 的 IClock 在 Unity 上的实现。
    /// 这类"适配器"是 Infrastructure 层最主要的产出物。
    /// </summary>
    public sealed class UnityClock : IClock
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float ElapsedTime => UnityEngine.Time.time;
    }

    /// <summary>
    /// Domain 的 IRandomSource 在 Unity 上的实现。
    ///
    /// 商业项目里更常见的是封装一个可设种子的自研随机数（保证战斗可回放、可复现），
    /// 这里为了简洁直接转发 UnityEngine.Random。
    /// </summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public float Range(float min, float max) => UnityEngine.Random.Range(min, max);

        public int Range(int minInclusive, int maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);
    }
}
