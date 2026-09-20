using UnityEngine;
using VContainer;

namespace VContainerTutorials.Lesson04
{
    /// <summary>
    /// 这个组件由容器通过 RegisterComponentOnNewGameObject 创建：
    /// 容器会 new 一个 GameObject → AddComponent → 注入 → SetActive(true)。
    ///
    /// 本类演示"字段注入"与"方法注入"的组合。
    /// </summary>
    public sealed class AutoCreatedView : MonoBehaviour
    {
        [Inject]
        ScoreService score = null;      // 私有字段也能注入

        [Inject]
        void Initialize()               // 私有方法也能作为注入点，任意方法名
        {
            Debug.Log($"[Lesson04] AutoCreatedView 的 Initialize() 被调用（方法注入，晚于字段注入），Score = {score.Score}");
        }
    }
}
