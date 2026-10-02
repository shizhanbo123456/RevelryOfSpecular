using Ros.Transport;
using UnityEngine;
using VolumetricFogAndMist;

public class EnvironmentManager : MonoBehaviour
{
    public const float CycleLength = 2f;

    public const float DayNightBoundary = 0.5f;

    public const float SkyExposureMidnight = 0.28f;
    public const float SkyExposureNoon = 1.92f;
    public const string SkyExposureProperty = "_Exposure";

    public const float SunIntensityMax = 1.5f;
    public const float SunRotationDawn = 0f;
    public const float SunRotationEvening = 180f;

    public const float FogVoidFallOffOn = 0f;
    public const float FogVoidFallOffOff = 1.55f;

    [Header("昼夜时长（秒）")]
    [Tooltip("白天时长：光照值 ≥ 0.5 区间的总耗时。修改后只改变推进速率，当前时间不变。\n（客户端上的值仅作首帧前的初始值，运行时会被服务器下发的值覆盖）")]
    [Min(0.01f)] public float dayDuration = 90f;

    [Tooltip("晚上时长：光照值 < 0.5 区间的总耗时。修改后只改变推进速率，当前时间不变。\n（客户端上的值仅作首帧前的初始值，运行时会被服务器下发的值覆盖）")]
    [Min(0.01f)] public float nightDuration = 90f;

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

    public static float CycleTime { get; private set; } = 1f;

    public static int Direction => CycleTime < 1f ? 1 : -1;

    public static float Time01 => 1f - Mathf.Abs(CycleTime - 1f);

    public static bool IsDay => Time01 >= DayNightBoundary;

    public static int State => IsDay ? 1 : 0;

    public static float DayFactor => Mathf.Clamp01((Time01 - DayNightBoundary) / DayNightBoundary);

    public static event System.Action<bool> DayNightFlipped;

    private float lastDayDuration;
    private float lastNightDuration;
    private bool syncRequested;

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
        NotifyFlipped(wasDay);

        // 外部改了任一昼夜时长 → 置位同步请求，由服务器补发完整快照
        if (dayDuration != lastDayDuration || nightDuration != lastNightDuration)
        {
            lastDayDuration = dayDuration;
            lastNightDuration = nightDuration;
            syncRequested = true;
        }

        return flipped;
    }

    public void ApplyServerSync(float cycleTime, float dayDuration, float nightDuration)
    {
        bool wasDay = IsDay;
        this.dayDuration = Mathf.Max(0.01f, dayDuration);
        this.nightDuration = Mathf.Max(0.01f, nightDuration);
        lastDayDuration = this.dayDuration;
        lastNightDuration = this.nightDuration;
        CycleTime = Mathf.Repeat(cycleTime, CycleLength);
        ApplyVisual();
        NotifyFlipped(wasDay);
    }

    public void SetCycleTime(float cycleTime)
    {
        bool wasDay = IsDay;
        CycleTime = Mathf.Repeat(cycleTime, CycleLength);
        ApplyVisual();
        NotifyFlipped(wasDay);
        syncRequested = true;
    }

    public void SetFogEnabled(bool enabled)
    {
        fogEnabled = enabled;
    }

    public void ResetDayNight()
    {
        bool wasDay = IsDay;
        CycleTime = 1f;
        ApplyVisual();
        NotifyFlipped(wasDay);
    }

    public SCDayNightInfo BuildSnapshot()
    {
        return new SCDayNightInfo()
        {
            cycleTime = CycleTime,
            dayDuration = dayDuration,
            nightDuration = nightDuration,
        };
    }

    public bool ConsumeSyncRequest()
    {
        bool requested = syncRequested;
        syncRequested = false;
        return requested;
    }

    private void ApplyVisual()
    {
        ApplySkyboxExposure();
        ApplySun();
        ApplyFogColor();
        ApplyFogVoid();
    }

    private void NotifyFlipped(bool wasDay)
    {
        if (wasDay == IsDay) return;
        EventManager.TrigEvent(ClientEvent.OnDayNightChange, State);
        if (BattleManager.AtServer && DayNightFlipped != null) DayNightFlipped.Invoke(IsDay);
    }

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

    private void ApplyFogColor()
    {
        if (fog == null) return;
        Color target = fogAlbedoNoon * DayFactor;
        target.a = 1f;
        if (fog.color != target) fog.color = target;
    }

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
