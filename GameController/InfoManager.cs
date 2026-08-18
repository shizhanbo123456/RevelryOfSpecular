using info=Ros.Info;
using UnityEngine;
using System.Collections.Generic;
using System;

public class InfoManager:MonoBehaviour
{
    public readonly Dictionary<EntityType, float> EntityBarYOffsetMap = new();

    private void Awake()
    {
        Tool.InfoManager = this;
        InitEntityBarYOffsetMap();
    }
    public List<info.EntityAttributeInfo> CharacterInfoList;
    public info.EntityAttributeInfo NPCInfo;
    public info.EntityAttributeInfo PlantInfo;
    public info.EntityAttributeInfo InfectedPlantInfo;
    public List<info.EntityAttributeInfo> OreInfoList;
    public info.EntityAttributeInfo InfectedOreInfo;
    public List<info.EntityAttributeInfo> InfectionInfoList;
    [Space]
    public List<SkillInfo> SkillInfoList;
    [Space]
    public List<int> AttributeValueMax;
    public List<int> AttributeValueMin;
    [Space]
    [Header("Server Templates")]
    public GameObject ZombieCharacters;
    public GameObject ZombieNPCs;
    public GameObject Plants;
    public GameObject InfectedPlants;
    public GameObject Ores;
    public GameObject InfectedOres;
    public List<GameObject> Infections;
    [Space]
    [Header("Physics Layers")]
    [Tooltip("实体层级：客户端角色/NPC 模型的物理组件所在层（需在 Project Settings→Tags and Layers 中确认该层存在，建议命名 Entity）")]
    public int EntityLayer = 8;
    [Tooltip("技能释放位置检测用的 LayerMask：Inspector 中配置为可被射线命中的层（地面/场景障碍等，应取消勾选实体层）；代码会兜底排除实体层")]
    public LayerMask SkillTargetLayerMask = ~0;

    private void InitEntityBarYOffsetMap()
    {
        EntityBarYOffsetMap.Clear();
        RegisterBarOffsetRange(EntityType.CharacterFirst, Config.character_count, ZombieCharacters);
        RegisterBarOffsetRange(EntityType.NpcFirst, Config.npc_count, ZombieNPCs);
        RegisterBarOffsetRange(EntityType.PlantFirst, Config.plant_count, Plants);
        RegisterBarOffset(EntityType.InfectedPlant, InfectedPlants);
        RegisterBarOffsetRange(EntityType.OreFirst, Config.ore_count, Ores);
        RegisterBarOffset(EntityType.InfectedOre, InfectedOres);
        RegisterBarOffsetRange(EntityType.InfectionFirst, Infections);
    }

    private void RegisterBarOffsetRange(EntityType startType, int count, GameObject template)
    {
        for (int i = 0; i < count; i++)
        {
            RegisterBarOffset(new EntityType(startType.category, startType.value + i), template);
        }
    }

    private void RegisterBarOffsetRange(EntityType startType, List<GameObject> templates)
    {
        if (templates == null) return;
        for (int i = 0; i < templates.Count; i++)
        {
            RegisterBarOffset(new EntityType(startType.category, startType.value + i), templates[i]);
        }
    }

    private void RegisterBarOffsetRange(EntityType startType, List<MultiObjectSource> templates)
    {
        if (templates == null) return;
        for (int i = 0; i < templates.Count; i++)
        {
            GameObject template = null;
            var source = templates[i];
            if (source != null && source.Source != null && source.Source.Count > 0)
            {
                template = source.Source[0];
            }
            RegisterBarOffset(new EntityType(startType.category, startType.value + i), template);
        }
    }

    private void RegisterBarOffset(EntityType type, List<GameObject> templates)
    {
        GameObject template = templates == null || templates.Count <= 0 ? null : templates[0];
        RegisterBarOffset(type, template);
    }

    private void RegisterBarOffset(EntityType type, GameObject template)
    {
        float yOffset = 2f;
        if (template != null && template.TryGetComponent<EntityData>(out var entityData))
        {
            yOffset = entityData.GetBarYOffset();
        }
        EntityBarYOffsetMap[type] = yOffset;
    }

    public float GetEntityBarYOffset(EntityType type)
    {
        return EntityBarYOffsetMap.TryGetValue(type, out float yOffset) ? yOffset : 2f;
    }
}
