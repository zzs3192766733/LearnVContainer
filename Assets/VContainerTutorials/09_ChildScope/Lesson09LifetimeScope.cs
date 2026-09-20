using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson09
{
    public sealed class Lesson09LifetimeScope : LifetimeScope
    {
        LevelLoader loader;

        protected override void Configure(IContainerBuilder builder)
        {
            // 父作用域只放"纯服务"，不放入口点。
            // 这样本章的注意力全在"作用域"上，也避免父子两层的入口点互相干扰。
            builder.Register<LevelSession>(Lifetime.Scoped);
        }

        protected override void Awake()
        {
            // base.Awake() 内部会 Configure + Build，
            // 而 Container 属性是在 Build 的 RegisterBuildCallback 里被赋值的，
            // 所以调用完 base.Awake() 之后 Container 已经可用了。
            base.Awake();

            loader = new LevelLoader(this);
            loader.LoadLevel();
        }

        protected override void OnDestroy()
        {
            // 先释放子作用域，再让 base 释放父作用域，顺序更清晰。
            loader?.Unload();
            base.OnDestroy();
        }
    }
}
