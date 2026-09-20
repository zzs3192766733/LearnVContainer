namespace VContainerTutorials.Lesson04
{
    /// <summary>一个要被注入到 MonoBehaviour 里的普通服务。</summary>
    public sealed class ScoreService
    {
        public int Score { get; private set; } = 100;

        public void Add(int value)
        {
            Score += value;
        }
    }
}
