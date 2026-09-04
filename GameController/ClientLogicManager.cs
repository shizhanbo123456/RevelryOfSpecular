using Ros.Transport;
using UnityEngine;

/// <summary>
/// 客户端逻辑管理器（客户端总控）。
/// 统一处理服务器传回的各类内容并加以呈现（子管理器 Sub 后续按需拆分：实体表现/技能特效/技能使用等）。
/// </summary>
public class ClientLogicManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.ClientLogicManager = this;
    }

    private void OnEnable()
    {
        EventManager.AddEvent<SCSkillRuntimeInfo>(ClientEvent.OnSkillRuntimeUpdate, OnSkillRuntimeUpdate);
    }

    private void OnDisable()
    {
        EventManager.RemoveEvent<SCSkillRuntimeInfo>(ClientEvent.OnSkillRuntimeUpdate, OnSkillRuntimeUpdate);
    }

    /// <summary>本地玩家技能运行时（服务器下发缓存）。</summary>
    public SCSkillRuntimeInfo SkillRuntime { get; private set; }

    /// <summary>本地玩家实体 id。</summary>
    public ushort LocalPlayerEntityId => NetworkManager.battleInfo != null ? NetworkManager.battleInfo.playerEntityId : (ushort)0;

    /// <summary>本地玩家阵营（0进攻 1防守 2中立）。</summary>
    public int LocalCamp => NetworkManager.battleInfo != null ? (int)NetworkManager.battleInfo.camp : 2;

    /// <summary>当前滚轮选中技能 id（-1 无）。</summary>
    public int SelectedSkillId
    {
        get
        {
            if (SkillRuntime == null || SkillRuntime.selectedIndex < 0) return -1;
            var slot = SkillRuntime.Get(SkillRuntime.selectedIndex);
            return slot != null ? slot.skillId : -1;
        }
    }

    private void OnSkillRuntimeUpdate(SCSkillRuntimeInfo info)
    {
        SkillRuntime = info;
    }

    /// <summary>本地玩家世界坐标（供 UI/瞄准使用）。</summary>
    public bool TryGetLocalPlayerPosition(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (Tool.ClientDisplayManager == null) return false;
        return Tool.ClientDisplayManager.TryGetEntityPosition(LocalPlayerEntityId, out pos);
    }

    // TODO: 子管理器框架（EntityPlayerManager / LabelPlayerManager / SettlementManager /
    //        ClientSkillManager / ClientBattleTimeManager 等）后续按需拆分完善
}
