using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson04
{
    public sealed class MbInjectionDemo : IStartable
    {
        readonly AutoCreatedView autoCreatedView;
        readonly IObjectResolver container;
        readonly LifetimeScope scope;

        /// <summary>
        /// 关键点：把 AutoCreatedView 写进构造函数参数，
        /// 容器为了构造本类，就必须先把 AutoCreatedView 造出来 ——
        /// 于是 RegisterComponentOnNewGameObject 的"懒创建"就被触发了。
        ///
        /// 另外：LifetimeScope 自身也会被自动注册（源码 InstallTo 里的
        /// builder.RegisterInstance&lt;LifetimeScope&gt;(this).AsSelf()），所以这里能直接注入。
        /// </summary>
        public MbInjectionDemo(
            AutoCreatedView autoCreatedView,
            IObjectResolver container,
            LifetimeScope scope)
        {
            this.autoCreatedView = autoCreatedView;
            this.container = container;
            this.scope = scope;
        }

        void IStartable.Start()
        {
            Debug.Log($"[Lesson04] AutoCreatedView 已被容器创建，GameObject 名字 = '{autoCreatedView.name}'，" +
                      $"父物体 = '{autoCreatedView.transform.parent?.name}'");

            Debug.Log("--------------------------------------------------");
            Debug.Log("[Lesson04] 方式③：手动给一个容器完全不知道的 GameObject 注入依赖");
            Debug.Log("--------------------------------------------------");

            // 模拟"运行时动态生成对象"：这种方式 Unity 不会帮你触发注入，
            // 必须自己调用 InjectGameObject（它会递归注入所有子物体）。
            var go = new GameObject("LateInjected (InjectGameObject)");
            go.transform.SetParent(scope.transform, false);
            var view = go.AddComponent<PlayerView>();
            view.Label = "运行时生成";

            Debug.Log($"[Lesson04] 注入之前，view.Score 是不是 null ? -> {view.Score == null}");

            container.InjectGameObject(go);

            Debug.Log($"[Lesson04] 注入之后，view.Score 是不是 null ? -> {view.Score == null}");
            view.Report();
        }
    }
}
