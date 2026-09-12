using Ros.Transport;
using UnityEngine;
using VolumetricFogAndMist;

/// <summary>
/// 环境管理器（昼夜时间与表现）。
/// 时间用归一化周期值 <see cref="CycleTime"/> 表示，范围 [0, 2) 循环：
///   [0, 1) —— 午夜(0) → 正午(1)：**变亮**（Direction = +1）
///   [1, 2) —— 正午(1) → 午夜(2)：**变暗**（Direction = -1）
///   到达 2 后回绕回 0（午夜），继续循环。
/// **方向由 CycleTime 自身推导**，因此服务器只需把「时间 + 白天时长 + 晚上时长」三个值下发给客户端，
/// 客户端即可完整复现推演，无需再传方向。
/// 每秒的归一化变化量 = 1 / 当前所处区间的时长（**倒数**）：修改昼夜时长只改变推进速率，
/// **不会跳变当前时间**（改时长的那一刻时间保持不变，只是之后走得快/慢）。
/// 白天 / 晚上以光照值 <see cref="DayNightBoundary"/>(0.5) 为界，对应 CycleTime 的 0.5 与 1.5：
/// 白天用 dayDuration，晚上用 nightDuration，一个完整周期 = dayDuration + nightDuration。
/// 服务器权威推进（BattleManager 每帧 Tick + 每 Config.daynight_sync_interval 秒心跳下发完整快照）；
/// 客户端按服务器下发的三个参数自行推演，时长字段会被服务器值覆盖。
/// 白天/晚上翻转统一走 EventManager（ClientEvent.OnDayNightChange，param = 1 白天 / 0 晚上）。
/// 表现（<see cref="ApplyVisual"/>）：天空盒 _Exposure、太阳强度与旋转、体积雾的 Fog Colors→Albedo 与 Fog Void FallOff。
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    /// <summary>周期长度（CycleTime 的上界，到达后回绕到 0）。</summary>
    public const float CycleLength = 2f;

    /// <summary>白天 / 晚上的光照分界值（0.5，对应 CycleTime 的 0.5 与 1.5）。</summary>
    public const float DayNightBoundary = 0.5f;

    /// <summary>天空盒曝光：午夜值。</summary>
    public const float SkyExposureMidnight = 0.28f;
    /// <summary>天空盒曝光：正午值。</summary>
    public const float SkyExposureNoon = 1.92f;
    /// <summary>天空盒曝光属性名（Unity 内置 Procedural 天空盒）。</summary>
    public const string SkyExposureProperty = "_Exposure";

    /// <summary>太阳强度峰值（正午）。区间外为 0。</summary>
    public const float SunIntensityMax = 1.5f;
    /// <summary>太阳旋转：凌晨（日出）X 角度。</summary>
    public const float SunRotationDawn = 0f;
    /// <summary>太阳旋转：傍晚（日落）X 角度。</summary>
    public const float SunRotationEvening = 180f;

    /// <summary>有雾效时 Fog Void 的 FallOff。</summary>
    public const float FogVoidFallOffOn = 0f;
    /// <summary>无雾效时 Fog Void 的 FallOff。</summary>
    public const float FogVoidFallOffOff = 1.55f;

    [Header("昼夜时长（秒）")]
    [Tooltip("白天时长：光照值 ≥ 0.5 区间的总耗时。修改后只改变推进速率，当前时间不变。\n（客户端上的值仅作首帧前的初始值，运行时会被服务器下发的值覆盖）")]
    [Min(0.01f)] public float dayDuration = 120f;

    [Tooltip("晚上时长：光照值 < 0.5 区间的总耗时。修改后只改变推进速率，当前时间不变。\n（客户端上的值仅作首帧前的初始值，运行时会被服务器下发的值覆盖）")]
    [Min(0.01f)] public float nightDuration = 60f;

    [Header("表现引用（两套场景各自挂自己那份；服务器可不填）")]
    [Tooltip("天空盒材质球：昼夜变化时线性调整其 " + SkyExposureProperty + "（午夜 0.28 ~ 正午 1.92）")]
    public Material skyboxMaterial;

    [Tooltip("体积雾组件（Volumetric Fog & Mist）：按是否有雾效平滑调整其 Fog Void 的 FallOff")]
    public VolumetricFog fog;

    [Header("雾气")]
    [Tooltip("是否启用雾效：开启 = Fog Void FallOff 0，关闭 = 1.55；切换时按 fogTransitionDuration 秒线性过渡。（客户端由「迷雾」Buff 自动驱动，此处只是初始值）")]
    public bool fogEnabled = false;

    [Tooltip("雾效开关的过渡时间（秒）")]
    [Min(0.01f)] public float fogTransitionDuration = 10f;

    [Tooltip("雾气 Albedo（Fog Colors → Albedo）在正午的颜色（8bit 64,82,89）；凌晨 / 傍晚（及夜间）线性衰减为黑")]
    public Color fogAlbedoNoon = new Color(64f / 255f, 82f / 255f, 89f / 255f, 1f);

    [Header("太阳")]
    [Tooltip("太阳（平行光）：强度与旋转由昼夜时间驱动")]
    public Light sun;

    /// <summary>归一化周期时间 [0, 2)：0 / 2 = 午夜，1 = 正午。</summary>
    public static float CycleTime { get; private set; } = 1f;

    /// <summary>推进方向（由 CycleTime 推导）：CycleTime &lt; 1 为变亮(+1)，≥ 1 为变暗(-1)。</summary>
    public static int Direction => CycleTime < 1f ? 1 : -1;

    /// <summary>光照归一值 [0, 1]：1 = 正午，0 = 午夜（= 1 - |CycleTime - 1|）。</summary>
    public static float Time01 => 1f - Mathf.Abs(CycleTime - 1f);

    /// <summary>是否白天（光照值 ≥ <see cref="DayNightBoundary"/>，等价于 CycleTime ∈ [0.5, 1.5)）。</summary>
    public static bool IsDay => Time01 >= DayNightBoundary;

    /// <summary>当前昼夜状态（1 = 白天，0 = 晚上），供 UI 初始化使用。</summary>
    public static int State => IsDay ? 1 : 0;

    /// <summary>白天进度因子 [0,1]：凌晨 / 傍晚为 0，正午为 1（太阳强度与雾气 Albedo 共用同一条三角波）。</summary>
    public static float DayFactor => Mathf.Clamp01((Time01 - DayNightBoundary) / DayNightBoundary);

    private float lastDayDuration;
    private float lastNightDuration;
    private bool syncRequested;

    /// <summary>Fog Void 的 FallOff 当前值（在 0 / 1.55 之间按过渡时间线性推进）。</summary>
    private float fogVoidFallOffCurrent;
    private int skyExposureId;
    private bool exposureWarningLogged;

    private void Awake()
    {
        Tool.EnvironmentManager = this;
        skyExposureId = Shader.PropertyToID(SkyExposureProperty);
        lastDayDuration = dayDuration;
        lastNightDuration = nightDuration;
        fogVoidFallOffCurrent = fogEnabled ? FogVoidFallOffOn : FogVoidFallOffOff;
        ApplyVisual();
    }

    /// <summary>客户端自行推演（服务器由 BattleManager 权威推进；仅对局进行中推演）。</summary>
    private void Update()
    {
        if (BattleManager.AtServer) return;
        if (!NetworkManager.BattleRunning)
        {
            // 非对局中不推进昼夜，但雾效开关的过渡仍要能走完（允许在组队大厅切换雾效）
            ApplyFogVoid();
            return;
        }
        Tick(Time.deltaTime);
    }

    /// <summary>
    /// 推进昼夜时间。服务器由 BattleManager 每帧调用，客户端由 Update 调用。
    /// 返回 true 表示本帧跨过了白天/晚上分界（仅用于内部通知，同步由外部的快照下发负责）。
    /// </summary>
    public bool Tick(float deltaTime)
    {
        bool wasDay = IsDay;

        // 速率 = 1 / 当前区间时长（倒数）：改时长只变速，不改当前时间
        float duration = wasDay ? dayDuration : nightDuration;
        if (duration < 0.0001f) duration = 0.0001f;
        CycleTime += Direction * (deltaTime / duration);

        // 周期回绕：[0, 2) 循环（0 与 2 都是午夜）
        CycleTime %= CycleLength;
        if (CycleTime < 0f) CycleTime += CycleLength;

        bool flipped = wasDay != IsDay;
        ApplyVisual();
        if (flipped) EventManager.TrigEvent(ClientEvent.OnDayNightChange, State);

        // 外部改了任一昼夜时长 → 置位同步请求，由服务器补发完整快照
        if (dayDuration != lastDayDuration || nightDuration != lastNightDuration)
        {
            lastDayDuration = dayDuration;
            lastNightDuration = nightDuration;
            syncRequested = true;
        }

        return flipped;
    }

    /// <summary>客户端应用服务器下发的完整快照（周期时间 + 两个时长），之后完全自行推演。</summary>
    public void ApplyServerSync(float cycleTime, float dayDuration, float nightDuration)
    {
        bool wasDay = IsDay;
        this.dayDuration = Mathf.Max(0.01f, dayDuration);
        this.nightDuration = Mathf.Max(0.01f, nightDuration);
        lastDayDuration = this.dayDuration;
        lastNightDuration = this.nightDuration;
        CycleTime = Mathf.Repeat(cycleTime, CycleLength);
        ApplyVisual();
        if (wasDay != IsDay) EventManager.TrigEvent(ClientEvent.OnDayNightChange, State);
    }

    /// <summary>外部设置当前周期时间（[0,2)，非正值/超界自动回绕）。会请求服务器补发快照。</summary>
    public void SetCycleTime(float cycleTime)
    {
        bool wasDay = IsDay;
        CycleTime = Mathf.Repeat(cycleTime, CycleLength);
        ApplyVisual();
        if (wasDay != IsDay) EventManager.TrigEvent(ClientEvent.OnDayNightChange, State);
        syncRequested = true;
    }

    /// <summary>设置是否启用雾效（Fog Void 的 FallOff 在 0 / 1.55 之间按过渡时间线性变化）。</summary>
    public void SetFogEnabled(bool enabled)
    {
        fogEnabled = enabled;
    }

    /// <summary>重置为正午（服务器开战时调用）。</summary>
    public void ResetDayNight()
    {
        bool wasDay = IsDay;
        CycleTime = 1f;
        ApplyVisual();
        if (wasDay != IsDay) EventManager.TrigEvent(ClientEvent.OnDayNightChange, State);
    }

    /// <summary>构造当前完整快照（服务器下发给客户端用）。</summary>
    public SCDayNightInfo BuildSnapshot()
    {
        return new SCDayNightInfo()
        {
            cycleTime = CycleTime,
            dayDuration = dayDuration,
            nightDuration = nightDuration,
        };
    }

    /// <summary>服务器：取出并清除「需要补发快照」的请求（时长被改动或外部改时间时置位）。</summary>
    public bool ConsumeSyncRequest()
    {
        bool requested = syncRequested;
        syncRequested = false;
        return requested;
    }

    /// <summary>刷新全部昼夜表现（天空盒曝光 / 太阳强度与旋转 / 雾效）。每帧由 Tick 调用。</summary>
    private void ApplyVisual()
    {
        ApplySkyboxExposure();
        ApplySun();
        ApplyFogColor();
        ApplyFogVoid();
    }

    /// <summary>天空盒曝光：午夜 0.28 ~ 正午 1.92，按光照值线性插值。</summary>
    private void ApplySkyboxExposure()
    {
        if (skyboxMaterial == null) return;
        if (!skyboxMaterial.HasProperty(skyExposureId))
        {
            if (!exposureWarningLogged)
            {
                exposureWarningLogged = true;
                Debug.LogWarning($"[EnvironmentManager] 天空盒材质 {skyboxMaterial.name} 没有 {SkyExposureProperty} 属性，曝光不会被调整。");
            }
            return;
        }
        skyboxMaterial.SetFloat(skyExposureId, Mathf.Lerp(SkyExposureMidnight, SkyExposureNoon, Time01));
    }

    /// <summary>
    /// 太阳：强度为白天区间 [0.5, 1.5] 上的三角波（0.5 / 1.5 处为 0，正午 1 处为 1.5，区间外为 0）；
    /// 旋转只在白天推进（凌晨 0° → 正午 90° → 傍晚 180°），晚上保持不变。
    /// </summary>
    private void ApplySun()
    {
        if (sun == null) return;

        sun.intensity = DayFactor * SunIntensityMax;

        if (IsDay)
        {
            // CycleTime - 0.5 ∈ [0,1] 覆盖白天区间：0.5 → 0°，1 → 90°，1.5 → 180°
            float x = (CycleTime - DayNightBoundary) * SunRotationEvening;
            sun.transform.eulerAngles = new Vector3(x, 0f, 0f);
        }
    }

    /// <summary>
    /// 雾气 Albedo（Fog Colors → Albedo）：正午为 <see cref="fogAlbedoNoon"/>，凌晨 / 傍晚为黑，两端之间线性插值。
    /// 与太阳强度共用 <see cref="DayFactor"/> 这条三角波。（客户端表现，服务器场景可不配）
    /// </summary>
    private void ApplyFogColor()
    {
        if (fog == null) return;
        Color target = fogAlbedoNoon * DayFactor;
        target.a = 1f;
        if (fog.color != target) fog.color = target;
    }

    /// <summary>
    /// 雾效：Fog Void 的 FallOff 在「有雾效 = 0」「无雾效 = 1.55」之间按 fogTransitionDuration 秒线性过渡。
    /// </summary>
    private void ApplyFogVoid()
    {
        float target = fogEnabled ? FogVoidFallOffOn : FogVoidFallOffOff;
        if (!Mathf.Approximately(fogVoidFallOffCurrent, target))
        {
            float speed = (FogVoidFallOffOff - FogVoidFallOffOn) / Mathf.Max(0.01f, fogTransitionDuration);
            fogVoidFallOffCurrent = Mathf.MoveTowards(fogVoidFallOffCurrent, target, speed * Time.deltaTime);
        }
        if (fog != null && !Mathf.Approximately(fog.fogVoidFallOff, fogVoidFallOffCurrent))
        {
            fog.fogVoidFallOff = fogVoidFallOffCurrent;
        }
    }
}
