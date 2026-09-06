using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 客户端表现管理器（Sub：统一处理服务器传回的实体表现并呈现）。
/// 客户端不实例化 EntityData（架构说明：客户端实体只含具体贴图模型表现），
/// 移动通过实体 id 获取目标 transform。
/// </summary>
public class ClientDisplayManager : MonoBehaviour
{
    /// <summary>客户端实体表现视图（纯表现，无战斗逻辑）。</summary>
    public class ClientEntityView : MonoBehaviour
    {
        public ushort id;
        public EntityType type;
        public EntityCamp camp;
        public Animator animator;
        public EntityAnim anim;
        public TextMesh nameLabel; // 玩家名字（仅玩家实体）
    }

    /// <summary>实体 id → 表现视图。</summary>
    private readonly Dictionary<ushort, ClientEntityView> views = new();

    /// <summary>客户端表现根节点（父物体）。</summary>
    public Transform displayRoot;

    private void Awake()
    {
        Tool.ClientDisplayManager = this;
        if (displayRoot == null)
        {
            var go = new GameObject("ClientDisplays");
            go.transform.SetParent(transform);
            displayRoot = go.transform;
        }
    }

    /// <summary>接收服务器实体表现摘要（创建或更新）。</summary>
    public void OnEntityDisplay(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (!views.TryGetValue(info.entityId, out var view))
        {
            view = CreateView(info);
            if (view == null) return;
            views[info.entityId] = view;
        }
        ApplyDisplay(view, info);

        // 详细数据（血量/Buff/技能槽）仅在完整同步（0.2s）时转发 UI/逻辑层
        if (info.includeRuntime) EventManager.TrigEvent(ClientEvent.OnEntityDisplayUpdate, info);

        // 本地玩家：绑定相机跟随
        if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            if (Tool.CameraController != null) Tool.CameraController.SetLookTarget(view.transform);
        }
    }

    /// <summary>移除实体表现。</summary>
    public void OnRemoveEntity(int entityId)
    {
        if (views.TryGetValue((ushort)entityId, out var view))
        {
            views.Remove((ushort)entityId);
            Destroy(view.gameObject);
        }
    }

    /// <summary>按实体 id 获取表现物体 transform。</summary>
    public bool TryGetEntityTransform(ushort id, out Transform transform)
    {
        transform = null;
        return views.TryGetValue(id, out var view) && view != null && (transform = view.transform) != null;
    }

    /// <summary>
    /// 取本地玩家视野内最近的敌方单位位置（自动索敌，策划案 D 组）。
    /// </summary>
    public bool TryGetNearestEnemyPosition(Vector3 from, float viewDistance, EntityCamp myCamp, out Vector3 pos)
    {
        pos = Vector3.zero;
        float nearest = viewDistance * viewDistance;
        bool found = false;
        foreach (var pair in views)
        {
            var view = pair.Value;
            if (view == null || view.camp == myCamp) continue;
            float dist = Vector3.SqrMagnitude(view.transform.position - from);
            if (dist <= nearest)
            {
                nearest = dist;
                pos = view.transform.position;
                found = true;
            }
        }
        return found;
    }

    /// <summary>按实体 id 获取世界坐标。</summary>
    public bool TryGetEntityPosition(ushort id, out Vector3 pos)
    {
        pos = Vector3.zero;
        if (!views.TryGetValue(id, out var view) || view == null) return false;
        pos = view.transform.position;
        return true;
    }

    /// <summary>清空全部表现（对局结束）。</summary>
    public void ClearAll()
    {
        foreach (var view in views.Values)
        {
            if (view != null) Destroy(view.gameObject);
        }
        views.Clear();
    }

    #region//Local
    private ClientEntityView CreateView(SCEntityDisplayInfo info)
    {
        // 客户端图形：优先 AssetsManager 配置；无配置时以空物体占位（保证 UI 可寻址）
        GameObject graphic = null;
        if (Tool.AssetsManager != null)
        {
            Tool.AssetsManager.TryGetGraphic(info.type, out graphic);
        }
        GameObject go;
        if (graphic != null)
        {
            go = Instantiate(graphic, displayRoot);
        }
        else
        {
            go = new GameObject($"Entity_{info.entityId}");
            go.transform.SetParent(displayRoot);
        }
        go.name = $"Entity_{info.entityId}_{info.type}";
        var view = go.AddComponent<ClientEntityView>();
        view.id = info.entityId;
        view.type = info.type;
        view.camp = info.camp;
        view.animator = go.GetComponentInChildren<Animator>();
        if (view.animator != null) view.anim = view.animator.GetComponent<EntityAnim>();

        // 玩家名字（头顶文字，无血条；攻红守蓝）
        if (info.type.category == EntityCategory.Character_Attack ||
            info.type.category == EntityCategory.Character_Defense)
        {
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(go.transform, false);
            float yOffset = Tool.InfoManager != null ? Tool.InfoManager.GetEntityBarYOffset(info.type) : 2f;
            labelGo.transform.localPosition = Vector3.up * yOffset;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = $"玩家{info.ownerClientId}";
            tm.fontSize = 64;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = info.camp == EntityCamp.Attack ? new Color(1f, 0.45f, 0.4f) : new Color(0.45f, 0.7f, 1f);
            view.nameLabel = tm;
        }
        return view;
    }

    private void ApplyDisplay(ClientEntityView view, SCEntityDisplayInfo info)
    {
        view.transform.position = info.position;
        view.transform.rotation = Quaternion.Euler(0f, info.yaw, 0f);

        // 表现完全由服务器下发的动画状态驱动（animState + animId + animFrame）
        if (view.anim != null)
        {
            var state = (EntityAnim.AnimState)info.animState;
            switch (state)
            {
                case EntityAnim.AnimState.Spawn:
                    view.anim.DoSpawn();
                    break;
                case EntityAnim.AnimState.Attack:
                    view.anim.DoAttack((EntityAnim.AttackType)info.animId);
                    break;
                case EntityAnim.AnimState.Hit:
                    view.anim.DoHit();
                    break;
                case EntityAnim.AnimState.Die:
                    view.anim.DoDie();
                    break;
                case EntityAnim.AnimState.Motion:
                default:
                    switch ((EntityAnim.MotionType)info.animId)
                    {
                        case EntityAnim.MotionType.Run: view.anim.Move(true); break;
                        case EntityAnim.MotionType.Jump: view.anim.InAir(true); break;
                        case EntityAnim.MotionType.Slide: view.anim.DoSlide(); break;
                        case EntityAnim.MotionType.Roll: view.anim.Roll(); break;
                        case EntityAnim.MotionType.Idle:
                        default: view.anim.Move(false); break;
                    }
                    break;
            }
        }
        // 动画移速载体（加速/减速/泥沼 = 移动状态播放速度）：完整同步时按 Buff 重算
        if (info.includeRuntime && view.anim != null && info.buffs.Count > 0)
        {
            var types = new List<int>(info.buffs.Count);
            foreach (var b in info.buffs)
            {
                if (b != null) types.Add(b.type);
            }
            view.anim.SetMoveSpeedScale(EntityEffectController.ComputeMoveAnimSpeedMultiplier(types));
        }
    }
    #endregion
}
