using System;
using UnityEngine;
using System.Collections.Generic;
/*
Tool.TransitionManager来访问
执行过渡：（action的float为(0,1]，返回false可提前停止过渡）
int Execute(Func<float, bool> action, float time, Action onFinish = null)
取消过渡：（参数值为Execute返回的handle）
void Cancel(int taskId)
*/
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

    /// <summary>
    /// 执行过渡动画，返回任务ID
    /// Func<float, bool> ：参数是进度(0~1)，返回 false 表示立即终止过渡
    /// </summary>
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

    /// <summary>
    /// 根据ID取消任务（字典查找，O(1) 效率）
    /// </summary>
    public void Cancel(int taskId)
    {
        if (_taskDict.ContainsKey(taskId))
        {
            _taskDict.Remove(taskId);
        }
    }

    /// <summary>
    /// 取消所有任务
    /// </summary>
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

    /// <summary>
    /// 任务类
    /// </summary>
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
            bool isActive = _updateAction?.Invoke(progress) ?? false;

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
                _onFinish?.Invoke();
            }
        }
    }
}