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

        // 转发给 UI/逻辑层（守护点 HUD、本地玩家技能栏等据此刷新）
        EventManager.TrigEvent(ClientEvent.OnEntityDisplayUpdate, info);

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
        // TODO: 血条/头顶信息（BarPos）、Buff 表现（info.buffs）后续完善
    }
    #endregion
}
