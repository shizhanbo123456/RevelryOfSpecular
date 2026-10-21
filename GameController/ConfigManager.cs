using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// 配表读取：Assets/StreamingAssets/ConfigData/*.csv
// 改数据值、改行顺序不需要改代码；改列（增删/换序/改名）会在表头校验时报错，需同步改本文件
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

        Debug.Log($"[配表] {dir} 加载完成 | battle_fury_layer {battleFury} | entity {entity} | skill {skill} | effect {effect} | " +
                  $"exp_character {expCharacter} | exp_player {expPlayer} | revive_time {revive} | global_variables {global}");
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

    #region 单元格解析
    public static string ParseString(string[] rowData, int col, string colName) => Cell(rowData, col, colName);

    public static int ParseInt(string[] rowData, int col, string colName)
    {
        string raw = Cell(rowData, col, colName);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"列「{colName}」不是整数：「{raw}」");
        return value;
    }

    public static int? ParseIntOrNull(string[] rowData, int col, string colName)
    {
        string raw = Cell(rowData, col, colName);
        if (raw.Length == 0) return null;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"列「{colName}」不是整数：「{raw}」");
        return value;
    }

    public static float? ParseFloatOrNull(string[] rowData, int col, string colName)
    {
        string raw = Cell(rowData, col, colName);
        if (raw.Length == 0) return null;
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            throw new FormatException($"列「{colName}」不是数值：「{raw}」");
        return value;
    }

    public static bool? ParseBoolOrNull(string[] rowData, int col, string colName)
    {
        string raw = Cell(rowData, col, colName);
        return raw switch
        {
            "" => null,
            "1" => true,
            "0" => false,
            _ => throw new FormatException($"列「{colName}」只能填 1/0 或留空：「{raw}」"),
        };
    }

    public static List<int> ParseIntList(string[] rowData, int col, string colName)
    {
        var result = new List<int>();
        string raw = Cell(rowData, col, colName);
        if (raw.Length == 0) return result;

        foreach (string token in raw.Split('|'))
        {
            if (!int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException($"列「{colName}」含非法整数：「{token}」");
            result.Add(value);
        }
        return result;
    }

    public static List<float> ParseFloatList(string[] rowData, int col, string colName)
    {
        var result = new List<float>();
        string raw = Cell(rowData, col, colName);
        if (raw.Length == 0) return result;

        foreach (string token in raw.Split('|'))
        {
            if (!float.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new FormatException($"列「{colName}」含非法数值：「{token}」");
            result.Add(value);
        }
        return result;
    }

    // 多个 Buff 用 | 分隔，单个 Buff 为 类型;等级;持续时间
    public static List<BuffEntry> ParseBuffList(string[] rowData, int col, string colName)
    {
        var result = new List<BuffEntry>();
        string raw = Cell(rowData, col, colName);
        if (raw.Length == 0) return result;

        foreach (string group in raw.Split('|'))
        {
            string[] parts = group.Split(';');
            if (parts.Length != 3)
                throw new FormatException($"列「{colName}」的 Buff 需为 类型;等级;持续时间：「{group}」");

            if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type) ||
                !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) ||
                !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
                throw new FormatException($"列「{colName}」的 Buff 数值非法：「{group}」");

            result.Add(new BuffEntry { type = type, level = level, duration = duration });
        }
        return result;
    }

    private static string Cell(string[] rowData, int col, string colName)
    {
        if (col >= rowData.Length)
            throw new FormatException($"缺少列「{colName}」（该行只有 {rowData.Length} 列）");
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
    #endregion

    #region 愈战愈勇层数表
    public struct BattleFuryLayerConfig
    {
        public const string FileName = "battle_fury_layer.csv";
        private static readonly string[] ExpectedHeader = { "愈战愈勇层数表", "死亡次数", "buff层数" };
        public static readonly Dictionary<int, BattleFuryLayerConfig> content = new();

        public int key;
        public int? deathCount;
        public int? buffLayerCount;

        public BattleFuryLayerConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            deathCount = ParseIntOrNull(rowData, 1, ExpectedHeader[1]);
            buffLayerCount = ParseIntOrNull(rowData, 2, ExpectedHeader[2]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new BattleFuryLayerConfig(row), row => row.key);
        }
    }
    #endregion

    #region 角色信息表
    public struct EntityConfig
    {
        public const string FileName = "entity.csv";
        private static readonly string[] ExpectedHeader =
        {
            "角色信息表", "姓名", "生命", "力量", "魔法", "暴击率", "暴击伤害", "视野", "技能槽数",
            "升级A", "升级B", "升级C", "解锁等级", "初始技能列表", "品质",
        };
        public static readonly Dictionary<string, EntityConfig> content = new();

        public string key;
        public string name;
        public float? health;
        public int? strength;
        public int? magic;
        public int? critRate;
        public float? critDamage;
        public float? viewDistance;
        public int? weaponSlotCount;
        public int? upgradeA;
        public int? upgradeB;
        public int? upgradeC;
        public int? unlockLevel;
        public List<int> initialSkills;
        public string quality;

        public EntityConfig(string[] rowData)
        {
            key = ParseString(rowData, 0, ExpectedHeader[0]);
            name = ParseString(rowData, 1, ExpectedHeader[1]);
            health = ParseFloatOrNull(rowData, 2, ExpectedHeader[2]);
            strength = ParseIntOrNull(rowData, 3, ExpectedHeader[3]);
            magic = ParseIntOrNull(rowData, 4, ExpectedHeader[4]);
            critRate = ParseIntOrNull(rowData, 5, ExpectedHeader[5]);
            critDamage = ParseFloatOrNull(rowData, 6, ExpectedHeader[6]);
            viewDistance = ParseFloatOrNull(rowData, 7, ExpectedHeader[7]);
            weaponSlotCount = ParseIntOrNull(rowData, 8, ExpectedHeader[8]);
            upgradeA = ParseIntOrNull(rowData, 9, ExpectedHeader[9]);
            upgradeB = ParseIntOrNull(rowData, 10, ExpectedHeader[10]);
            upgradeC = ParseIntOrNull(rowData, 11, ExpectedHeader[11]);
            unlockLevel = ParseIntOrNull(rowData, 12, ExpectedHeader[12]);
            initialSkills = ParseIntList(rowData, 13, ExpectedHeader[13]);
            quality = ParseString(rowData, 14, ExpectedHeader[14]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new EntityConfig(row), row => row.key);
        }
    }
    #endregion

    #region 技能表
    public struct SkillConfig
    {
        public const string FileName = "skill.csv";
        private static readonly string[] ExpectedHeader =
        {
            "技能表", "名称", "技能描述", "图标编号", "品质", "技能类型", "技能标签", "CD", "存储上限",
            "武器分类", "武器细分id", "技能数值来源分类", "击退力度", "Buff",
        };
        public static readonly Dictionary<int, SkillConfig> content = new();

        public int key;
        public string name;
        public string description;
        public int? iconIndex;
        public string quality;
        public string skillType;
        public string skillTag;
        public float? cd;
        public int? storeLimit;
        public int? weaponCategory;
        public int? weaponSubId;
        public int? valueSource;
        public float? knockback;
        public List<BuffEntry> buffs;

        public SkillConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            name = ParseString(rowData, 1, ExpectedHeader[1]);
            description = ParseString(rowData, 2, ExpectedHeader[2]);
            iconIndex = ParseIntOrNull(rowData, 3, ExpectedHeader[3]);
            quality = ParseString(rowData, 4, ExpectedHeader[4]);
            skillType = ParseString(rowData, 5, ExpectedHeader[5]);
            skillTag = ParseString(rowData, 6, ExpectedHeader[6]);
            cd = ParseFloatOrNull(rowData, 7, ExpectedHeader[7]);
            storeLimit = ParseIntOrNull(rowData, 8, ExpectedHeader[8]);
            weaponCategory = ParseIntOrNull(rowData, 9, ExpectedHeader[9]);
            weaponSubId = ParseIntOrNull(rowData, 10, ExpectedHeader[10]);
            valueSource = ParseIntOrNull(rowData, 11, ExpectedHeader[11]);
            knockback = ParseFloatOrNull(rowData, 12, ExpectedHeader[12]);
            buffs = ParseBuffList(rowData, 13, ExpectedHeader[13]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new SkillConfig(row), row => row.key);
        }
    }
    #endregion

    #region Buff 表
    public struct EffectConfig
    {
        public const string FileName = "effect.csv";
        private static readonly string[] ExpectedHeader =
        {
            "buff表", "名称", "描述", "叠加模式", "禁止移动输入", "禁止技能输入", "冻结动画",
            "霸体等级覆盖", "正面效果", "特殊计算效果",
        };
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
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            name = ParseString(rowData, 1, ExpectedHeader[1]);
            description = ParseString(rowData, 2, ExpectedHeader[2]);
            stackMode = ParseIntOrNull(rowData, 3, ExpectedHeader[3]);
            lockMoveInput = ParseBoolOrNull(rowData, 4, ExpectedHeader[4]);
            lockSkillInput = ParseBoolOrNull(rowData, 5, ExpectedHeader[5]);
            freezeAnimation = ParseBoolOrNull(rowData, 6, ExpectedHeader[6]);
            superArmorOverride = ParseIntOrNull(rowData, 7, ExpectedHeader[7]);
            positive = ParseBoolOrNull(rowData, 8, ExpectedHeader[8]);
            specialCalculate = ParseIntOrNull(rowData, 9, ExpectedHeader[9]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new EffectConfig(row), row => row.key);
        }
    }
    #endregion

    #region 角色经验表
    public struct ExpCharacterConfig
    {
        public const string FileName = "exp_character.csv";
        private static readonly string[] ExpectedHeader = { "角色经验表", "升级所需经验" };
        public static readonly Dictionary<int, ExpCharacterConfig> content = new();

        public int key;
        public int? exp;

        public ExpCharacterConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            exp = ParseIntOrNull(rowData, 1, ExpectedHeader[1]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new ExpCharacterConfig(row), row => row.key);
        }
    }
    #endregion

    #region 玩家经验表
    public struct ExpPlayerConfig
    {
        public const string FileName = "exp_player.csv";
        private static readonly string[] ExpectedHeader = { "角色经验表", "所需经验" };
        public static readonly Dictionary<int, ExpPlayerConfig> content = new();

        public int key;
        public int? exp;

        public ExpPlayerConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            exp = ParseIntOrNull(rowData, 1, ExpectedHeader[1]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new ExpPlayerConfig(row), row => row.key);
        }
    }
    #endregion

    #region 复活时间表
    public struct ReviveTimeConfig
    {
        public const string FileName = "revive_time.csv";
        private static readonly string[] ExpectedHeader = { "复活次数-时间表", "第x次复活", "进攻方复活时间", "防守方复活时间" };
        public static readonly Dictionary<int, ReviveTimeConfig> content = new();

        public int key;
        public int? reviveIndex;
        public int? attackReviveSeconds;
        public int? defenseReviveSeconds;

        public ReviveTimeConfig(string[] rowData)
        {
            key = ParseInt(rowData, 0, ExpectedHeader[0]);
            reviveIndex = ParseIntOrNull(rowData, 1, ExpectedHeader[1]);
            attackReviveSeconds = ParseIntOrNull(rowData, 2, ExpectedHeader[2]);
            defenseReviveSeconds = ParseIntOrNull(rowData, 3, ExpectedHeader[3]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new ReviveTimeConfig(row), row => row.key);
        }
    }
    #endregion

    #region 全局参数表
    public struct GlobalVariableConfig
    {
        public const string FileName = "global_variables.csv";
        private static readonly string[] ExpectedHeader = { "全局参数表", "功能描述", "ints", "floats" };
        public static readonly Dictionary<string, GlobalVariableConfig> content = new();

        public string key;
        public string description;
        public List<int> ints;
        public List<float> floats;

        public GlobalVariableConfig(string[] rowData)
        {
            key = ParseString(rowData, 0, ExpectedHeader[0]);
            description = ParseString(rowData, 1, ExpectedHeader[1]);
            ints = ParseIntList(rowData, 2, ExpectedHeader[2]);
            floats = ParseFloatList(rowData, 3, ExpectedHeader[3]);
        }

        public static int Load(ConfigTable table)
        {
            table.RequireHeader(ExpectedHeader);
            return Fill(table, content, row => new GlobalVariableConfig(row), row => row.key);
        }
    }
    #endregion
}

