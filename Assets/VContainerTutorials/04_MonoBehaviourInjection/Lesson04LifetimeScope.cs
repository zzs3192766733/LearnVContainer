using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson04
{
    public sealed class Lesson04LifetimeScope : LifetimeScope
    {
        /// <summary>
        /// "伪 Prefab"模板。本课不引入真实的 Prefab 资源文件，所以留空时由 Awake() 用代码造一个。
        /// 真实项目里它就是你在 Inspector 上拖进来的 Prefab 引用。
        /// </summary>
        [SerializeField]
        EnemyPrefabView enemyPrefab;

        protected override void Awake()
        {
            // ------------------------------------------------------------------
            // 说明（只为了这一课开箱即用）：
            // RegisterComponentInHierarchy<T>() 会在"本 LifetimeScope 所在的场景"里查找 T。
            // 真实项目里，PlayerView 通常是你已经在编辑器里摆好的场景对象；
            // 这里为了不用你手动搭场景，先用代码把它造出来，再构建容器。
            //
            // 注意调用顺序：必须"先造对象、再 base.Awake()"，
            // 因为 base.Awake() 内部会调用 Configure() 并立刻构建容器。
            // ------------------------------------------------------------------
            var go = new GameObject("PlayerView (Scene Object)");
            go.transform.SetParent(transform, false);
            go.AddComponent<PlayerView>();

            // ------------------------------------------------------------------
            // 追加：造一个"伪 Prefab"当作模板（本课不引入真实的 Prefab 资源文件）。
            //
            // ⚠️ 模板必须是 active 的：
            //    容器创建组件时会先 SetActive(false)，让 Instantiate 出来的克隆体也是
            //    inactive（这样克隆体的 Awake 不会提前跑），注入完成后再 SetActive(true)。
            //    如果模板本身是 inactive，最后的 SetActive(wasActive) 会让克隆体也保持 inactive。
            // ------------------------------------------------------------------
            if (enemyPrefab == null)
            {
                var template = new GameObject("EnemyTemplate (伪 Prefab)");
                template.transform.SetParent(transform, false);
                enemyPrefab = template.AddComponent<EnemyPrefabView>();
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ScoreService>(Lifetime.Singleton);

            // 方式①-a：注册场景中已存在的组件。
            //   注册后会"强制"执行一次 Resolve，所以即使没人依赖它，注入也会发生。
            builder.RegisterComponentInHierarchy<PlayerView>();

            // 方式①-b：让容器自己新建 GameObject 并挂上组件（懒创建，被 Resolve 时才创建）。
            //   UnderTransform(transform) 让这个新物体挂在 LifetimeScope 下面，
            //   这样 LifetimeScope 被销毁时它也会一起被销毁。
            builder.RegisterComponentOnNewGameObject<AutoCreatedView>(Lifetime.Scoped, "AutoCreatedView")
                .UnderTransform(transform);

            // 入口点：它的构造函数依赖 AutoCreatedView，从而把上面那条"懒创建"拉起来
            builder.RegisterEntryPoint<MbInjectionDemo>();

            // ==================================================================
            // 追加：动态创建的 Prefab 怎么注入（详见 README 第 9 节）
            // ==================================================================

            // (1) Prefab 引用是 Unity 资源，不是容器里的服务。
            //     用 WithParameter 把它作为【构造参数】塞进 EnemySpawner ——
            //     这样 EnemyPrefabView 这个类型不会被容器占用（Resolve<EnemyPrefabView>() 依然会失败，
            //     这正是我们想要的：一个"资源模板"不该霸占类型名）。
            //
            //     警告：不要写 builder.RegisterInstance(enemyPrefab)。
            //     它的 TInterface 会被推断成 EnemyPrefabView，于是 Resolve<EnemyPrefabView>()
            //     返回的是【prefab 资源本身】而不是敌人实例，属于埋雷写法。
            builder.Register<EnemySpawner>(Lifetime.Scoped)
                   .WithParameter(enemyPrefab);

            // (2) 工厂：把"怎么造一个敌人"变成一项可注入的能力。
            //     警告：必须用带 Func<IObjectResolver, Func<T>> 的重载。
            //     若用 RegisterFactory<T>(Func<T>)，内部只是 RegisterInstance(factory)，
            //     闭包里拿不到 resolver —— 造出来的对象不会被注入。
            builder.RegisterFactory<EnemyPrefabView>(
                resolver => () => resolver.Instantiate(enemyPrefab, transform),
                Lifetime.Singleton);

            builder.RegisterEntryPoint<PrefabSpawnDemo>();
        }
    }
}
