using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 头顶信息子管理器（客户端逻辑）：玩家头顶的名字与血条。
/// 标签作为实体表现视图的子物体（跟随与层级沿用视图），朝向由本管理器每帧对齐相机。
/// </summary>
public class LabelPlayerManager : ClientSubManager
{
    // 表现参数（占位初值，待策划定表现规格后调整）
    private const float BarWidth = 1f;
    private const float BarHeight = 0.12f;
    private const float BarDepth = 0.02f;
    private const float BarOffsetY = 0.45f;          // 相对名字锚点的抬高量
    private const float NameFallbackOffsetY = 0.9f;  // InfoManager 未配置锚点时用
    private const float NameCharacterSize = 0.08f;
    private const int NameFontSize = 64;
    private const string BarShaderName = "Unlit/Color";

    /// <summary>血条材质下标。</summary>
    private const int MatTrack = 0;
    private const int MatAttack = 1;
    private const int MatDefense = 2;

    private class OverheadLabel
    {
        public Transform barRoot;  // 每帧对齐相机朝向
        public Transform barFill;  // 横向缩放表示血量比（左对齐）
        public float barFullWidth;
    }

    private readonly Dictionary<ushort, OverheadLabel> labels = new();

    private static readonly Material[] s_barMaterials = new Material[3];
    private static bool s_barShaderMissing;

    public override void Tick(float deltaTime)
    {
        if (labels.Count == 0) return;
        var camera = Camera.main;
        if (camera == null) return; // 无主相机时血条不转向，位置跟随仍由视图层级保证
        Quaternion facing = camera.transform.rotation;
        foreach (var pair in labels)
        {
            var label = pair.Value;
            if (label.barRoot != null) label.barRoot.rotation = facing;
        }
    }

    /// <summary>为玩家视图挂上头顶标签（名字 + 血条）。非玩家实体不挂。</summary>
    public void Attach(EntityPlayerManager.ClientEntityView view, SCEntityDisplayInfo info)
    {
        if (view == null || info == null) return;
        if (view.type.category != EntityCategory.Character_Attack &&
            view.type.category != EntityCategory.Character_Defense) return;

        Detach(view.id);

        var labelGo = new GameObject("OverheadLabel");
        labelGo.transform.SetParent(view.transform, false);
        float yOffset = Tool.InfoManager != null
            ? Tool.InfoManager.GetEntityBarYOffset(view.type)
            : NameFallbackOffsetY;
        labelGo.transform.localPosition = Vector3.up * yOffset;

        CreateName(labelGo.transform, info);
        var label = new OverheadLabel();
        CreateBar(labelGo.transform, info, label);
        labels[view.id] = label;
    }

    /// <summary>解除记录（标签物体随视图一起销毁）。</summary>
    public void Detach(ushort id) => labels.Remove(id);

    /// <summary>清空全部记录（对局结束）。</summary>
    public void ClearAll() => labels.Clear();

    protected override void BindEvents()
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
    }

    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null || info.maxHealth <= 0) return;
        if (!labels.TryGetValue(info.entityId, out var label)) return;
        RefreshBar(label, (float)info.health / info.maxHealth);
    }

    #region//Local
    private static void CreateName(Transform parent, SCEntityDisplayInfo info)
    {
        var go = new GameObject("Name");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMesh>();
        text.text = $"玩家{info.ownerClientId}";
        text.fontSize = NameFontSize;
        text.characterSize = NameCharacterSize;
        text.anchor = TextAnchor.LowerCenter;
        text.alignment = TextAlignment.Center;
        // 攻红守蓝，与 HUD 阵营配色一致（UITheme 为外观唯一真值）
        text.color = info.camp == EntityCamp.Attack ? UITheme.Attack : UITheme.Defense;
    }

    private static void CreateBar(Transform parent, SCEntityDisplayInfo info, OverheadLabel label)
    {
        var trackMaterial = GetBarMaterial(MatTrack, new Color(0.1f, 0.1f, 0.12f, 1f));
        var fillMaterial = GetBarMaterial(
            info.camp == EntityCamp.Attack ? MatAttack : MatDefense,
            info.camp == EntityCamp.Attack ? UITheme.Attack : UITheme.Defense);
        // 着色器缺失时整条血条放弃，避免只留下底色
        if (trackMaterial == null || fillMaterial == null) return;

        var barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(parent, false);
        barRoot.transform.localPosition = Vector3.up * BarOffsetY;
        label.barRoot = barRoot.transform;

        // 用薄立方体而非四边形：四边形有背面剔除，朝向判断错就会整条看不见
        CreateBox("Track", barRoot.transform, trackMaterial, Vector3.zero);
        // 填充层沿本地 -Z 前推（对齐相机后本地 -Z 恒朝相机），避免与底条共面闪烁
        label.barFill = CreateBox("Fill", barRoot.transform, fillMaterial, new Vector3(0f, 0f, -0.05f));
        label.barFullWidth = BarWidth;
        RefreshBar(label, 1f);
    }

    /// <summary>血量比变化：填充层左对齐缩放（整体左移补齐差值的一半，视觉上从右侧缩短）。</summary>
    private static void RefreshBar(OverheadLabel label, float ratio)
    {
        if (label.barFill == null) return;
        float width = label.barFullWidth * Mathf.Clamp01(ratio);
        label.barFill.localScale = new Vector3(width, BarHeight, BarDepth);
        var pos = label.barFill.localPosition;
        pos.x = -(label.barFullWidth - width) * 0.5f;
        label.barFill.localPosition = pos;
    }

    private static Transform CreateBox(string name, Transform parent, Material material, Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        // 自带碰撞体：宿主模式（服务器与客户端同进程）下会进物理查询，必须去掉
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(BarWidth, BarHeight, BarDepth);
        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return t;
    }

    private static Material GetBarMaterial(int slot, Color color)
    {
        if (s_barShaderMissing) return null;
        if (s_barMaterials[slot] != null) return s_barMaterials[slot];
        var shader = Shader.Find(BarShaderName);
        if (shader == null)
        {
            s_barShaderMissing = true;
            Debug.LogWarning($"[LabelPlayerManager] 找不到着色器 {BarShaderName}，头顶血条已跳过（打包时需加入 Always Included Shaders）。");
            return null;
        }
        s_barMaterials[slot] = new Material(shader) { color = color };
        return s_barMaterials[slot];
    }
    #endregion
}
