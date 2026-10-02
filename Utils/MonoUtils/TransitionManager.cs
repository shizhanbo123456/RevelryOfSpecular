using System;
using UnityEngine;
using System.Collections.Generic;
// 通过 Tool.TransitionManager 访问；Execute(action, time, onFinish) 返回 handle，Cancel(handle) 可提前停止（action 返回 false 也停）
public class TransitionManager : MonoBehaviour
{
    // 单例
    private void Awake()
    {
        Tool.TransitionManager = this;
    }

    // 改用字典：key = 任务ID，value = 任务
    private Dictionary<int, TransitionTask> _taskDict = new Dictionary<int, TransitionTask>();
    private int _nextTaskId = 1; // 唯一自增ID

    public int Execute(Func<float, bool> action, float time, Action onFinish = null)
    {
        if (action == null || time <= 0)
        {
            Debug.LogError("过渡参数无效！");
            return -1;
        }

        int taskId = _nextTaskId++;
        TransitionTask task = new TransitionTask(taskId, action, time, onFinish);
        _taskDict.Add(taskId, task);

        return taskId;
    }

    public void Cancel(int taskId)
    {
        if (_taskDict.ContainsKey(taskId))
        {
            _taskDict.Remove(taskId);
        }
    }

    public void CancelAll()
    {
        _taskDict.Clear();
    }

    private void Update()
    {
        if (_taskDict.Count == 0) return;

        // 遍历字典副本，防止遍历时修改报错
        List<int> taskIds = new List<int>(_taskDict.Keys);
        List<int> finishedTasks = new List<int>();

        foreach (int id in taskIds)
        {
            var task = _taskDict[id];
            task.UpdateTask(Time.deltaTime);

            if (task.IsCompleted)
            {
                finishedTasks.Add(id);
            }
        }

        // 移除已完成的任务
        foreach (int id in finishedTasks)
        {
            _taskDict.Remove(id);
        }
    }

    private class TransitionTask
    {
        public int TaskId { get; }
        private Func<float, bool> _updateAction; // 已改为 Func
        private Action _onFinish;
        private float _totalTime;
        private float _elapsedTime;
        public bool IsCompleted { get; private set; }

        public TransitionTask(int taskId, Func<float, bool> action, float totalTime, Action onFinish)
        {
            TaskId = taskId;
            _updateAction = action;
            _totalTime = totalTime;
            _onFinish = onFinish;
            _elapsedTime = 0;
            IsCompleted = false;
        }

        public void UpdateTask(float deltaTime)
        {
            if (IsCompleted) return;

            _elapsedTime += deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _totalTime);

            // 执行过渡逻辑，接收返回值
            bool isActive = false;
            if (_updateAction != null) isActive = _updateAction.Invoke(progress);

            // 如果返回 false → 立即终止，不执行完成回调
            if (!isActive)
            {
                IsCompleted = true;
                return;
            }

            // 正常结束
            if (_elapsedTime >= _totalTime)
            {
                IsCompleted = true;
                if (_onFinish != null) _onFinish.Invoke();
            }
        }
    }
}