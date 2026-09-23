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
    private static readonly List<TimerTask> _tasks = new();

    public static void AddTimer<T>(T value, Action<T> act, float invokeInterval, int invokeTime, bool invokeInstantly)
    {
        if (act == null) return;
        if (invokeTime <= 0) return;
        _tasks.Add(new TimerTask<T>(value,act,invokeInterval,invokeTime,invokeInstantly));
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
