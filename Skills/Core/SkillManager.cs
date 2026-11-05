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
        return ConfigManager.SkillConfig.Get(id).cd;
    }

    public static int GetSkillStore(int id)
    {
        return ConfigManager.SkillConfig.Get(id).storeLimit;
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

    public static void PlayWave(SCSkillDisplayContext context)
    {
        if (context == null) throw new System.ArgumentNullException(nameof(context), "波次包为空");
        if (!s_map.TryGetValue(context.SkillId, out var skill))
        {
            Debug.LogError($"[技能] id={context.SkillId} 未注册，无法播放表现");
            return;
        }
        skill.PlayWave(context);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAll()
    {
        s_map.Clear();
        SkillPools.RegisterAll(); // 新技能统一在 SkillPools 注册
    }
}
