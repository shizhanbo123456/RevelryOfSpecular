using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

        Debug.Log($"[配表] {dir} 加载完成 | battle_fury_layer {battleFury} | entity {entity} | skill {skill} | effect {effect} | " +
                  $"exp_character {expCharacter} | exp_player {expPlayer} | revive_time {revive} | global_variables {global} | notice {notice} | attr_range {attrRange}");
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
        public float critDamage;
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
            critDamage = ParseFloat(rowData, 7);
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
                    case 0: attr.health += attr.health * 0.25f; break;
                    case 1: attr.strength += (int)(attr.strength * 0.25f); break;
                    case 2: attr.magic += (int)(attr.magic * 0.25f); break;
                    case 3: attr.critRate += (int)(attr.critRate * 0.1f); break;
                    case 4: attr.critDamage += attr.critDamage * 0.2f; break;
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
        private const int ColumnCount = 24;
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
        private const int ColumnCount = 10;
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
            return Fill(table, content, row => new ExpCharacterConfig(row), row => row.key);
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

    #region 全局参数表
    public struct GlobalVariableConfig
    {
        public const string FileName = "global_variables.csv";
        private const int ColumnCount = 4;
        public static readonly Dictionary<string, GlobalVariableConfig> content = new();

        // 每行一个静态字段（字段名即表内 key），Load 时由 Apply 填入
        public static List<float> minimap_view_radius;  // 小地图显示半径档位，首档即默认档
        public static int day_duration;                 // 默认白天时长
        public static int night_duration;               // 默认晚上时长
        public static int distance_of_bar_default_size; // 实体血条为设计大小时的距离

        public string key;
        public string description;
        public List<int> ints;
        public List<float> floats;

        public GlobalVariableConfig(string[] rowData)
        {
            key = ParseString(rowData, 0);
            description = ParseString(rowData, 1);
            ints = ParseIntList(rowData, 2);
            floats = ParseFloatList(rowData, 3);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireColumnCount(ColumnCount);
            int count = Fill(table, content, row => new GlobalVariableConfig(row), row => row.key);
            foreach (GlobalVariableConfig row in content.Values) Apply(row);
            return count;
        }

        // 表内行 → 静态字段：多出未处理的行直接抛错（新增配表行必须同步这里）
        private static void Apply(GlobalVariableConfig row)
        {
            switch (row.key)
            {
                case "minimap_view_radius":
                    minimap_view_radius = RequireValues(row.floats, row.key);
                    break;
                case "day_duration":
                    day_duration = RequireValue(row.ints, row.key);
                    break;
                case "night_duration":
                    night_duration = RequireValue(row.ints, row.key);
                    break;
                case "distance_of_bar_default_size":
                    distance_of_bar_default_size = RequireValue(row.ints, row.key);
                    if (distance_of_bar_default_size <= 0) // 血条 scale 以它为分母
                        throw new FormatException($"[配表] {FileName} 的 distance_of_bar_default_size 必须大于 0");
                    break;
                default:
                    throw new FormatException($"[配表] {FileName} 的行「{row.key}」没有对应的静态字段");
            }
        }

        // 单值行取首项
        private static int RequireValue(List<int> values, string key)
        {
            if (values == null || values.Count == 0)
                throw new FormatException($"[配表] {FileName} 的行「{key}」ints 列必须填值");
            return values[0];
        }

        private static List<float> RequireValues(List<float> values, string key)
        {
            if (values == null || values.Count == 0)
                throw new FormatException($"[配表] {FileName} 的行「{key}」floats 列必须填值");
            return values;
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
