using Ros.Transport;
using UnityEngine;

public class ClientBattleTimeManager : ClientSubManager
{
    public SCScoreInfo LatestScore { get; private set; }

    public float RemainTime { get; private set; }

    public bool BattleOver { get; private set; }

    private float startTime;
    private bool running;
    private bool hasResyncSnapshot; // 加载期收到过中途快照：开战回执不得把它覆盖成满时长

    protected override void BindEvents()
    {
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
    }

    public override void Tick(float deltaTime)
    {
        // 开战前与终局后都不推演，否则在菜单里待久了剩余时间会被算成 0
        if (!running || BattleOver) return;
        RemainTime = Mathf.Max(0f, ConfigManager.GlobalVariableConfig.battle_duration - (Time.time - startTime));
    }

    public void OnDayNightSync(SCDayNightInfo info)
    {
        if (info == null) return;
        if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.ApplyServerSync(info.cycleTime, info.dayDuration, info.nightDuration);
    }

    public void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        LatestScore = info;
        if (info.gameState != 0)
        {
            BattleOver = true;
            hasResyncSnapshot = false;
            RemainTime = Mathf.Max(0f, info.remainTime);
        }
        else if (info.resync)
        {
            // 中途快照（重连 / 中途进入）：用服务器剩余时间反推本地起点，否则断线期间的时间会被算成已消耗
            startTime = Time.time - Mathf.Max(0f, ConfigManager.GlobalVariableConfig.battle_duration - info.remainTime);
            running = true;
            BattleOver = false;
            hasResyncSnapshot = true;
            RemainTime = Mathf.Max(0f, info.remainTime);
        }
        EventManager.TrigEvent(ClientEvent.OnScoreUpdate, info);
    }

    private void OnBattleStart(int camp)
    {
        LatestScore = null;
        BattleOver = false;
        running = true;
        // 重连 / 中途进入：起点与剩余时间沿用加载期快照；正常开战从满时长起算
        bool fromSnapshot = hasResyncSnapshot && ClientLoadingManager.Current != null && ClientLoadingManager.Current.IsResync;
        hasResyncSnapshot = false;
        if (fromSnapshot) return;
        startTime = Time.time;
        RemainTime = ConfigManager.GlobalVariableConfig.battle_duration;
    }
}
