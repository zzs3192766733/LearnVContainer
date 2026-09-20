using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace VContainerTutorials.Lesson09
{
    /// <summary>
    /// 演示三种创建作用域的方式。
    ///
    /// 它刻意是一个普通 C# 类（不注册成 EntryPoint），
    /// 由 Lesson09LifetimeScope 在容器构建完成后直接 new 出来驱动，
    /// 这样能把注意力完全放在"作用域"上（原因见本章 README 第 7 节）。
    /// </summary>
    public sealed class LevelLoader
    {
        readonly LifetimeScope rootScope;
        LifetimeScope levelBScope;

        public LevelLoader(LifetimeScope rootScope)
        {
            this.rootScope = rootScope;
        }

        public void LoadLevel()
        {
            DemoContainerScope();
            DemoUnityChildScope();
            DemoCodeFirstRootScope();
        }

        public void Unload()
        {
            if (levelBScope == null)
            {
                return;
            }

            Debug.Log("[Lesson09] ---- 卸载：Dispose 子作用域 ----");
            // Dispose() = 释放容器 + 销毁它创建的 GameObject（源码 LifetimeScope.Dispose）
            levelBScope.Dispose();
            levelBScope = null;
        }

        // ------------------------------------------------------------------
        // A. 纯容器层的子作用域
        // ------------------------------------------------------------------
        void DemoContainerScope()
        {
            Debug.Log("[Lesson09] ===== A. 纯容器层的子作用域：IObjectResolver.CreateScope =====");

            var parentSession = rootScope.Container.Resolve<LevelSession>();
            Debug.Log($"[Lesson09] 父作用域解析 LevelSession -> #{parentSession.Id}");

            using (var child = rootScope.Container.CreateScope(builder =>
                   {
                       // 这个注册只存在于这个子作用域
                       builder.RegisterInstance(new StageInfo("关卡 A（子作用域私有的注册）"));
                   }))
            {
                var s1 = child.Resolve<LevelSession>();
                var s2 = child.Resolve<LevelSession>();
                Debug.Log($"[Lesson09] 子作用域解析 LevelSession -> #{s1.Id} / #{s2.Id} " +
                          "（同一子作用域内同一实例，但与父作用域不同 —— 这就是 Lifetime.Scoped）");

                Debug.Log($"[Lesson09] 子作用域私有的注册 StageInfo -> {child.Resolve<StageInfo>()}");
                Debug.Log($"[Lesson09] 父作用域能解析到 StageInfo 吗？ -> {rootScope.Container.TryResolve<StageInfo>(out _)}");
            }

            Debug.Log("[Lesson09] 离开 using：子作用域被 Dispose");
        }

        // ------------------------------------------------------------------
        // B. Unity 层的子作用域（会新建 GameObject）
        // ------------------------------------------------------------------
        void DemoUnityChildScope()
        {
            Debug.Log("[Lesson09] ===== B. Unity 层的子作用域：LifetimeScope.CreateChild =====");
            Debug.Log("[Lesson09] CreateChild 会新建一个 GameObject 挂上 LifetimeScope，并挂到当前作用域的 transform 下");

            levelBScope = rootScope.CreateChild(builder =>
            {
                builder.RegisterInstance(new StageInfo("关卡 B"));

                // 子作用域里注册的 EntryPoint 会在作用域创建后立刻被驱动
                builder.RegisterEntryPoint<StageWatcher>();
            }, "LevelB_Scope");

            Debug.Log($"[Lesson09] 子作用域已创建，名字 = '{levelBScope.name}'，" +
                      $"Parent = '{(levelBScope.Parent == null ? "(null)" : levelBScope.Parent.name)}'");
            Debug.Log($"[Lesson09] 子作用域里的 LevelSession -> #{levelBScope.Container.Resolve<LevelSession>().Id}");
            Debug.Log("[Lesson09] 这个子作用域会一直存活到停止 Play / 切换课程，用来观察 StageWatcher.Tick()");
        }

        // ------------------------------------------------------------------
        // C. 纯代码创建一个根作用域
        // ------------------------------------------------------------------
        void DemoCodeFirstRootScope()
        {
            Debug.Log("[Lesson09] ===== C. 纯代码创建一个根作用域：LifetimeScope.Create =====");

            using (var codeScope = LifetimeScope.Create(builder =>
                   {
                       builder.RegisterInstance(new StageInfo("代码创建的根作用域"));
                       builder.Register<LevelSession>(Lifetime.Scoped);
                   }, "CodeFirst_Scope"))
            {
                Debug.Log($"[Lesson09] 纯代码作用域解析 StageInfo -> {codeScope.Container.Resolve<StageInfo>()}");
                Debug.Log($"[Lesson09] 它的 Parent 是？ -> {(codeScope.Parent == null ? "(null，说明它是根作用域)" : codeScope.Parent.name)}");
                Debug.Log($"[Lesson09] 同一份注册在这个作用域里的 LevelSession -> #{codeScope.Container.Resolve<LevelSession>().Id}");
            }

            Debug.Log("[Lesson09] 离开 using：代码创建的作用域被 Dispose，它创建的 GameObject 也被销毁");
        }
    }
}
