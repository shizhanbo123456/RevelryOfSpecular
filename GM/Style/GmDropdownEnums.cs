namespace Ros.Gm
{
    // GM 下拉框专用枚举：命名一律以 Gm 开头，按类别控粒度（不按实例）

    // 实体类型（刷新 / 击杀）
    public enum GmEntityType
    {
        [GmLabel("普通僵尸")]
        Zombie,
        [GmLabel("精英僵尸")]
        EliteZombie,
        [GmLabel("珊瑚")]
        Coral,
        [GmLabel("中立单位")]
        Neutral,
        [GmLabel("水晶雕像")]
        CrystalStatue,
    }

    // 立即结算方式
    public enum GmSettleType
    {
        [GmLabel("时间结束")]
        TimeEnd,
        [GmLabel("进攻方团灭")]
        Annihilate,
    }

    // 目标筛选方式
    public enum GmFilterType
    {
        [GmLabel("所有")]
        All,
        [GmLabel("最近")]
        Nearest,
        [GmLabel("n个")]
        Count,
        [GmLabel("r米半径内")]
        Radius,
    }

    // 雕像（3 外围 + 1 核心）
    public enum GmStatue
    {
        [GmLabel("贤者雕像1")]
        Outer0,
        [GmLabel("贤者雕像2")]
        Outer1,
        [GmLabel("贤者雕像3")]
        Outer2,
        [GmLabel("核心雕像")]
        Core,
    }

    // 角色（9 攻击 + 6 防守）
    public enum GmCharacter
    {
        [GmLabel("攻击角色0")]
        Attack0,
        [GmLabel("攻击角色1")]
        Attack1,
        [GmLabel("攻击角色2")]
        Attack2,
        [GmLabel("攻击角色3")]
        Attack3,
        [GmLabel("攻击角色4")]
        Attack4,
        [GmLabel("攻击角色5")]
        Attack5,
        [GmLabel("攻击角色6")]
        Attack6,
        [GmLabel("攻击角色7")]
        Attack7,
        [GmLabel("攻击角色8")]
        Attack8,
        [GmLabel("白眼伯爵")]
        Defense0,
        [GmLabel("瘟疫使者")]
        Defense1,
        [GmLabel("蒙面教皇")]
        Defense2,
        [GmLabel("鹿铠怪人")]
        Defense3,
        [GmLabel("苍白舞者")]
        Defense4,
        [GmLabel("死灵漫步者")]
        Defense5,
    }
}
