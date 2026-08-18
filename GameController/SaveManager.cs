using System;
using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

//规范：读取信息只读取public字段，写入需要通过公开方法
public class SaveManager : MonoBehaviour
{
    private const string SaveKey = "GameSaveDataV2";
    private const string Separator = "|";

    #region Save Data
    public List<int> characterLevels = new();
    public List<int> characterExp = new();
    public List<int> characterTokenCounts = new();
    public List<int> imprintLevels = new();
    public List<int> imprintExp = new();
    public List<int> noteCounts = new();
    public List<int> runeCounts = new();
    #endregion

    #region Unity
    private void Awake()
    {
        Tool.SaveManager = this;
        LoadOrCreate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            EventManager.TrigEvent(ClientEvent.ShowNotice, "已重置存档");
            GenerateDefaultSave();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }
    #endregion

    #region Save File
    public void LoadOrCreate()
    {
        if (!PlayerPrefs.HasKey(SaveKey) || !TryLoad(PlayerPrefs.GetString(SaveKey)))
        {
            GenerateDefaultSave();
            return;
        }

        NormalizeData();
        Save();
    }

    public void Save()
    {
        NormalizeData();
        PlayerPrefs.SetString(SaveKey, string.Join(Separator, new[]
        {
            EncodeList(characterLevels),
            EncodeList(characterExp),
            EncodeList(characterTokenCounts),
            EncodeList(imprintLevels),
            EncodeList(imprintExp),
            EncodeList(noteCounts),
            EncodeList(runeCounts)
        }));
        PlayerPrefs.Save();
    }

    public void GenerateDefaultSave()
    {
        characterLevels = CreateList(Config.character_count, 0);
        characterExp = CreateList(Config.character_count, 0);
        characterTokenCounts = CreateList(Config.character_count, 0);
        imprintLevels = CreateList(Config.imprint_count, 0);
        imprintExp = CreateList(Config.imprint_count, 0);
        noteCounts = CreateList(Config.note_count, 0);
        runeCounts = CreateList(Config.skill_count, 0);

        characterLevels[0] = Config.min_entity_level;
        characterTokenCounts[0] = 3;
        InitDefaultCRuneCounts();
        Save();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tool/Clear Save")]
#endif
    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
    }
    #endregion

    #region Read Save Info
    public int GetCharacterUpgradeNeedExp(int charIndex)
    {
        return GetCharUpgradeNeedExpByLevel(characterLevels[charIndex]);
    }

    public bool IsCharacterMaxLevel(int charIndex)
    {
        return characterLevels[charIndex] >= Config.max_entity_level;
    }

    public bool CanConsumeCharacterToken(int charIndex)
    {
        return characterTokenCounts[charIndex] > 0;
    }

    public int GetImprintUpgradeNeedExp(int imprintIndex)
    {
        return GetImprintUpgradeNeedExpByLevel(imprintLevels[imprintIndex]);
    }

    public bool IsImprintMaxLevel(int imprintIndex)
    {
        return imprintLevels[imprintIndex] >= Config.max_imprint_level;
    }

    public int GetRuneCount(int skillId)
    {
        return runeCounts[skillId];
    }
    #endregion

    #region Modify Save Info
    public void UnlockCharacter(int charIndex)
    {
        if (characterLevels[charIndex] >= Config.min_entity_level) return;

        characterLevels[charIndex] = Config.min_entity_level;
        characterExp[charIndex] = 0;
        Save();
    }

    public void AddCharacterExp(int charIndex, int addExp)
    {
        if (addExp <= 0) return;
        UnlockCharacter(charIndex);
        if (IsCharacterMaxLevel(charIndex)) return;

        characterExp[charIndex] += addExp;
        while (characterLevels[charIndex] < Config.max_entity_level)
        {
            int needExp = GetCharacterUpgradeNeedExp(charIndex);
            if (characterExp[charIndex] < needExp) break;
            characterExp[charIndex] -= needExp;
            characterLevels[charIndex]++;
        }

        Save();
    }

    public void AddCharacterToken(int charIndex, int count)
    {
        if (count <= 0) return;
        characterTokenCounts[charIndex] += count;
        Save();
    }

    public bool TryConsumeCharacterToken(int charIndex)
    {
        if (!CanConsumeCharacterToken(charIndex)) return false;
        characterTokenCounts[charIndex]--;
        Save();
        return true;
    }

    public void AddImprintExp(int imprintIndex, int addExp)
    {
        if (addExp <= 0) return;
        if (IsImprintMaxLevel(imprintIndex)) return;

        imprintExp[imprintIndex] += addExp;
        while (imprintLevels[imprintIndex] < Config.max_imprint_level)
        {
            int needExp = GetImprintUpgradeNeedExp(imprintIndex);
            if (imprintExp[imprintIndex] < needExp) break;
            imprintExp[imprintIndex] -= needExp;
            imprintLevels[imprintIndex]++;
        }

        Save();
    }

    public void AddNote(int noteIndex, int count)
    {
        if (count <= 0) return;
        noteCounts[noteIndex] += count;
        Save();
    }

    public void AddRune(int skillId, int count = 1)
    {
        if (count <= 0) return;
        runeCounts[skillId] += count;
        Save();
    }

    public bool TryConsumeRune(int skillId, int count = 1)
    {
        if (count <= 0) return false;
        if (runeCounts[skillId] < count) return false;

        runeCounts[skillId] -= count;
        Save();
        return true;
    }

