using System.Collections.Generic;
using Ros.Skill;
using Ros.Transport;
using UnityEngine;

public static class SkillManager
{
    private static readonly Dictionary<int, SkillBase> s_map = new();

    public static void Register(SkillBase skill)
    {
        if (skill == null) return;
        s_map[skill.Id] = skill;
    }

    public static void Unregister(int id)
    {
        s_map.Remove(id);
    }

    public static bool TryGet(int id, out SkillBase skill)
    {
        return s_map.TryGetValue(id, out skill);
    }

    public static float GetSkillCD(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.CD : 0f;
    }

    public static int GetSkillStore(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.Store : -1;
    }

    public static int GetSkillCount()
    {
        return s_map.Count;
    }

    public static List<int> GetRegisteredIds()
    {
        var ids = new List<int>(s_map.Keys);
        ids.Sort();
        return ids;
    }

    public static WeaponRef GetFlyWeapon(int id)
    {
        return s_map.TryGetValue(id, out var skill) ? skill.Weapon : WeaponRef.None;
    }

    public static void PlayVFX(int id, SkillContext context)
    {
        if (!s_map.TryGetValue(id, out var skill))
        {
            Debug.LogWarning($"未知技能 id：{id}");
            return;
        }
        skill.PlayVFX(context);
    }

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
