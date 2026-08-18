using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePage : PageBase
{
    [Header("Player")]
    [SerializeField] private Image characterIcon;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthLabel;
    [SerializeField] private Image staminaFill;
    [SerializeField] private Text staminaLabel;

    [Header("Summary")]
    [SerializeField] private Text labelTime;
    [SerializeField] private List<BattleSkillUnit> skillUnits = new();

    [Header("Kills")]
    [SerializeField] private Text labelKillPlayer;
    [SerializeField] private Text labelKillZombie;
    [SerializeField] private Text labelKillPlant;
    [SerializeField] private Text labelKillOre;
    [SerializeField] private Text labelKillInfection;

    [Header("Exit")]
    [SerializeField] private GameObject exitProcessRoot;
    [SerializeField] private Image exitProcessFill;
    [SerializeField] private Text exitProcessLabel;
    [SerializeField] private Button lostButton;

    [Header("Scene UI")]
    [SerializeField] private List<EntityBar> entityBars = new();
    [SerializeField] private List<BattleLabel> battleLabels = new();

    private float enterTime;
    private bool inBattle;
    private float exitProcess;
    private int latestBattleSeconds;

    public override void Init()
    {
        EventManager.AddEvent<float>(ClientEvent.UpdateExitProcess, OnExitProcessUpdated);
        EventManager.AddEvent<BattleKillSummary>(ClientEvent.UpdateBattleKillSummary, RefreshKillSummary);
        EventManager.AddEvent<BattleHealthInfo>(ClientEvent.UpdateBattleHealth, RefreshHealth);
        EventManager.AddEvent<BattleStaminaInfo>(ClientEvent.UpdateBattleStamina, RefreshStamina);
        EventManager.AddEvent<BattleSkillRuntimeInfo>(ClientEvent.UpdateBattleSkillRuntime, RefreshSkillRuntime);
        EventManager.AddEvent<int>(ClientEvent.UpdateBattleTime, RefreshTime);
        EventManager.AddEvent<List<SceneLabelData>>(LabelPlayerManager.UpdateSceneLabelsEvent, OnUpdateSceneLabels);
        EventManager.AddEvent<List<SceneEntityBarData>>(LabelPlayerManager.UpdateSceneEntityBarsEvent, OnUpdateSceneEntityBars);
        if (lostButton != null)
        {
            lostButton.onClick.AddListener(() => Tool.ClientLogicManager.SettlementManager.Settle(SettlementManager.ExitMode.Lost));
        }
    }

    public override void Enter()
    {
        enterTime = Time.time;
        latestBattleSeconds = 0;
        exitProcess = 0f;
        inBattle = true;
        exitProcessRoot.SetActive(false);
        RefreshTime(0);
        RefreshKillSummary(default);
        RefreshCurrentHealth();
        RefreshCurrentStamina();

        //角色图标
        int characterIndex = GetSelectedCharacterIndex();
        characterIcon.sprite = Tool.AssetsManager.CharacterIcons[characterIndex];

        var skills = NetworkManager.playerInfo?.skills;
        Tool.ActiveFor(skillUnits, skills.Count);

        int activeSkillCount = skills.Count;
        for (int i = 0; i < activeSkillCount; i++)
        {
            var skill = GetSkillAt(skills, i);
            skillUnits[i].RefreshBasic(skill.Key);
            skillUnits[i].RefreshRuntime(skill.Value, 0f, i == 0);
        }
        RefreshExitProcess();
    }

    public override void Exit()
    {
        inBattle = false;
    }

    /// <summary>
    /// 接收 LabelPlayerManager 每帧推送的飘字数据列表，批量刷新一帧内所有飘字。
    /// Tool.ActiveFor 统一处理实例化与 active 状态，转换失败的飘字单独隐藏。
    /// </summary>
    private void OnUpdateSceneLabels(List<SceneLabelData> list)
    {
        // 不超过预配置数量，避免 ActiveFor 额外实例化 UI
        int count = Mathf.Min(list.Count, battleLabels.Count);
        Tool.ActiveFor(battleLabels, count);
        for (int i = 0; i < count; i++)
        {
            var label = battleLabels[i];
            if (label == null) continue;

            var data = list[i];
            if (LabelPlayerManager.TryWorldToParentPoint(data.worldPos, label.RectTransform, out var layerPos, out float scale))
            {
                label.Refresh(data.content, data.color, data.createTime, data.lifeTime,
                    data.alphaCurve, data.scaleCurve, data.riseCurve, layerPos, scale);
            }
            else
            {
                // 世界坐标在屏幕外（如相机背后）：隐藏该飘字，避免残留上一帧内容
                label.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 接收 LabelPlayerManager 每帧推送的血条数据列表，批量刷新一帧内所有血条。
    /// Tool.ActiveFor 统一处理实例化与 active 状态，转换失败的血条单独隐藏。
    /// </summary>
    private void OnUpdateSceneEntityBars(List<SceneEntityBarData> list)
    {
        // 不超过预配置数量，避免 ActiveFor 额外实例化 UI
        int count = Mathf.Min(list.Count, entityBars.Count);
        Tool.ActiveFor(entityBars, count);
        for (int i = 0; i < count; i++)
        {
            var bar = entityBars[i];
            if (bar == null) continue;

            var data = list[i];
            if (LabelPlayerManager.TryWorldToParentPoint(data.worldPos, bar.RectTransform, out var layerPos, out float scale))
            {
                bar.RectTransform.anchoredPosition = layerPos;
                bar.RectTransform.localScale = Vector3.one * scale;
                bar.Refresh(data.health, data.maxHealth, data.level);
            }
            else
            {
                // 世界坐标在屏幕外（如相机背后）：隐藏该血条
                bar.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!inBattle) return;
        if (Mathf.FloorToInt(Time.time - enterTime) != latestBattleSeconds)
        {
            RefreshTime(Mathf.FloorToInt(Time.time - enterTime));
        }
        //RefreshCurrentSkillRuntime();
    }

    private void RefreshTime(int seconds)
    {
        latestBattleSeconds = seconds;
        if (labelTime != null) labelTime.text = FormatTime(seconds);
    }

    private void RefreshKillSummary(BattleKillSummary summary)
    {
        if (labelKillPlayer != null) labelKillPlayer.text = summary.player.ToString();
        if (labelKillZombie != null) labelKillZombie.text = summary.zombie.ToString();
        if (labelKillPlant != null) labelKillPlant.text = (summary.plant + summary.infectedPlant).ToString();
        if (labelKillOre != null) labelKillOre.text = (summary.ore + summary.infectedOre).ToString();
        if (labelKillInfection != null) labelKillInfection.text = summary.infection.ToString();
    }

    private void RefreshHealth(BattleHealthInfo info)
    {
        int maxHealth = Mathf.Max(1, info.maxHealth);
        int health = Mathf.Clamp(info.health, 0, maxHealth);
        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(health / (float)maxHealth);
        if (healthLabel != null) healthLabel.text = $"{health}/{maxHealth}";
    }

    private void RefreshCurrentHealth()
    {
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.TryGetLocalPlayerDisplayInfo(out var info))
        {
            RefreshHealth(new BattleHealthInfo
            {
                health = info.health,
                maxHealth = info.maxHealth
            });
            return;
        }
        RefreshHealth(default);
    }

    private void RefreshStamina(BattleStaminaInfo info)
    {
        float maxStamina = Mathf.Max(1f, info.maxStamina);
        float stamina = Mathf.Clamp(info.stamina, 0f, maxStamina);
        if (staminaFill != null) staminaFill.fillAmount = Mathf.Clamp01(stamina / maxStamina);
        if (staminaLabel != null) staminaLabel.text = $"{Mathf.CeilToInt(stamina)}/{Mathf.CeilToInt(maxStamina)}";
    }

    private void RefreshCurrentStamina()
    {
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.TryGetBattleStaminaInfo(out var info))
        {
            RefreshStamina(info);
            return;
        }
        RefreshStamina(default);
    }

    private void RefreshSkillRuntime(BattleSkillRuntimeInfo info)
    {
        if (info.slot < 0 || info.slot >= skillUnits.Count) return;
        skillUnits[info.slot].RefreshRuntime(info.remainingCount, info.cooldownRate, info.selected);
    }

    private void OnExitProcessUpdated(float value)
    {
        exitProcess = value;
        RefreshExitProcess();
    }

    private void RefreshExitProcess()
    {
        if (exitProcess < 0.01f || exitProcess > 0.99f)
        {
            exitProcessRoot.SetActive(false);
            return;
        }
        exitProcessRoot.SetActive(true);
        exitProcessFill.fillAmount = exitProcess;
        exitProcessLabel.text = exitProcess <= 0f ? "" : $"{Mathf.RoundToInt(exitProcess * 100f)}%";
    }

    private static string FormatTime(int seconds)
    {
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
    private static KeyValuePair<int, int> GetSkillAt(Dictionary<int, int> skills, int slot)
    {
        int index = 0;
        foreach (var skill in skills)
        {
            if (index == slot) return skill;
            index++;
        }
        return default;
    }

    private static int GetSelectedCharacterIndex()
    {
        if (NetworkManager.playerInfo == null) return HomePage.currentSelectedCharacter;
        var type = NetworkManager.playerInfo.type;
        return type.category == EntityCategory.Character ? type.value : HomePage.currentSelectedCharacter;
    }
}

