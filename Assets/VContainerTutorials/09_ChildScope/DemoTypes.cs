using System;
using UnityEngine;

namespace VContainerTutorials.Lesson09
{
    /// <summary>
    /// 属于"某一层作用域"的状态对象（Scoped）。
    /// 每个作用域各有一份，作用域被释放时跟着被 Dispose。
    /// </summary>
    public sealed class LevelSession : IDisposable
    {
        static int nextId;

        public readonly int Id;

        public LevelSession()
        {
            Id = ++nextId;
            Debug.Log($"[Lesson09] 创建 LevelSession #{Id}");
        }

        public void Dispose()
        {
            Debug.Log($"[Lesson09] 释放 LevelSession #{Id}");
        }
    }

    /// <summary>一个纯数据对象，用来演示"某个注册只存在于子作用域里"。</summary>
    public sealed class StageInfo
    {
        public readonly string Name;

        public StageInfo(string name)
        {
            Name = name;
        }

        public override string ToString() => $"StageInfo({Name})";
    }
}
