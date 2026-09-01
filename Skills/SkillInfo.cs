using UnityEngine;

/// <summary>
/// 技能配置（ScriptableObject，以 Info 结尾）。
/// 技能统一模型见策划案 11.1：武器显示（可选）/ 释放动作（可配置）/ 释放效果（必须）。
/// 技能包具体实现待后续完善，本类仅承载静态配置。
/// </summary>
[CreateAssetMenu(menuName = "Ros/SkillInfo", fileName = "SkillInfo")]
public class SkillInfo : ScriptableObject
{
    /// <summary>技能类别。</summary>
    public enum SkillType
    {
        /// <summary>武器（进攻方技能必须绑定武器并显示；防守方局内获取的武器）。</summary>
        Weapon,
        /// <summary>角色主动技能（防守方：无武器显示）。</summary>
        ActiveSkill,
        /// <summary>大招（防守方：无武器显示，长冷却）。</summary>
        Ultimate,
    }

    /// <summary>技能 id（与 SkillBase.Id 对应）。</summary>
    public int id;

    /// <summary>技能名。</summary>
    public string skillName = "";

    /// <summary>图标（技能列表 UI）。</summary>
    public Sprite icon;

    /// <summary>类别。</summary>
    public SkillType type = SkillType.Weapon;

    /// <summary>是否远程/施法类（决定右键是否可触发；近战武器/徒手=false）。</summary>
    public bool ranged = true;

    /// <summary>是否有武器显示（悬浮武器）。</summary>
    public bool hasWeaponDisplay = true;

    /// <summary>释放动作（0=None）。</summary>
    public EntityAnim.AttackType castAnim;

    /// <summary>CD（秒）。</summary>
    public float cd = 5f;

    /// <summary>库存（-1 无限制）。</summary>
    public int store = -1;

    /// <summary>武器初始等级（0 未升级）。</summary>
    public int startWeaponLevel;
}
