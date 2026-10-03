using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

public class EntityPlayerManager : ClientSubManager
{
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
        public Rigidbody rb;       // 客户端表现刚体：kinematic，仅由 MovePosition 驱动，物理做碰撞解析防穿墙抽搐
        public Vector3 predictedPos;  // dead-reckoning 基准，避免读 transform 造成 velocity 重复叠加偏移
        public float predictedYaw;
        public bool predictedInit;
        public float lastSeenTime; // 最近一次收到同步的时间（超时移除用）

        public string pendingForcedSwitch;

        public int pendingPlayHash;
        public float pendingPlayFrame;
        public int pendingPlayTick;

        public WeaponRef heldWeapon;
        public GameObject[] weaponVisuals;
        public WeaponRef[] weaponRefs;
        public SpringWeapon springWeapon; // 武器漂浮弹簧实例（InfoManager.SpringWeapon 复制，挂表现体下自动跟随）

        // 蘑菇感染表现（仅水晶实体）：服务器不存在蘑菇实体，「蘑菇感染」是水晶上的 Buff；
        // 客户端按同步 Buff 显隐切换（水晶/蘑菇模型均无动画，直接显隐，见策划案 11.3）
        public Renderer[] crystalRenderers; // 水晶模型渲染器（CreateView 时缓存）
        public GameObject mushroomVisual;   // 蘑菇模型（首次感染时懒实例化）
        private bool mushroomized;

        public readonly Dictionary<int, GameObject> buffVfx = new();

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
            // 包间推演：用预测位置/朝向积分速度，绝不再读 transform（否则上一物理步已含 velocity 会重复叠加偏移）
            if (predictedInit)
            {
                if (velocity.sqrMagnitude > 0f) predictedPos += velocity * Time.deltaTime;
                if (!Mathf.Approximately(yawSpeed, 0f)) predictedYaw += yawSpeed * Time.deltaTime;
                if (rb != null)
                {
                    rb.MovePosition(predictedPos);
                    rb.MoveRotation(Quaternion.Euler(0f, predictedYaw, 0f));
                }
                else
                {
                    transform.position = predictedPos;
                    transform.rotation = Quaternion.Euler(0f, predictedYaw, 0f);
                }
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

            // 武器漂浮：弹簧实例在角色首次定位后初始化，再用平滑位姿驱动每个武器视觉
            if (springWeapon != null && predictedInit)
            {
                springWeapon.Init();
                if (weaponVisuals != null)
                {
                    for (int i = 0; i < weaponVisuals.Length; i++)
                    {
                        if (weaponVisuals[i] == null) continue;
                        springWeapon.GetPos(i, out var p, out var q);
                        weaponVisuals[i].transform.SetPositionAndRotation(p, q);
                    }
                }
            }
        }

