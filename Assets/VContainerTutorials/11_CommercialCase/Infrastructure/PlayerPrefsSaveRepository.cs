using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Infrastructure
{
    /// <summary>
    /// ISaveRepository 的 PlayerPrefs 实现。
    ///
    /// 想换成 JSON 文件存档？只需要再写一个实现类，
    /// 然后在组合根里把这一行替换掉 —— Application / Domain 一行都不用改。
    /// 这就是"面向接口注册"带来的可替换性。
    /// </summary>
    public sealed class PlayerPrefsSaveRepository : ISaveRepository
    {
        const string BestScoreKey = "vcontainer_tutorials.lesson11.best_score";

        public int LoadBestScore() => UnityEngine.PlayerPrefs.GetInt(BestScoreKey, 0);

        public void SaveBestScore(int score)
        {
            UnityEngine.PlayerPrefs.SetInt(BestScoreKey, score);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
