using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档管理器（局外养成，策划案 8.1：只保留角色解锁 + 角色升级；双等级制见 8.1）。
/// 玩家等级（账号级，解锁角色）+ 角色等级（每角色独立，属性成长）。
/// PlayerPrefs 存档（"|" 分隔）。修改即保存。
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string SaveKey = "GameSaveDataV3";

    private void Awake()
    {
        Tool.SaveManager = this;
        LoadOrCreate();
    }

    #region 数据
    /// <summary>玩家等级（账号级）。</summary>
    public int playerLevel = 1;
    /// <summary>玩家经验。</summary>
    public int playerExp;

    /// <summary>角色等级（全局角色索引 0~23，进攻 0~17 / 防守 18~23）。</summary>
    public List<int> characterLevels = new();
    /// <summary>角色经验。</summary>
    public List<int> characterExp = new();
    /// <summary>角色解锁。</summary>
    public List<bool> characterUnlocked = new();

    /// <summary>全局角色数量（进攻 + 防守）。</summary>
    public static int CharacterTotalCount => Config.attack_character_count + Config.defense_character_count;
    #endregion

    #region 查询
    public int GetCharacterLevel(int index)
    {
        if (index < 0 || index >= characterLevels.Count) return 1;
        return characterLevels[index];
    }

    public bool IsCharacterUnlocked(int index)
    {
        if (index < 0 || index >= characterUnlocked.Count) return false;
        return characterUnlocked[index];
    }

    /// <summary>按玩家等级是否解锁：读角色 SO 的 unlockPlayerLevel（策划案 10.2，达标自动解锁；SO 未配置视为已解锁）。</summary>
    public bool IsUnlockedByPlayerLevel(int characterIndex)
    {
        var info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(characterIndex) : null;
        return info == null || playerLevel >= info.unlockPlayerLevel;
    }
    #endregion

    #region 修改
    /// <summary>解锁角色。</summary>
    public void UnlockCharacter(int index)
    {
        EnsureListSize(index);
        characterUnlocked[index] = true;
        Save();
    }

    /// <summary>
    /// 给角色加经验（策划案 17.3：获得经验 = 对水晶造成的伤害量；
    /// 升级所需经验表见 Config.level_up_exp，从 1→2 级起依次取用）。
    /// </summary>
    public void AddCharacterExp(int index, int exp)
    {
        EnsureListSize(index);
        characterExp[index] += exp;
        int level = characterLevels[index];
        while (level < Config.max_entity_level)
        {
            int need = Config.level_up_exp[level - 1];
            if (characterExp[index] < need) break;
            characterExp[index] -= need;
            level++;
        }
        characterLevels[index] = level;
        Save();
    }

    /// <summary>给玩家加经验（账号级；每级所需经验公式见 Config.GetPlayerLevelUpExp）。</summary>
    public void AddPlayerExp(int exp)
    {
        playerExp += exp;
        while (playerLevel < Config.player_max_level && playerExp >= Config.GetPlayerLevelUpExp(playerLevel))
        {
            playerExp -= Config.GetPlayerLevelUpExp(playerLevel);
            playerLevel++;
        }
        Save();
    }
    #endregion

    #region 序列化（PlayerPrefs，| 分隔）
    private void EnsureListSize(int index)
    {
        while (characterLevels.Count < CharacterTotalCount) characterLevels.Add(1);
        while (characterExp.Count < CharacterTotalCount) characterExp.Add(0);
        while (characterUnlocked.Count < CharacterTotalCount)
        {
            characterUnlocked.Add(characterUnlocked.Count < Config.initial_unlocked_character_count);
        }
    }

    private void LoadOrCreate()
    {
        EnsureListSize(0);
        string data = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(data))
        {
            Save();
            return;
        }
        try
        {
            var parts = data.Split('|');
            int idx = 0;
            playerLevel = int.Parse(parts[idx++]);
            playerExp = int.Parse(parts[idx++]);
            int count = int.Parse(parts[idx++]);
            for (int i = 0; i < count && idx < parts.Length; i++) characterLevels[i] = int.Parse(parts[idx++]);
            for (int i = 0; i < count && idx < parts.Length; i++) characterExp[i] = int.Parse(parts[idx++]);
            for (int i = 0; i < count && idx < parts.Length; i++) characterUnlocked[i] = parts[idx++] == "1";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"存档解析失败，已重建：{e.Message}");
            Save();
        }
    }

    private void Save()
    {
        EnsureListSize(0);
        var sb = new System.Text.StringBuilder();
        sb.Append(playerLevel).Append('|').Append(playerExp).Append('|');
        sb.Append(CharacterTotalCount).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterLevels[i]).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterExp[i]).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterUnlocked[i] ? "1" : "0").Append('|');
        PlayerPrefs.SetString(SaveKey, sb.ToString());
        PlayerPrefs.Save();
    }

    [ContextMenu("ClearSave")]
    private void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        playerLevel = 1;
        playerExp = 0;
        EnsureListSize(0);
        Save();
        Debug.Log("存档已清除");
    }
    #endregion
}
