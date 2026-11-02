using System;
using UnityEngine;
using XLua;

public class LuaManager : MonoBehaviour
{
    private LuaEnv _luaEnv;

    private void Start()
    {
        TestLua();
    }

    // 冒烟测试：能取到 _VERSION 且 xlua.util 可 require，说明原生库与加载链路都通了
    private void TestLua()
    {
        string chunk = @"
            local jitVer = jit and jit.version or 'none'
            local ok = pcall(require, 'xlua.util')
            return string.format('engine=%s | jit=%s | xlua.util=%s | path=%s',
                _VERSION, jitVer, tostring(ok), package.path)
        ";

        try
        {
            _luaEnv = new LuaEnv();
            object[] result = _luaEnv.DoString(chunk, "LuaManagerTest");
            Debug.Log($"[LuaManager] Lua 可运行 → {result[0]}");
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError("[LuaManager] xlua.dll 加载失败：确认 Assets/xLua/Plugins/x86_64/xlua.dll 存在，" +
                           "且该 dll 的 Inspector 勾选了 x86_64 平台。" + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LuaManager] Lua 初始化或执行失败：{e.GetType().Name} - {e.Message}");
        }
    }

    private void OnDestroy()
    {
        _luaEnv?.Dispose();
        _luaEnv = null;
    }
}
