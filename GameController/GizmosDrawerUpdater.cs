using System;
using UnityEngine;

/// <summary>
/// GizmosDrawer 的绘制载体：接收 Unity 的 Update 与 OnDrawGizmos 回调。
/// 必须为独立文件 + 顶层 public 类——Unity 的 AddComponent 只识别"文件名==类名"的顶层公共类，
/// 嵌套类/非公共类会 AddComponent 失败，导致 GizmosDrawer 的绘制任务永远不渲染。
/// </summary>
public class GizmosDrawerUpdater : MonoBehaviour
{
    public event Action OnUpdateEvent;
    public event Action OnDrawGizmosEvent;

    private void Update()
    {
        OnUpdateEvent?.Invoke();
    }

    private void OnDrawGizmos()
    {
        OnDrawGizmosEvent?.Invoke();
    }

    private void OnDestroy()
    {
        // 清理 GizmosDrawer 对更新器对象的引用
        GizmosDrawer.ClearUpdater();
    }
}
