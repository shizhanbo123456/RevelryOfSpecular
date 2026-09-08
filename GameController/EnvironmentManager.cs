using UnityEngine;

/// <summary>
/// 环境管理器（昼夜表现与阶段状态）。
/// 昼夜节奏见策划案 13 章：白天 / 黄昏 / 夜晚 / 黎明（时长见 Config）。
/// 服务器权威推进（BattleManager），战斗开始与阶段切换时下发 SCDayNightInfo（阶段+已进行时间+时间流速）；
/// 客户端收到后按配置的阶段时长与流速自行推演推进，服务器数据包仅做权威校正。
/// 阶段变化统一走 EventManager（ClientEvent.OnDayNightChange，param=int 阶段）。
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    public const int Day = 0;
    public const int Dusk = 1;
    public const int Night = 2;
    public const int Dawn = 3;
    public const int PhaseCount = 4;

    /// <summary>当前昼夜阶段（服务器权威写入，客户端同步后自行推演）。</summary>
    public static int CurrentPhase { get; private set; } = Day;

    /// <summary>当前阶段已进行时间（秒）。</summary>
    public static float PhaseTime { get; private set; }

    /// <summary>时间流速倍率（1 = 正常；由服务器 SCDayNightInfo 下发）。</summary>
    public static float DayNightRate { get; private set; } = 1f;

    /// <summary>太阳光源（表现，TODO 绑定场景灯光）。</summary>
    public Light sun;

    private void Awake()
    {
        Tool.EnvironmentManager = this;
    }

    /// <summary>
    /// 每帧推进（客户端推演）：服务器由 BattleManager 权威推进，本方法只对客户端生效。
    /// 战斗进行中按 时间流速 × 阶段时长 自动切阶段；未开战不推演。
    /// </summary>
    private void Update()
    {
        if (BattleManager.AtServer) return;
        if (NetworkManager.battleInfo == null) return;

        PhaseTime += Time.deltaTime * DayNightRate;
        if (PhaseTime >= GetPhaseDuration(CurrentPhase))
        {
            SetPhase((CurrentPhase + 1) % PhaseCount);
        }
    }

    /// <summary>服务器权威同步（收到 SCDayNightInfo 时调用）：整体校正阶段/时间/流速。</summary>
    public void ApplyServerSync(int phase, float phaseTime, float rate)
    {
        CurrentPhase = Mathf.Clamp(phase, 0, PhaseCount - 1);
        PhaseTime = Mathf.Max(0f, phaseTime);
        DayNightRate = rate > 0f ? rate : 1f;
        ApplyVisual();
        EventManager.TrigEvent(ClientEvent.OnDayNightChange, CurrentPhase);
    }

    /// <summary>重置为白天开始（服务器开战时调用）。</summary>
    public void ResetDayNight()
    {
        SetPhase(Day);
    }

    /// <summary>设置阶段（服务器权威推进用；触发 OnDayNightChange 事件并刷新表现）。</summary>
    public void SetPhase(int phase)
    {
        CurrentPhase = Mathf.Clamp(phase, 0, PhaseCount - 1);
        PhaseTime = 0f;
        ApplyVisual();
        EventManager.TrigEvent(ClientEvent.OnDayNightChange, CurrentPhase);
    }

    /// <summary>推进阶段时间（仅服务器：由 BattleManager 驱动调用）。</summary>
    public void TickPhase(float deltaTime)
    {
        PhaseTime += deltaTime;
    }

    /// <summary>当前阶段总时长（秒）。</summary>
    public static float GetPhaseDuration(int phase)
    {
        switch (phase)
        {
            case Dusk: return Config.dusk_duration;
            case Night: return Config.night_duration;
            case Dawn: return Config.dawn_duration;
            default: return Config.day_duration;
        }
    }

    private void ApplyVisual()
    {
        // TODO: 天空盒/灯光/雾/色调随阶段切换（表现后续完善）
        if (sun != null)
        {
            // 简单示意：夜晚调暗
            sun.intensity = CurrentPhase == Night ? 0.15f : (CurrentPhase == Dawn || CurrentPhase == Dusk ? 0.5f : 1f);
        }
    }
}
