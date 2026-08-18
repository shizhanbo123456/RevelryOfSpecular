using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HomePage : PageBase
{
    [SerializeField] private List<Toggle> SwitchPageToggles;
    [Header("Character")]
    [SerializeField] private CanvasGroup CharacterCanvasGroup;
    [SerializeField] private Text CharacterName;
    [SerializeField] private Text CharacterJob;
    [SerializeField] private Text CharacterDescription;
    [SerializeField] private Text CharacterAbilityName;
    [SerializeField] private Text CharacterAbilityDes;
    [SerializeField] private Text CharacterUseTime;
    [SerializeField] private List<Toggle> CharacterStars;
    [SerializeField] private List<HomeCharacterUnit> CharacterUnits;
    [SerializeField] private List<HomeAttributeUnit> AttributeUnits;
    [Header("Imprint")]
    [SerializeField] private CanvasGroup ImprintCanvasGroup;
    [SerializeField] private List<HomeImprintUnit> ImprintUnits;
    [Header("Collection")]
    [SerializeField] private CanvasGroup CollectionCanvasGroup;
    [SerializeField] private List<HomeCollectionUnit> CollectionUnits;
    [Header("Skill")]
    [SerializeField] private CanvasGroup SkillCanvasGroup;
    [SerializeField]private List<HomeSkillUnit>SkillUnits;
    [Header("Explore")]
    [SerializeField] private CanvasGroup ExploreCanvasGroup;
    [SerializeField] private List<Transform> ExploreIconTemplates;//入口、解锁、高级、中级、低级
    [SerializeField] private Transform ExploreMapLeftBottom;
    [SerializeField] private Transform ExploreMapRightTop;
    [SerializeField] private Toggle ExploreEntryToggle;
    [SerializeField] private Toggle ExploreUnlockPropToggle;
    [SerializeField] private Toggle ExploreExitToggle;
    [Space]
    [Header("Login")]
    [SerializeField] private GameObject LoginConnected;
    [SerializeField] private GameObject LoginDisconnected;
    [SerializeField] private InputField LoginIpInput;
    [SerializeField] private Button LoginButton;
    [SerializeField] private Text LoginIp;
    [SerializeField] private Text LoginLabelDifficulty;
    [SerializeField] private Text LoginLabelMemberCount;
    [SerializeField] private Text LoginLabelInfection;
    [SerializeField] private Button LoginGoIn;

    private int alphaTransitionHandle;
    private List<CanvasGroup> canvasGroupList;
    private List<List<Transform>> ExploreIcons;

    public static int currentSelectedCharacter;
    public static HashSet<int> SelectedImprints=new HashSet<int>();
    public static Dictionary<int, int> SelectedSkillCounts=new Dictionary<int, int>();

    public override void Init()
    {
        canvasGroupList=new List<CanvasGroup>() 
        { CollectionCanvasGroup,ImprintCanvasGroup,CharacterCanvasGroup,
            SkillCanvasGroup,ExploreCanvasGroup};
        for(int i = 0; i < SwitchPageToggles.Count; i++)
        {
            int j = i;
            SwitchPageToggles[i].onValueChanged.AddListener(on =>
            {
                TurnPage(j);
            });
        }
        //character
        Tool.ActiveFor(CharacterStars, Config.max_entity_level);
        Tool.ActiveFor(CharacterUnits,Config.character_count);
        for(int i = 0; i < CharacterUnits.Count; i++)
        {
            CharacterUnits[i].Name.text = Tool.InfoManager.CharacterInfoList[i].Name;
            CharacterUnits[i].Icon.sprite=Tool.AssetsManager.CharacterIcons[i];
            int j = i;
            CharacterUnits[i].Button.onClick.AddListener(() => 
            {
                currentSelectedCharacter = j;
                UpdateSelectedCharacterInfo();
            });
        }
        //imprint
        Tool.ActiveFor(ImprintUnits, Config.imprint_count);
        for (int i = 0; i < ImprintUnits.Count; i++)
        {
            ImprintUnits[i].Name.text = Config.ImprintName[i];
            ImprintUnits[i].Des.text = Config.ImprintAbilityFormat(i, Tool.SaveManager.imprintLevels[i]);
            ImprintUnits[i].Icon.sprite = Tool.AssetsManager.ImprintIcons[i];
            ImprintUnits[i].IconOwn.sprite = Tool.AssetsManager.ImprintIcons[i];
            int j = i;
            ImprintUnits[i].Click.onClick.AddListener(()=>OnImprintClicked(j));
            Tool.ActiveFor(ImprintUnits[i].Stars, Tool.SaveManager.imprintLevels[i]);
        }
        //skill
        Tool.ActiveFor(SkillUnits, Config.skill_count);
        for (int i = 0; i < Config.skill_count; i++)
        {
            var info = Tool.InfoManager.SkillInfoList[i];
            SkillUnits[i].Name.text = Config.SkillNameList[i];
            if (SkillUnits[i].Quality != null)
            {
                SkillUnits[i].Quality.text = GetSkillQualityName(info.quality);
                SkillUnits[i].Quality.color = GetSkillQualityColor(info.quality);
            }
            SkillUnits[i].Icon.sprite = info.sprite;
            int j = i;
            SkillUnits[i].Increase.onClick.AddListener(()=>ChangeSkillTakeCount(j, 1));
            SkillUnits[i].Decrease.onClick.AddListener(()=>ChangeSkillTakeCount(j, -1));
        }
        //explore
        ExploreIcons = new();
        while (ExploreIcons.Count < ExploreIconTemplates.Count)
        {
            ExploreIcons.Add(new());
        }
        ApplyMapIcon(0, ExploreEntryToggle,Landscape.instance.SpawnPosList);
        ApplyMapIcon(1, ExploreUnlockPropToggle,Landscape.instance.CharacterUnlockPropPos);
        ApplyMapIcon(2, ExploreExitToggle, Landscape.instance.ExitPortList);
        foreach(var i in ExploreIconTemplates)
        {
            i.gameObject.SetActive(false);
        }
        //login
        EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnDisconnect);
        LoginButton.onClick.AddListener(() =>
        {
            Func<Task> func = async () =>
            {
                var result = await Tool.NetworkManager.TryConnect(LoginIpInput.text);
                switch (result)
                {
                    case NetworkManager.ConnectResult.TooFrequent:
                        EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "点击过于频繁");
                        break;
                    case NetworkManager.ConnectResult.Failed:
                        EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "连接失败");
                        break;
                    case NetworkManager.ConnectResult.Success:
                        EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "连接成功");
                        break;
                }
            };
            _=Tool.UIManager.Execute(func);
        });
        LoginGoIn.onClick.AddListener(() =>
        {
            EnterWorld();
        });
    }
    public override void Enter()
    {
        LoadLocalSelectCache();
        TurnPage(2);
        Tool.EnvironmentManager.EntrySetSpotLightActive(true);
        //character
        UpdateSelectedCharacterInfo();
        //imprint
        Tool.ActiveFor(ImprintUnits, Config.imprint_count);
        for (int i = 0; i < ImprintUnits.Count; i++)
        {
            var unit = ImprintUnits[i];
            var level = Tool.SaveManager.imprintLevels[i];
            unit.Click.interactable = level > 0;
            unit.Icon.gameObject.SetActive(level > 0);
            unit.Selected.gameObject.SetActive(SelectedImprints.Contains(i));
            Tool.ActiveFor(unit.Stars, level);
            if (level == 0)
            {
                ImprintUnits[i].BarLabel.text = "未解锁";
                ImprintUnits[i].BarFill.fillAmount = 0;
            }
            else if (level == Config.max_imprint_level)
            {
                ImprintUnits[i].BarLabel.text = "MAX";
                ImprintUnits[i].BarFill.fillAmount = 1;
            }
            else
            {
                var curr = Tool.SaveManager.imprintExp[i];
                var next = Tool.SaveManager.GetImprintUpgradeNeedExp(i);
                ImprintUnits[i].BarLabel.text = curr + "/" + next;
                ImprintUnits[i].BarFill.fillAmount = curr/(float)next;
            }
        }
        //collection
        for (int i = 0; i < 4; i++)
        {
            CollectionUnits[i].Count.text = Tool.SaveManager.noteCounts[i].ToString();
        }
        //skill
        Tool.ActiveFor(SkillUnits, Config.skill_count);
        RefreshSkillSelectedState();
        //explore
        ExploreEntryToggle.isOn = true;
        ExploreUnlockPropToggle.isOn = true;
        ExploreExitToggle.isOn = true;
        //login
        OnDisconnect();
    }
    public override void Exit()
    {
        base.Exit();
        Tool.EnvironmentManager.EntryHideCharacter();
        Tool.EnvironmentManager.EntrySetSpotLightActive(false);
        SaveLocalSelectCache();
    }
    private void TurnPage(int index)
    {
        for (int i = 0; i < canvasGroupList.Count; i++)
        {
            canvasGroupList[i].gameObject.SetActive(i == index);
        }
        Tool.TransitionManager.Cancel(alphaTransitionHandle);
        canvasGroupList[index].alpha = 0;
        alphaTransitionHandle =Tool.TransitionManager.Execute((t) =>
        {
            canvasGroupList[index].alpha =t;
            return true;
        },0.5f);
    }
    private void UpdateSelectedCharacterInfo()
    {
        Tool.EnvironmentManager.EntryShowCharacter(currentSelectedCharacter);

        var config = Config.CharacterConfigDict[EntityType.Character(currentSelectedCharacter)];
        CharacterName.text=Tool.InfoManager.CharacterInfoList[currentSelectedCharacter].Name;
        CharacterJob.text = config.JobName;
        CharacterDescription.text = config.Background;
        CharacterAbilityName.text= config.AbilityName;
        CharacterAbilityDes.text= config.AbilityDescFormat(Tool.SaveManager.characterLevels[currentSelectedCharacter]);
        CharacterUseTime.text =Tool.SaveManager.characterTokenCounts[currentSelectedCharacter].ToString();

        for(int i = 0; i < CharacterStars.Count; i++)
        {
            CharacterStars[i].isOn = i < Tool.SaveManager.characterLevels[currentSelectedCharacter];
        }
        EntityAttribute _base = Tool.InfoManager.CharacterInfoList[currentSelectedCharacter].GetAttribute();
        EntityAttribute _current = Tool.InfoManager.CharacterInfoList[currentSelectedCharacter].
            GetAttribute(Tool.SaveManager.characterLevels[currentSelectedCharacter]);
        List<int> baseValues = _base.GetValueList();
        List<int> currentValues = _current.GetValueList();
        for (int i = 0; i < baseValues.Count; i++)
        {
            AttributeUnits[i].Value.text = currentValues[i].ToString();
            AttributeUnits[i].FillBase.fillAmount = Mathf.InverseLerp(
                Tool.InfoManager.AttributeValueMin[i],
                Tool.InfoManager.AttributeValueMax[i], currentValues[i]);
            AttributeUnits[i].FillTop.fillAmount = Mathf.InverseLerp(
                Tool.InfoManager.AttributeValueMin[i],
                Tool.InfoManager.AttributeValueMax[i], baseValues[i]);
        }
    }
    private void OnImprintClicked(int index)
    {
        if (SelectedImprints.Contains(index))
        {
            SelectedImprints.Remove(index);
        }
        else
        {
            if (SelectedImprints.Count == Config.max_selected_imprint_count)
            {
                EventManager.TrigEvent<string>(ClientEvent.ShowNotice, $"最多携带{Config.max_selected_imprint_count}枚印记");
                return;
            }
            if (Tool.SaveManager.imprintLevels[index] == 0) return;
            SelectedImprints.Add(index);
        }
        RefreshImprintSelectedState();
    }
    private void RefreshImprintSelectedState()
    {
        for (int i = 0; i < ImprintUnits.Count; i++)
        {
            ImprintUnits[i].Selected.SetActive(SelectedImprints.Contains(i));
        }
    }
    private void RefreshSkillSelectedState()
    {
        for (int i = 0; i < SkillUnits.Count; i++)
        {
            int takeCount = GetSelectedSkillCount(i);
            int ownCount = Tool.SaveManager.GetRuneCount(i);
            bool canIncreaseByCount = takeCount < ownCount;
            bool canIncreaseByKind = takeCount > 0 || SelectedSkillCounts.Count < Config.max_selected_skill_count;
            bool canIncreaseByWeight = GetSelectedRuneWeight() + SkillManager.GetSkillWeight(i) <= Config.max_take_in_rune_weight;

            SkillUnits[i].OwnCount.text = ownCount.ToString();
            SkillUnits[i].TakeCount.text = takeCount.ToString();
            SkillUnits[i].Increase.interactable = canIncreaseByCount && canIncreaseByKind && canIncreaseByWeight;
            SkillUnits[i].Decrease.interactable = takeCount > 0;
            SkillUnits[i].haveRune.SetActive(ownCount > 0);
            SkillUnits[i].selected.SetActive(takeCount > 0);
        }
    }
    private void ChangeSkillTakeCount(int index, int delta)
    {
        if (index < 0 || index >= Config.skill_count || delta == 0) return;

        int current = GetSelectedSkillCount(index);
        int next = current + delta;
        if (next <= 0)
        {
            SelectedSkillCounts.Remove(index);
            RefreshSkillSelectedState();
            return;
        }

        if (current == 0 && SelectedSkillCounts.Count >= Config.max_selected_skill_count)
        {
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, $"最多携带{Config.max_selected_skill_count}种技能");
            return;
        }
        if (next > Tool.SaveManager.GetRuneCount(index))
        {
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "符文数量不足");
            return;
        }

        int nextWeight = GetSelectedRuneWeight() + SkillManager.GetSkillWeight(index) * delta;
        if (nextWeight > Config.max_take_in_rune_weight)
        {
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "携带符文负重已满");
            return;
        }

        SelectedSkillCounts[index] = next;
        RefreshSkillSelectedState();
    }
    private int GetSelectedSkillCount(int skillId)
    {
        return SelectedSkillCounts.TryGetValue(skillId, out var count) ? count : 0;
    }
    private int GetSelectedRuneWeight()
    {
        int weight = 0;
        foreach (var pair in SelectedSkillCounts)
        {
            if (pair.Value <= 0) continue;
            weight += SkillManager.GetSkillWeight(pair.Key) * pair.Value;
        }
        return weight;
    }

    private static string GetSkillQualityName(SkillInfo.Quality quality)
    {
        return quality switch
        {
            SkillInfo.Quality.C => "平凡",
            SkillInfo.Quality.B => "稀有",
            SkillInfo.Quality.A => "史诗",
            SkillInfo.Quality.S => "神话",
            _ => quality.ToString()
        };
    }

    private static Color GetSkillQualityColor(SkillInfo.Quality quality)
    {
        return quality switch
        {
            SkillInfo.Quality.C => new Color(0.9f,0.9f,0.9f,1f),
            SkillInfo.Quality.B => new Color(0.3f,0.9f,0.3f,1f),
            SkillInfo.Quality.A => new Color(0.9f,0.2f,0.8f,1f),
            SkillInfo.Quality.S => new Color(0.9f,0.3f,0.3f,1f),
            _ => Color.white
        };
    }

    private void EnterWorld()
    {
        if (NetworkManager.levelInfo == null)
        {
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "请先连接服务器");
            return;
        }
        if (!CanEnterBattle(out var reason))
        {
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, reason);
            return;
        }
        SaveLocalSelectCache();
        Tool.NetworkManager.EnterWorld();
    }
    private bool CanEnterBattle(out string reason)
    {
        if (!Tool.SaveManager.CanConsumeCharacterToken(currentSelectedCharacter))
        {
            reason = "缺少角色信物";
            return false;
        }

        int skillKindCount = 0;
        int weight = 0;
        foreach (var pair in SelectedSkillCounts)
        {
            int skillId = pair.Key;
            int count = pair.Value;
            if (count <= 0) continue;

            skillKindCount++;
            if (Tool.SaveManager.runeCounts[skillId] < count)
            {
                reason = "存在没有足够剩余次数的技能符文";
                return false;
            }

            weight += SkillManager.GetSkillWeight(skillId) * count;
        }

        if (skillKindCount <= 0)
        {
            reason = "至少选择1种技能";
            return false;
        }
        if (skillKindCount > Config.max_selected_skill_count)
        {
            reason = $"最多携带{Config.max_selected_skill_count}种技能";
            return false;
        }
        if (weight > Config.max_take_in_rune_weight)
        {
            reason = "携带符文负重超出上限";
            return false;
        }

        reason = string.Empty;
        return true;
    }
    private void ApplyMapIcon(int Index,Toggle linkedToggle,List<Transform>anchors)
    {
        float size = Landscape.chunkCount * Landscape.chunkSize;
        foreach(var t in anchors)
        {
            float x = t.position.x / size;
            float y = t.position.z / size;
            x = ExploreMapLeftBottom.position.x + (ExploreMapRightTop.position.x - ExploreMapLeftBottom.position.x) * x;
            y = ExploreMapLeftBottom.position.y + (ExploreMapRightTop.position.y - ExploreMapLeftBottom.position.y) * y;
            var n=Instantiate(ExploreIconTemplates[Index],new Vector3(x,y),Quaternion.identity, ExploreIconTemplates[Index].parent);
            ExploreIcons[Index].Add(n);
        }
        linkedToggle.onValueChanged.AddListener(isOn=>{
            foreach(var obj in ExploreIcons[Index])
            {
                obj.gameObject.SetActive(isOn);
            }
        });
    }
    private void OnConnect()
    {
        LoginConnected.SetActive(true);
        LoginDisconnected.SetActive(false);
        LoginIp.text = LoginIpInput.text;
        LoginLabelDifficulty.text = NetworkManager.levelInfo.difficulty.ToString();
        LoginLabelMemberCount.text = NetworkManager.levelInfo.playerCount.ToString();
        LoginLabelInfection.text = NetworkManager.levelInfo.infectionDegree.ToString();
    }
    private void OnDisconnect()
    {
        LoginConnected.SetActive(false);
        LoginDisconnected.SetActive(true);
    }
    #region 本地选中缓存读写
    private const string Key_SelectChar = "Home_SelectChar";
    private const string Key_SelectImprint = "Home_SelectImprint";
    private const string Key_SelectSkill = "Home_SelectSkill";

    // 加载本地缓存到静态变量
    private void LoadLocalSelectCache()
    {
        if (!PlayerPrefs.HasKey(Key_SelectSkill)) return;
        // 读取选中角色
        currentSelectedCharacter = PlayerPrefs.GetInt(Key_SelectChar, 0);

        // 读取印记集合
        SelectedImprints.Clear();
        string imprintStr = PlayerPrefs.GetString(Key_SelectImprint, "");
        if (!string.IsNullOrEmpty(imprintStr))
        {
            var arr = imprintStr.Split(',').Select(int.Parse).ToList();
            foreach (var id in arr)
            {
                if (SelectedImprints.Count >= Config.max_selected_imprint_count) break;
                SelectedImprints.Add(id);
            }
        }

        // 读取技能集合
        SelectedSkillCounts.Clear();
        string skillStr = PlayerPrefs.GetString(Key_SelectSkill, "");
        if (!string.IsNullOrEmpty(skillStr))
        {
            var arr = skillStr.Split(',');
            foreach (var item in arr)
            {
                if (SelectedSkillCounts.Count >= Config.max_selected_skill_count) break;
                if (string.IsNullOrWhiteSpace(item)) continue;

                var pair = item.Split(':');
                if (!int.TryParse(pair[0], out var id)) continue;
                int count = 1;
                if (pair.Length > 1) int.TryParse(pair[1], out count);
                if (id < 0 || id >= Config.skill_count || count <= 0) continue;
                SelectedSkillCounts[id] = count;
            }
        }
        Debug.Log("已加载用户选择");
    }

    // 保存当前选中状态到本地
    private void SaveLocalSelectCache()
    {
        PlayerPrefs.SetInt(Key_SelectChar, currentSelectedCharacter);
        PlayerPrefs.SetString(Key_SelectImprint, string.Join(",", SelectedImprints));
        PlayerPrefs.SetString(Key_SelectSkill, string.Join(",", SelectedSkillCounts.Select(i => i.Key + ":" + i.Value)));
        PlayerPrefs.Save();
        Debug.Log("已保存用户选择");
    }
    #endregion

}
