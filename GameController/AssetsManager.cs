using System.Collections.Generic;
using UnityEngine;

public class AssetsManager : MonoBehaviour
{
    private void Awake()
    {
        if(Application.platform == RuntimePlatform.WindowsServer)
        {
            throw new System.Exception("Assets Manager can't be used on server");
        }
        Tool.AssetsManager=this;
    }
    [Header("Human")]
    public List<GameObject> ZombieCharacters;
    public List<GameObject> ZombieNPCs;
    [Header("PlantsAndOres")]
    public List<MultiObjectSource> Plants;
    public List<GameObject> InfectedPlants;
    public List<MultiObjectSource> Ores;
    public List<GameObject> InfectedOres;
    public List<GameObject> Infections;
    [Header("VFX")]
    public List<GameObject> SkillVFX;
    /*
    深红：0生命流失、1瞬间流失
    品红：2狂化、3致命
    浅红：4生命恢复、5瞬间恢复
    深蓝：6移速降低
    浅蓝：18冰冻
    青色：7速度提升
    浅橙色：8体力恢复、9防御提升、10燃烧
    深橙色：11体力流失、12防御降低
    白色：13净化
    深绿：14中毒
    粉色：15瘟疫
    红色爆发：16爆炸
    17大范围白色雾气
    */
    public List<GameObject> Dusts;
    /*
    蓝绿紫红黄
    爆炸0-4
    星星5-9
    闪耀轨迹10-14
    */
    public List<GameObject> Projections;
    [Space]
    public GameObject CharacterUnlockProp;
    public GameObject PortMax;
    public GameObject PortNormal;
    public GameObject PortMin;
    [Header("Others")]
    public List<Sprite> CharacterIcons;
    public List<Sprite> ImprintIcons;
}
