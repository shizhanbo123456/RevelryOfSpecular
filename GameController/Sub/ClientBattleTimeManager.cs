using Ros.Transport;
using UnityEngine;

public class ClientBattleTimeManager : ClientSubManager
{
    public SCScoreInfo LatestScore { get; private set; }

    public float RemainTime { get; private set; }

    public bool BattleOver { get; private set; }

    private float startTime;
    private bool running;

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
        RemainTime = Mathf.Max(0f, Config.battle_duration - (Time.time - startTime));
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
            RemainTime = Mathf.Max(0f, info.remainTime);
        }
        EventManager.TrigEvent(ClientEvent.OnScoreUpdate, info);
    }

    private void OnBattleStart(int camp)
    {
        startTime = Time.time;
        running = true;
        RemainTime = Config.battle_duration;
        LatestScore = null;
        BattleOver = false;
    }
}