public class ConfigTable
{
    public readonly string fileName;
    public readonly string[] header;
    public readonly List<string[]> rows = new();

    public ConfigTable(string fileName, List<string[]> lines)
    {
        this.fileName = fileName;
        if (lines.Count == 0) throw new FormatException($"[配表] {fileName} 没有任何数据行");

        header = lines[0];
        for (int i = 1; i < lines.Count; i++) rows.Add(lines[i]);
    }

    // 列名比对只取括号前的内容，括号内的说明可以随便改
    public void RequireHeader(params string[] expected)
    {
        if (header.Length != expected.Length)
            throw new FormatException($"[配表] {fileName} 列数不符：期望 {expected.Length} 列，实际 {header.Length} 列（{string.Join("|", header)}）");

        for (int i = 0; i < expected.Length; i++)
        {
            string actual = HeaderName(header[i]);
            if (actual != expected[i])
                throw new FormatException($"[配表] {fileName} 第 {i} 列名为「{actual}」，代码期望「{expected[i]}」：改列需同步改代码");
        }
    }

    public string[] RequireRow(int index)
    {
        string[] row = rows[index];
        if (row.Length != header.Length)
            throw new FormatException($"[配表] {fileName} 第 {index + 2} 行：列数不符，期望 {header.Length} 列，实际 {row.Length} 列");
        return row;
    }

    private static string HeaderName(string raw)
    {
        int cut = raw.IndexOf('（');
        if (cut < 0) cut = raw.IndexOf('(');
        return (cut < 0 ? raw : raw.Substring(0, cut)).Trim();
    }
}

public struct BuffEntry
{
    public int type;
    public int level;
    public float duration;
}
