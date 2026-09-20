using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson04
{
    public sealed class Lesson04LifetimeScope : LifetimeScope
    {
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
        }
    }
}
