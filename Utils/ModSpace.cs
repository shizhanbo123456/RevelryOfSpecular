using System;

/// <summary>1维模空间</summary>
public readonly struct ModSpace1
{
    private readonly int _m1;
    public int TotalSize { get; }

    public ModSpace1(int m1)
    {
        if (m1 < 1)
            throw new ArgumentOutOfRangeException(nameof(m1), "必须 ≥ 1");

        long total = m1;
        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1;
        TotalSize = (int)total;
    }

    public int Encode(int v1)
    {
        if (v1 < 0 || v1 >= _m1)
            throw new ArgumentOutOfRangeException(nameof(v1));
        return v1;
    }

    public void Decode(int value, out int v1)
    {
        if (value < 0 || value >= TotalSize)
            throw new ArgumentOutOfRangeException(nameof(value));
        v1 = value;
    }
}

/// <summary>2维模空间</summary>
public readonly struct ModSpace2
{
    private readonly int _m1;
    private readonly int _m2;
    private readonly int _b1;

    public int TotalSize { get; }

    public ModSpace2(int m1, int m2)
    {
        if (m1 < 1 || m2 < 1)
            throw new ArgumentOutOfRangeException("维度大小必须 ≥ 1");

        long b1 = m1;
        long total = b1 * m2;

        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1;
        _m2 = m2;
        _b1 = (int)b1;
        TotalSize = (int)total;
    }

    public int Encode(int v1, int v2)
    {
        if (v1 < 0 || v1 >= _m1) throw new ArgumentOutOfRangeException(nameof(v1));
        if (v2 < 0 || v2 >= _m2) throw new ArgumentOutOfRangeException(nameof(v2));
        return v1 + v2 * _b1;
    }

    public void Decode(int value, out int v1, out int v2)
    {
        if (value < 0 || value >= TotalSize)
            throw new ArgumentOutOfRangeException(nameof(value));

        v1 = value % _b1;
        v2 = value / _b1;
    }
}

/// <summary>3维模空间</summary>
public readonly struct ModSpace3
{
    private readonly int _m1;
    private readonly int _m2;
    private readonly int _m3;
    private readonly int _b1;
    private readonly int _b2;

    public int TotalSize { get; }

    public ModSpace3(int m1, int m2, int m3)
    {
        if (m1 < 1 || m2 < 1 || m3 < 1)
            throw new ArgumentOutOfRangeException("维度大小必须 ≥ 1");

        long b1 = m1;
        long b2 = b1 * m2;
        long total = b2 * m3;

        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1;
        _m2 = m2;
        _m3 = m3;
        _b1 = (int)b1;
        _b2 = (int)b2;
        TotalSize = (int)total;
    }

    public int Encode(int v1, int v2, int v3)
    {
        if (v1 < 0 || v1 >= _m1) throw new ArgumentOutOfRangeException(nameof(v1));
        if (v2 < 0 || v2 >= _m2) throw new ArgumentOutOfRangeException(nameof(v2));
        if (v3 < 0 || v3 >= _m3) throw new ArgumentOutOfRangeException(nameof(v3));

        return v1 + v2 * _b1 + v3 * _b2;
    }

    public void Decode(int value, out int v1, out int v2, out int v3)
    {
        if (value < 0 || value >= TotalSize)
            throw new ArgumentOutOfRangeException(nameof(value));

        v3 = value / _b2;
        int rem = value % _b2;
        v2 = rem / _b1;
        v1 = rem % _b1;
    }
}

/// <summary>4维模空间</summary>
public readonly struct ModSpace4
{
    private readonly int _m1, _m2, _m3, _m4;
    private readonly int _b1, _b2, _b3;
    public int TotalSize { get; }

    public ModSpace4(int m1, int m2, int m3, int m4)
    {
        if (m1 < 1 || m2 < 1 || m3 < 1 || m4 < 1)
            throw new ArgumentOutOfRangeException("维度大小必须 ≥ 1");

        long b1 = m1;
        long b2 = b1 * m2;
        long b3 = b2 * m3;
        long total = b3 * m4;

        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1; _m2 = m2; _m3 = m3; _m4 = m4;
        _b1 = (int)b1; _b2 = (int)b2; _b3 = (int)b3;
        TotalSize = (int)total;
    }

    public int Encode(int v1, int v2, int v3, int v4)
    {
        if (v1 < 0 || v1 >= _m1) throw new ArgumentOutOfRangeException(nameof(v1));
        if (v2 < 0 || v2 >= _m2) throw new ArgumentOutOfRangeException(nameof(v2));
        if (v3 < 0 || v3 >= _m3) throw new ArgumentOutOfRangeException(nameof(v3));
        if (v4 < 0 || v4 >= _m4) throw new ArgumentOutOfRangeException(nameof(v4));

        return v1 + v2 * _b1 + v3 * _b2 + v4 * _b3;
    }

    public void Decode(int value, out int v1, out int v2, out int v3, out int v4)
    {
        if (value < 0 || value >= TotalSize)
            throw new ArgumentOutOfRangeException(nameof(value));

        v4 = value / _b3;
        int rem = value % _b3;
        v3 = rem / _b2;
        rem %= _b2;
        v2 = rem / _b1;
        v1 = rem % _b1;
    }
}

