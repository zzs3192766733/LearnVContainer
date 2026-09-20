using UnityEngine;

namespace VContainerTutorials.Lesson03
{
    /// <summary>一个普通的可注入依赖。</summary>
    public sealed class Logger
    {
        public void Log(string message)
        {
            Debug.Log($"[Lesson03] {message} (Logger 实例 Id = {GetHashCode()})");
        }
    }

    /// <summary>另一个普通的可注入依赖。</summary>
    public sealed class AppConfig
    {
        public string Name { get; set; } = "默认配置";
    }
}
