using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 实体表现子管理器（客户端逻辑）：按服务器摘要创建 / 更新 / 移除实体表现视图。
/// 客户端不实例化 EntityData（架构说明：客户端实体只含具体贴图模型表现），移动通过实体 id 获取 transform。
/// </summary>
public class EntityPlayerManager : ClientSubManager
{
    /// <summary>客户端实体表现视图（纯表现，无战斗逻辑）。</summary>
    public class ClientEntityView : MonoBehaviour
    {
        public ushort id;
        public EntityType type;
        public EntityCamp camp;
        public float modelTop = float.NaN; // 模型最高点（EntityModelInfo 烘焙值，懒解析缓存；头顶 UI 锚定用）
        public Animator animator;
        public EntityAnim anim;
        public int animHash = int.MinValue; // 当前动画片段 hash（判断是否需要切换）
        public Vector3 velocity;   // 服务器下发速度（包间推演用）
        public float yawSpeed;     // 绕 Y 角速度（度/秒，包间推演用）
        public float lastSeenTime; // 最近一次收到同步的时间（超时移除用）

        /// <summary>待打印的强制切换日志（Play 要到下一次 Animator 更新才生效，片段名与落地进度下一帧才读得到）。</summary>
        public string pendingForcedSwitch;

        /// <summary>待执行的片段切换（0 = 无）：收到事件只写参数，切换延后一帧再判断是否需要硬切。</summary>
        public int pendingPlayHash;
        /// <summary>该次切换要求的归一化进度。</summary>
        public float pendingPlayFrame;
        /// <summary>收到请求时的帧号（下一帧才执行，先给状态机一帧时间按新参数自行转换）。</summary>
        public int pendingPlayTick;

        /// <summary>手上临时握着的武器（服务器下发；近战类技能期间才有，用于变化检测）。</summary>
        public WeaponRef heldWeapon;
        /// <summary>常驻悬浮武器实例（按槽位下标；null = 该槽无武器）。</summary>
        public GameObject[] weaponVisuals;
        /// <summary>各槽当前显示的武器（变化检测用）。</summary>
        public WeaponRef[] weaponRefs;

        // 蘑菇感染表现（仅水晶实体）：服务器不存在蘑菇实体，「蘑菇感染」是水晶上的 Buff；
        // 客户端按同步 Buff 显隐切换（水晶/蘑菇模型均无动画，直接显隐，见策划案 11.3）
        public Renderer[] crystalRenderers; // 水晶模型渲染器（CreateView 时缓存）
        public GameObject mushroomVisual;   // 蘑菇模型（首次感染时懒实例化）
        private bool mushroomized;

        /// <summary>按 Buff 类型挂载的持续特效（key = EffectType 的 int 值），随视图一起销毁。</summary>
        public readonly Dictionary<int, GameObject> buffVfx = new();

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

        private void OnDestroy()
        {
            foreach (var pair in buffVfx)
            {
                if (pair.Value != null) Destroy(pair.Value);
            }
            buffVfx.Clear();
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

            // 强制切换日志（延后一帧：Play 生效后片段名与落地进度才读得到）
            if (pendingForcedSwitch != null)
            {
                string actual = animator != null
                    ? $"片段={ResolvePlayingClips()} 进度={animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f:F2}"
                    : "片段=?";
                Debug.LogWarning($"{pendingForcedSwitch} → {actual}");
                pendingForcedSwitch = null;
            }

            // 片段切换**延后一帧**执行：参数已在收到事件时写入，先让状态机用它自己的转换走一帧；
            // 若下一帧本地已经（或正在）切到目标状态，就不硬切——让控制器配的过渡时长生效。
            if (pendingPlayHash != 0 && animator != null && Time.frameCount > pendingPlayTick)
            {
                int target = pendingPlayHash;
                float progress = pendingPlayFrame;
                pendingPlayHash = 0;
                animHash = target; // 该目标已被"记账"，避免重复请求
                if (!IsHeadingTo(target))
                {
                    animator.Play(target, 0, progress);
                    if (logForcedAnimSwitch)
                    {
                        pendingForcedSwitch = $"[动画强制切换] 实体{id}({type}) hash={target} 要求进度={progress:F2}";
                    }
                }
            }
        }