/// <summary>5维模空间</summary>
public readonly struct ModSpace5
{
    private readonly int _m1, _m2, _m3, _m4, _m5;
    private readonly int _b1, _b2, _b3, _b4;
    public int TotalSize { get; }

    public ModSpace5(int m1, int m2, int m3, int m4, int m5)
    {
        if (m1 < 1 || m2 < 1 || m3 < 1 || m4 < 1 || m5 < 1)
            throw new ArgumentOutOfRangeException("维度大小必须 ≥ 1");

        long b1 = m1;
        long b2 = b1 * m2;
        long b3 = b2 * m3;
        long b4 = b3 * m4;
        long total = b4 * m5;

        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1; _m2 = m2; _m3 = m3; _m4 = m4; _m5 = m5;
        _b1 = (int)b1; _b2 = (int)b2; _b3 = (int)b3; _b4 = (int)b4;
        TotalSize = (int)total;
    }

    public int Encode(int v1, int v2, int v3, int v4, int v5)
    {
        if (v1 < 0 || v1 >= _m1) throw new ArgumentOutOfRangeException(nameof(v1));
        if (v2 < 0 || v2 >= _m2) throw new ArgumentOutOfRangeException(nameof(v2));
        if (v3 < 0 || v3 >= _m3) throw new ArgumentOutOfRangeException(nameof(v3));
        if (v4 < 0 || v4 >= _m4) throw new ArgumentOutOfRangeException(nameof(v4));
        if (v5 < 0 || v5 >= _m5) throw new ArgumentOutOfRangeException(nameof(v5));

        return v1 + v2 * _b1 + v3 * _b2 + v4 * _b3 + v5 * _b4;
    }

    public void Decode(int value, out int v1, out int v2, out int v3, out int v4, out int v5)
    {
        if (value < 0 || value >= TotalSize) throw new ArgumentOutOfRangeException(nameof(value));

        v5 = value / _b4;
        int rem = value % _b4;
        v4 = rem / _b3;
        rem %= _b3;
        v3 = rem / _b2;
        rem %= _b2;
        v2 = rem / _b1;
        v1 = rem % _b1;
    }
}

/// <summary>6维模空间</summary>
public readonly struct ModSpace6
{
    private readonly int _m1, _m2, _m3, _m4, _m5, _m6;
    private readonly int _b1, _b2, _b3, _b4, _b5;
    public int TotalSize { get; }

    public ModSpace6(int m1, int m2, int m3, int m4, int m5, int m6)
    {
        if (m1 < 1 || m2 < 1 || m3 < 1 || m4 < 1 || m5 < 1 || m6 < 1)
            throw new ArgumentOutOfRangeException("维度大小必须 ≥ 1");

        long b1 = m1;
        long b2 = b1 * m2;
        long b3 = b2 * m3;
        long b4 = b3 * m4;
        long b5 = b4 * m5;
        long total = b5 * m6;

        if (total > int.MaxValue)
            throw new OverflowException($"总模空间超出 int 上限 {int.MaxValue}");

        _m1 = m1; _m2 = m2; _m3 = m3; _m4 = m4; _m5 = m5; _m6 = m6;
        _b1 = (int)b1; _b2 = (int)b2; _b3 = (int)b3; _b4 = (int)b4; _b5 = (int)b5;
        TotalSize = (int)total;
    }

    public int Encode(int v1, int v2, int v3, int v4, int v5, int v6)
    {
        if (v1 < 0 || v1 >= _m1) throw new ArgumentOutOfRangeException(nameof(v1));
        if (v2 < 0 || v2 >= _m2) throw new ArgumentOutOfRangeException(nameof(v2));
        if (v3 < 0 || v3 >= _m3) throw new ArgumentOutOfRangeException(nameof(v3));
        if (v4 < 0 || v4 >= _m4) throw new ArgumentOutOfRangeException(nameof(v4));
        if (v5 < 0 || v5 >= _m5) throw new ArgumentOutOfRangeException(nameof(v5));
        if (v6 < 0 || v6 >= _m6) throw new ArgumentOutOfRangeException(nameof(v6));

        return v1 + v2 * _b1 + v3 * _b2 + v4 * _b3 + v5 * _b4 + v6 * _b5;
    }

    public void Decode(int value, out int v1, out int v2, out int v3, out int v4, out int v5, out int v6)
    {
        if (value < 0 || value >= TotalSize)
            throw new ArgumentOutOfRangeException(nameof(value));

        v6 = value / _b5;
        int rem = value % _b5;
        v5 = rem / _b4;
        rem %= _b4;
        v4 = rem / _b3;
        rem %= _b3;
        v3 = rem / _b2;
        rem %= _b2;
        v2 = rem / _b1;
        v1 = rem % _b1;
    }
}