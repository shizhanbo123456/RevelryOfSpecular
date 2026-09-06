using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 技能管理器（注册表 + 统一入口）。
/// 外部只向 SkillManager 传入技能 id 和上下文即可（见架构说明）。
/// 技能包（A/B/C）在 RegisterAll 中注册各自的技能。
/// </summary>
public static class SkillManager
{
    private static readonly Dictionary<int, SkillBase> s_map = new();

    /// <summary>注册技能（技能包 PackageManager.RegisterAll 中调用）。</summary>
    public static void Register(SkillBase skill)
    {
        if (skill == null) return;
        s_map[skill.Id] = skill;
    }

    /// <summary>反注册。</summary>
    public static void Unregister(int id)
    {
        s_map.Remove(id);
    }

    /// <summary>查询技能。</summary>
    public static bool TryGet(int id, out SkillBase skill)
    {
        return s_map.TryGetValue(id, out skill);
    }

    /// <summary>技能 CD。</summary>
    public static float GetSkillCD(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.CD : 0f;
    }

    /// <summary>技能库存。</summary>
    public static int GetSkillStore(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.Store : -1;
    }

    /// <summary>是否远程/施法类（右键可触发）。</summary>
    public static bool IsRanged(int id)
    {
        return s_map.TryGetValue(id, out var skill) && skill.Ranged;
    }

    /// <summary>是否有武器显示。</summary>
    public static bool HasWeaponDisplay(int id)
    {
        return s_map.TryGetValue(id, out var skill) && skill.HasWeaponDisplay;
    }

    /// <summary>释放动作。</summary>
    public static EntityAnim.AttackType GetCastAnim(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.CastAnim : 0;
    }

    /// <summary>伤害侧（服务器权威执行）。</summary>
    public static void DoDamageActs(int id, EntityData entity, Vector3 dest)
    {
        if (!s_map.TryGetValue(id, out var skill))
        {
            Debug.LogWarning($"未知技能 id：{id}");
            return;
        }
        skill.DoDamageActs(entity, dest);
    }

    /// <summary>表现侧（客户端执行，收到"使用技能"RPC 后调用）。</summary>
    public static void PlayVFX(int id, TrajectoryContext context)
    {
        if (!s_map.TryGetValue(id, out var skill))
        {
            Debug.LogWarning($"未知技能 id：{id}");
            return;
        }
        skill.PlayVFX(context);
    }

    /// <summary>程序集加载时注册所有技能包（技能包实现后自动生效）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAll()
    {
        s_map.Clear();
        SkillPackageA.PackageManager.RegisterAll();
        SkillPackageB.PackageManager.RegisterAll();
        SkillPackageC.PackageManager.RegisterAll();
    }
}
