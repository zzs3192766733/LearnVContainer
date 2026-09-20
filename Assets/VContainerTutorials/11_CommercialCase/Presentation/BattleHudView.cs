using UnityEngine;
using VContainerTutorials.Lesson11.Domain;

namespace VContainerTutorials.Lesson11.Presentation
{
    /// <summary>
    /// 表现层：只负责「把值显示出来」。
    ///
    /// 真实项目里它持有一堆 TMP_Text / Image / Slider，并且由 Prefab 实例化；
    /// 本课程用 Debug.Log 代替，保持零额外依赖。
    ///
    /// 两个值得注意的商业实践：
    ///   1. **脏值检查**：SetXxx 每帧都会被调用，但只有真的变化时才刷新 UI。
    ///      UI 副作用（SetText、SetActive）远比一个 if 判断贵。
    ///   2. **量化显示**：血量是 float，每帧都在变；用"每 10 点一个档位"
    ///      避免刷屏（真实项目里对应的是"数字滚动动画"这类节流手段）。
    /// </summary>
    public sealed class BattleHudView : MonoBehaviour, IBattleHud
    {
        string status = string.Empty;
        int score = int.MinValue;
        int target = int.MinValue;
        int best = int.MinValue;
        int alive = int.MinValue;
        int healthBucket = int.MinValue;

        public void SetStatus(string text)
        {
            if (status == text)
            {
                return;
            }

            status = text;
            Debug.Log($"[Lesson11][HUD] 状态: {text}");
        }

        public void SetScore(int value, int targetScore)
        {
            if (score == value && target == targetScore)
            {
                return;
            }

            score = value;
            target = targetScore;
            Debug.Log($"[Lesson11][HUD] 得分: {value} / {targetScore}");
        }

        public void SetBestScore(int bestScore)
        {
            if (best == bestScore)
            {
                return;
            }

            best = bestScore;
            Debug.Log($"[Lesson11][HUD] 历史最高: {bestScore}");
        }

        public void SetEnemiesAlive(int count)
        {
            if (alive == count)
            {
                return;
            }

            alive = count;
            Debug.Log($"[Lesson11][HUD] 场上敌人: {count}");
        }

        public void SetPlayerHealth(float health, float max)
        {
            var bucket = Mathf.CeilToInt(health / 10f) * 10;
            if (healthBucket == bucket)
            {
                return;
            }

            healthBucket = bucket;
            Debug.Log($"[Lesson11][HUD] 玩家血量: {health:F0} / {max:F0}");
        }
    }
}
