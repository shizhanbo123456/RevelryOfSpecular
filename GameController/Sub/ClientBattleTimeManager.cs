using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗时间子管理器（客户端逻辑）：昼夜快照转交与本机对局时间轴。
/// 服务器只在终局下发一次权威分数（SCScoreInfo），局内剩余时间只能由本机从开战时刻推演。
/// </summary>
public class ClientBattleTimeManager : ClientSubManager
{
    /// <summary>最近一次收到的权威分数快照（局内为 null，终局才有值）。</summary>
    public SCScoreInfo LatestScore { get; private set; }

    /// <summary>对局剩余时间（秒）：局内为本机推演，收到终局快照后被权威值校准。</summary>
    public float RemainTime { get; private set; }

    /// <summary>对局是否已结束（收到非 0 的 gameState）。</summary>
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

    /// <summary>接收服务器昼夜快照（周期时间 + 两个时长）：交由 EnvironmentManager 应用后自行推演。</summary>
    public void OnDayNightSync(SCDayNightInfo info)
    {
        if (info == null) return;
        Tool.EnvironmentManager?.ApplyServerSync(info.cycleTime, info.dayDuration, info.nightDuration);
    }

    /// <summary>接收服务器分数：缓存快照，并照旧广播给表现层（UI 的数据来源不变）。</summary>
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
