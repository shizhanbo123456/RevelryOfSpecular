using Ros.Skill;
using System.Collections.Generic;
using UnityEngine;
using a=Ros.Skill.PackageA;
using b=Ros.Skill.PackageB;
using c=Ros.Skill.PackageC;

public class SkillManager
{
    private static readonly HashSet<int> s_targetBuffer = new();
    private static readonly List<SkillBase> s_list = new()
    {
        new a.Skill0(),
        new a.Skill1(),
        new a.Skill2(),
        new a.Skill3(),
        new a.Skill4(),
        new a.Skill5(),
        new a.Skill6(),
        new a.Skill7(),
        new a.Skill8(),
        new a.Skill9(),
        new a.Skill10(),
        new a.Skill11(),
        new a.Skill12(),
        new a.Skill13(),
        new a.Skill14(),
        new a.Skill15(),
        new a.Skill16(),
        new a.Skill17(),
        new a.Skill18(),
        new a.Skill19(),
        new a.Skill20(),
        new a.Skill21(),
        new a.Skill22(),
        new a.Skill23(),
        new a.Skill24(),
        new a.Skill25(),
        new a.Skill26(),
        new a.Skill27(),
        new a.Skill28(),
        new a.Skill29(),
        new a.Skill30(),
    };
    private static Dictionary<int, SkillBase> s_map;
    private static Dictionary<int, SkillBase> Map
    {
        get
        {
            if (s_map != null) return s_map;
            s_map = new Dictionary<int, SkillBase>();
            foreach (var skill in s_list)
            {
                s_map[skill.Id] = skill;
            }
            return s_map;
        }
    }

    public static float GetSkillCD(int id)
    {
        return Map.TryGetValue(id, out var skill) ? skill.CD : 0f;
    }

    public static int GetSkillStore(int id)
    {
        return Map.TryGetValue(id, out var skill) ? skill.Store : 0;
    }

    public static SkillInfo.Quality GetSkillQuality(int id)
    {
        if (Tool.InfoManager != null &&
            id >= 0 &&
            id < Tool.InfoManager.SkillInfoList.Count &&
            Tool.InfoManager.SkillInfoList[id] != null)
        {
            return Tool.InfoManager.SkillInfoList[id].quality;
        }
        throw new System.Exception("非玩家技能不可获取quality:" + id);
    }

    public static int GetSkillWeight(int id)
    {
        if (Tool.InfoManager != null &&
            id >= 0 &&
            id < Tool.InfoManager.SkillInfoList.Count &&
            Tool.InfoManager.SkillInfoList[id] != null)
        {
            var weight = Tool.InfoManager.SkillInfoList[id].weight;
            if (weight == 0)
            {
                switch(GetSkillQuality(id))
                {
                    case SkillInfo.Quality.C:
                        weight = 3;
                        break;
                    case SkillInfo.Quality.B:
                        weight = 5;
                        break;
                    case SkillInfo.Quality.A:
                        weight = 10;
                        break;
                    case SkillInfo.Quality.S:
                        weight = 20;
                        break;
                }
            }
            return weight;
        }
        throw new System.Exception("非玩家技能不可获取weight:" + id);
    }

    public static bool GetSkillInfectious(int id)
    {
        if (Tool.InfoManager != null &&
            id >= 0 &&
            id < Tool.InfoManager.SkillInfoList.Count &&
            Tool.InfoManager.SkillInfoList[id] != null)
        {
            return Tool.InfoManager.SkillInfoList[id].infectionSkill;
        }
        throw new System.Exception("非玩家技能不可获取quality:" + id);
    }

    public static (Vector3, Vector3) DoDamageActs(int id, EntityData entity)
    {
        return DoDamageActs(id, entity, GetDefaultDest(entity));
    }
    public static (Vector3, Vector3) DoDamageActs(int id,EntityData entity, Vector3 dest)
    {
        if (a.PackageManager.TryDoDamageActs(id,entity, dest, out var output)) return output;
        if (b.PackageManager.TryDoDamageActs(id,entity, dest, out output)) return output;
        if (c.PackageManager.TryDoDamageActs(id,entity, dest, out output)) return output;
        throw new System.Exception("未知技能id：" + id);
    }
    private static Vector3 GetDefaultDest(EntityData entity)
    {
        BattleManager.EntityContainer.Entities.GetIdsInRange(entity.transform.position, Config.default_skill_auto_target_radius, s_targetBuffer);
        EntityData target = null;
        float sqrDistance = float.MaxValue;
        foreach (var entityId in s_targetBuffer)
        {
            var enemy = BattleManager.EntityContainer.Entities[entityId];
            if (entity.id == enemy.id) continue;
            float distance = Vector3.SqrMagnitude(enemy.transform.position - entity.transform.position);
            if (distance >= sqrDistance) continue;
            sqrDistance = distance;
            target = enemy;
        }
        s_targetBuffer.Clear();
        return target == null ? entity.transform.position + entity.transform.forward * Config.default_skill_target_distance : target.transform.position;
    }
    public static void PlayVFX(int id, Vector3 pos, Vector3 dest)
    {
        if (a.PackageManager.TryPlayVFX(id,pos, dest)) return;
        if (b.PackageManager.TryPlayVFX(id,pos, dest)) return;
        if (c.PackageManager.TryPlayVFX(id,pos, dest)) return;
        throw new System.Exception("未知技能id：" + id);
    }
}
