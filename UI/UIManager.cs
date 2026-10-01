using System.Collections.Generic;
using FairyGUI;
using Ros.UI.Main;
using UnityEngine;

/// <summary>
/// UI 总控（FGUI 版）：加载 Main 包并注册 binder，持有三个页面（Logic 层，不直接操作生成组件），
/// 统一切页（OnConnect/OnBattleStart/OnRestartGame 事件驱动）、驱动当前页 Tick、分辨率适配与全局飘字。
/// </summary>
public class UIManager : MonoBehaviour
{
    private HomePage home;
    private LobbyPage lobby;
    private BattlePage battle;
    private PageBase currentPage;
    private int lastScreenWidth, lastScreenHeight;

    // 全局飘字（任意界面可用，屏幕上方居中、随时间上浮、透明度淡入淡出），用 UI_NoticePanel 承载
    private readonly List<FloatingLabel> floatingLabels = new();
    private const float FloatingLife = 2.5f;   // 总存活时长
    private const float FloatRise = 90f;       // 存活期内上移的设计像素
    private const float FadeIn = 0.3f;         // 淡入时长
    private const float FadeOut = 0.6f;        // 淡出时长

    private class FloatingLabel
    {
        public GComponent comp;
        public float startTime;
        public float startY;
    }

    private void Start()
    {
        try
        {
            Tool.UIManager = this;
            Debug.Log("[UIManager] 启动：加载 Main 包...");

            UIPackage.AddPackage("GUI/Main");
            Debug.Log($"[UIManager] 包已加载：{(UIPackage.GetByName("Main") != null ? "成功" : "失败")}");
            MainBinder.BindAll();

            home = new HomePage(UI_HomePanel.CreateInstance());
            lobby = new LobbyPage(UI_LobbyPanel.CreateInstance());
            battle = new BattlePage(UI_BattlePanel.CreateInstance());
            Debug.Log($"[UIManager] 页面已创建：home={home != null}, lobby={lobby != null}, battle={battle != null}");

            //注册回调/渲染器（Construct 里会设置 GList.itemRenderer，必须在设置 numItems 之前完成）
            home.Construct();
            lobby.Construct();
            battle.Construct();
            Debug.Log("[UIManager] Construct 完成");

            ApplyResize();
            Debug.Log($"[UIManager] 适配完成：屏幕 {Screen.width}x{Screen.height}，面板宽 {home.PanelWidth:0}，缩放 {home.UiScale:0.00}，GRoot {GRoot.inst.width:0}x{GRoot.inst.height:0}");

            currentPage = home;
            home.Enter(null);
            Debug.Log($"[UIManager] 首页已显示：home.visible={home.IsVisible}，children={GRoot.inst.numChildren}");

            //切页事件（发送方在 NetworkManager）：连接成功→大厅、开战→战斗、断开/超时→首页
            EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
            EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
            EventManager.AddEvent(ClientEvent.OnRestartGame, OnRestartGame);
            EventManager.AddEvent(ClientEvent.OnExitWorld, OnExitWorld);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e); // 任何被吞掉的异常都显式暴露
        }
    }

    private void OnDestroy()
    {
        //EventManager 是静态的，订阅必须与解绑成对，否则跨场景累积
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnRestartGame);
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, OnExitWorld);
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) ApplyResize();
        if (currentPage != null) currentPage.Tick(Time.deltaTime);
        TickFloating();
    }

    /// <summary>分辨率适配：按当前宽高比算出 1080 高对应的宽度，通知所有页面。</summary>
    private void ApplyResize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        float width = 1080f * ((float)Screen.width / Screen.height);
        if (home != null) home.OnResize(width, 1080f);
        if (lobby != null) lobby.OnResize(width, 1080f);
        if (battle != null) battle.OnResize(width, 1080f);
    }

    public void ShowFlyText(string text)
    {
        var panel = UI_NoticePanel.CreateInstance();
        if (panel.m_title != null) panel.m_title.text = text;
        GRoot.inst.AddChild(panel);
        float x = (GRoot.inst.width - panel.width) * 0.5f;
        float y = 140f;
        panel.SetXY(x, y);
        panel.alpha = 0f; // 从透明起步，由 TickFloating 淡入
        floatingLabels.Add(new FloatingLabel { comp = panel, startTime = Time.time, startY = y });
    }

    public void TurnPage(PageType type, ShowParam param = null)
    {
        if (currentPage != null) currentPage.Exit();
        currentPage = GetPage(type);
        if (currentPage != null) currentPage.Enter(param);
    }

    private PageBase GetPage(PageType type)
    {
        return type switch
        {
            PageType.Home =>home,
            PageType.Lobby => lobby,
            PageType.Battle => battle,
            _ => home,
        };
    }

    private void TickFloating()
    {
        for (int i = floatingLabels.Count - 1; i >= 0; i--)
        {
            var item = floatingLabels[i];
            float elapsed = Time.time - item.startTime;
            if (elapsed >= FloatingLife)
            {
                if (item.comp != null) item.comp.Dispose();
                floatingLabels.RemoveAt(i);
                continue;
            }
            // 上浮：存活期内匀速上移
            item.comp.y = item.startY - FloatRise * (elapsed / FloatingLife);
            // 透明度：开头淡入、结尾淡出，中间保持不透明
            float fadeIn = Mathf.Clamp01(elapsed / FadeIn);
            float fadeOut = Mathf.Clamp01((FloatingLife - elapsed) / FadeOut);
            item.comp.alpha = Mathf.Min(fadeIn, fadeOut);
        }
    }

    private void OnConnect()
    {
        TurnPage(PageType.Lobby);
    }

    private void OnBattleStart(int camp)
    {
        TurnPage(PageType.Battle);
    }

    private void OnRestartGame()
    {
        ReturnToHome();
    }

    /// <summary>主动退出世界（组队/战斗页退出按钮）：清空残留并切回主界面。</summary>
    private void OnExitWorld()
    {
        ReturnToHome();
    }

    /// <summary>断开/超时/主动退出统一返回主界面，并清空上一局实体表现残留。</summary>
    private void ReturnToHome()
    {
        if (Tool.ClientLogicManager != null) Tool.ClientLogicManager.EntityPlayers.ClearAll();
        TurnPage(PageType.Home);
    }
}

public enum PageType
{
    Home,
    Lobby,
    Battle
}
