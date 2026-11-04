// 全局唯一的 LuaEnv 持有者：任何模块不得自行实例化 LuaEnv；场景销毁（含退出游戏）时释放
using UnityEngine;
using XLua;

public class LuaManager : MonoBehaviour
{
    private LuaEnv _luaEnv;

    private void Awake()
    {
        Tool.LuaManager = this;
        _luaEnv = new LuaEnv();
    }

    public LuaEnv Env => _luaEnv;

    private void Update()
    {
        if (_luaEnv != null) _luaEnv.Tick();
    }

    private void OnDestroy()
    {
        if (_luaEnv != null) _luaEnv.Dispose();
        _luaEnv = null;
        if (Tool.LuaManager == this) Tool.LuaManager = null;
    }
}
