using System;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]private HomePage home;
    [SerializeField] private BattlePage battle;
    [SerializeField] private AwardPage award;

    [Space]
    [SerializeField] private Loading loading;
    [SerializeField] private Notice notice;

    public BattlePage BattlePage => battle;

    private void Awake()
    {
        notice.Init();
        loading.Init();
        Tool.UIManager = this;
        EventManager.AddEvent(ClientEvent.OnEnterWorld, () => ShowPage(PageType.Battle));
        EventManager.AddEvent(ClientEvent.OnRestartGame, () => ShowPage(PageType.Home));

        home.gameObject.SetActive(false);
        battle.gameObject.SetActive(false);
        award.gameObject.SetActive(false);
    }
    private void Start()
    {
        home.Init();
        battle.Init();
        award.Init();

        home.Show();
    }

    public void ShowPage(PageType type)
    {
        home.Hide();
        battle.Hide();
        award.Hide();
        switch (type)
        {
            case PageType.Home:home.Show();break;
            case PageType.Battle:battle.Show();break;
            case PageType.Award:award.Show();break;
        }
    }

    public async Task Execute(Func<Task> task, Action result = null)
    {
        await loading.Execute(task, result);
    }
}
public enum PageType
{
    Home, Battle, Award
}
