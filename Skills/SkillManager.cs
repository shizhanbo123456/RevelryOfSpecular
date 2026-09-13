using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 技能管理器（注册表 + 统一入口）。
/// 外部只向 SkillManager 传入技能 id 和上下文即可（见架构说明）。
/// 各技能池在 RegisterAll 中注册自己的技能。
/// </summary>
public static class SkillManager
{
    private static readonly Dictionary<int, SkillBase> s_map = new();

    /// <summary>注册技能（各技能池的 RegisterAll 中调用）。</summary>
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

    /// <summary>技能对应的漂浮武器（客户端常驻漂浮显示用；无武器返回 <see cref="WeaponRef.None"/>）。</summary>
    public static WeaponRef GetFlyWeapon(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.FlyWeapon : WeaponRef.None;
    }

    /// <summary>表现侧（客户端执行，收到"使用技能"RPC 后调用）。</summary>
    public static void PlayVFX(int id, SkillContext context)
    {
        if (!s_map.TryGetValue(id, out var skill))
        {
            Debug.LogWarning($"未知技能 id：{id}");
            return;
        }
        skill.PlayVFX(context);
    }

    /// <summary>程序集加载时注册全部技能池。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAll()
    {
        s_map.Clear();
        SkillPoolCrystal.RegisterAll();   // 水晶掉落（武器技能 0~49）
        SkillPoolDefense.RegisterAll();   // 防守方角色专属 50~73（被动不占 id）
        SkillPoolNonPlayer.RegisterAll(); // 非玩家单位 100~179
        SkillPoolUnarmed.RegisterAll();   // 空手攻击 180~182
    }
}
