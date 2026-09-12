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
        public int animHash = int.MinValue; // 当前动画片段 hash（判断是否需要切换）
        public TextMesh nameLabel; // 玩家名字（仅玩家实体）
        public Vector3 velocity;   // 服务器下发速度（包间推演用）
        public float yawSpeed;     // 绕 Y 角速度（度/秒，包间推演用）
        public float lastSeenTime; // 最近一次收到同步的时间（超时移除用）

        /// <summary>当前已挂载的悬浮武器预制体（避免每次同步都销毁重建；由服务器下发的 castSkillId 决定）。</summary>
        public GameObject heldWeapon;

        // 蘑菇感染表现（仅水晶实体）：服务器不存在蘑菇实体，「蘑菇感染」是水晶上的 Buff；
        // 客户端按同步 Buff 显隐切换（水晶/蘑菇模型均无动画，直接显隐，见策划案 11.3）
        public Renderer[] crystalRenderers; // 水晶模型渲染器（CreateView 时缓存）
        public GameObject mushroomVisual;   // 蘑菇模型（首次感染时懒实例化）
        private bool mushroomized;

        /// <summary>按「蘑菇感染」Buff 显隐切换：隐藏水晶模型、显示蘑菇模型（随机外观仅选一次，避免刷新跳变）。</summary>
        public void SetMushroomized(bool on)
        {
            if (mushroomized == on) return;
            mushroomized = on;
            if (crystalRenderers != null)
            {
                foreach (var r in crystalRenderers) if (r != null) r.enabled = !on;
            }
            if (on && mushroomVisual == null)
            {
                var assets = Tool.AssetsManager;
                if (assets != null && assets.MushroomGraphics.Count > 0)
                {
                    mushroomVisual = Instantiate(assets.MushroomGraphics[Random.Range(0, assets.MushroomGraphics.Count)], transform, false);
                }
            }
            if (mushroomVisual != null) mushroomVisual.SetActive(on);
        }

        private void Update()
        {
            // 包间推演：位置 + 速度 / 朝向 + 角速度（收到同步包时已重置为权威值）
            if (velocity.sqrMagnitude > 0f)
            {
                transform.position += velocity * Time.deltaTime;
            }
            if (!Mathf.Approximately(yawSpeed, 0f))
            {
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + yawSpeed * Time.deltaTime, 0f);
            }
        }
    }

    /// <summary>实体 id → 表现视图。</summary>
    private readonly Dictionary<ushort, ClientEntityView> views = new();

    /// <summary>表现超时移除时长（秒）：超过该时长未收到同步即移除，兜底防漏删。</summary>
    private const float ViewTimeoutSeconds = 3f;

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
        view.lastSeenTime = Time.time;
        view.velocity = info.velocity;
        view.yawSpeed = info.yawSpeed;
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
            EventManager.TrigEvent(ClientEvent.OnEntityDisplayRemove, entityId);
        }
    }

    /// <summary>超时兜底移除：超过 ViewTimeoutSeconds 未收到同步的表现自动移除（防服务器漏发移除消息）。</summary>
    private void Update()
    {
        float now = Time.time;
        List<ushort> expired = null;
        foreach (var pair in views)
        {
            var view = pair.Value;
            if (view == null || now - view.lastSeenTime > ViewTimeoutSeconds)
            {
                (expired ??= new List<ushort>()).Add(pair.Key);
            }
        }
        if (expired == null) return;
        foreach (var id in expired)
        {
            OnRemoveEntity(id);
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
        // 水晶实体：缓存模型渲染器，供「蘑菇感染」Buff 显隐换模（水晶/蘑菇均无动画，直接显隐）
        if (info.type.category == EntityCategory.Crystal)
        {
            view.crystalRenderers = go.GetComponentsInChildren<Renderer>(true);
        }
        view.animator = go.GetComponentInChildren<Animator>();
        if (view.animator != null)
        {
            view.anim = view.animator.GetComponent<EntityAnim>();
            // 客户端动画：与服务器同一 Controller 资产；无 EntityData，攻击帧回调不传
            view.anim?.Init(null, null);
        }

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

    /// <summary>按 castSkillId 挂悬浮武器（&lt;0 或技能无武器 = 空手）</summary>
    private static void ApplyHeldWeapon(ClientEntityView view, int castSkillId)
    {
        if (view == null || view.anim == null) return;
        GameObject prefab = null;
        if (castSkillId >= 0 && Tool.AssetsManager != null
            && SkillManager.TryGet(castSkillId, out var skill))
        {
            Tool.AssetsManager.TryGetWeaponPrefab(skill.Weapon, out prefab);
        }
        if (view.heldWeapon == prefab) return;
        view.heldWeapon = prefab;
        view.anim.SetHeldObject(prefab);
    }

    private void ApplyDisplay(ClientEntityView view, SCEntityDisplayInfo info)
    {
        view.transform.position = info.position;
        view.transform.rotation = Quaternion.Euler(0f, info.yaw, 0f);

        ApplyHeldWeapon(view, info.castSkillId); // 悬浮武器按 castSkillId 驱动

        // 按状态 hash 定位并播放动画片段，进度取自服务器；同片段不重播，让本地动画继续
        if (view.animator != null && info.animId > 0 && info.animId != view.animHash)
        {
            view.animHash = info.animId;
            view.animator.Play(info.animId, 0, info.animFrame);
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

        // 蘑菇感染表现：完整同步时按 Buff 列表切换水晶/蘑菇模型（仅水晶缓存了 crystalRenderers）
        if (info.includeRuntime && view.crystalRenderers != null)
        {
            bool infected = false;
            foreach (var b in info.buffs)
            {
                if (b != null && b.type == (int)EffectType.MushroomInfect)
                {
                    infected = true;
                    break;
                }
            }
            view.SetMushroomized(infected);
        }

        // 迷雾表现（PC103 苍白舞者大招「为全体敌方添加」，策划案 125/322/474 行）：
        // 本地玩家身上有「迷雾」Buff 时开启体积雾，Buff 消失后由 EnvironmentManager 按
        // fogTransitionDuration 平滑关闭（「缩小视野」的机制由可见距离负责，这里只做画面表现）
        if (info.includeRuntime && NetworkManager.battleInfo != null
            && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            bool fogged = false;
            foreach (var b in info.buffs)
            {
                if (b != null && b.type == (int)EffectType.Fog)
                {
                    fogged = true;
                    break;
                }
            }
            Tool.EnvironmentManager?.SetFogEnabled(fogged);
        }
    }
    #endregion
}
