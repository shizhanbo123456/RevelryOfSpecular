using UnityEngine;

/// <summary>
/// 技能配置（ScriptableObject，以 Info 结尾）：只承载<b>展示用</b>数据 —— 技能 id / 名称 / 图标。
///
/// 【重要】行为参数（CD、库存、是否有武器显示、释放动作、伤害与轨迹）一律定义在 <see cref="Ros.Skill.SkillBase"/> 子类中，
/// 本类<b>不再重复声明</b>。历史上这里曾与 SkillBase 有 5 个同名字段（cd / store / ranged / hasWeaponDisplay / castAnim），
/// 二者同名却各存一份值，且本类那 5 个字段全项目零读取 —— 已全部删除，避免再出现"改了这个不生效"。
/// </summary>
[CreateAssetMenu(menuName = "Ros/SkillInfo", fileName = "SkillInfo")]
public class SkillInfo : ScriptableObject
{
    /// <summary>技能 id（与 SkillBase.Id 对应）。</summary>
    public int id;

    /// <summary>技能名（战斗 HUD 技能栏显示）。</summary>
    /// <remarks>字段名保留 skillName：不可改名为 name —— UnityEngine.Object 已有 name 成员，
    /// 同名会导致 CS0108（隐藏继承成员）并让序列化字段与 m_Name 混淆。</remarks>
    public string skillName = "";

    /// <summary>图标（战斗 HUD 技能栏显示）。</summary>
    public Sprite icon;
}
