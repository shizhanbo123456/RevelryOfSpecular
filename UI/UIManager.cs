using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private HomePage home;
    [SerializeField] private LobbyPage lobby;
    [SerializeField] private BattlePage battle;
    private List<RosPage> pages;
    private RosPage currentPage;
    private void Start()
    {
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
    }
    public void TurnPage(PageType type,ShowParam param = null)
    {
        currentPage.Exit();
        currentPage.gameObject.SetActive(false);
        currentPage = pages[(int)type];
        currentPage.gameObject.SetActive(true);
        currentPage.Enter(param);
    }
}
public enum PageType
{
    Home,
    Lobby,
    Battle
}