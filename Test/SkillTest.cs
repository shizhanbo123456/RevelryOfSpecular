using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Ros.Skill;

public class SkillTestEntity : EntityData { } // 测试专用实体：无附加逻辑，仅借 OnCreate 撑起技能链路依赖

public class SkillTest : MonoBehaviour
{
    // 攻击动画配置是 protected，工具只读取以复刻 BeginCast 分支，不改技能代码
    private static readonly PropertyInfo HasCastAnimProp =
        typeof(SkillBase).GetProperty("HasCastAnim", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly PropertyInfo CastAnimProp =
        typeof(SkillBase).GetProperty("CastAnim", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly string[] TabNames = { "水晶武器", "防守方", "非玩家单位", "空手" };

    private readonly List<int> skillIds = new();
    private readonly List<int> tabStarts = new();
    private readonly List<(int id, SkillTestEntity entity)> dummies = new(); // 靶子：手删 GameObject 后按 id 清容器
    private Vector2 scroll;
    private int currentTab;
    private int nextDummyId = 100;
    private SkillTestEntity entity;
    private string status = "";
    private Rect windowRect = new(20f, 20f, 340f, 640f);

    [Tooltip("发射方向（实体 yaw，度，0 = 朝 +Z）")]
    [SerializeField] private float yaw;
    [Tooltip("OnGUI 整体缩放（编辑器小窗口看不清时调大）")]
    [SerializeField] private float guiScale = 1.5f;
    [Tooltip("接管相机看向原点（关闭则不动相机）")]
    [SerializeField] private bool camControl = true;
    [Tooltip("相机距原点的水平距离（z = -距离）")]
    [SerializeField] private float camDistance = 15f;
    [Tooltip("相机距原点的垂直高度")]
    [SerializeField] private float camHeight = 8f;

    private Camera testCam;

    private void Start()
    {
        EnsureVfxManager();
        EnsureCamera();
        BuildSkillIds();
        SpawnEntity();
    }

    private void Update()
    {
        PruneDummies();
        if (entity == null) return;
        entity.transform.rotation = Quaternion.Euler(0f, yaw, 0f); // 无敌人时 AimPos 沿实体 forward 取点，yaw 即发射方向
        entity.OnUpdate();
        entity.TickVelocity(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!camControl || testCam == null) return;
        Vector3 pos = new Vector3(0f, camHeight, -camDistance); // x=0、z<0，朝向 z+ 看向原点
        testCam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos, Vector3.up));
    }

    private void OnDestroy()
    {
        if (entity != null) BattleManager.EntityContainer.Entities.Remove(entity.id); // 退出时清掉全局容器登记
        foreach (var dummy in dummies) BattleManager.EntityContainer.Entities.Remove(dummy.id);
        dummies.Clear();
    }

    // 靶子被手动删掉后同步清容器，否则索敌取到已销毁条目会在 transform 上抛 MissingReference
    private void PruneDummies()
    {
        for (int i = dummies.Count - 1; i >= 0; i--)
        {
            if (dummies[i].entity != null) continue;
            BattleManager.EntityContainer.Entities.Remove(dummies[i].id);
            dummies.RemoveAt(i);
        }
    }

    // 画武器漂浮槽位：球=浮动位置（ShotOrigin 取值处），线=该槽位朝向
    private void OnDrawGizmos()
    {
        if (entity == null || entity.springWeapon == null) return;
        int slot = entity.skillController != null && entity.skillController.CastingSlotIndex >= 0
            ? entity.skillController.CastingSlotIndex : 0;
        entity.springWeapon.GetPos(slot, out Vector3 pos, out Quaternion rot);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(pos, 0.12f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pos, pos + rot * Vector3.forward * 2f);
    }

