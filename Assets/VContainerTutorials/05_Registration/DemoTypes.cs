using UnityEngine;

namespace VContainerTutorials.Lesson05
{
    public interface IServiceA
    {
        void Run();
    }

    public interface IInputPort
    {
        string Read();
    }

    public interface IOutputPort
    {
        void Write(string text);
    }

    /// <summary>
    /// 一个类实现了 3 个接口 —— 用来演示"一次注册、多种身份"。
    /// </summary>
    public sealed class ServiceA : IServiceA, IInputPort, IOutputPort
    {
        public void Run() => Debug.Log("[Lesson05] ServiceA.Run() 被调用");

        public string Read() => "来自 ServiceA 的输入";

        public void Write(string text) => Debug.Log($"[Lesson05] ServiceA 输出: {text}");
    }

    /// <summary>
    /// 需要一个"值"（url、timeout）而不是"依赖"的类 —— 用 WithParameter 注册。
    /// </summary>
    public sealed class HttpConfig
    {
        public readonly string Url;
        public readonly int Timeout;

        public HttpConfig(string url, int timeout)
        {
            Url = url;
            Timeout = timeout;
        }

        public override string ToString() => $"{Url} (timeout={Timeout})";
    }

    /// <summary>开放泛型示例。</summary>
    public sealed class GenericRepository<T>
    {
        public string Describe() => $"GenericRepository<{typeof(T).Name}> (Id={GetHashCode()})";
    }

    /// <summary>用 RegisterInstance 注册的"已存在的对象"。</summary>
    public sealed class AppInfo
    {
        public readonly string Version;

        public AppInfo(string version)
        {
            Version = version;
        }

        public override string ToString() => $"AppInfo(v{Version})";
    }
}
