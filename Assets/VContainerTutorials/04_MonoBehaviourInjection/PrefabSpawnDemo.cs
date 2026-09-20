using System;
using UnityEngine;
using VContainer.Unity;

namespace VContainerTutorials.Lesson04
{
    /// <summary>
    /// 入口点：把"动态创建 Prefab 并注入"的四种写法跑一遍并对照。
    ///
    /// ④ 是故意留的反例，用来和第 ①②③ 做对比 —— 差异会体现在
    /// "Score 是不是 null" 和 "Awake 时已注入" 这两个字段上。
    /// </summary>
    public sealed class PrefabSpawnDemo : IStartable
    {
        readonly EnemySpawner spawner;

        /// <summary>
        /// 注意这里：本类完全没有出现 IObjectResolver，
        /// 它只声明"我需要一个能造出 EnemyPrefabView 的能力"。
        /// 这个委托由 builder.RegisterFactory&lt;EnemyPrefabView&gt;(...) 提供。
        /// </summary>
        readonly Func<EnemyPrefabView> createEnemy;

        readonly LifetimeScope scope;

        public PrefabSpawnDemo(
            EnemySpawner spawner,
            Func<EnemyPrefabView> createEnemy,
            LifetimeScope scope)
        {
            this.spawner = spawner;
            this.createEnemy = createEnemy;
            this.scope = scope;
        }

        void IStartable.Start()
        {
            var parent = scope.transform;

            Debug.Log("==================================================");
            Debug.Log("[Lesson04] 追加：动态创建的 Prefab 怎么注入");
            Debug.Log("==================================================");

            // ① 推荐：resolver.Instantiate —— 实例化 + 自动递归注入
            spawner.Spawn(parent).Report("① resolver.Instantiate(prefab, parent)");

            // ② 对象已经被别处创建好了，事后补一刀 InjectGameObject
            var raw = spawner.CreateRaw(parent);
            Debug.Log($"[Lesson04] EnemyPrefabView#{raw.Id} 补注入【之前】: Score 是 null ? -> {raw.ScoreIsNull}");
            spawner.Patch(raw).Report("② Object.Instantiate + InjectGameObject");

            // ③ 工厂：只依赖 Func<EnemyPrefabView>，业务类完全不碰 IObjectResolver
            createEnemy().Report("③ 注入 Func<EnemyPrefabView> 工厂");

            // ④ 反例：Unity 原生 Instantiate，没有触发注入
            spawner.CreateRaw(parent).Report("④ 反例 Unity 原生 Instantiate（没注入）");

            Debug.Log("--------------------------------------------------");
            Debug.Log("[Lesson04] 对照结论：");
            Debug.Log("[Lesson04]   ①③ 的 Awake 在【注入之后】才跑（所以 Awake 里读 [Inject] 字段是安全的）");
            Debug.Log("[Lesson04]   ② 的 Awake 早于注入（对象是 Unity 原生 Instantiate 出来的），靠事后 InjectGameObject 补回来");
            Debug.Log("[Lesson04]   ④ 从头到尾没人注入，字段一直是 null");
            Debug.Log("--------------------------------------------------");
        }
    }
}
