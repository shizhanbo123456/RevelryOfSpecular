using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型计时器：注册延时回调，到期执行一次。
/// 用法：
///   GenericTimer.AddTimer(value, 1f, v => { ... });   // 注册
///   GenericTimer.Update();                            // 外部每帧驱动（Tool.Update 已代为驱动）
/// T 值通过闭包捕获，调用时即可访问，与注册时的快照一致。
/// </summary>
public static class GenericTimer
{
    private class TimerTask
    {
        public float Elapsed;   // 已累计时间
        public float Delay;     // 延时（秒）
        public Action Action;   // 包装后的无参回调（闭包捕获 T 值）
    }

    private static readonly List<TimerTask> _tasks = new();

    /// <summary>
    /// 注册一个泛型延时回调：delay 秒后携带 value 执行 act。
    /// delay &lt;= 0 时立即同步执行。
    /// </summary>
    public static void AddTimer<T>(T value, float delay, Action<T> act)
    {
        if (act == null) return;
        if (delay <= 0f)
        {
            act(value);
            return;
        }
        _tasks.Add(new TimerTask
        {
            Elapsed = 0f,
            Delay = delay,
            Action = () => act(value),
        });
    }

    /// <summary>
    /// 每帧更新：推进所有任务计时，到期的执行回调并移除。
    /// 由 Tool.Update 每帧驱动。先移除再执行：回调抛异常不会导致任务每帧重试。
    /// </summary>
    public static void Update()
    {
        if (_tasks.Count == 0) return;
        for (int i = _tasks.Count - 1; i >= 0; i--)
        {
            var task = _tasks[i];
            task.Elapsed += Time.deltaTime;
            if (task.Elapsed < task.Delay) continue;
            _tasks.RemoveAt(i);
            try
            {
                task.Action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
