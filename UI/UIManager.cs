using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private HomePage home;
    [SerializeField] private LobbyPage lobby;
    [SerializeField] private BattlePage battle;

    [Header("飘字（挂在页面之外，任何界面都能显示）")]
    [SerializeField] private RectTransform floatingPanel;  // 飘字容器（中央偏上）
    [SerializeField] private Text floatingTextTemplate;    // 飘字文字模板

    private List<RosPage> pages;
    private RosPage currentPage;
    private readonly List<(Text label, float time)> floatingLabels = new();
    private const float FloatingLife = 2.5f;

    private void Start()
    {
        Tool.UIManager = this;
        pages = new()
        {
            home,lobby,battle
        };
        foreach (var p in pages)
        {
            p.Construct();
        }
        foreach (var p in pages)
        {
            p.Init();
            p.gameObject.SetActive(false);
        }
        home.gameObject.SetActive(true);
        currentPage = home;
        home.Enter(null);

        //切页事件（发送方在 NetworkManager）：连接成功→大厅、开战→战斗、断开/超时→首页
        EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnRestartGame);
    }

    private void OnDestroy()
    {
        //EventManager 是静态的，订阅必须与解绑成对，否则跨场景累积
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnRestartGame);
    }

    private void Update()
    {
        currentPage?.Tick(Time.deltaTime);
        TickFloating();
    }

    /// <summary>飘字提示（事件提示/规则提醒等，2.5s 自动消失，任何界面都能调）。</summary>
    public void ShowFloating(string text, Color color)
    {
        if (floatingPanel == null || floatingTextTemplate == null) return;
        var label = Instantiate(floatingTextTemplate, floatingPanel);
        label.text = text;
        label.color = color;
        floatingLabels.Add((label, Time.time));
    }

    public void TurnPage(PageType type,ShowParam param = null)
    {
        currentPage.Exit();
        currentPage.gameObject.SetActive(false);
        currentPage = pages[(int)type];
        currentPage.gameObject.SetActive(true);
        currentPage.Enter(param);
    }

    private void TickFloating()
    {
        for (int i = floatingLabels.Count - 1; i >= 0; i--)
        {
            var item = floatingLabels[i];
            if (Time.time - item.time > FloatingLife)
            {
                if (item.label != null) Destroy(item.label.gameObject);
                floatingLabels.RemoveAt(i);
            }
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
        Tool.ClientLogicManager?.EntityPlayers.ClearAll(); // 清空上一局实体表现残留
        TurnPage(PageType.Home);
    }
}
public enum PageType
{
    Home,
    Lobby,
    Battle
}
