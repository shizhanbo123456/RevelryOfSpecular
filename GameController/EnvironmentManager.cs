using UnityEngine;

/// <summary>
/// 环境管理器（昼夜表现与阶段状态）。
/// 昼夜节奏见策划案 13 章：白天 3 分钟 / 黄昏 20 秒 / 夜晚 2 分钟 / 黎明 20 秒。
/// 阶段推进由 BattleManager（服务器权威）驱动，切换时广播 SCBattleEvent(DayNight)；
/// 客户端接收后同样调用 SetPhase 更新表现。
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    public const int Day = 0;
    public const int Dusk = 1;
    public const int Night = 2;
    public const int Dawn = 3;
    public const int PhaseCount = 4;

    /// <summary>当前昼夜阶段（服务器权威写入，客户端接收同步）。</summary>
    public static int CurrentPhase { get; private set; } = Day;

    /// <summary>当前阶段已进行时间（秒）。</summary>
    public static float PhaseTime { get; private set; }

    /// <summary>阶段切换事件（参数=新阶段）。</summary>
    public static event System.Action<int> OnPhaseChanged;

    /// <summary>太阳光源（表现，TODO 绑定场景灯光）。</summary>
    public Light sun;

    private void Awake()
    {
        Tool.EnvironmentManager = this;
    }

    /// <summary>重置为白天开始。</summary>
    public void ResetDayNight()
    {
        SetPhase(Day);
    }

    /// <summary>设置阶段（服务器与客户端共用；服务器在切换时调用并广播）。</summary>
    public void SetPhase(int phase)
    {
        CurrentPhase = Mathf.Clamp(phase, 0, PhaseCount - 1);
        PhaseTime = 0f;
        OnPhaseChanged?.Invoke(CurrentPhase);
        ApplyVisual();
    }

    /// <summary>推进阶段时间（由 BattleManager 服务器驱动调用）。</summary>
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
