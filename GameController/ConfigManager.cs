using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Ros.Skill;
using UnityEngine;

// 配表读取：Assets/StreamingAssets/ConfigData/*.csv
// 全部按列号读写，不认列名；改数据值、改行顺序不需要改代码，改列（增删/换序/改名）需同步改本文件
[DefaultExecutionOrder(-200)]
public class ConfigManager : MonoBehaviour
{
    private const string ConfigDirName = "ConfigData";

    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private void Awake()
    {
        Tool.ConfigManager = this;
        Load();
    }

    public void Load()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, ConfigDirName);

        int battleFury = BattleFuryLayerConfig.Load(ReadTable(dir, BattleFuryLayerConfig.FileName));
        int entity = EntityConfig.Load(ReadTable(dir, EntityConfig.FileName));
        int skill = SkillConfig.Load(ReadTable(dir, SkillConfig.FileName));
        int effect = EffectConfig.Load(ReadTable(dir, EffectConfig.FileName));
        int expCharacter = ExpCharacterConfig.Load(ReadTable(dir, ExpCharacterConfig.FileName));
        int expPlayer = ExpPlayerConfig.Load(ReadTable(dir, ExpPlayerConfig.FileName));
        int revive = ReviveTimeConfig.Load(ReadTable(dir, ReviveTimeConfig.FileName));
        int global = GlobalVariableConfig.Load(ReadTable(dir, GlobalVariableConfig.FileName));
        int notice = NoticeConfig.Load(ReadTable(dir, NoticeConfig.FileName));
        int attrRange = AttrRangeConfig.Load(ReadTable(dir, AttrRangeConfig.FileName));
        int settingTips = SettingTipsConfig.Load(ReadTable(dir, SettingTipsConfig.FileName));

        Debug.Log($"[配表] {dir} 加载完成 | battle_fury_layer {battleFury} | entity {entity} | skill {skill} | effect {effect} | " +
                  $"exp_character {expCharacter} | exp_player {expPlayer} | revive_time {revive} | global_variables {global} | notice {notice} | attr_range {attrRange} | setting_tips {settingTips}");
    }

    #region 文件读取与拆分
    private static ConfigTable ReadTable(string dir, string fileName)
    {
        string path = Path.Combine(dir, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"[配表] 找不到文件 {path}", path);
        return new ConfigTable(fileName, SplitLines(ReadText(path)));
    }

    private static string ReadText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;

        try
        {
            return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            // 现有配表为 GBK：UTF-8 严格解码失败后按 GBK 读
            try
            {
                return Encoding.GetEncoding(936).GetString(bytes, offset, bytes.Length - offset);
            }
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException)
            {
                throw new FormatException($"[配表] {path} 无法解码：文件既不是 UTF-8 也不是 GBK，或当前运行时不支持 936 代码页", e);
            }
        }
    }

    private static List<string[]> SplitLines(string text)
    {
        var rows = new List<string[]>();

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0) continue;

            string[] cells = line.Split(',');
            bool allEmpty = true;
            foreach (string cell in cells)
            {
                if (cell.Trim().Length == 0) continue;
                allEmpty = false;
                break;
            }
            if (allEmpty) continue; // 配表用全空行做分组，跳过
            rows.Add(cells);
        }

        return rows;
    }
    #endregion

    #region 单元格解析（按列号取，列号含义由各表字段顺序决定）
    public static string ParseString(string[] rowData, int col) => Cell(rowData, col);

    public static int ParseInt(string[] rowData, int col)
    {
        string raw = Cell(rowData, col);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"第 {col} 列不是整数：「{raw}」");
        return value;
    }

    public static int? ParseIntOrNull(string[] rowData, int col)
    {
        string raw = Cell(rowData, col);
        if (raw.Length == 0) return null;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"第 {col} 列不是整数：「{raw}」");
        return value;
    }

    public static float ParseFloat(string[] rowData, int col)
    {
        string raw = Cell(rowData, col);
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            throw new FormatException($"第 {col} 列不是数值：「{raw}」");
        return value;
    }

    public static EntityCategory ParseCategory(string[] rowData, int col)
    {
        int raw = ParseInt(rowData, col);
        if (!Enum.IsDefined(typeof(EntityCategory), raw))
            throw new FormatException($"第 {col} 列的实体类型 {raw} 未在 EntityCategory 中定义");
        return (EntityCategory)raw;
    }

    // 升级路线只允许 LevelUpType 的 0~4（生命/力量/魔法/暴击率/暴击伤害）
    public static int ParseUpgrade(string[] rowData, int col)
    {
        int value = ParseInt(rowData, col);
        if (value < 0 || value > 4)
            throw new FormatException($"第 {col} 列的升级类型 {value} 超出 0~4");
        return value;
    }

    // 品质档只允许 0~2（B/A/S），与 RoleHead 的 m_quality 页序一致
    public static int ParseQuality(string[] rowData, int col)
    {
        int value = ParseInt(rowData, col);
        if (value < 0 || value > 2)
            throw new FormatException($"第 {col} 列的角色品质 {value} 超出 0~2（0:B / 1:A / 2:S）");
        return value;
    }

    // 技能攻击动画：空/0 = 无动画，其余必须是 EntityAnim.AttackType 里定义的值
    public static int ParseAttackAnim(string[] rowData, int col)
    {
        int value = ParseIntOrNull(rowData, col) ?? 0;
        if (value == 0) return 0;
        if (!Enum.IsDefined(typeof(EntityAnim.AttackType), value))
            throw new FormatException($"第 {col} 列的攻击动画 {value} 未在 EntityAnim.AttackType 中定义");
        return (int)(EntityAnim.AttackType)value;
    }

    // Buff 特效类别：留空 = 不播特效，其余必须是 SkillVfxKind 里定义的值
    public static SkillVfxKind? ParseVfxKind(string[] rowData, int col)
    {
        int? raw = ParseIntOrNull(rowData, col);
        if (!raw.HasValue) return null;
        if (!Enum.IsDefined(typeof(SkillVfxKind), raw.Value))
            throw new FormatException($"第 {col} 列的特效类型 {raw.Value} 未在 SkillVfxKind 中定义");
        return (SkillVfxKind)raw.Value;
    }

    public static float? ParseFloatOrNull(string[] rowData, int col)
    {
        string raw = Cell(rowData, col);
        if (raw.Length == 0) return null;
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            throw new FormatException($"第 {col} 列不是数值：「{raw}」");
        return value;
    }

    public static bool? ParseBoolOrNull(string[] rowData, int col)
    {
        string raw = Cell(rowData, col);
        return raw switch
        {
            "" => null,
            "1" => true,
            "0" => false,
            _ => throw new FormatException($"第 {col} 列只能填 1/0 或留空：「{raw}」"),
        };
    }

    public static List<int> ParseIntList(string[] rowData, int col)
    {
        var result = new List<int>();
        string raw = Cell(rowData, col);
        if (raw.Length == 0) return result;

        foreach (string token in raw.Split('|'))
        {
            if (!int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException($"第 {col} 列含非法整数：「{token}」");
            result.Add(value);
        }
        return result;
    }

    public static List<float> ParseFloatList(string[] rowData, int col)
    {
        var result = new List<float>();
        string raw = Cell(rowData, col);
        if (raw.Length == 0) return result;

        foreach (string token in raw.Split('|'))
        {
            if (!float.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new FormatException($"第 {col} 列含非法数值：「{token}」");
            result.Add(value);
        }
        return result;
    }

    // 多个 Buff 用 | 分隔，单个 Buff 为 类型;等级;持续时间
    public static List<BuffEntry> ParseBuffList(string[] rowData, int col)
    {
        var result = new List<BuffEntry>();
        string raw = Cell(rowData, col);
        if (raw.Length == 0) return result;

        foreach (string group in raw.Split('|'))
        {
            string[] parts = group.Split(';');
            if (parts.Length != 3)
                throw new FormatException($"第 {col} 列的 Buff 需为 类型;等级;持续时间：「{group}」");

            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type) ||
                !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) ||
                !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
                throw new FormatException($"第 {col} 列的 Buff 数值非法：「{group}」");

            result.Add(new BuffEntry { type = type, level = level, duration = duration });
        }
        return result;
    }

    private static string Cell(string[] rowData, int col)
    {
        if (col < 0 || col >= rowData.Length)
            throw new FormatException($"缺少第 {col} 列（该行只有 {rowData.Length} 列）");
        return rowData[col].Trim();
    }

    internal static int Fill<TKey, TRow>(ConfigTable table, Dictionary<TKey, TRow> content,
        Func<string[], TRow> parse, Func<TRow, TKey> keyOf)
    {
        content.Clear();

        for (int i = 0; i < table.rows.Count; i++)
        {
            string[] rowData = table.RequireRow(i);

            TRow item;
            try
            {
                item = parse(rowData);
            }
            catch (FormatException e)
            {
                throw new FormatException($"[配表] {table.fileName} 第 {i + 2} 行：{e.Message}", e);
            }

            TKey key = keyOf(item);
            if (content.ContainsKey(key))
                throw new FormatException($"[配表] {table.fileName} 第 {i + 2} 行：主键「{key}」重复");
            content.Add(key, item);
        }

        return content.Count;
    }

    internal static TRow GetRequired<TKey, TRow>(Dictionary<TKey, TRow> content, TKey key, string fileName)
    {
        if (content.TryGetValue(key, out TRow row)) return row;

        throw new FormatException($"[配表] {fileName} 中没有主键「{key}」，请检查配表是否漏配");
    }

    // 超出配表范围时退到不大于 key 的最大主键（如复活次数超过表尾、死亡次数超过层数表）
    internal static TRow GetClampedRow<TRow>(Dictionary<int, TRow> content, int key, string fileName)
    {
        if (content.TryGetValue(key, out TRow row)) return row;

        for (int k = key - 1; k >= 0; k--)
            if (content.TryGetValue(k, out row)) return row;

        Debug.LogError($"[配表] {fileName} 中没有主键「{key}」，也没有更小的主键，请检查配表");
        return default;
    }
    #endregion

    #region 愈战愈勇层数表：0 层数 / 1 死亡次数 / 2 buff层数
    public struct BattleFuryLayerConfig
    {
        public const string FileName = "battle_fury_layer.csv";
        private const int ColumnCount = 3;
        public static readonly Dictionary<int, BattleFuryLayerConfig> content = new();

        public int key;
        public int deathCount;
        public int buffLayerCount;

        public BattleFuryLayerConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            deathCount = ParseInt(rowData, 1);
            buffLayerCount = ParseInt(rowData, 2);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new BattleFuryLayerConfig(row), row => row.key);
        }

        public static BattleFuryLayerConfig Get(int key) => GetRequired(content, key, FileName);

        public static BattleFuryLayerConfig GetClamped(int key) => GetClampedRow(content, key, FileName);
    }
    #endregion

    #region 角色信息表：0 实体类型 / 1 副键 / 2 姓名 / 3 生命 …… 14 初始技能列表 / 15 品质
    public struct EntityConfig
    {
        public const string FileName = "entity.csv";
        private const int ColumnCount = 16;
        public static readonly Dictionary<EntityType, EntityConfig> content = new();

        public EntityCategory category;
        public int value;
        public string name;
        public float health;
        public int strength;
        public int magic;
        public int critRate;
        public int critDamage;
        public float viewDistance;
        public int weaponSlotCount;
        public int upgradeA;
        public int upgradeB;
        public int upgradeC;
        public int unlockLevel;
        public List<int> initialSkills;
        public int quality;           // 0:B / 1:A / 2:S，直接对应 RoleHead 的 m_quality 页序

        public EntityConfig(string[] rowData)
        {
            category = ParseCategory(rowData, 0);
            value = ParseInt(rowData, 1);
            name = ParseString(rowData, 2);
            health = ParseFloat(rowData, 3);
            strength = ParseInt(rowData, 4);
            magic = ParseInt(rowData, 5);
            critRate = ParseInt(rowData, 6);
            critDamage = ParseInt(rowData, 7);
            viewDistance = ParseFloat(rowData, 8);
            weaponSlotCount = ParseInt(rowData, 9);
            upgradeA = ParseUpgrade(rowData, 10);
            upgradeB = ParseUpgrade(rowData, 11);
            upgradeC = ParseUpgrade(rowData, 12);
            unlockLevel = ParseInt(rowData, 13);
            initialSkills = ParseIntList(rowData, 14);
            quality = ParseQuality(rowData, 15);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new EntityConfig(row), row => new EntityType(row.category, row.value));
        }

        public static EntityConfig Get(EntityType type) => GetRequired(content, type, FileName);

        // 一行覆盖整类：先按类别规则归一化副键，再精确查；查不到直接抛，不做任何兜底
        public static EntityConfig GetByCategory(EntityType type)
        {
            var key = new EntityType(type.category, NormalizeValue(type));
            if (content.TryGetValue(key, out EntityConfig config)) return config;

            throw new FormatException($"[配表] {FileName} 缺少实体类型 ({key.category}, {key.value})：{type} 归一化后无对应行");
        }

        private static int NormalizeValue(EntityType type)
        {
            switch (type.category)
            {
                case EntityCategory.Zombie:
                case EntityCategory.EliteZombie:
                case EntityCategory.CrystalStatue:
                    return 0;
                case EntityCategory.GuardStatue:
                    return type.value < Config.sage_statue_count ? 0 : Config.sage_statue_count;
                default:
                    return type.value;
            }
        }

        public EntityAttribute GetAttribute(int level)
        {
            var attr = new EntityAttribute
            {
                health = health,
                strength = strength,
                magic = magic,
                critRate = critRate,
                critDamage = critDamage,
                viewDistance = viewDistance,
                weaponSlotCount = weaponSlotCount,
            };

            level = Mathf.Max(1, level);
            for (int lv = 2; lv <= level; lv++)
            {
                int step = (lv - 2) % 3;
                int upgrade = step == 0 ? upgradeA : step == 1 ? upgradeB : upgradeC;
                switch (upgrade)
                {
                    case 0: attr.health += attr.health * GlobalVariableConfig.attr_growth_health; break;
                    case 1: attr.strength += (int)(attr.strength * GlobalVariableConfig.attr_growth_strength); break;
                    case 2: attr.magic += (int)(attr.magic * GlobalVariableConfig.attr_growth_magic); break;
                    case 3: attr.critRate += GlobalVariableConfig.attr_growth_critRate; break;
                    case 4: attr.critDamage += GlobalVariableConfig.attr_growth_critDamage; break;
                }
            }
            return attr;
        }
    }
    #endregion

    #region 技能表
    public struct SkillConfig
    {
        public const string FileName = "skill.csv";
        private const int ColumnCount = 25;
        public static readonly Dictionary<int, SkillConfig> content = new();

        public int key;
        public string name;
        public string description;
        public int? iconIndex;
        public string quality;
        public string skillType;
        public string skillTag;
        public float cd;
        public int storeLimit;
        public int? weaponCategory;
        public int? weaponSubId;
        public int? valueSource;
        public float? knockback;
        public List<BuffEntry> buffs;
        public List<float> bulletSizes; // 第15列：有特效=缩放倍率（1=原大小），无特效近战=判定半径
        public float damageInterval;     // 第16列：同一目标最短再伤害间隔（秒）；留空 = 100（只结算一次）
        public List<float> damageRates;  // 第17列：伤害倍率，空 = 全 1；1 项 = 所有段共用；多项 = 按段序
        public int attackAnim;           // 第18列：EntityAnim.AttackType 数值，空 = 0 = 无动画
        public List<int> scores;         // 第19~24列：输出/冷却恢复/控制/支援/攻击范围/攻击速度，0~4 = D/C/B/A/S
        public bool broadcast;           // 第25列：使用时是否全场播报，1 = 播报，空/0 = 不播报

        public SkillConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            name = ParseString(rowData, 1);
            description = ParseString(rowData, 2);
            iconIndex = ParseIntOrNull(rowData, 3);
            quality = ParseString(rowData, 4);
            skillType = ParseString(rowData, 5);
            skillTag = ParseString(rowData, 6);
            cd = ParseFloat(rowData, 7);
            storeLimit = ParseInt(rowData, 8);
            weaponCategory = ParseIntOrNull(rowData, 9);
            weaponSubId = ParseIntOrNull(rowData, 10);
            valueSource = ParseIntOrNull(rowData, 11);
            knockback = ParseFloatOrNull(rowData, 12);
            buffs = ParseBuffList(rowData, 13);
            bulletSizes = ParseFloatList(rowData, 14);
            damageInterval = ParseFloatOrNull(rowData, 15) ?? 100f;
            damageRates = ParseFloatList(rowData, 16);
            attackAnim = ParseAttackAnim(rowData, 17);
            scores = new List<int>
            {
                ParseIntOrNull(rowData, 18) ?? 0,
                ParseIntOrNull(rowData, 19) ?? 0,
                ParseIntOrNull(rowData, 20) ?? 0,
                ParseIntOrNull(rowData, 21) ?? 0,
                ParseIntOrNull(rowData, 22) ?? 0,
                ParseIntOrNull(rowData, 23) ?? 0,
            };
            broadcast = ParseBoolOrNull(rowData, 24) ?? false;
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new SkillConfig(row), row => row.key);
        }

        public static SkillConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion

    #region Buff 表
    public struct EffectConfig
    {
        public const string FileName = "effect.csv";
        private const int ColumnCount = 12;
        public static readonly Dictionary<int, EffectConfig> content = new();

        public int key;
        public string name;
        public string description;
        public int? stackMode;
        public bool? lockMoveInput;
        public bool? lockSkillInput;
        public bool? freezeAnimation;
        public int? superArmorOverride;
        public bool? positive;
        public int? specialCalculate;
        public SkillVfxKind? vfxKind;  // 第11列：持续表现的特效类别，留空 = 不播特效
        public int? vfxIndex;          // 第12列：该类别下的特效下标

        public EffectConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            name = ParseString(rowData, 1);
            description = ParseString(rowData, 2);
            stackMode = ParseIntOrNull(rowData, 3);
            lockMoveInput = ParseBoolOrNull(rowData, 4);
            lockSkillInput = ParseBoolOrNull(rowData, 5);
            freezeAnimation = ParseBoolOrNull(rowData, 6);
            superArmorOverride = ParseIntOrNull(rowData, 7);
            positive = ParseBoolOrNull(rowData, 8);
            specialCalculate = ParseIntOrNull(rowData, 9);
            vfxKind = ParseVfxKind(rowData, 10);
            vfxIndex = ParseIntOrNull(rowData, 11);
            if (vfxKind.HasValue != vfxIndex.HasValue)
                throw new FormatException("特效类型与特效下标必须同时填写或同时留空");
            if (vfxIndex.HasValue && vfxIndex.Value < 0)
                throw new FormatException($"特效下标不能为负数：{vfxIndex.Value}");
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new EffectConfig(row), row => row.key);
        }

        public static EffectConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion

    #region 角色经验表
    public struct ExpCharacterConfig
    {
        public const string FileName = "exp_character.csv";
        private const int ColumnCount = 2;
        public static readonly Dictionary<int, ExpCharacterConfig> content = new();
        public static int MaxLevel { get; private set; } // 配表覆盖到的最高等级 = 角色等级上限

        public int key;
        public int exp;

        public ExpCharacterConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            exp = ParseInt(rowData, 1);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            int count = Fill(table, content, row => new ExpCharacterConfig(row), row => row.key);
            MaxLevel = 0;
            foreach (int key in content.Keys)
            {
                if (key > MaxLevel) MaxLevel = key;
            }
            if (MaxLevel <= 0) throw new FormatException($"[配表] {FileName} 没有任何等级行，角色等级上限推不出来");
            return count;
        }

        public static ExpCharacterConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion

    #region 玩家经验表
    public struct ExpPlayerConfig
    {
        public const string FileName = "exp_player.csv";
        private const int ColumnCount = 2;
        public static readonly Dictionary<int, ExpPlayerConfig> content = new();
        public static int MaxLevel { get; private set; } // 配表覆盖到的最高等级 = 玩家等级上限

        public int key;
        public int exp;

        public ExpPlayerConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            exp = ParseInt(rowData, 1);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            int count = Fill(table, content, row => new ExpPlayerConfig(row), row => row.key);
            MaxLevel = 0;
            foreach (int key in content.Keys)
            {
                if (key > MaxLevel) MaxLevel = key;
            }
            if (MaxLevel <= 0) throw new FormatException($"[配表] {FileName} 没有任何等级行，玩家等级上限推不出来");
            return count;
        }

        public static ExpPlayerConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion

    #region 复活时间表
    public struct ReviveTimeConfig
    {
        public const string FileName = "revive_time.csv";
        private const int ColumnCount = 4;
        public static readonly Dictionary<int, ReviveTimeConfig> content = new();

        public int key;
        public int reviveIndex;
        public int attackReviveSeconds;
        public int defenseReviveSeconds;

        public ReviveTimeConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            reviveIndex = ParseInt(rowData, 1);
            attackReviveSeconds = ParseInt(rowData, 2);
            defenseReviveSeconds = ParseInt(rowData, 3);
            if (attackReviveSeconds <= 0 || defenseReviveSeconds <= 0)
                throw new FormatException($"复活秒数必须大于 0（进攻 {attackReviveSeconds} / 防守 {defenseReviveSeconds}）");
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new ReviveTimeConfig(row), row => row.key);
        }

        public static ReviveTimeConfig Get(int key) => GetRequired(content, key, FileName);

        public static ReviveTimeConfig GetClamped(int key) => GetClampedRow(content, key, FileName);
    }
    #endregion

    #region 全局参数表：0 key / 1 功能描述 / 2 value（| 分隔多个）
    public struct GlobalVariableConfig
    {
        public const string FileName = "global_variables.csv";
        private const int ColumnCount = 3;
        public static readonly Dictionary<string, GlobalVariableConfig> content = new();

        // 每行一个静态字段（字段名即表内 key），Load 时由 Apply 填入
        public static List<float> minimap_view_radius;  // 小地图显示半径档位，首档即默认档
        public static int day_duration;                 // 默认白天时长
        public static int night_duration;               // 默认晚上时长
        public static int distance_of_bar_default_size; // 实体血条为设计大小时的距离
        public static float attr_growth_health;         // 升级增益系数：生命
        public static float attr_growth_strength;       // 升级增益系数：力量
        public static float attr_growth_magic;          // 升级增益系数：魔法
        public static int attr_growth_critRate;         // 升级增益：暴击率，每级加点（百分点）
        public static int attr_growth_critDamage;       // 升级增益：暴击伤害，每级加点（百分点）
        public static int battle_max_level;             // 战斗期等级上限（生成实体时钳制）
        public static float status_auto_cast_interval_crystal; // 水晶雕像自动施法间隔
        public static float status_auto_cast_interval_neutral; // 中立雕像自动施法间隔
        public static float buff_speed_up;              // 加速 Buff 移速倍率
        public static float buff_speed_down;            // 减速 Buff 移速倍率
        public static float buff_mire_speed_rate;       // 泥沼 Buff 移速倍率
        public static float buff_death_stroll_speed_up; // 死灵漫步 Buff 移速倍率
        public static float move_turn_rate;             // 转向角速度（度/秒）
        public static float fly_vertical_speed;         // GM 飞行升降速度（米/秒）
        public static float gm_speed_multiplier;        // GM 极速移速倍率
        public static float battle_duration;            // 单场战斗时长（秒）
        public static float ready_start_delay;          // 全员准备后到开战的倒计时（秒）
        public static float coral_spawn_as_large_chance; // 刷新珊瑚时出大型的概率
        public static float coral_small_drop_chance;    // 小珊瑚被摧毁时的掉武器概率
        public static float coral_large_drop_chance;    // 大珊瑚被摧毁时的掉武器概率
        public static int zombie_max;                   // 场上僵尸数量上限
        public static float zombie_refresh_rate_fast;   // 僵尸刷新速度上限（场上剩余越少越快，单位：只/秒）
        public static float zombie_refresh_rate_slow;   // 僵尸刷新速度下限
        public static int zombie_spawn_level;           // 初始僵尸刷新等级
        public static float zombie_level_step_ratio;    // 刷新等级每 +1 所需的战斗进程比例
        public static float terrain_size;               // 地形边长（米），必须为 chunkSize 的整数倍
        public static float terrain_height;             // 地形最大高度（米）
        public static float ground_probe_up;            // 地面检测探针起点抬高（米）
        public static float ground_probe_length;        // 地面检测探针向下长度（米）
        public static int max_ai_player_count;          // 单个阵营的 AI 玩家数量上限

        // 实体同步量化参数（服务器 → 客户端）：范围按正负对称理解，精度为量化步长
        public static float sync_precision_entity_xz;            // 位姿：水平位置精度（米）
        public static float sync_precision_entity_y;             // 位姿：垂直位置精度（米）
        public static float sync_precision_entity_yaw;           // 位姿：朝向精度（度）
        public static float sync_entity_velocity_xz;             // 位姿：水平速度范围（±米/秒）
        public static float sync_precision_entity_velocity_xz;   // 位姿：水平速度精度
        public static float sync_entity_velocity_y;              // 位姿：垂直速度范围（±米/秒）
        public static float sync_precision_entity_velocity_y;    // 位姿：垂直速度精度
        public static float sync_precision_minimap_xz;           // 小地图：位置精度（米）
        public static int sync_max_buff_count;                   // 单实体最多下发的 buff 条数
        public static int max_entity_health;                     // 实体生命值上限

        public string key;
        public string description;
        public string value;

        public GlobalVariableConfig(string[] rowData)
        {
            key = ParseString(rowData, 0);
            description = ParseString(rowData, 1);
            value = ParseString(rowData, 2);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            int count = Fill(table, content, row => new GlobalVariableConfig(row), row => row.key);
            foreach (GlobalVariableConfig row in content.Values) Apply(row);
            Validate();
            return count;
        }

        // 表内行 → 静态字段：多出未处理的行直接抛错（新增配表行必须同步这里）
        private static void Apply(GlobalVariableConfig row)
        {
            switch (row.key)
            {
                case "minimap_view_radius":
                    minimap_view_radius = Floats(row);
                    break;
                case "day_duration":
                    day_duration = Int(row);
                    break;
                case "night_duration":
                    night_duration = Int(row);
                    break;
                case "distance_of_bar_default_size":
                    distance_of_bar_default_size = Int(row);
                    if (distance_of_bar_default_size <= 0) // 血条 scale 以它为分母
                        throw new FormatException($"[配表] {FileName} 的 distance_of_bar_default_size 必须大于 0");
                    break;
                case "attr_growth_health":
                    attr_growth_health = NonNegative(Float(row), row.key);
                    break;
                case "attr_growth_strength":
                    attr_growth_strength = NonNegative(Float(row), row.key);
                    break;
                case "attr_growth_magic":
                    attr_growth_magic = NonNegative(Float(row), row.key);
                    break;
                case "attr_growth_critRate":
                    attr_growth_critRate = NonNegative(Int(row), row.key);
                    break;
                case "attr_growth_critDamage":
                    attr_growth_critDamage = NonNegative(Int(row), row.key);
                    break;
                case "battle_max_level":
                    battle_max_level = Int(row);
                    if (battle_max_level <= 0)
                        throw new FormatException($"[配表] {FileName} 的 battle_max_level 必须大于 0");
                    break;
                case "status_auto_cast_interval_crystal":
                    status_auto_cast_interval_crystal = Positive(Float(row), row.key);
                    break;
                case "status_auto_cast_interval_neutral":
                    status_auto_cast_interval_neutral = Positive(Float(row), row.key);
                    break;
                case "buff_speed_up":
                    buff_speed_up = Positive(Float(row), row.key);
                    break;
                case "buff_speed_down":
                    buff_speed_down = Positive(Float(row), row.key);
                    break;
                case "buff_mire_speed_rate":
                    buff_mire_speed_rate = Positive(Float(row), row.key);
                    break;
                case "buff_death_stroll_speed_up":
                    buff_death_stroll_speed_up = Positive(Float(row), row.key);
                    break;
                case "move_turn_rate":
                    move_turn_rate = Positive(Float(row), row.key);
                    break;
                case "fly_vertical_speed":
                    fly_vertical_speed = Positive(Float(row), row.key);
                    break;
                case "gm_speed_multiplier":
                    gm_speed_multiplier = Positive(Float(row), row.key);
                    break;
                case "battle_duration":
                    battle_duration = Positive(Float(row), row.key);
                    break;
                case "ready_start_delay":
                    ready_start_delay = NonNegative(Float(row), row.key);
                    break;
                case "coral_spawn_as_large_chance":
                    coral_spawn_as_large_chance = Ratio(Float(row), row.key);
                    break;
                case "coral_small_drop_chance":
                    coral_small_drop_chance = Ratio(Float(row), row.key);
                    break;
                case "coral_large_drop_chance":
                    coral_large_drop_chance = Ratio(Float(row), row.key);
                    break;
                case "zombie_max":
                    zombie_max = Positive(Int(row), row.key);
                    break;
                case "zombie_refresh_rate_fast":
                    zombie_refresh_rate_fast = NonNegative(Float(row), row.key);
                    break;
                case "zombie_refresh_rate_slow":
                    zombie_refresh_rate_slow = NonNegative(Float(row), row.key);
                    break;
                case "zombie_spawn_level":
                    zombie_spawn_level = NonNegative(Int(row), row.key);
                    break;
                case "zombie_level_step_ratio":
                    zombie_level_step_ratio = Positive(Float(row), row.key);
                    break;
                case "terrain_size":
                    terrain_size = Positive(Float(row), row.key);
                    if (terrain_size % Landscape.chunkSize != 0f)
                        throw new FormatException(
                            $"[配表] {FileName} 的 terrain_size 必须是区块边长 {Landscape.chunkSize} 的整数倍：{terrain_size}");
                    break;
                case "terrain_height":
                    terrain_height = Positive(Float(row), row.key);
                    break;
                case "ground_probe_up":
                    ground_probe_up = NonNegative(Float(row), row.key);
                    break;
                case "ground_probe_length":
                    ground_probe_length = Positive(Float(row), row.key);
                    break;
                case "max_ai_player_count":
                    max_ai_player_count = Positive(Int(row), row.key);
                    break;
                case "sync_precision_entity_xz":
                    sync_precision_entity_xz = Positive(Float(row), row.key);
                    break;
                case "sync_precision_entity_y":
                    sync_precision_entity_y = Positive(Float(row), row.key);
                    break;
                case "sync_precision_entity_yaw":
                    sync_precision_entity_yaw = Positive(Float(row), row.key);
                    break;
                case "sync_entity_velocity_xz":
                    sync_entity_velocity_xz = Positive(Float(row), row.key);
                    break;
                case "sync_precision_entity_velocity_xz":
                    sync_precision_entity_velocity_xz = Positive(Float(row), row.key);
                    break;
                case "sync_entity_velocity_y":
                    sync_entity_velocity_y = Positive(Float(row), row.key);
                    break;
                case "sync_precision_entity_velocity_y":
                    sync_precision_entity_velocity_y = Positive(Float(row), row.key);
                    break;
                case "sync_precision_minimap_xz":
                    sync_precision_minimap_xz = Positive(Float(row), row.key);
                    break;
                case "sync_max_buff_count":
                    sync_max_buff_count = Positive(Int(row), row.key);
                    break;
                case "max_entity_health":
                    max_entity_health = Positive(Int(row), row.key);
                    break;
                default:
                    throw new FormatException($"[配表] {FileName} 的行「{row.key}」没有对应的静态字段");
            }
        }

        // 精度必须小于量程，否则量化后位宽为 0、数据被静默丢弃（跨行校验，须在全部行 Apply 之后执行）
        private static void Validate()
        {
            // 量程行缺失会退化成 0，让下面的比较报出误导性信息，先显式拦下
            if (terrain_size <= 0f || terrain_height <= 0f || sync_entity_velocity_xz <= 0f || sync_entity_velocity_y <= 0f)
                throw new FormatException($"[配表] {FileName} 缺少 terrain_size / terrain_height / sync_entity_velocity_xz / sync_entity_velocity_y，无法校验同步精度");
            if (sync_precision_entity_xz >= terrain_size)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_entity_xz 必须小于 terrain_size：{sync_precision_entity_xz} vs {terrain_size}");
            if (sync_precision_entity_y >= terrain_height)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_entity_y 必须小于 terrain_height：{sync_precision_entity_y} vs {terrain_height}");
            if (sync_precision_entity_yaw >= 360f)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_entity_yaw 必须小于 360：{sync_precision_entity_yaw}");
            if (sync_precision_minimap_xz >= terrain_size)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_minimap_xz 必须小于 terrain_size：{sync_precision_minimap_xz} vs {terrain_size}");
            if (sync_precision_entity_velocity_xz >= 2f * sync_entity_velocity_xz)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_entity_velocity_xz 必须小于速度量程 2×{sync_entity_velocity_xz}：{sync_precision_entity_velocity_xz}");
            if (sync_precision_entity_velocity_y >= 2f * sync_entity_velocity_y)
                throw new FormatException($"[配表] {FileName} 的 sync_precision_entity_velocity_y 必须小于速度量程 2×{sync_entity_velocity_y}：{sync_precision_entity_velocity_y}");
        }

        private static float NonNegative(float value, string key)
        {
            if (value < 0f) throw new FormatException($"[配表] {FileName} 的行「{key}」不能为负数：{value}");
            return value;
        }

        private static int NonNegative(int value, string key)
        {
            if (value < 0) throw new FormatException($"[配表] {FileName} 的行「{key}」不能为负数：{value}");
            return value;
        }

        private static float Positive(float value, string key)
        {
            if (value <= 0f) throw new FormatException($"[配表] {FileName} 的行「{key}」必须大于 0：{value}");
            return value;
        }

        private static int Positive(int value, string key)
        {
            if (value <= 0) throw new FormatException($"[配表] {FileName} 的行「{key}」必须大于 0：{value}");
            return value;
        }

        // 概率行：0~1（含端点）
        private static float Ratio(float value, string key)
        {
            if (value < 0f || value > 1f)
                throw new FormatException($"[配表] {FileName} 的行「{key}」必须是 0~1 的概率：{value}");
            return value;
        }

        private static string[] Tokens(GlobalVariableConfig row)
        {
            if (row.value.Length == 0)
                throw new FormatException($"[配表] {FileName} 的行「{row.key}」value 不能为空");
            return row.value.Split('|');
        }

        // 单值行
        private static int Int(GlobalVariableConfig row)
        {
            string[] tokens = Tokens(row);
            if (tokens.Length != 1 ||
                !int.TryParse(tokens[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException($"[配表] {FileName} 的行「{row.key}」value 必须是单个整数：「{row.value}」");
            return value;
        }

        // 单值行（浮点）
        private static float Float(GlobalVariableConfig row)
        {
            string[] tokens = Tokens(row);
            if (tokens.Length != 1 ||
                !float.TryParse(tokens[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new FormatException($"[配表] {FileName} 的行「{row.key}」value 必须是单个数值：「{row.value}」");
            return value;
        }

        // 多值行（如雷达半径档位）
        private static List<float> Floats(GlobalVariableConfig row)
        {
            string[] tokens = Tokens(row);
            var result = new List<float>(tokens.Length);
            foreach (string token in tokens)
            {
                if (!float.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    throw new FormatException($"[配表] {FileName} 的行「{row.key}」value 含非法数值：「{token}」");
                result.Add(value);
            }
            return result;
        }
    }
    #endregion

    #region 提示消息表
    public struct NoticeConfig
    {
        public const string FileName = "notice.csv";
        private const int ColumnCount = 2;
        public static readonly Dictionary<int, NoticeConfig> content = new();

        public int key;
        public string text;

        public NoticeConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            text = ParseString(rowData, 1);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new NoticeConfig(row), row => row.key);
        }

        public static NoticeConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion

    #region 属性范围表：0 属性下标 / 1 属性名 / 2 最小值 / 3 最大值
    public struct AttrRangeConfig
    {
        public const string FileName = "attr_range.csv";
        private const int ColumnCount = 4;
        public static readonly Dictionary<int, AttrRangeConfig> content = new();

        public int key;
        public string name;
        public float min;
        public float max;

        public AttrRangeConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            name = ParseString(rowData, 1);
            min = ParseFloat(rowData, 2);
            max = ParseFloat(rowData, 3);
            if (max <= min) throw new FormatException($"属性「{name}」的最大值 {max} 必须大于最小值 {min}");
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new AttrRangeConfig(row), row => row.key);
        }

        public static AttrRangeConfig Get(int key) => GetRequired(content, key, FileName);

        // 属性值映射成 0~1 的填充比例：最小值=空、最大值=满
        public float Ratio(float value)
        {
            return Mathf.Clamp01((value - min) / (max - min));
        }
    }
    #endregion

    #region 设置界面玩法说明表：0 序号 / 1 栏目名称 / 2 内容
    public struct SettingTipsConfig
    {
        public const string FileName = "setting_tips.csv";
        private const int ColumnCount = 3;
        public static readonly Dictionary<int, SettingTipsConfig> content = new();

        public int key;
        public string name;
        public string text;

        public SettingTipsConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0);
            name = ParseString(rowData, 1);
            text = ParseString(rowData, 2);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            return Fill(table, content, row => new SettingTipsConfig(row), row => row.key);
        }

        public static SettingTipsConfig Get(int key) => GetRequired(content, key, FileName);
    }
    #endregion
}

public class ConfigTable
{
    public readonly string fileName;
    public readonly int columnCount;
    public readonly List<string[]> rows;

    public ConfigTable(string fileName, List<string[]> lines)
    {
        this.fileName = fileName;
        if (lines.Count == 0) throw new FormatException($"[配表] {fileName} 没有任何数据行");

        columnCount = lines[0].Length;
        rows = lines.GetRange(1, lines.Count - 1);
    }

    public void RequireColumnCount(int expected)
    {
        if (columnCount != expected)
            throw new FormatException($"[配表] {fileName} 列数为 {columnCount}，代码按 {expected} 列读取：改列需同步改本文件");
    }

    public string[] RequireRow(int index)
    {
        string[] row = rows[index];
        if (row.Length != columnCount)
            throw new FormatException($"[配表] {fileName} 第 {index + 2} 行：列数为 {row.Length}，表头为 {columnCount} 列");
        return row;
    }
}

public struct BuffEntry
{
    public int type;
    public int level;
    public float duration;
}
