using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson04
{
    /// <summary>
    /// 业务服务：负责"造一个敌人"。
    ///
    /// 三个方法对应三种真实需求：
    ///   Spawn      —— 由我们负责创建（走 resolver.Instantiate，注入自动完成）
    ///   CreateRaw  —— 对象由别处创建（走 Unity 原生 Instantiate，不会注入）
    ///   Patch      —— 对已经存在的对象补注入（走 resolver.InjectGameObject，递归处理所有子物体）
    ///
    /// ⚠️ 构造函数第二个参数 <c>prefab</c> 是由
    /// <c>builder.Register&lt;EnemySpawner&gt;(...).WithParameter(enemyPrefab)</c> 提供的，
    /// 它【不会】去容器里解析。所以容器里没有 EnemyPrefabView 这条注册，
    /// Resolve&lt;EnemyPrefabView&gt;() 依然会抛异常 —— 这正是我们想要的：
    /// "一个资源模板"不占用 EnemyPrefabView 这个类型名。
    /// </summary>
    public sealed class EnemySpawner
    {
        readonly IObjectResolver resolver;
        readonly EnemyPrefabView prefab;

        public EnemySpawner(IObjectResolver resolver, EnemyPrefabView prefab)
        {
            this.resolver = resolver;
            this.prefab = prefab;
        }

        /// <summary>
        /// ① 推荐写法：实例化 + 自动递归注入。
        /// 扩展方法在 VContainer.Unity 命名空间的 ObjectResolverUnityExtensions 里。
        /// </summary>
        public EnemyPrefabView Spawn(Transform parent)
            => resolver.Instantiate(prefab, parent);

        /// <summary>
        /// ②-a 反例：走 Unity 原生 Instantiate，没有人会帮我们触发注入。
        /// （骨架代码里也用它来模拟"对象已经被第三方/Unity 代码创建好了"。）
        /// </summary>
        public EnemyPrefabView CreateRaw(Transform parent)
        {
            var go = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
            return go.GetComponent<EnemyPrefabView>();
        }

        /// <summary>
        /// ②-b 补一刀：InjectGameObject 会递归注入该物体及其所有后代上的所有 MonoBehaviour。
        /// 注意它只负责注入，容器并不会因此"认识"这个对象（之后 Resolve 拿不到它）。
        /// </summary>
        public EnemyPrefabView Patch(EnemyPrefabView alreadyCreated)
        {
            resolver.InjectGameObject(alreadyCreated.gameObject);
            return alreadyCreated;
        }
    }
}
