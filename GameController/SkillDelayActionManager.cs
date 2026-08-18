using System;
using UnityEngine;

/// <summary>
/// 技能延时行为管理器（Tool 单例）。
/// 持有 SkillBase 中两个 DelayActs 的对应参数版本，SkillBase 通过 Tool.SkillDelayActionManager 调用。
/// 内部由 GenericTimer 驱动，本组件每帧 Update 推进计时。
/// </summary>
public class SkillDelayActionManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.SkillDelayActionManager = this;
    }

    private void Update()
    {
        GenericTimer.Update();
    }

    /// <summary>
    /// 服务端延时：延迟 delay 秒后携带 (EntityData, Vector3, Vector3) 参数快照执行回调。
    /// 对应 SkillBase.DelayActs(float, (EntityData,Vector3,Vector3), Action&lt;(EntityData,Vector3,Vector3)&gt;)。
    /// </summary>
    public void DelayActs(float delay, (EntityData, Vector3, Vector3) param, Action<(EntityData, Vector3, Vector3)> action)
    {
        GenericTimer.AddTimer(param, delay, action);
    }

    /// <summary>
    /// 客户端表现侧延时：延迟 delay 秒后携带 (Vector3, Vector3) 参数快照执行回调。
    /// 对应 SkillBase.DelayActs(float, (Vector3,Vector3), Action&lt;(Vector3,Vector3)&gt;)。
    /// </summary>
    public void DelayActs(float delay, (Vector3, Vector3) param, Action<(Vector3, Vector3)> action)
    {
        GenericTimer.AddTimer(param, delay, action);
    }
}