    private void OnGUI()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(guiScale, guiScale), Vector2.zero); // 整体放大，适配编辑器小窗口
        windowRect.height = Mathf.Min(windowRect.height, Screen.height / guiScale);
        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "SkillTest 技能试放");
        GUI.matrix = oldMatrix;
    }

    // 复用主相机，测试场景没有则补一个
    private void EnsureCamera()
    {
        testCam = Camera.main;
        if (testCam == null)
        {
            var go = new GameObject("SkillTest Camera");
            testCam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
    }

    // PlayVFX 表现链路依赖 VfxManager（VfxHelper 内是 null 静默跳过），场景没有则补最小实例
    private static void EnsureVfxManager()
    {
        if (Tool.VfxManager != null) return;
        new GameObject("[SkillTest] VfxManager").AddComponent<VfxManager>();
    }

    private void BuildSkillIds()
    {
        skillIds.Clear();
        tabStarts.Clear();
        var ids = SkillManager.GetRegisteredIds();
        foreach (int id in ids) skillIds.Add(id);
        tabStarts.Add(IndexOfRange(0));
        tabStarts.Add(IndexOfRange(50));
        tabStarts.Add(IndexOfRange(100));
        tabStarts.Add(IndexOfRange(180));
    }

    private int IndexOfRange(int minId)
    {
        for (int i = 0; i < skillIds.Count; i++)
            if (skillIds[i] >= minId) return i;
        return skillIds.Count;
    }

    private void SpawnEntity()
    {
        status = "";
        if (Tool.InfoManager == null)
        {
            status = "场景中没有 InfoManager：请放置一个配置完整的 InfoManager（属性模板 + SpringWeapon）后重新运行";
            return;
        }
        GameObject prefab = FirstGraphic(Tool.AssetsManager != null ? Tool.AssetsManager.AttackCharacterGraphics : null);
        prefab = prefab != null ? prefab : FirstGraphic(Tool.AssetsManager != null ? Tool.AssetsManager.DefenseCharacterGraphics : null);
        if (prefab == null)
        {
            status = "AssetsManager 里没有可用的角色预制体，无法生成测试实体";
            return;
        }
        try
        {
            var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            go.name = "SkillTestEntity";
            entity = go.AddComponent<SkillTestEntity>();
            entity.OnCreate(1, new EntityType(EntityCategory.Character_Attack, 0), 1, EntityCamp.Attack);
            // 注册进战斗容器：ShotOrigin 才能查到实体，弹道起点 = 真实武器锚点（否则兜底 Vector3.zero）
            BattleManager.EntityContainer.Entities.Add(entity.id, entity);
            if (entity.springWeapon == null)
                status = "InfoManager.SpringWeapon 未配置：武器类技能会释放失败（空手/近战不受影响），请检查 InfoManager 配置";
        }
        catch (System.Exception ex)
        {
            entity = null;
            status = $"测试实体初始化失败：{ex.Message}";
            Debug.LogException(ex); // 面板只有摘要，完整堆栈进 Console 便于定位
        }
    }

    // 靶子：关重力并置运动学，位置留给 Inspector 手调；阵营 Defense 供技能索敌命中
    private void SpawnDummy()
    {
        if (Tool.InfoManager == null) { status = "场景中没有 InfoManager，无法生成测试敌人"; return; }
        GameObject prefab = FirstGraphic(Tool.AssetsManager != null ? Tool.AssetsManager.DefenseCharacterGraphics : null);
        prefab = prefab != null ? prefab : FirstGraphic(Tool.AssetsManager != null ? Tool.AssetsManager.AttackCharacterGraphics : null);
        if (prefab == null) { status = "AssetsManager 里没有可用的角色预制体"; return; }
        try
        {
            var go = Instantiate(prefab, new Vector3(0f, 0f, 5f), Quaternion.identity);
            go.name = $"SkillTestDummy_{nextDummyId}";
            var dummy = go.AddComponent<SkillTestEntity>();
            dummy.OnCreate((ushort)nextDummyId, new EntityType(EntityCategory.Character_Defense, 0), 1, EntityCamp.Defense);
            if (dummy.rb != null)
            {
                dummy.rb.useGravity = false;
                dummy.rb.isKinematic = true;
            }
            BattleManager.EntityContainer.Entities.Add(dummy.id, dummy);
            dummies.Add((dummy.id, dummy));
            nextDummyId++;
            status = $"已添加测试敌人 {go.name}（阵营 Defense，位置可在 Inspector 手调）";
        }
        catch (System.Exception ex)
        {
            status = $"测试敌人生成失败：{ex.Message}";
            Debug.LogException(ex); // 面板只有摘要，完整堆栈进 Console 便于定位
        }
    }

    private static GameObject FirstGraphic(List<GameObject> list)
    {
        if (list == null) return null;
        foreach (var go in list)
            if (go != null) return go;
        return null;
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label(status);

        GUILayout.Label($"发射方向 {yaw:F0}°");
        yaw = GUILayout.HorizontalSlider(yaw, 0f, 360f);

        if (GUILayout.Button($"添加测试敌人（当前 {dummies.Count} 个）")) SpawnDummy();

        camControl = GUILayout.Toggle(camControl, "接管相机看向原点");
        GUILayout.Label($"相机水平距离 {camDistance:F1} m");
        camDistance = GUILayout.HorizontalSlider(camDistance, 1f, 100f);
        GUILayout.Label($"相机垂直高度 {camHeight:F1} m");
        camHeight = GUILayout.HorizontalSlider(camHeight, 0f, 50f);

        if (currentTab >= tabStarts.Count) currentTab = 0;
        currentTab = GUILayout.Toolbar(currentTab, TabNames);
        int start = tabStarts[currentTab];
        int next = currentTab + 1 < tabStarts.Count ? tabStarts[currentTab + 1] : skillIds.Count;

        scroll = GUILayout.BeginScrollView(scroll);
        for (int i = start; i < next; i++)
        {
            int skillId = skillIds[i];
            string name = System.Enum.GetName(typeof(SkillType), skillId) ?? skillId.ToString();
            if (GUILayout.Button($"[{skillId}] {name.TrimStart('S', 'k', 'i', 'l', 'l')}"))
                Cast(skillId);
        }
        GUILayout.EndScrollView();
    }

    // 复刻 SkillBase.BeginCast 的分支：有攻击动画则在攻击帧播表现，没有则立即播（避免调用 private CastNow 的服务器链路）
    private void Cast(int skillId)
    {
        if (entity == null) { status = "测试实体未生成，无法释放"; return; }
        if (!SkillManager.TryGet(skillId, out var skill)) { status = $"技能未注册：{skillId}"; return; }

        if (HasCastAnimProp == null || CastAnimProp == null)
        {
            status = "反射读取 SkillBase.HasCastAnim/CastAnim 失败，无法判断攻击动画";
            return;
        }
        bool hasAnim = entity.anim != null && (bool)HasCastAnimProp.GetValue(skill);
        if (!hasAnim)
        {
            PlayVfx(skill, skillId, "无攻击动画，立即释放");
            return;
        }

        var castAnim = (EntityAnim.AttackType)CastAnimProp.GetValue(skill);
        if (castAnim is EntityAnim.AttackType.Attack_Weapon_R
            or EntityAnim.AttackType.Attack_Weapon_L
            or EntityAnim.AttackType.Attack_Weapon_R_And_L)
            entity.heldWeapon = skill.Weapon;
        entity.anim.onAttack = _ => PlayVfx(skill, skillId, "攻击帧释放"); // 多段攻击帧会逐次触发，与标准一致
        entity.anim.DoAttack(castAnim);
        status = $"[{skillId}] 已进入攻击动画，等待攻击帧";
    }

    // 播放 = SkillLogic 现场构造上下文（技能内部）+ PlayVFX；上下文在攻击帧才生成，位置/朝向与施放时刻一致
    private void PlayVfx(SkillBase skill, int skillId, string when)
    {
        try
        {
            var context = skill.SkillLogic(entity);
            if (context == null) { status = $"[{skillId}] 技能未生成上下文"; return; }
            skill.PlayVFX(context);
            status = $"[{skillId}] {when}";
        }
        catch (System.Exception ex)
        {
            status = $"[{skillId}] 释放失败：{ex.GetType().Name} {ex.Message}";
            Debug.LogException(ex); // 面板只有摘要，完整堆栈进 Console 便于定位
        }
    }
}
