using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 同时继承EditorWindow作为编辑器面板，内部包含运行时调试逻辑
public class SkillDebugger : EditorWindow
{
    #region 编辑器面板缓存数据
    private int _skillId;
    private Transform _sourceTrans;   // 发射源 Transform（施法者位置，获取测试实体）
    private Transform _targetTrans;   // 发射目标 Transform（技能 dest 落点）
    // 计时器：记录上次点击按钮的时间戳
    private double _lastClickTime = -1;
    // 绘制选项
    private bool _showDamageRange = true;      // 绘制子弹实际伤害范围
    private bool _showTrajectory = true;       // 绘制弹道曲线
    private bool _showSweptCapsule = false;    // 绘制子弹扫掠判定胶囊（BulletHit 线段判定）
    private float _refTargetRadius = 0.4f;     // 参考目标碰撞半径（用于显示命中范围 = 子弹半径+目标半径）
    private int _trajectorySegments = 16;      // 弹道曲线采样段数
    #endregion

    #region 菜单入口
    [MenuItem("Tool/SkillDebugger")]
    private static void OpenSkillDebugWindow()
    {
        SkillDebugger window = GetWindow<SkillDebugger>("技能调试面板");
        window.minSize = new Vector2(340, 320);
        window.Show();
    }
    #endregion

    #region 编辑器绘制
    private void OnGUI()
    {
        // 非运行模式提示
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("当前非Play运行模式，该工具仅游戏运行时可用！", MessageType.Warning);
            return;
        }

        // 需求：Debug.Assert保证Tool.BattleManager==null否则报错技能驱动冲突
        //Debug.Assert(Tool.BattleManager == null, "技能驱动冲突：Tool.BattleManager已存在！");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("技能调试参数", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _skillId = EditorGUILayout.IntField("Skill Id：", _skillId);
        _sourceTrans = EditorGUILayout.ObjectField("发射源Transform：", _sourceTrans, typeof(Transform), true) as Transform;
        _targetTrans = EditorGUILayout.ObjectField("发射目标Transform：", _targetTrans, typeof(Transform), true) as Transform;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("伤害范围可视化", EditorStyles.boldLabel);
        _showDamageRange = EditorGUILayout.Toggle("绘制实际伤害范围", _showDamageRange);
        _showTrajectory = EditorGUILayout.Toggle("绘制弹道曲线", _showTrajectory);
        _showSweptCapsule = EditorGUILayout.Toggle("绘制扫掠判定胶囊", _showSweptCapsule);
        _refTargetRadius = EditorGUILayout.FloatField("参考目标碰撞半径", _refTargetRadius);
        _trajectorySegments = EditorGUILayout.IntSlider("曲线采样段数", _trajectorySegments, 4, 32);
        EditorGUILayout.HelpBox(
            "红色线框=子弹判定半径；橙色线框=子弹+参考目标半径（实际命中范围）；黄色折线=弹道曲线；绿色胶囊=子弹上一帧→当前帧扫掠判定体（BulletHit 线段判定）。",
            MessageType.Info);

        EditorGUILayout.Space();
        // 显示距离上次点击的计时器
        if (_lastClickTime < 0)
        {
            EditorGUILayout.LabelField("距上次点击：暂无点击记录");
        }
        else
        {
            double curTime = Time.realtimeSinceStartup;
            double interval = curTime - _lastClickTime;
            EditorGUILayout.LabelField($"距上次点击：{interval.ToString("F1")} 秒");
        }

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(_sourceTrans == null || _targetTrans == null);
        if (GUILayout.Button("使用技能", GUILayout.Height(32)))
        {
            // 点击时更新上次点击时间
            _lastClickTime = Time.realtimeSinceStartup;
            UseSkill(_skillId, _sourceTrans, _targetTrans);
        }
        EditorGUI.EndDisabledGroup();

        if (_sourceTrans == null || _targetTrans == null)
        {
            EditorGUILayout.HelpBox("请拖拽发射源与发射目标的Transform至对应对象框", MessageType.Info);
        }
    }

    // 窗口持续刷新，实时更新计时器
    private void Update()
    {
        Repaint();
        BattleManager.BulletContainer.RemoveExpiredBullets();
        DrawBulletDebugInfo();
        System.Threading.Thread.Sleep(20);
    }
    #endregion

    #region 子弹伤害范围可视化
    /// <summary>
    /// 遍历当前活跃子弹，用 GizmosDrawer 在 Scene 视图绘制：
    /// 1) 弹道曲线（黄色折线，按贝塞尔曲线采样）
    /// 2) 子弹当前判定球（红色线框，半径 = b.radius）
    /// 3) 实际命中范围球（橙色线框，半径 = b.radius + 参考目标半径）
    /// 4) 扫掠判定胶囊（绿色，上一帧位置→当前帧位置 + 半径，对应 BulletHit 线段判定）
    /// </summary>
    private void DrawBulletDebugInfo()
    {
        if (!_showDamageRange && !_showTrajectory && !_showSweptCapsule) return;
        foreach (var b in BattleManager.BulletContainer.Bullets)
        {
            if (_showTrajectory)
            {
                Vector3 prev = b.curve.Lerp(0f);
                for (int i = 1; i <= _trajectorySegments; i++)
                {
                    float t = i / (float)_trajectorySegments;
                    Vector3 next = b.curve.Lerp(t);
                    GizmosDrawer.DrawLine(prev, next, new Color(1f, 0.9f, 0.2f, 1f), 0.12f);
                    prev = next;
                }
            }
            if (_showSweptCapsule)
            {
                // BulletHit 判定：p1=上一帧位置, p2=当前帧位置, dist = b.radius + 目标半径
                // 用胶囊线框表达这一帧子弹扫过的判定体
                GizmosDrawer.DrawWireCapsuleBetween(b.LastPosition, b.Position, b.radius + _refTargetRadius, new Color(0.3f, 1f, 0.4f, 1f), 0.12f);
            }
            if (_showDamageRange)
            {
                // 子弹判定半径（实际 BulletHit 用 b.radius 与目标胶囊求距离）
                GizmosDrawer.DrawWireSphere(b.Position, b.radius, new Color(1f, 0.25f, 0.25f, 1f), 0.12f);
                // 命中范围 = 子弹半径 + 目标碰撞半径
                GizmosDrawer.DrawWireSphere(b.Position, b.radius + _refTargetRadius, new Color(1f, 0.55f, 0.1f, 1f), 0.12f);
            }
        }
    }
    #endregion

    /// <summary>
    /// 释放技能：发射源 Transform 上挂测试实体（EntityData），发射目标 Transform 位置作为 dest 落点。
    /// </summary>
    public void UseSkill(int index, Transform source, Transform target)
    {
        // 从发射源获取测试实体，无则自动添加（不再绑定原目标 GameObject）
        var data = source.GetComponent<InfectionData>();
        if (data == null)
        {
            data = source.gameObject.AddComponent<InfectionData>();
        }
        // 为测试实体补充默认碰撞体（与玩家一致），保证发射点高度与命中可视化合理
        if (data.colliderInfo.radius <= 0f)
        {
            data.colliderInfo = new EntityData.EntityColliderInfo { bottom = 0.3f, top = 1.7f, radius = 0.4f };
        }
        var output = SkillManager.DoDamageActs(index, data, target.position);
        SkillManager.PlayVFX(index, output.Item1, output.Item2);
    }
}
