using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string SaveKey = "GameSaveDataV3";

    private void Awake()
    {
        Tool.SaveManager = this;
        LoadOrCreate();
        ClientSelection.playerName = playerName;
    }

    #region 数据
    public int playerLevel = 1;
    public int playerExp;

    public string playerName = "";

    public List<int> characterLevels = new();
    public List<int> characterExp = new();
    public List<bool> characterUnlocked = new();

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
        if (index < 0 || index >= characterUnlocked.Count) return IsUnlockedByPlayerLevel(index);
        return characterUnlocked[index] || IsUnlockedByPlayerLevel(index);
    }

    public bool IsUnlockedByPlayerLevel(int characterIndex)
    {
        var info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(characterIndex) : null;
        return info == null || playerLevel >= info.unlockPlayerLevel;
    }
    #endregion

    #region 修改
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
            characterUnlocked.Add(false); // 解锁由角色 SO 的 unlockPlayerLevel 判定，存档标记默认全 false
        }
    }

    private void LoadOrCreate()
    {
        EnsureListSize(0);
        string data = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(data))
        {
            CreateNewSave();
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
            if (idx < parts.Length) playerName = parts[idx++];
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"存档解析失败，已重建：{e.Message}");
            CreateNewSave();
            return;
        }

        // 名字为空 → 视为无效存档 → 新建存档（随机六位数名字）
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("存档玩家名为空，视为无效，新建存档");
            CreateNewSave();
        }
    }

    public void Save()
    {
        EnsureListSize(0);
        var sb = new System.Text.StringBuilder();
        sb.Append(playerLevel).Append('|').Append(playerExp).Append('|');
        sb.Append(CharacterTotalCount).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterLevels[i]).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterExp[i]).Append('|');
        for (int i = 0; i < CharacterTotalCount; i++) sb.Append(characterUnlocked[i] ? "1" : "0").Append('|');
        sb.Append((playerName ?? "").Replace("|", ""));
        PlayerPrefs.SetString(SaveKey, sb.ToString());
        PlayerPrefs.Save();
    }

    private string GenerateRandomName()
    {
        return UnityEngine.Random.Range(100000, 1000000).ToString();
    }

    private void CreateNewSave()
    {
        playerLevel = 1;
        playerExp = 0;
        characterLevels.Clear();
        characterExp.Clear();
        characterUnlocked.Clear();
        EnsureListSize(0);
        playerName = GenerateRandomName();
        Save();
    }

    [ContextMenu("ClearSave")]
    private void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        CreateNewSave();
        Debug.Log("存档已清除");
    }
    #endregion
}
