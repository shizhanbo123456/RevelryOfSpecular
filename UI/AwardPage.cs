using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AwardPage : PageBase
{
    public static int timeCount;
    public static int expGrowth;
    public static List<int> noteCount = new() { 0, 0, 0, 0 };
    public static List<(int, int)> ImprintIconAndCount = new();

    [SerializeField] private List<GameObject> ratings;//f/d/c/b/a/s/ss
    [SerializeField] private Text labelTime;
    [SerializeField] private Text labelExp;
    [SerializeField] private List<Text> labelNoteList;
    [SerializeField] private Transform templateImprint;
    [SerializeField] private Button buttonBackHome;

    private List<Transform> imprints;

    public override void Init()
    {
        imprints = new List<Transform>()
        {
            templateImprint
        };
        if (buttonBackHome != null)
        {
            buttonBackHome.onClick.AddListener(BackHome);
        }
        EventManager.AddEvent<SettleRewardInfo>(ClientEvent.UpdateSettlementReward, SetSettlement);
    }
    public override void Enter()
    {
        RefreshContent();
    }

    private void SetSettlement(SettleRewardInfo reward)
    {
        timeCount = reward.timeCount;
        expGrowth = reward.exp;
        noteCount = new List<int>(reward.note);

        ImprintIconAndCount = new List<(int, int)>();
        for (int i = 0; i < reward.imprint.Count; i++)
        {
            int count = reward.imprint[i];
            if (count <= 0) continue;
            ImprintIconAndCount.Add((i, count));
        }
        if (ImprintIconAndCount.Count == 0)
        {
            ImprintIconAndCount.Add((0, 0));
        }

        if (gameObject.activeInHierarchy)
        {
            RefreshContent();
        }
    }

    private void RefreshContent()
    {
        if (labelTime != null) labelTime.text = FormatTime(timeCount);
        if (labelExp != null) labelExp.text= expGrowth.ToString();
        for(int i = 0; i < labelNoteList.Count && i < noteCount.Count; i++)
        {
            labelNoteList[i].text = noteCount[i].ToString();
        }
        if (ImprintIconAndCount == null) ImprintIconAndCount = new();
        if (templateImprint == null) return;
        Tool.ActiveFor(imprints, ImprintIconAndCount.Count);
        for (int index = 0; index < imprints.Count; index++)
        {
            Transform obj = imprints[index];
            obj.GetChild(0).GetComponent<Image>().sprite = Tool.AssetsManager.ImprintIcons[ImprintIconAndCount[index].Item1];
            obj.GetChild(1).GetComponent<Text>().text = ImprintIconAndCount[index].Item2.ToString();
        }
    }

    private void BackHome()
    {
        Tool.NetworkManager.ExitWorld();
        Tool.UIManager.ShowPage(PageType.Home);
    }

    private static string FormatTime(int seconds)
    {
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

}
