using UnityEngine;

//周期计时器：每 interval 秒返回一次 true，取到 true 时把计时往后推一个 interval
public class Ticker
{
    private float lastTick;
    private float interval;

    public Ticker(float interval) : this(interval, false)
    {
        lastTick = Time.time - interval; // 首帧即触发
    }

    public Ticker(float interval, bool random)
    {
        if (interval < 0.0001f) throw new System.Exception($"Ticker 间隔必须大于 0，当前 {interval}");
        this.interval = interval;
        lastTick = random ? Time.time - Random.Range(0f, interval) : Time.time;
    }

    //立即重开计时：下一次取值要等满一个 interval
    public void Reset()
    {
        lastTick = Time.time;
    }

    public bool Tick()
    {
        if (Time.time - lastTick <= interval) return false;
        lastTick += interval; // 掉帧欠的账由随后几帧连续返回 true 补齐
        return true;
    }

    public static implicit operator bool(Ticker ticker)
    {
        return ticker.Tick();
    }
}
