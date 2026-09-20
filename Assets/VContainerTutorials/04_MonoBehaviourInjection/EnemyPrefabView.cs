using UnityEngine;
using VContainer;

namespace VContainerTutorials.Lesson04
{
    /// <summary>
    /// 模拟"挂在 Prefab 上"的 View 组件。
    ///
    /// 它有两个注入点：一个字段、一个方法（VContainer 的注入顺序是 字段 → 属性 → 方法）。
    ///
    /// 关键观察点是 <see cref="WasInjectedBeforeAwake"/>：
    ///   VContainer 的 Instantiate 路径会 先 SetActive(false) → 注入 → 再 SetActive(true)，
    ///   所以走这条路创建的实例，Awake() 跑的时候 [Inject] 字段已经有值了。
    ///   而 Unity 原生 Object.Instantiate 没这个保护，Awake() 会先跑、字段还是 null。
    /// </summary>
    public sealed class EnemyPrefabView : MonoBehaviour
    {
        static int nextId;

        [Inject]
        ScoreService score = null;      // 私有字段也能注入（= null 是为了消掉 CS0649 警告）

        /// <summary>创建序号，方便在 Console 里区分是哪一个实例。</summary>
        public int Id { get; } = ++nextId;

        /// <summary>Awake 执行的那一刻，注入是否已经完成了。</summary>
        public bool WasInjectedBeforeAwake { get; private set; }

        /// <summary>当前 Score 字段是否为 null（用来观察"到底有没有被注入"）。</summary>
        public bool ScoreIsNull => score == null;

        void Awake()
        {
            // 注意：作为"伪 Prefab 模板"的那个对象永远不会被注入，
            // 所以它这里记录的也是 false。我们只报告 Instantiate 出来的实例。
            WasInjectedBeforeAwake = score != null;
        }

        [Inject]
        void Initialize()
        {
            Debug.Log($"[Lesson04] EnemyPrefabView#{Id} 的 [Inject] 方法注入被调用，Score = {score.Score}");
        }

        public void Report(string how)
        {
            var value = score != null ? score.Score.ToString() : "null（没有被注入！）";
            Debug.Log($"[Lesson04] EnemyPrefabView#{Id} [{how}] Score = {value}, " +
                      $"Awake 时已注入 = {WasInjectedBeforeAwake}");
        }
    }
}