        /// <summary>本地是否已经（或正在）处于目标状态：已到位或正在向它过渡时不必硬切。</summary>
        private bool IsHeadingTo(int targetHash)
        {
            if (animator == null) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == targetHash) return true;
            return animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).fullPathHash == targetHash;
        }

        /// <summary>当前正在播放的片段名（多个=混合中；拿不到返回 "?"）。</summary>
        private string ResolvePlayingClips()
        {
            if (animator == null) return "?";
            var infos = animator.GetCurrentAnimatorClipInfo(0);
            if (infos == null || infos.Length == 0) return "?";
            var sb = new System.Text.StringBuilder();
            foreach (var ci in infos)
            {
                if (ci.clip == null) continue;
                if (sb.Length > 0) sb.Append('+');
                sb.Append(ci.clip.name);
            }
            return sb.Length > 0 ? sb.ToString() : "?";
        }
    }

    /// <summary>实体 id → 表现视图。</summary>
    private readonly Dictionary<ushort, ClientEntityView> views = new();

    /// <summary>视图尚未创建时先到的动画事件（可靠通道可能快于姿态包），建好视图后补应用。</summary>
    private readonly Dictionary<ushort, SCEntityAnimInfo> pendingAnim = new();

    /// <summary>动画调试日志开关（强制切换 + 本地状态变化/自转观察；确认后置 false）。静态：嵌套的 ClientEntityView 也要用。</summary>
    private static bool logForcedAnimSwitch = true;

    /// <summary>表现超时移除时长（秒）：超过该时长未收到同步即移除，兜底防漏删。</summary>
    private const float ViewTimeoutSeconds = 3f;

    /// <summary>客户端表现根节点（父物体）。</summary>
    private Transform displayRoot;

    public override void Init(ClientLogicManager owner)
    {
        base.Init(owner);
        var go = new GameObject("ClientDisplays");
        go.transform.SetParent(logic.transform);
        displayRoot = go.transform;
    }

    /// <summary>超时兜底移除：超过 ViewTimeoutSeconds 未收到同步的表现自动移除（防服务器漏发移除消息）。</summary>
    public override void Tick(float deltaTime)
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

        // 先到的动画事件（可靠通道可能快于姿态包）：视图建好后补应用，避免初始参数丢失
        if (pendingAnim.TryGetValue(info.entityId, out var pending))
        {
            pendingAnim.Remove(info.entityId);
            ApplyAnim(view, pending);
        }

        // 详细数据（血量/Buff/技能槽）仅在完整同步（0.2s）时转发 UI/逻辑层
        if (info.includeRuntime) EventManager.TrigEvent(ClientEvent.OnEntityDisplayUpdate, info);

        // 本地玩家：绑定相机跟随
        if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            if (Tool.CameraController != null) Tool.CameraController.SetLookTarget(view.transform);
        }
    }

    /// <summary>
    /// 接收动画事件（服务器在状态或播放速度变化时下发，低频；首次可见时也会补发一次）。
    /// 顺序：还原持久参数 → 镜像播放速度（含强控暂停）→ 状态 hash 变化时按服务器进度切换。
    /// </summary>
    public void OnEntityAnim(SCEntityAnimInfo info)
    {
        if (info == null) return;
        if (!views.TryGetValue(info.entityId, out var view) || view == null)
        {
            pendingAnim[info.entityId] = info; // 视图尚未创建：暂存，等首个姿态包建好视图后补应用
            return;
        }
        view.lastSeenTime = Time.time;
        ApplyAnim(view, info);
    }

    /// <summary>
    /// 应用一条动画事件：**立即**还原参数、镜像播放速度（含强控暂停）；状态 hash 变化时只登记待切换，
    /// 由 <see cref="ClientEntityView.Update"/> 在下一帧决定"状态机已自行切过去（不硬切）"还是"强制 Play"。
    /// </summary>
    private void ApplyAnim(ClientEntityView view, SCEntityAnimInfo info)
    {
        if (view.anim != null) view.anim.ApplyParamPack(info.animParams);
        if (view.anim != null) view.anim.SetMoveSpeedScale(info.moveSpeedScale);
        if (view.anim != null) view.anim.SetPaused(info.paused); // 强控期间置 0：与服务器一致地冻结动画

        // -1 = "未进入任何状态"哨兵；fullPathHash 是路径哈希、可能为负，不能用 > 0 判有效
        if (view.animator != null && info.animId != -1 && info.animId != view.animHash)
        {
            view.pendingPlayHash = info.animId;
            view.pendingPlayFrame = info.animFrame;
            view.pendingPlayTick = Time.frameCount; // 下一帧才执行
        }
    }

    /// <summary>移除实体表现。</summary>
    public void OnRemoveEntity(int entityId)
    {
        pendingAnim.Remove((ushort)entityId);
        if (views.TryGetValue((ushort)entityId, out var view))
        {
            views.Remove((ushort)entityId);
            UnityEngine.Object.Destroy(view.gameObject);
            EventManager.TrigEvent(ClientEvent.OnEntityDisplayRemove, entityId);
        }
    }

    /// <summary>按实体 id 获取世界坐标。</summary>
    public bool TryGetEntityPosition(ushort id, out Vector3 pos)
    {
        return TryGetEntityTransform(id, out pos, out _);
    }

    /// <summary>按实体 id 获取完整变换（位置 + 朝向）：复原依赖朝向的挂点位置（如悬浮武器发射点）需要朝向。</summary>
    public bool TryGetEntityTransform(ushort id, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        if (!views.TryGetValue(id, out var view) || view == null) return false;
        var t = view.transform;
        pos = t.position;
        rot = t.rotation;
        return true;
    }

    /// <summary>清空全部表现（对局结束）。</summary>
    public void ClearAll()
    {
        foreach (var view in views.Values)
        {
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
        }
        views.Clear();
        pendingAnim.Clear();
    }

    /// <summary>按实体 id 获取头顶锚点世界坐标（模型最高点，懒解析缓存；无烘焙信息回退 2m）。</summary>
    public bool TryGetEntityHeadPos(ushort id, out Vector3 pos)
    {
        if (!TryGetEntityTransform(id, out pos, out _)) return false;
        if (views.TryGetValue(id, out var view) && view != null)
        {
            if (float.IsNaN(view.modelTop))
            {
                var modelInfo = view.GetComponentInChildren<EntityModelInfo>();
                view.modelTop = modelInfo != null ? modelInfo.yRange.y : 2f;
            }
            pos += Vector3.up * view.modelTop;
        }
        return true;
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
            go = UnityEngine.Object.Instantiate(graphic, displayRoot);
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
            // EntityAnim 挂在预制体根节点、Animator 在子物体（模型）上，故从根往下找，不能用 animator.GetComponent
            view.anim = go.GetComponentInChildren<EntityAnim>();
            // 客户端动画：与服务器同一 Controller 资产；无 EntityData，攻击帧回调不传（伤害只由服务器算）
            if (view.anim != null) view.anim.Init(null, null);
        }
        return view;
    }

    /// <summary>手上武器：近战类技能期间武器从悬浮位置到手部，攻击动作结束由服务器清空</summary>
    private void ApplyHeldWeapon(ClientEntityView view, int weaponCategory, int weaponIndex)
    {
        var weapon = new WeaponRef((WeaponCategory)weaponCategory, weaponIndex);
        if (view.heldWeapon == weapon) return;
        view.heldWeapon = weapon;
        if (view.anim == null) return; // 非人形单位没有手部，不显示手上武器
        if (!weapon.IsValid || Tool.AssetsManager == null
            || !Tool.AssetsManager.TryGetWeaponPrefab(weapon, out var prefab))
        {
            view.anim.SetHeldObject(null);
            return;
        }
        view.anim.SetHeldObject(prefab); // 挂点未配置时自动取 Humanoid 手部骨骼，无需手工配置
    }

    /// <summary>常驻悬浮武器：每个技能槽一把（武器 = 该槽技能对应的武器），按槽位下标取挂点</summary>
    private void ApplyFloatingWeapons(ClientEntityView view, SCEntityDisplayInfo info)
    {
        int count = Config.weapon_float_offsets.Length;
        if (view.weaponVisuals == null || view.weaponVisuals.Length != count)
        {
            view.weaponVisuals = new GameObject[count];
            view.weaponRefs = new WeaponRef[count];
        }
        for (int i = 0; i < count; i++)
        {
            var weapon = WeaponRef.None;
            if (i < info.skills.Count && info.skills[i] != null)
            {
                weapon = SkillManager.GetFlyWeapon(info.skills[i].skillId);
                if (weapon == view.heldWeapon) weapon = WeaponRef.None; // 已拿到手上，不重复漂浮
            }
            if (view.weaponRefs[i] == weapon) continue;
            view.weaponRefs[i] = weapon;
            if (view.weaponVisuals[i] != null) UnityEngine.Object.Destroy(view.weaponVisuals[i]);
            view.weaponVisuals[i] = null;
            if (!weapon.IsValid || Tool.AssetsManager == null
                || !Tool.AssetsManager.TryGetWeaponPrefab(weapon, out var prefab)) continue;

            var obj = UnityEngine.Object.Instantiate(prefab, view.transform); // 挂在实体根物体上，任何实体通用
            obj.transform.localPosition = Config.GetWeaponFloatOffset(i);
            obj.transform.localRotation = Quaternion.identity;
            view.weaponVisuals[i] = obj;
        }
    }

    private static readonly HashSet<int> s_activeBuffs = new();
    private static readonly HashSet<int> s_expiredBuffs = new();

    /// <summary>按同步 Buff 列表维持持续特效：新出现的挂载跟随特效、消失的销毁（分配表见 Config.buff_vfx）。</summary>
    private static void ApplyBuffVfx(ClientEntityView view, List<SCEntityDisplayInfo.BuffRuntime> buffs)
    {
        s_activeBuffs.Clear();
        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b == null || !s_activeBuffs.Add(b.type)) continue;
            if (!Config.buff_vfx.TryGetValue((EffectType)b.type, out var vfx)) continue;
            if (view.buffVfx.ContainsKey(b.type)) continue;

            var trajectory = new FollowTrajectory(view.id);
            trajectory.Duration = Config.buff_vfx_life_time;
            var obj = PlayTrackedVfx(vfx.kind, vfx.index, trajectory);
            if (obj != null) view.buffVfx[b.type] = obj;
        }

        s_expiredBuffs.Clear();
        foreach (var pair in view.buffVfx)
        {
            if (!s_activeBuffs.Contains(pair.Key)) s_expiredBuffs.Add(pair.Key);
        }
        foreach (var type in s_expiredBuffs)
        {
            if (view.buffVfx[type] != null) UnityEngine.Object.Destroy(view.buffVfx[type]);
            view.buffVfx.Remove(type);
        }
    }

    /// <summary>按特效类别播放跟随轨迹特效（时长由轨迹自带）。</summary>
    private static GameObject PlayTrackedVfx(SkillVfxKind kind, int index, BulletTrajectory trajectory)
    {
        var vfx = Tool.VfxManager;
        if (vfx == null || kind == SkillVfxKind.None || index < 0) return null;
        switch (kind)
        {
            case SkillVfxKind.Buff:
                return vfx.PlayBuffVFX(index, trajectory);
            case SkillVfxKind.Shield:
                return vfx.PlayShieldVFX(index, trajectory);
            case SkillVfxKind.MagicCircle:
                return vfx.Play(vfx.GetMagicCircleVfx(index), trajectory);
            default:
                return null;
        }
    }

    private void ApplyDisplay(ClientEntityView view, SCEntityDisplayInfo info)
    {
        view.transform.position = info.position;
        view.transform.rotation = Quaternion.Euler(0f, info.yaw, 0f);

        ApplyHeldWeapon(view, info.weaponCategory, info.weaponIndex); // 手上武器（近战类）按服务器下发
        ApplyFloatingWeapons(view, info);                              // 常驻悬浮武器按技能槽推算

        // 动画不在本（高频）包里：状态切换、参数、播放速度一律由动画事件驱动（见 OnEntityAnim）。
        // 这里的代价要知道：客户端一旦自己转离了服务器的状态，只能等服务器下一次状态变化才被纠正。
        // 持续型 Buff 特效（护盾/麻痹/燃烧/各类标记）：按同步 Buff 列表增删（分配表见 Config.buff_vfx）
        if (info.includeRuntime) ApplyBuffVfx(view, info.buffs);

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
            if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.SetFogEnabled(fogged);
        }
    }
    #endregion
}
