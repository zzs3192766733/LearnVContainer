using System;
using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials
{
    /// <summary>
    /// 教程目录。数字前缀对应文件夹编号，方便对照。
    /// </summary>
    public enum TutorialLesson
    {
        Lesson01_HelloWorld = 1,
        Lesson02_Lifetime = 2,
        Lesson03_Injection = 3,
        Lesson04_MonoBehaviourInjection = 4,
        Lesson05_Registration = 5,
        Lesson06_FactoryAndContainerApi = 6,
        Lesson07_EntryPoint = 7,
        Lesson08_KeysAndCollections = 8,
        Lesson09_ChildScope = 9,
        Lesson10_DiagnosticsAndOptimization = 10,
        Lesson11_CommercialCase = 11,
    }

    /// <summary>
    /// 教程启动器：一个 GameObject 跑完所有课。
    ///
    /// 它做的事情，等价于你在真实项目里手动做的：
    ///     new GameObject("Xxx") → AddComponent&lt;XxxLifetimeScope&gt;() → 该 LifetimeScope 在 Awake 里构建容器
    ///
    /// 之所以先 SetActive(false) 再加组件，是为了让 AddComponent 时不触发 Awake，
    /// 等 SetActive(true) 时再统一触发 —— 这样行为和你在编辑器里挂组件完全一致。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialLauncher : MonoBehaviour
    {
        [Tooltip("选择要学习的课程，点 Play 即可")]
        [SerializeField]
        TutorialLesson lesson = TutorialLesson.Lesson01_HelloWorld;

        LifetimeScope currentScope;

        void Start()
        {
            Launch(lesson);
        }

        void OnDestroy()
        {
            DisposeCurrent();
        }

#if UNITY_EDITOR
        // 编辑器里直接切换下拉框即可换课：先销毁旧作用域（会触发 Dispose），再建新的。
        void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                DisposeCurrent();
                Launch(lesson);
            }
        }
#endif

        void Launch(TutorialLesson target)
        {
            // 幂等：编辑器里可能先触发一次 OnValidate 再触发 Start，
            // 这里先清掉已有作用域，保证任何时刻只有一个课程在跑。
            DisposeCurrent();

            var go = new GameObject("[" + target + "]");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            // 挂上对应课程的 LifetimeScope。
            // 该组件的 Awake() 会自动 Build() 出一个容器。
            currentScope = (LifetimeScope)go.AddComponent(ScopeTypeOf(target));

            go.SetActive(true);
        }

        void DisposeCurrent()
        {
            if (currentScope == null)
            {
                return;
            }

            // LifetimeScope.OnDestroy → DisposeCore → Container.Dispose()
            // 也就是说：这一课里用 Lifetime.Singleton / Lifetime.Scoped 建出来的、
            // 并且实现了 IDisposable 的对象，此刻会被统一释放。
            Destroy(currentScope.gameObject);
            currentScope = null;
        }

        static Type ScopeTypeOf(TutorialLesson target)
        {
            switch (target)
            {
                case TutorialLesson.Lesson01_HelloWorld:
                    return typeof(Lesson01.Lesson01LifetimeScope);
                case TutorialLesson.Lesson02_Lifetime:
                    return typeof(Lesson02.Lesson02LifetimeScope);
                case TutorialLesson.Lesson03_Injection:
                    return typeof(Lesson03.Lesson03LifetimeScope);
                case TutorialLesson.Lesson04_MonoBehaviourInjection:
                    return typeof(Lesson04.Lesson04LifetimeScope);
                case TutorialLesson.Lesson05_Registration:
                    return typeof(Lesson05.Lesson05LifetimeScope);
                case TutorialLesson.Lesson06_FactoryAndContainerApi:
                    return typeof(Lesson06.Lesson06LifetimeScope);
                case TutorialLesson.Lesson07_EntryPoint:
                    return typeof(Lesson07.Lesson07LifetimeScope);
                case TutorialLesson.Lesson08_KeysAndCollections:
                    return typeof(Lesson08.Lesson08LifetimeScope);
                case TutorialLesson.Lesson09_ChildScope:
                    return typeof(Lesson09.Lesson09LifetimeScope);
                case TutorialLesson.Lesson10_DiagnosticsAndOptimization:
                    return typeof(Lesson10.Lesson10LifetimeScope);
                case TutorialLesson.Lesson11_CommercialCase:
                    return typeof(Lesson11.Composition.Lesson11LifetimeScope);
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "未知课程");
            }
        }
    }
}
