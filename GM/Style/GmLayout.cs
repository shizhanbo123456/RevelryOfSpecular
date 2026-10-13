using System;
using System.Collections.Generic;
using Ros.Skill;

namespace Ros.Gm
{
    public enum LayoutType
    {
        Label,
        Button,
        IntInput,
        FloatInput,
        IntSlider,
        FloatSlider,
        Dropdown,
    }
    public abstract class LayoutInfo
    {
        public abstract LayoutType LayoutType { get; }
    }
    public class Label : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.Label;
        public string left;
        public string right;
        public Label(string left, string right)
        {
            this.left = left;
            this.right = right;
        }
    }
    public class Button : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.Button;
        public string left;
        public string buttonTitle;
        public Button(string left, string buttonTitle)
        {
            this.left = left;
            this.buttonTitle = buttonTitle;
        }
    }
    public class InputInt : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.IntInput;
        public string left;
        public InputInt(string left)
        {
            this.left = left;
        }
    }
    public class FloatInput : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.FloatInput;
        public string left;
        public FloatInput(string left)
        {
            this.left = left;
        }
    }
    public class IntSlider : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.IntSlider;
        public string left;
        public int min;
        public int max;
        public IntSlider(string left, int min, int max)
        {
            this.left = left;
            this.min = min;
            this.max = max;
        }
    }
    public class FloatSlider : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.FloatSlider;
        public string left;
        public float min;
        public float max;
        public FloatSlider(string left, float min, float max)
        {
            this.left = left;
            this.min = min;
            this.max = max;
        }
    }
    public class Dropdown : LayoutInfo
    {
        public override LayoutType LayoutType => LayoutType.Dropdown;
        public string left;
        public Type enumType;
        public Dropdown(string left, Type enumType)
        {
            this.left = left;
            this.enumType = enumType;
        }
    }

    public static class Layout
    {
        private static Dictionary<GmEnum, List<LayoutInfo>> layoutStyle=new();
        static Layout()
        {
            // ===== 对局 =====
            layoutStyle.Add(GmEnum.Battle_RemainTime_Add, new()
            {
                new FloatInput("时间进度"),
                new Button(string.Empty,"应用增加时间")
            });
            layoutStyle.Add(GmEnum.Battle_RemainTime_Reduce, new()
            {
                new FloatInput("时间进度"),
                new Button(string.Empty, "应用减少时间")
            });
            layoutStyle.Add(GmEnum.Battle_RemainTime_Set, new()
            {
                new FloatSlider("时间进度", 0f, 1f),
                new Button(string.Empty, "应用设置时间")
            });
            layoutStyle.Add(GmEnum.Battle_Settle, new()
            {
                new Dropdown("结算方式", typeof(GmSettleType)),
                new Button(string.Empty, "立即结算")
            });
            layoutStyle.Add(GmEnum.Battle_DayNight_SetTimePoint, new()
            {
                new FloatSlider("天时间点", 0f, 1f),
                new Button(string.Empty, "应用")
            });
            layoutStyle.Add(GmEnum.Battle_DayNight_SetDayDuration, new()
            {
                new FloatInput("白天时长(秒)"),
                new Button(string.Empty, "应用")
            });
            layoutStyle.Add(GmEnum.Battle_DayNight_SetNightDuration, new()
            {
                new FloatInput("晚上时长(秒)"),
                new Button(string.Empty, "应用")
            });

            // ===== 实体 =====
            layoutStyle.Add(GmEnum.Entity_Spawn, new()
            {
                new Dropdown("单位类型", typeof(GmEntityType)),
                new InputInt("数量"),
                new Button(string.Empty, "刷新")
            });
            layoutStyle.Add(GmEnum.Entity_Kill, new()
            {
                new Dropdown("目标类型", typeof(GmEntityType)),
                new Dropdown("筛选方式", typeof(GmFilterType)),
                new FloatInput("数量/半径"),
                new Button(string.Empty, "击杀")
            });
            layoutStyle.Add(GmEnum.Entity_Knockback, new()
            {
                new FloatInput("水平力度"),
                new FloatInput("垂直力度"),
                new Dropdown("筛选方式", typeof(GmFilterType)),
                new FloatInput("数量/半径"),
                new Button(string.Empty, "击飞")
            });

            // ===== 玩家 =====
            layoutStyle.Add(GmEnum.Player_Teleport_ToCoordinate, new()
            {
                new FloatInput("X"),
                new FloatInput("Z"),
                new Button(string.Empty, "传送")
            });
            layoutStyle.Add(GmEnum.Player_Teleport_ToGuardPoint, new()
            {
                new Dropdown("守护点", typeof(GmBeacon)),
                new Button(string.Empty, "传送")
            });
            layoutStyle.Add(GmEnum.Player_Revive, new()
            {
                new Button(string.Empty, "立即复活")
            });
            layoutStyle.Add(GmEnum.Player_Skill_GetSkill, new()
            {
                new Dropdown("技能", typeof(SkillType)),
                new Button(string.Empty, "获得")
            });
            layoutStyle.Add(GmEnum.Player_Skill_DirectUseSkill, new()
            {
                new Dropdown("技能", typeof(SkillType)),
                new Button(string.Empty, "使用")
            });
            layoutStyle.Add(GmEnum.Player_Skill_ResetCd, new()
            {
                new Button(string.Empty, "CD回满")
            });
            layoutStyle.Add(GmEnum.Player_Skill_ClearSkill, new()
            {
                new Button(string.Empty, "清空技能")
            });
            layoutStyle.Add(GmEnum.Player_Skill_PassiveEnable, new()
            {
                new Button(string.Empty, "被动1"),
                new Button(string.Empty, "被动2"),
                new Button(string.Empty, "被动3"),
                new Button(string.Empty, "被动4"),
                new Button(string.Empty, "被动5")
            });
            layoutStyle.Add(GmEnum.Player_Buff_AddBuff, new()
            {
                new Dropdown("buff", typeof(EffectType)),
                new Button(string.Empty, "添加")
            });
            layoutStyle.Add(GmEnum.Player_Buff_RemoveBuff, new()
            {
                new Dropdown("buff", typeof(EffectType)),
                new Button(string.Empty, "移除")
            });
            layoutStyle.Add(GmEnum.Player_Buff_RemoveAllBuff, new()
            {
                new Button(string.Empty, "移除全部")
            });
            layoutStyle.Add(GmEnum.Player_Cheat_Flight, new()
            {
                new Button(string.Empty, "飞行")
            });
            layoutStyle.Add(GmEnum.Player_Cheat_Invincible, new()
            {
                new Button(string.Empty, "无敌")
            });
            layoutStyle.Add(GmEnum.Player_Cheat_Speed, new()
            {
                new Button(string.Empty, "极速")
            });
            layoutStyle.Add(GmEnum.Player_Cheat_IgnoreCd, new()
            {
                new Button(string.Empty, "无视CD")
            });

            // ===== 画面 =====
            layoutStyle.Add(GmEnum.Screen_Terrain_ShowRadius, new()
            {
                new FloatInput("半径"),
                new Button(string.Empty, "应用")
            });
            layoutStyle.Add(GmEnum.Screen_Terrain_ToggleTerrain, new()
            {
                new Button(string.Empty, "显隐地形")
            });
            layoutStyle.Add(GmEnum.Screen_Fog, new()
            {
                new Button(string.Empty, "显隐雾气")
            });

            // ===== 存档 =====
            layoutStyle.Add(GmEnum.Save_PlayerLevel_AddExp, new()
            {
                new InputInt("经验"),
                new Button(string.Empty, "增加")
            });
            layoutStyle.Add(GmEnum.Save_PlayerLevel_ReduceExp, new()
            {
                new InputInt("经验"),
                new Button(string.Empty, "减少")
            });
            layoutStyle.Add(GmEnum.Save_PlayerLevel_AddLevel, new()
            {
                new InputInt("等级"),
                new Button(string.Empty, "增加")
            });
            layoutStyle.Add(GmEnum.Save_PlayerLevel_ReduceLevel, new()
            {
                new InputInt("等级"),
                new Button(string.Empty, "减少")
            });
            layoutStyle.Add(GmEnum.Save_CharacterLevel, new()
            {
                new Dropdown("角色", typeof(GmCharacter)),
                new InputInt("等级"),
                new Button(string.Empty, "设置")
            });

            // ===== 其它 =====
            layoutStyle.Add(GmEnum.Other_Settlement, new()
            {
                new Button(string.Empty, "演示结算")
            });
            layoutStyle.Add(GmEnum.Other_Debug, new()
            {
                new Label("FPS", string.Empty),
                new Label("实体数量", string.Empty),
                new Label("延迟", string.Empty),
                new Label("客户端时间", string.Empty),
                new Button(string.Empty, "刷新")
            });
            layoutStyle.Add(GmEnum.Other_Collect_MeshFaces, new()
            {
                new Button(string.Empty, "刷新"),
                new Label("模型网格面数", string.Empty)
            });
            layoutStyle.Add(GmEnum.Other_Collect_ServerFps, new()
            {
                new Button(string.Empty, "刷新"),
                new Label("服务器帧率", string.Empty)
            });
            layoutStyle.Add(GmEnum.Other_Collect_ServerTime, new()
            {
                new Button(string.Empty, "刷新"),
                new Label("服务器时间", string.Empty)
            });
            layoutStyle.Add(GmEnum.Other_EnemyDialog, new()
            {
                new Button(string.Empty, "敌方对话")
            });
        }
        public static List<LayoutInfo> GetLayout(this GmEnum type) => layoutStyle[type];
    }
}