    public bool TryConsumeCarriedRunes(CSPlayerInfo info)
    {
        if (info?.skills == null) return false;

        foreach (var rune in info.skills)
        {
            if (rune.Value <= 0) continue;
            if (runeCounts[rune.Key] < rune.Value) return false;
        }

        foreach (var rune in info.skills)
        {
            if (rune.Value <= 0) continue;
            runeCounts[rune.Key] -= rune.Value;
        }

        Save();
        return true;
    }

    public void ApplySuccessfulExtraction(Dictionary<int, int> carriedRunes)
    {
        if (carriedRunes == null) return;
        foreach (var rune in carriedRunes)
        {
            if (rune.Value <= 0) continue;
            runeCounts[rune.Key] += rune.Value;
        }
        Save();
    }
    #endregion

    #region Internal Save Format
    private bool TryLoad(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        string[] sections = raw.Split(new[] { Separator }, StringSplitOptions.None);
        if (sections.Length != 7) return false;

        characterLevels = DecodeList(sections[0]);
        characterExp = DecodeList(sections[1]);
        characterTokenCounts = DecodeList(sections[2]);
        imprintLevels = DecodeList(sections[3]);
        imprintExp = DecodeList(sections[4]);
        noteCounts = DecodeList(sections[5]);
        runeCounts = DecodeList(sections[6]);
        return true;
    }

    private void NormalizeData()
    {
        EnsureListSize(characterLevels, Config.character_count, 0);
        EnsureListSize(characterExp, Config.character_count, 0);
        EnsureListSize(characterTokenCounts, Config.character_count, 0);
        EnsureListSize(imprintLevels, Config.imprint_count, 0);
        EnsureListSize(imprintExp, Config.imprint_count, 0);
        EnsureListSize(noteCounts, Config.note_count, 0);
        EnsureListSize(runeCounts, Config.skill_count, 0);
    }

    private static string EncodeList(List<int> list)
    {
        return list == null ? string.Empty : string.Join(",", list);
    }

    private static List<int> DecodeList(string raw)
    {
        List<int> result = new();
        if (string.IsNullOrEmpty(raw)) return result;

        string[] parts = raw.Split(',');
        foreach (var part in parts)
        {
            result.Add(int.TryParse(part, out var value) ? value : 0);
        }
        return result;
    }
    #endregion

    #region Utilities
    private void InitDefaultCRuneCounts()
    {
        for (int skillId = 0; skillId < Config.skill_count; skillId++)
        {
            if (SkillManager.GetSkillQuality(skillId) == SkillInfo.Quality.C && !SkillManager.GetSkillInfectious(skillId))
            {
                runeCounts[skillId] = Config.default_c_rune_count;
            }
        }
    }

    private static List<int> CreateList(int count, int value)
    {
        List<int> result = new();
        for (int i = 0; i < count; i++)
        {
            result.Add(value);
        }
        return result;
    }

    private static void EnsureListSize(List<int> list, int count, int defaultValue)
    {
        if (list == null) return;
        while (list.Count < count) list.Add(defaultValue);
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
    }

    private static int GetCharUpgradeNeedExpByLevel(int level)
    {
        foreach (var cfg in Config.character_upgrade_exp)
        {
            if (cfg.level == level) return cfg.exp;
        }
        return int.MaxValue;
    }

    private static int GetImprintUpgradeNeedExpByLevel(int level)
    {
        foreach (var cfg in Config.imprint_upgrade_exp)
        {
            if (cfg.level == level) return cfg.exp;
        }
        return int.MaxValue;
    }
    #endregion

#if UNITY_EDITOR
    #region Editor
    public void GenerateRandomSave()
    {
        characterLevels = CreateList(Config.character_count, 0);
        characterExp = CreateList(Config.character_count, 0);
        characterTokenCounts = CreateList(Config.character_count, 0);
        imprintLevels = CreateList(Config.imprint_count, 0);
        imprintExp = CreateList(Config.imprint_count, 0);
        noteCounts = CreateList(Config.note_count, 0);
        runeCounts = CreateList(Config.skill_count, 0);

        for (int i = 0; i < Config.character_count; i++)
        {
            if (UnityEngine.Random.value < 0.7f)
            {
                characterLevels[i] = UnityEngine.Random.Range(Config.min_entity_level, Config.max_entity_level + 1);
            }
            characterTokenCounts[i] = UnityEngine.Random.Range(0, 6);
        }

        for (int i = 0; i < Config.imprint_count; i++)
        {
            imprintLevels[i] = UnityEngine.Random.Range(0, Config.max_imprint_level + 1);
        }

        for (int i = 0; i < Config.note_count; i++)
        {
            noteCounts[i] = UnityEngine.Random.Range(0, 100);
        }

        for (int i = 0; i < Config.skill_count; i++)
        {
            runeCounts[i] = UnityEngine.Random.Range(0, 6);
        }

        InitDefaultCRuneCounts();
        Save();
        Debug.Log("随机测试存档生成完成");
    }

    [UnityEditor.MenuItem("Tool/Random Generate Save")]
    public static void MenuRandomGenerateSave()
    {
        if (Tool.SaveManager != null)
        {
            Tool.SaveManager.GenerateRandomSave();
        }
        else
        {
            UnityEditor.EditorUtility.DisplayDialog("错误", "SaveManager 实例未初始化，请先运行游戏。", "OK");
        }
    }
    #endregion
#endif
}
