using UnityEngine;
using VContainer;

namespace VContainerTutorials.Lesson03
{
    // ---------------------------------------------------------------------
    // ① 构造函数注入：最常见、也是官方唯一推荐的方式
    // ---------------------------------------------------------------------
    public sealed class ConstructorInjectionTarget
    {
        readonly Logger logger;

        // 不需要任何特性。VContainer 会自动选这个构造函数。
        public ConstructorInjectionTarget(Logger logger)
        {
            this.logger = logger;
        }

        public void Report()
        {
            logger.Log("① 构造函数注入成功");
        }
    }

    // ---------------------------------------------------------------------
    // ② 方法注入：给 MonoBehaviour 或"框架帮你 new 的对象"用
    // ---------------------------------------------------------------------
    public sealed class MethodInjectionTarget
    {
        Logger logger;

        // 方法名任意、访问级别任意（这里故意用 private 来证明这一点）。
        [Inject]
        void Construct(Logger logger)
        {
            this.logger = logger;
        }

        public void Report()
        {
            logger.Log("② 方法注入成功（方法是 private 也能注入）");
        }
    }

    // ---------------------------------------------------------------------
    // ③ 字段注入 + 属性注入
    // ---------------------------------------------------------------------
    public sealed class FieldPropertyInjectionTarget
    {
        // 私有字段。初始化成 null 只是为了消除编译器的 CS0649 警告，
        // 实际值会在注入时被覆盖。
        [Inject]
        Logger logger = null;

        [Inject]
        public AppConfig Config { get; set; }

        public void Report()
        {
            logger.Log($"③ 字段 + 属性注入成功，Config.Name = {Config.Name}");
        }
    }

    // ---------------------------------------------------------------------
    // ④ 多构造函数：必须恰好有一个标了 [Inject]
    // ---------------------------------------------------------------------
    public sealed class MultiConstructorTarget
    {
        public readonly string UsedConstructor;

        public MultiConstructorTarget()
        {
            UsedConstructor = "无参构造函数（参数 0 个）";
        }

        [Inject]
        public MultiConstructorTarget(Logger logger)
        {
            UsedConstructor = "被 [Inject] 标记的构造函数（参数 1 个）";
        }

        public void Report()
        {
            Debug.Log($"[Lesson03] ④ MultiConstructorTarget 实际使用的是：{UsedConstructor}");
        }
    }

    // ---------------------------------------------------------------------
    // ⑤ 继承：基类的 [Inject] 也会生效
    // ---------------------------------------------------------------------
    public abstract class BaseTarget
    {
        [Inject]
        protected Logger BaseLogger = null;

        public void ReportBase()
        {
            BaseLogger.Log("⑤ 基类的 [Inject] 字段注入成功（当前类型：" + GetType().Name + "）");
        }
    }

    public sealed class DerivedTarget : BaseTarget
    {
        AppConfig config;

        [Inject]
        void Construct(AppConfig config)
        {
            this.config = config;
        }

        public void ReportDerived()
        {
            Debug.Log($"[Lesson03] ⑤ DerivedTarget 同时拿到基类字段与自身方法注入的依赖，Config = {config.Name}");
        }
    }
}