        private bool IsHeadingTo(int targetHash)
        {
            if (animator == null) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == targetHash) return true;
            return animator.IsInTransition(0)
                && animator.GetNextAnimatorStateInfo(0).fullPathHash == targetHash;
        }

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

    private readonly Dictionary<ushort, ClientEntityView> views = new();

    public ClientEntityView GetView(ushort id) => views.TryGetValue(id, out var v) ? v : null;

    private readonly Dictionary<ushort, SCEntityAnimInfo> pendingAnim = new();

    private static bool logForcedAnimSwitch = true;

    private const float ViewTimeoutSeconds = 3f;

    private Transform displayRoot;

    public override void Init(ClientLogicManager owner)
    {
        base.Init(owner);
        var go = new GameObject("ClientDisplays");
        go.transform.SetParent(logic.transform);
        displayRoot = go.transform;
    }

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

        // 本地玩家：绑定相机跟随（跟随根骨骼 Hips，无骨骼回退表现体根）
        if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            if (Tool.CameraController != null)
            {
                var hips = view.animator != null ? view.animator.GetBoneTransform(HumanBodyBones.Hips) : null;
                Tool.CameraController.SetLookTarget(hips != null ? hips : view.transform);
            }
        }
    }

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

    private void ApplyAnim(ClientEntityView view, SCEntityAnimInfo info)
    {
        if (view.anim != null) view.anim.ApplyParamPack(info.animParams);
        if (view.anim != null) view.anim.SetMoveSpeedScale(info.moveSpeedScale);
        if (view.anim != null) view.anim.SetPaused(info.paused); // 强控期间置 0：与服务器一致地冻结动画
        ApplyHeldWeapon(view, info.heldWeaponCategory, info.heldWeaponIndex); // 手持武器随动画事件同步（仅武器类攻击动画期间有值）

        // -1 = "未进入任何状态"哨兵；fullPathHash 是路径哈希、可能为负，不能用 > 0 判有效
        if (view.animator != null && info.animId != -1 && info.animId != view.animHash)
        {
            view.pendingPlayHash = info.animId;
            view.pendingPlayFrame = info.animFrame;
            view.pendingPlayTick = Time.frameCount; // 下一帧才执行
        }
    }

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

    public bool TryGetEntityPosition(ushort id, out Vector3 pos)
    {
        return TryGetEntityTransform(id, out pos, out _);
    }

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

    public void ClearAll()
    {
        foreach (var view in views.Values)
        {
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
        }
        views.Clear();
        pendingAnim.Clear();
    }

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
        // 武器漂浮弹簧：复制 InfoManager.SpringWeapon 预制体，挂根骨骼(Hips)下跟随；无骨骼回退表现体根。Scale=模型高度/2（尺寸取自 EntityModelInfo.yRange 烘焙值）
        var swPrefab = Tool.InfoManager != null ? Tool.InfoManager.SpringWeapon : null;
        if (swPrefab != null)
        {
            var animator = go.GetComponentInChildren<Animator>();
            var rootBone = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            var sw = UnityEngine.Object.Instantiate(swPrefab, rootBone != null ? rootBone : go.transform);
            sw.transform.localPosition = Vector3.zero;
            sw.transform.localRotation = Quaternion.identity;
            var modelInfo = go.GetComponentInChildren<EntityModelInfo>();
            float modelHeight = modelInfo != null ? (modelInfo.yRange.y - modelInfo.yRange.x) : 2f;
            sw.transform.localScale = Vector3.one * (modelHeight * 0.5f);
            view.springWeapon = sw.GetComponent<SpringWeapon>();
        }
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

            // 客户端表现刚体：kinematic，仅由 MovePosition 驱动，物理做碰撞解析防穿墙抽搐
            var modelInfo = go.GetComponentInChildren<EntityModelInfo>();
            if (modelInfo != null) modelInfo.BuildCapsuleCollider();
            view.rb = go.GetComponent<Rigidbody>();
            if (view.rb == null) view.rb = go.AddComponent<Rigidbody>();
            view.rb.isKinematic = true;
            view.rb.interpolation = RigidbodyInterpolation.None; // 关闭插值：Update 喂可变帧率目标会与物理步拍子打架产生 judder
            view.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            view.rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        return view;
    }

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

            // 挂在表现体下，初始位姿由每帧弹簧位姿（ClientEntityView.Update）驱动，不再写死偏移
            var obj = UnityEngine.Object.Instantiate(prefab, view.transform);
            view.weaponVisuals[i] = obj;
        }
    }

    private static readonly HashSet<int> s_activeBuffs = new();
    private static readonly HashSet<int> s_expiredBuffs = new();

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
        view.predictedPos = info.position;
        view.predictedYaw = info.yaw;
        view.predictedInit = true;
        if (view.rb != null)
        {
            view.rb.MovePosition(info.position);
            view.rb.MoveRotation(Quaternion.Euler(0f, info.yaw, 0f));
        }
        else
        {
            view.transform.position = info.position;
            view.transform.rotation = Quaternion.Euler(0f, info.yaw, 0f);
        }

        ApplyFloatingWeapons(view, info);                              // 常驻悬浮武器按技能槽推算（手持中的武器不再漂浮）

        // 动画不在本高频包：状态/参数/速度全由动画事件驱动；客户端一旦偏离服务器状态，只能等下次状态变化才被纠正
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

        // 迷雾表现：本地玩家带「迷雾」Buff 时开体积雾，Buff 消失由 EnvironmentManager 按 fogTransitionDuration 平滑关闭（缩视野由可见距离负责）
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
