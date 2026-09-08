using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI 管理器（UI Toolkit）。
/// 管理页面切换（初始界面 / 匹配大厅 / 战斗 HUD）与通用控件（提示/确认/加载）。
/// 使用方式：场景中挂 UIDocument，将 UIManager.uiDocument 指向它。
/// </summary>
public class UIManager : MonoBehaviour
{
    public enum PageType
    {
        Home,
        Lobby,
        Battle,
    }

    /// <summary>场景中的 UIDocument（需在 Inspector 配置）。</summary>
    public UIDocument uiDocument;

    private VisualElement root;
    private readonly PageBase[] pages = new PageBase[3];
    private PageType currentPage = PageType.Home;

    /// <summary>通用提示。</summary>
    public NoticeUnit notice = new();
    /// <summary>确认面板。</summary>
    public ConfirmUnit confirm = new();
    /// <summary>加载遮罩。</summary>
    public LoadingUnit loading = new();

    /// <summary>当前页面。</summary>
    public PageType CurrentPage => currentPage;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIManager 未配置 uiDocument，请在场景中挂载 UIDocument 并拖拽引用");
            return;
        }
        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("UIDocument 根元素为空，请检查 PanelSettings 配置");
            return;
        }

        // 构建页面
        pages[(int)PageType.Home] = new HomePage();
        pages[(int)PageType.Lobby] = new LobbyPage();
        pages[(int)PageType.Battle] = new BattlePage();
        foreach (var page in pages)
        {
            if (page != null) page.Init(this, root);
        }

        // 通用控件（置顶）
        notice.Init(root);
        confirm.Init(root);
        loading.Init(root);

        // 事件绑定
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnRestartGame);
        EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.AddEvent<string>(ClientEvent.ShowNotice, ShowNotice);
        EventManager.AddEvent<bool>(ClientEvent.ShowLoading, ShowLoading);

        ShowPage(PageType.Home);
    }

    private void OnDestroy()
    {
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnRestartGame);
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.RemoveEvent<string>(ClientEvent.ShowNotice, ShowNotice);
        EventManager.RemoveEvent<bool>(ClientEvent.ShowLoading, ShowLoading);
        foreach (var page in pages)
        {
            page?.OnDisable();
        }
    }

    private void Update()
    {
        var page = GetPage(currentPage);
        page?.OnUpdate();
    }

    #region 页面切换
    /// <summary>切换页面。</summary>
    public void ShowPage(PageType page)
    {
        if (currentPage == page) return;
        GetPage(currentPage)?.OnDisable();
        GetPage(currentPage)?.Exit();
        currentPage = page;
        var next = GetPage(page);
        next?.OnEnable();
        next?.Enter();
    }

    private PageBase GetPage(PageType page)
    {
        return pages[(int)page];
    }

    /// <summary>获取页面（供外部访问数据）。</summary>
    public T GetPage<T>() where T : PageBase
    {
        foreach (var page in pages)
        {
            if (page is T t) return t;
        }
        return null;
    }
    #endregion

    #region 事件回调
    private void OnBattleStart(int camp)
    {
        ShowPage(PageType.Battle);
    }

    private void OnRestartGame()
    {
        Tool.ClientDisplayManager?.ClearAll(); // 清空上一局实体表现残留
        ShowPage(PageType.Home);
    }

    private void OnConnect()
    {
        ShowNotice("已连接服务器");
    }
    #endregion

    #region 通用控件
    /// <summary>显示提示消息（自动消失）。</summary>
    public void ShowNotice(string msg)
    {
        notice.Show(msg);
    }

    /// <summary>显示确认面板。</summary>
    public void ShowConfirm(string msg, Action onConfirm)
    {
        confirm.Show(msg, onConfirm);
    }

    /// <summary>显示/隐藏加载遮罩。</summary>
    public void ShowLoading(bool show)
    {
        loading.Show(show);
    }
    #endregion
}
