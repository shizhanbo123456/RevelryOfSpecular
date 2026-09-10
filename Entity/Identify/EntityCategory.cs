/// <summary>
/// 实体类别（V0.8 非对称攻防）。
/// 进攻方角色 / 防守方角色 / 僵尸 / 精英僵尸 / 守护点 / 水晶 / 防御塔 / 瘟疫树。
/// 注意：不存在蘑菇实体——「蘑菇感染」是水晶上的 Buff（服务器只存 Buff 剩余时间），
/// 客户端按同步 Buff 显隐切换水晶/蘑菇模型（见 ClientDisplayManager）。
/// </summary>
public enum EntityCategory
{
    /// <summary>进攻方角色。</summary>
    Character_Attack,
    /// <summary>防守方角色。</summary>
    Character_Defense,
    /// <summary>普通僵尸。</summary>
    Zombie,
    /// <summary>精英僵尸。</summary>
    EliteZombie,
    /// <summary>守护点（瘟疫信标）。</summary>
    Beacon,
    /// <summary>可采集水晶。</summary>
    Crystal,
    /// <summary>防御塔（瘟疫孢子）。</summary>
    Tower,
    /// <summary>瘟疫树（中立争抢单位）。</summary>
    PlagueTree,
    /// <summary>场景道具/其它。</summary>
    Prop,
}
