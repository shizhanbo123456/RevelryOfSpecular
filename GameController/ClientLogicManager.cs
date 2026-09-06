using Ros.Transport;
using UnityEngine;

/// <summary>
/// 客户端逻辑管理器（客户端总控）。
/// 本地玩家的技能槽/选中项/状态随实体表现摘要（SCEntityDisplayInfo）缓存。
/// </summary>
public class ClientLogicManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.ClientLogicManager = this;
    }

    private void OnEnable()
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
    }

    private void OnDisable()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
    }

    /// <summary>本地玩家最近一次实体表现摘要（含技能槽/选中项/Buff）。</summary>
    public SCEntityDisplayInfo LocalDisplay { get; private set; }

    /// <summary>本地玩家实体 id。</summary>
    public ushort LocalPlayerEntityId => NetworkManager.battleInfo != null ? NetworkManager.battleInfo.playerEntityId : (ushort)0;

    /// <summary>本地玩家阵营（0进攻 1防守 2中立）。</summary>
    public int LocalCamp => NetworkManager.battleInfo != null ? (int)NetworkManager.battleInfo.camp : 2;

    /// <summary>当前滚轮选中技能 id（-1 无）。</summary>
    public int SelectedSkillId
    {
        get
        {
            var d = LocalDisplay;
            if (d == null || d.selectedIndex < 0 || d.selectedIndex >= d.skills.Count) return -1;
            var slot = d.skills[d.selectedIndex];
            return slot != null ? slot.skillId : -1;
        }
    }

    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null || LocalPlayerEntityId == 0 || info.entityId != LocalPlayerEntityId) return;
        LocalDisplay = info;
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
