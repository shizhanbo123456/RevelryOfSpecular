using System;
using System.Collections.Generic;
using UnityEngine;

public static class Timer
{
    private abstract class TimerTask
    {
        public int invokeTimeLeft;//剩余的调用次数
        public float timeBeforeNextInvoke;
        public readonly float invokeInterval;
        public TimerTask(float invokeInterval, int invokeTime, bool invokeInstantly)
        {
            this.invokeInterval = invokeInterval;
            invokeTimeLeft = invokeTime;
            if (invokeInstantly) timeBeforeNextInvoke = -1;
            else timeBeforeNextInvoke = invokeInterval;
        }
        public abstract void Invoke();
    }
    private class TimerTask<T> : TimerTask
    {
        private T value;
        private Action<T> callback;
        public TimerTask(T value, Action<T> callback, float invokeInterval, int invokeTime, bool invokeInstantly) : base(invokeInterval, invokeTime, invokeInstantly)
        {
            this.value = value;
            this.callback = callback;
        }
        public override void Invoke()
        {
            try
            {
                callback?.Invoke(value);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    /// <summary>过渡任务句柄：可随时取消；任务自然结束后再调用 Cancel 无副作用。</summary>
    public sealed class TransitionHandle
    {
        private TransitionTask task;

        public void Cancel()
        {
            if (task is TransitionTask t) t.cancelled = true;
        }
    }

    private class TransitionTask<T> : TimerTask
    {
        public bool cancelled;
        private readonly T value;
        private readonly float duration;
        private readonly Action<T, float> callback;
        private float elapsed;

        public TransitionTask(T value, Action<T, float> callback, float duration) : base(0f, int.MaxValue, true)
        {
            this.value = value;
            this.callback = callback;
            this.duration = duration;
        }

        public override void Invoke()
        {
            if (cancelled)
            {
                invokeTimeLeft = 0;// 交给 Update 移除
                return;
            }
            elapsed += Time.deltaTime;
            float t01 = elapsed >= duration ? 1f : elapsed / duration;
            try
            {
                callback?.Invoke(value, t01);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (t01 >= 1f) invokeTimeLeft = 0;// 时间结束，交给 Update 移除
        }
    }

    private static readonly List<TimerTask> _tasks = new();

    public static void AddTimer<T>(T value, Action<T> act, float invokeInterval, int invokeTime, bool invokeInstantly)
    {
        if (act == null) return;
        if (invokeTime <= 0) return;
        _tasks.Add(new TimerTask<T>(value,act,invokeInterval,invokeTime,invokeInstantly));
    }

    /// <summary>过渡任务：持续 duration 秒，每帧调用 onTick(value, t01)（t01 = 归一化时间 0~1，最后一帧保证传 1），返回句柄可随时取消。</summary>
    public static TransitionHandle AddTransition<T>(T value, float duration, Action<T, float> onTick)
    {
        var handle = new TransitionHandle();
        if (onTick == null || duration <= 0) return handle;
        var task = new TransitionTask<T>(value, onTick, duration);
        handle.task = task;
        _tasks.Add(task);
        return handle;
    }

    public static void Update()
    {
        if (_tasks.Count == 0) return;
        for (int i = _tasks.Count - 1; i >= 0; i--)
        {
            var task = _tasks[i];
            if (task.timeBeforeNextInvoke <= 0)
            {
                task.Invoke();
                task.invokeTimeLeft--;
                if (task.invokeTimeLeft <= 0)
                {
                    _tasks.RemoveAt(i);
                    continue;
                }
                task.timeBeforeNextInvoke = task.invokeInterval;
                task.timeBeforeNextInvoke -= Time.deltaTime;
            }
            else
            {
                task.timeBeforeNextInvoke -= Time.deltaTime;
            }
        }
    }
}
