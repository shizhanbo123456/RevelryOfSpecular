using System;
using System.Collections.Generic;

public static class EventManager
{
    // 存储无参事件委托
    private static readonly Dictionary<int, Action> _eventDict = new Dictionary<int, Action>();
    // 存储带单个泛型参数事件委托
    private static readonly Dictionary<int, Delegate> _genericEventDict = new Dictionary<int, Delegate>();

    #region 订阅事件 AddEvent
    public static void AddEvent(int eventId, Action _event)
    {
#if !RELEASE
        if (_event == null)
        {
            throw new ArgumentNullException(nameof(_event), $"事件ID:{eventId} 传入订阅委托为空");
        }
#endif

        if (_eventDict.ContainsKey(eventId))
        {
            _eventDict[eventId] += _event;
        }
        else
        {
            _eventDict[eventId] = _event;
        }
    }

    public static void AddEvent<T>(int eventId, Action<T> _event)
    {
#if !RELEASE
        if (_event == null)
        {
            throw new ArgumentNullException(nameof(_event), $"事件ID:{eventId} 传入泛型订阅委托为空");
        }
#endif

        if (_genericEventDict.TryGetValue(eventId, out Delegate del))
        {
#if !RELEASE
            if (!(del is Action<T>))
            {
                throw new InvalidOperationException($"事件ID:{eventId}已绑定其他泛型类型委托，类型冲突，现有类型：{del.GetType().Name}，本次绑定：Action<{typeof(T).Name}>");
            }
#endif
            _genericEventDict[eventId] = (Action<T>)del + _event;
        }
        else
        {
            _genericEventDict[eventId] = _event;
        }
    }
    #endregion

    #region 取消订阅 RemoveEvent 重载
    public static void RemoveEvent(int eventId)
    {
        if (_eventDict.ContainsKey(eventId))
            _eventDict.Remove(eventId);

        if (_genericEventDict.ContainsKey(eventId))
            _genericEventDict.Remove(eventId);
    }

    public static void RemoveEvent(int eventId, Action _event)
    {
#if !RELEASE
        if (_event == null)
        {
            throw new ArgumentNullException(nameof(_event), $"解绑事件ID:{eventId} 委托不能为空");
        }
        if (!_eventDict.ContainsKey(eventId))
        {
            throw new KeyNotFoundException($"解绑失败，不存在ID为{eventId}的无参事件");
        }
#endif

        _eventDict[eventId] -= _event;
        // 委托为空时清理字典，释放内存
        if (_eventDict[eventId] == null)
            _eventDict.Remove(eventId);
    }

    public static void RemoveEvent<T>(int eventId, Action<T> _event)
    {
#if !RELEASE
        if (_event == null)
        {
            throw new ArgumentNullException(nameof(_event), $"解绑泛型事件ID:{eventId} 委托不能为空");
        }
        if (!_genericEventDict.TryGetValue(eventId, out Delegate del))
        {
            throw new KeyNotFoundException($"解绑失败，不存在ID为{eventId}的泛型事件");
        }
        if (!(del is Action<T>))
        {
            throw new InvalidCastException($"事件ID:{eventId} 绑定委托类型与解绑委托类型不匹配");
        }
#endif

        Action<T> action = (Action<T>)_genericEventDict[eventId];
        var newDel = action - _event;
        if (newDel == null)
            _genericEventDict.Remove(eventId);
        else
            _genericEventDict[eventId] = newDel;
    }
    #endregion

    #region 触发事件 TrigEvent
    public static void TrigEvent(int eventId)
    {
        if (_eventDict.TryGetValue(eventId, out Action action))
        {
            //UnityEngine.Debug.Log($"触发无参事件 ID = {eventId}");
            if (action != null) action.Invoke();
        }
#if !RELEASE
        else
        {
            UnityEngine.Debug.Log($"警告：触发无监听事件 ID = {eventId}");
        }
#endif
    }

    public static void TrigEvent<T>(int eventId, T param)
    {
        if (_genericEventDict.TryGetValue(eventId, out Delegate del))
        {
#if !RELEASE
            if (!(del is Action<T> action))
            {
                throw new InvalidCastException($"事件ID:{eventId}绑定委托类型不是Action<{typeof(T).Name}>，触发类型不匹配，存储类型：{del.GetType().Name}");
            }
            //UnityEngine.Debug.Log($"触发泛型事件 ID = {eventId}，参数类型：{typeof(T).Name}，参数值：{param}");
            ((Action<T>)del).Invoke(param);
#else
            // Release 直接强制转换，牺牲校验换性能
            ((Action<T>)del).Invoke(param);
#endif
        }
#if !RELEASE
        else
        {
            // Console.WriteLine($"警告：触发无监听泛型事件 ID = {eventId}");
        }
#endif
    }
    #endregion

    #region 辅助工具方法
    public static void ClearAllEvents()
    {
        _eventDict.Clear();
        _genericEventDict.Clear();
    }

    public static bool HasEventSubscriber(int eventId)
    {
        bool hasNormal = _eventDict.TryGetValue(eventId, out Action act) && act != null;
        bool hasGeneric = _genericEventDict.TryGetValue(eventId, out Delegate del) && del != null;
        return hasNormal || hasGeneric;
    }
    #endregion
}