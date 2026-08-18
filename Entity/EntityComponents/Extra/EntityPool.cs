using Ros.Info;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class EntityPool
{


    private static Dictionary<int, ObjectPool<GameObject>> GraphicPools = new Dictionary<int, ObjectPool<GameObject>>();
    public static GameObject GetGraphic(EntityType type, Vector3 pos)
    {
        GameObject obj = null;
        if (type.IsCharacter())
        {
            obj = Tool.AssetsManager.ZombieCharacters[type.value];
        }
        else if (type.IsNpc())
        {
            obj = Tool.AssetsManager.ZombieNPCs[type.value];
        }
        else if (type.IsPlant())
        {
            var objs = Tool.AssetsManager.Plants[type.value];
            int v = RandomMapper.Random(GetPositionHash(pos), 0, objs.Source.Count - 1);
            obj = objs.Source[v];
        }
        else if (type.IsInfectedPlant())
        {
            var objs = Tool.AssetsManager.InfectedPlants;
            int v = RandomMapper.Random(GetPositionHash(pos), 0, objs.Count - 1);
            obj = Tool.AssetsManager.InfectedPlants[v];
        }
        else if (type.IsOre())
        {
            var objs = Tool.AssetsManager.Ores[type.value];
            int v = RandomMapper.Random(GetPositionHash(pos), 0, objs.Source.Count - 1);
            obj = objs.Source[v];
        }
        else if (type.IsInfectedOre())
        {
            var objs = Tool.AssetsManager.InfectedOres;
            int v = RandomMapper.Random(GetPositionHash(pos), 0, objs.Count - 1);
            obj = Tool.AssetsManager.InfectedOres[v];
        }
        else if (type.IsInfection())
        {
            obj = Tool.AssetsManager.Infections[type.value];
        }
        else
        {
            throw new System.Exception("未知物体类型:" + type + " " + pos);
        }
        int id = obj.GetInstanceID();
        if (GraphicPools.TryGetValue(id, out var pool))
        {
            var instance = pool.Get();
            EnsureGraphicPhysics(instance, type);
            return instance;
        }
        else
        {
            var p = new ObjectPool<GameObject>(() =>
            {
                var instance = Object.Instantiate(obj);
                instance.name = type + "_" + id;
                return instance;
            }, obj => obj.SetActive(true), obj => obj.SetActive(false));
            GraphicPools.Add(id, p);
            var instance = p.Get();
            EnsureGraphicPhysics(instance, type);
            return instance;
        }
    }

    /// <summary>
    /// 客户端角色/NPC 模型启用物理组件（仅这两类会移动，防穿墙需求范围内）：
    /// 1) 添加 Kinematic Rigidbody——不参与物理积分、不受力，位置仍由网络插值直接赋值，兼容现有移动逻辑；
    /// 2) 从 InfoManager 服务器模板读取胶囊碰撞体参数（EntityData.colliderInfo: bottom/top/radius），
    ///    在模型上添加/同步相同的 CapsuleCollider（center=两端中点，height=两端间距+2r，direction=Y）。
    /// 幂等：池复用实例时组件已存在，重复调用仅同步参数。
    /// </summary>
    private static void EnsureGraphicPhysics(GameObject instance, EntityType type)
    {
        if (!type.IsCharacter() && !type.IsNpc()) return;

        // 角色/NPC 模型放入实体层：物理检测（防穿墙约束、技能瞄准射线）通过 LayerMask 排除该层，避免命中实体自身或其它实体
        instance.layer = Tool.InfoManager.EntityLayer;

        if (!instance.TryGetComponent<Rigidbody>(out var rb))
        {
            rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var template = type.IsCharacter() ? Tool.InfoManager.ZombieCharacters : Tool.InfoManager.ZombieNPCs;
        if (template == null || !template.TryGetComponent<EntityData>(out var templateData)) return;
        var collider = templateData.colliderInfo;

        if (!instance.TryGetComponent<CapsuleCollider>(out var capsule))
        {
            capsule = instance.AddComponent<CapsuleCollider>();
        }
        capsule.direction = 1;   // Y 轴胶囊
        capsule.radius = collider.radius;
        capsule.height = (collider.top - collider.bottom) + 2f * collider.radius;
        capsule.center = Vector3.up * ((collider.bottom + collider.top) * 0.5f);
        capsule.isTrigger = false;
    }
    public static void ReturnGraphic(GameObject obj)
    {
        if (!TryGetGraphicPoolKey(obj.name, out int id))
        {
            Debug.LogError("Unknown graphic pool instance:" + obj.name);
            Object.Destroy(obj);
            return;
        }
        if (GraphicPools.TryGetValue(id, out var pool)) pool.Release(obj);
        else
        {
            Debug.LogError("Unknown graphic pool instance:" + obj.name);
            Object.Destroy(obj);
        }
    }

    private static int GetPositionHash(Vector3 pos)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Mathf.RoundToInt(pos.x * 100f);
            hash = hash * 31 + Mathf.RoundToInt(pos.y * 100f);
            hash = hash * 31 + Mathf.RoundToInt(pos.z * 100f);
            return hash;
        }
    }

    private static bool TryGetGraphicPoolKey(string objName, out int key)
    {
        int splitIndex = objName.LastIndexOf('_');
        if (splitIndex < 0 || splitIndex >= objName.Length - 1)
        {
            key = 0;
            return false;
        }
        return int.TryParse(objName.Substring(splitIndex + 1), out key);
    }

    //private static Dictionary<int, ObjectPool<GameObject>> TemplatePools = new Dictionary<int, ObjectPool<GameObject>>();
    public static EntityData GetTemplate(EntityType type)
    {
        GameObject obj = null;
        if (type.IsCharacter())
        {
            obj = Tool.InfoManager.ZombieCharacters;
        }
        else if (type.IsNpc())
        {
            obj = Tool.InfoManager.ZombieNPCs;
        }
        else if (type.IsPlant())
        {
            obj = Tool.InfoManager.Plants;
        }
        else if (type.IsInfectedPlant())
        {
            obj = Tool.InfoManager.InfectedPlants;
        }
        else if (type.IsOre())
        {
            obj = Tool.InfoManager.Ores;
        }
        else if (type.IsInfectedOre())
        {
            obj = Tool.InfoManager.InfectedOres;
        }
        else if (type.IsInfection())
        {
            obj = Tool.InfoManager.Infections[type.value];
        }
        else
        {
            throw new System.Exception("未知物体类型:" + type);
        }
        /*
        int id = obj.GetInstanceID();
        if (TemplatePools.TryGetValue(id, out var pool)) return pool.Get().GetComponent<EntityData>();
        else
        {
            var p = new ObjectPool<GameObject>(() => Object.Instantiate(obj), obj => obj.SetActive(true), obj => obj.SetActive(false));
            TemplatePools.Add(id, p);
            return p.Get().GetComponent<EntityData>();
        }*/
        return Object.Instantiate(obj).GetComponent<EntityData>();
    }
    public static void ReturnTemplate(EntityData entity)
    {
        /*
        var obj = entity.gameObject;
        int id = obj.GetInstanceID();
        if (TemplatePools.TryGetValue(id, out var pool)) pool.Release(obj);
        else
        {
            Debug.LogError("未知物体类型" + obj.name);
        }*/
        Object.Destroy(entity.gameObject);
    }


    public static EntityAttributeInfo GetAttributeInfo(EntityType type)
    {
        if (type.IsCharacter())
        {
            return Tool.InfoManager.CharacterInfoList[type.value];
        }
        else if (type.IsNpc())
        {
            return Tool.InfoManager.NPCInfo;
        }
        else if (type.IsPlant())
        {
            return Tool.InfoManager.PlantInfo;
        }
        else if (type.IsInfectedPlant())
        {
            return Tool.InfoManager.InfectedPlantInfo;
        }
        else if (type.IsOre())
        {
            return Tool.InfoManager.OreInfoList[type.value];
        }
        else if (type.IsInfectedOre())
        {
            return Tool.InfoManager.InfectedOreInfo;
        }
        else
        {
            return Tool.InfoManager.InfectionInfoList[type.value];
        }
    }
}
