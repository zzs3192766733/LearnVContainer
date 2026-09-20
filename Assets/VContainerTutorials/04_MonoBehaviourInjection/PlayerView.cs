using UnityEngine;
using VContainer;

namespace VContainerTutorials.Lesson04
{
    /// <summary>
    /// 模拟"场景里已经摆好"的 View 组件。
    ///
    /// 同时演示属性注入与方法注入，方便你观察到执行顺序：
    ///     字段 → 属性 → 方法
    /// 所以那行"方法注入"的日志一定在"属性注入"之后打印。
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        public string Label = "Player";

        [Inject]
        public ScoreService Score { get; set; }

        [Inject]
        public void Construct(ScoreService scoreService)
        {
            Debug.Log($"[Lesson04] PlayerView '{name}' 的 [Inject] 方法被调用（方法注入），Score = {scoreService.Score}");
        }

        public void Report()
        {
            var score = Score != null ? Score.Score.ToString() : "null（没有被注入）";
            Debug.Log($"[Lesson04] PlayerView '{name}' ({Label}) 当前 Score = {score}");
        }
    }
}
