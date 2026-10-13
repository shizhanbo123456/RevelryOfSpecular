using System;
using System.Collections.Generic;
using FairyGUI;
using Ros.Gm;
using Ros.Transport;
using Ros.UI.Main;
using UnityEngine;

// GM 面板：一级菜单=组，二级菜单=该组下的"子功能"（路径第二段）。
// Item 列表显示当前子功能下的所有指令：每条指令前插一行分割标题（标题=该指令最深层名字；二级指令即其二级名），
// 分割标题用 Label（左=标题、右空）。按钮默认行为 = 收集本指令控件值 → 按行顺序填 CSGm → 发包。
// 未联网时只显示带 [GmOffline] 的指令。
// 待正式工程核对：GmItem 内容页名（文本/按钮/滑动条/输入框/下拉框）、菜单 button_list 的 item 类型。
public class GmPanel
{
    private readonly UI_GmPanel panel;

    private readonly List<string> groups = new();                              // 一级菜单：组名
    private readonly List<string> subs = new();                                // 二级菜单：当前组的子功能
    private readonly List<LayoutInfo> rows = new();                            // Item 列表行（含分割标题）
    private readonly List<(GmEnum cmd, int start, int end)> cmdRanges = new(); // 每条指令的行区间 [start,end)（不含分割标题）

    private string curGroup;
    private string curSub;

    // 面板入口：1 秒内连按 3 次 V 打开，Backspace 关闭
    private const KeyCode panelOpenKey = KeyCode.V;
    private const KeyCode panelCloseKey = KeyCode.Backspace;
    private const int panelOpenPressCount = 3;
    private const float panelOpenPressWindow = 1f;
    private int pressCount;
    private float firstPressTime;
    private string meshFacesText = string.Empty;   // 收集-模型网格面数：点刷新时算一次，渲染时读缓存
    private string serverFpsText = string.Empty;   // 收集-服务器帧率：服务器回包后填
    private string serverTimeText = string.Empty;  // 收集-服务器时间：服务器回包后填

    // 开关类指令的本地状态（服务端按目标状态设置；本地只用于按钮标题与切换）
    private readonly HashSet<GmEnum> toggleOn = new();
    private readonly HashSet<int> passiveOn = new(); // 防守方被动下标 0~4

    public GmPanel(UI_GmPanel panel)
    {
        this.panel = panel;
        GRoot.inst.AddChild(panel);
        panel.visible = false;
    }

    public bool IsOpen => panel.visible;

    public void Construct()
    {
        panel.m_firstMenu.m_button_list.itemRenderer = RenderGroupButton;
        panel.m_firstMenu.m_button_list.onClickItem.Add(OnClickGroup);
        panel.m_secondMenu.m_button_list.itemRenderer = RenderSubButton;
        panel.m_secondMenu.m_button_list.onClickItem.Add(OnClickSub);
        panel.m_GmItemList.itemRenderer = RenderRow;

        // EventManager 是静态的，订阅必须与解绑成对
        EventManager.AddEvent(ClientEvent.OnConnect, OnNetChanged);
        EventManager.AddEvent(ClientEvent.OnExitWorld, OnNetChanged);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnNetChanged);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStartChanged);
        EventManager.AddEvent<SCGm>(ClientEvent.OnGmResult, OnGmResult);
    }

    public void Shutdown()
    {
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnNetChanged);
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, OnNetChanged);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnNetChanged);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStartChanged);
        EventManager.RemoveEvent<SCGm>(ClientEvent.OnGmResult, OnGmResult);
    }

    // 与页面同规则的整体缩放（设计高度 1080）
    public void OnResize(float uiScale) => panel.scale = new Vector2(uiScale, uiScale);

    // 每帧由 UIManager 驱动：检测面板开关快捷键
    public void Tick()
    {
        if (GRoot.inst.focus is GTextInput) return; // 正在编辑输入框时不响应，避免 Backspace 误关 / V 误开
        if (Input.GetKeyDown(panelCloseKey))
        {
            ResetPress();
            Close();
            return;
        }
        if (!Input.GetKeyDown(panelOpenKey)) return;
        if (pressCount == 0 || Time.unscaledTime - firstPressTime > panelOpenPressWindow)
        {
            pressCount = 1;
            firstPressTime = Time.unscaledTime;
        }
        else pressCount++;
        if (pressCount >= panelOpenPressCount)
        {
            ResetPress();
            Open();
        }
    }

    private void ResetPress()
    {
        pressCount = 0;
        firstPressTime = 0f;
    }

    public void Open()
    {
        panel.visible = true;
        BuildGroups();
    }

    public void Close() => panel.visible = false;

    private static bool Online => Tool.NetworkManager != null && NetworkManager.CanSendWorldCommand;

    private void OnNetChanged() { if (panel.visible) BuildGroups(); }

    // 开战：被动启用项只属于组队期，要在战斗期隐藏 → 重建菜单
    private void OnBattleStartChanged(int camp) => OnNetChanged();

    #region//菜单
    private static string GroupOf(GmEnum t) => SplitLabel(t)[0];
    private static string SubOf(GmEnum t) => SplitLabel(t)[1];

    private static string LeafOf(GmEnum t)
    {
        string[] s = SplitLabel(t);
        return s[s.Length - 1];
    }

    private static string[] SplitLabel(GmEnum type) => GetLabel(type).Split('-');

    private static string GetLabel(GmEnum type)
    {
        var f = typeof(GmEnum).GetField(type.ToString());
        var a = f != null ? (GmLabelAttribute)Attribute.GetCustomAttribute(f, typeof(GmLabelAttribute)) : null;
        return a != null ? a.label : type.ToString();
    }

    private static bool IsOffline(GmEnum type)
    {
        var f = typeof(GmEnum).GetField(type.ToString());
        return f != null && Attribute.GetCustomAttribute(f, typeof(GmOfflineAttribute)) != null;
    }

    // 未联网时只显示可离线项；「被动启用」只在组队期（非战斗）显示
    private static bool Visible(GmEnum t) =>
        (Online || IsOffline(t)) && (t != GmEnum.Player_Skill_PassiveEnable || !NetworkManager.BattleRunning);

    // numItems 置相同值不会重刷，先置 0 再置 N
    private static void Refresh(GList list, int count)
    {
        list.numItems = 0;
        list.numItems = count;
    }

    private void BuildGroups()
    {
        groups.Clear();
        foreach (GmEnum t in Enum.GetValues(typeof(GmEnum)))
        {
            string g = GroupOf(t);
            if (!groups.Contains(g)) groups.Add(g);
        }
        Refresh(panel.m_firstMenu.m_button_list, groups.Count);
        if (groups.Count > 0) SelectGroup(groups[0]);
    }

    private void SelectGroup(string g)
    {
        curGroup = g;
        subs.Clear();
        foreach (GmEnum t in Enum.GetValues(typeof(GmEnum)))
        {
            if (GroupOf(t) != g || !Visible(t)) continue;
            string s = SubOf(t);
            if (!subs.Contains(s)) subs.Add(s);
        }
        Refresh(panel.m_secondMenu.m_button_list, subs.Count);
        if (subs.Count > 0) SelectSub(subs[0]);
        else ClearItems();
    }

    private void SelectSub(string sub)
    {
        curSub = sub;
        rows.Clear();
        cmdRanges.Clear();
        foreach (GmEnum t in Enum.GetValues(typeof(GmEnum)))
        {
            if (GroupOf(t) != curGroup || SubOf(t) != sub || !Visible(t)) continue;
            rows.Add(new Label(LeafOf(t), string.Empty)); // 分割标题：左=最深层名字、右空
            int start = rows.Count;
            var r = Layout.GetLayout(t);
            rows.AddRange(r);
            cmdRanges.Add((t, start, start + r.Count));
        }
        Refresh(panel.m_GmItemList, rows.Count);
    }

    private void ClearItems()
    {
        rows.Clear();
        cmdRanges.Clear();
        Refresh(panel.m_GmItemList, 0);
    }

    private void RenderGroupButton(int index, GObject obj)
    {
        if (obj is GButton b && index < groups.Count) b.title = groups[index];
    }

    private void RenderSubButton(int index, GObject obj)
    {
        if (obj is GButton b && index < subs.Count) b.title = subs[index];
    }

    private void OnClickGroup(EventContext ctx) => SelectGroup(groups[panel.m_firstMenu.m_button_list.GetChildIndex((GObject)ctx.data)]);

    private void OnClickSub(EventContext ctx) => SelectSub(subs[panel.m_secondMenu.m_button_list.GetChildIndex((GObject)ctx.data)]);
    #endregion

    #region//参数行
    // 某行属于哪条指令（分割标题行不属于任何指令，返回 null）
    private (GmEnum cmd, int start, int end)? FindRange(int rowIndex)
    {
        foreach (var r in cmdRanges)
            if (rowIndex >= r.start && rowIndex < r.end) return r;
        return null;
    }

    private void RenderRow(int index, GObject obj)
    {
        if (obj is not UI_GmItem item || index >= rows.Count) return;
        var row = rows[index];
        item.m_title.text = RowLeft(row);

        var range = FindRange(index);
        if (range != null && ApplySpecial(range.Value.cmd, index - range.Value.start, item)) return;

        switch (row.LayoutType)
        {
            case LayoutType.Label:
                item.m_content.selectedPage = "文本";
                item.m_content_text.text = ((Label)row).right;
                break;

            case LayoutType.Button:
                item.m_content.selectedPage = "按钮";
                item.m_content_btn.title = range != null
                    ? ButtonTitle(range.Value.cmd, index - range.Value.start, ((Button)row).buttonTitle)
                    : ((Button)row).buttonTitle;
                // GList 复用 item，回调需先清空再挂，避免重复
                item.m_content_btn.onClick.Clear();
                item.m_content_btn.onClick.Add(() => OnAction(index));
                break;

            case LayoutType.IntInput:
            case LayoutType.FloatInput:
                item.m_content.selectedPage = "输入框";
                item.m_content_inputField.text = string.Empty;
                break;

            case LayoutType.IntSlider:
                SetupSlider(item, ((IntSlider)row).min, ((IntSlider)row).max);
                break;

            case LayoutType.FloatSlider:
                SetupSlider(item, ((FloatSlider)row).min, ((FloatSlider)row).max);
                break;

            case LayoutType.Dropdown:
                SetupDropdown(item, ((Dropdown)row).enumType);
                break;
        }
    }

    private static void SetupSlider(UI_GmItem item, float min, float max)
    {
        item.m_content.selectedPage = "滑动条";
        item.m_content_slider.min = min;
        item.m_content_slider.max = max;
        item.m_content_slider.value = min;
        item.m_content_slider_value.text = min.ToString("0.##");
    }

    private static void SetupDropdown(UI_GmItem item, Type enumType)
    {
        item.m_content.selectedPage = "下拉框";
        item.m_content_dropdown.items = GetDropdownLabels(enumType);
        item.m_content_dropdown.selectedIndex = 0;
    }

    // 下拉可选项：优先读成员的 [GmLabel]，否则用枚举名
    private static string[] GetDropdownLabels(Type enumType)
    {
        if (enumType == null || !enumType.IsEnum) return new string[0];
        string[] names = Enum.GetNames(enumType);
        for (int i = 0; i < names.Length; i++)
        {
            var f = enumType.GetField(names[i]);
            var a = f != null ? (GmLabelAttribute)Attribute.GetCustomAttribute(f, typeof(GmLabelAttribute)) : null;
            if (a != null) names[i] = a.label;
        }
        return names;
    }

    private static string RowLeft(LayoutInfo row) => row switch
    {
        Label r => r.left,
        Button r => r.left,
        InputInt r => r.left,
        FloatInput r => r.left,
        IntSlider r => r.left,
        FloatSlider r => r.left,
        Dropdown r => r.left,
        _ => string.Empty,
    };

    // 按钮标题：开关型按钮带当前状态（敌方对话 / 四个外挂 / 被动启用），其余用布局里的静态标题
    private string ButtonTitle(GmEnum cmd, int relativeIndex, string staticTitle)
    {
        if (cmd == GmEnum.Other_EnemyDialog) return $"敌方对话：{(ChatPlugin.ShowEnemyDialog ? "开" : "关")}";
        if (cmd == GmEnum.Player_Skill_PassiveEnable) return $"{staticTitle}：{(passiveOn.Contains(relativeIndex) ? "开" : "关")}";
        if (cmd is GmEnum.Player_Cheat_Flight or GmEnum.Player_Cheat_Invincible or GmEnum.Player_Cheat_Speed or GmEnum.Player_Cheat_IgnoreCd)
            return $"{staticTitle}：{(toggleOn.Contains(cmd) ? "开" : "关")}";
        return staticTitle;
    }
    #endregion

    #region//默认行为：收集 → 组包 → 发包（本地项除外）
    private void OnAction(int rowIndex)
    {
        var range = FindRange(rowIndex);
        if (range == null) return;
        var (cmd, start, end) = range.Value;

        // 开关类：本地记状态 + 按钮标题随状态更新（敌方对话纯本地，其余把目标状态发服务器）
        if (IsToggle(cmd))
        {
            HandleToggle(cmd, rowIndex, start);
            return;
        }

        // 本地项（纯客户端指令）直接本地执行，不需要联网
        if (IsLocalExecutable(cmd))
        {
            RunLocal(cmd, start, end);
            return;
        }

        if (!Online) return; // 联网项未联网不发包
        var gm = new CSGm { type = cmd };
        FillPacket(gm, start, end, rowIndex);
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendGm(gm);
    }

    // 按行顺序：int 值的行依次填 int1/int2，float 值的行依次填 float1/float2/float3
    private void FillPacket(CSGm gm, int start, int end, int clickedRowIndex)
    {
        int intSlot = 0, floatSlot = 0;
        for (int i = start; i < end; i++)
        {
            var item = panel.m_GmItemList.GetChildAt(i) as UI_GmItem;
            if (item == null) continue;
            var row = rows[i];

            switch (row.LayoutType)
            {
                case LayoutType.IntInput:
                    PutInt(gm, ref intSlot, ParseInt(item.m_content_inputField.text));
                    break;
                case LayoutType.FloatInput:
                    PutFloat(gm, ref floatSlot, ParseFloat(item.m_content_inputField.text));
                    break;
                case LayoutType.IntSlider:
                    PutInt(gm, ref intSlot, (int)item.m_content_slider.value);
                    break;
                case LayoutType.FloatSlider:
                    PutFloat(gm, ref floatSlot, item.m_content_slider.value);
                    break;
                case LayoutType.Dropdown:
                    PutInt(gm, ref intSlot, EnumValueOf(((Dropdown)row).enumType, item.m_content_dropdown.selectedIndex));
                    break;
                case LayoutType.Button:
                    if (i == clickedRowIndex) PutInt(gm, ref intSlot, i - start); // 多按钮指令（如被动×5）用指令内序号区分
                    break;
            }
        }
    }

    private static void PutInt(CSGm gm, ref int slot, int v)
    {
        if (slot == 0) gm.int1 = v;
        else if (slot == 1) gm.int2 = v;
        slot++;
    }

    private static void PutFloat(CSGm gm, ref int slot, float v)
    {
        if (slot == 0) gm.float1 = v;
        else if (slot == 1) gm.float2 = v;
        else if (slot == 2) gm.float3 = v;
        slot++;
    }

    private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : 0;
    private static float ParseFloat(string s) => float.TryParse(s, out var v) ? v : 0f;

    private static int EnumValueOf(Type enumType, int index)
    {
        if (enumType == null || !enumType.IsEnum) return index;
        var values = Enum.GetValues(enumType);
        return index >= 0 && index < values.Length ? Convert.ToInt32(values.GetValue(index)) : 0;
    }
    #endregion

    #region//特殊处理（Label 数据 / 本地项）
    // 本地项（纯客户端指令）：不联网也生效，各条实现在 RunLocal 里一一对应
    private static bool IsLocalExecutable(GmEnum cmd) => cmd switch
    {
        GmEnum.Screen_Terrain_ShowRadius or
        GmEnum.Screen_Terrain_ToggleTerrain or
        GmEnum.Screen_Fog or
        GmEnum.Save_PlayerLevel_AddExp or
        GmEnum.Save_PlayerLevel_ReduceExp or
        GmEnum.Save_PlayerLevel_AddLevel or
        GmEnum.Save_PlayerLevel_ReduceLevel or
        GmEnum.Save_CharacterLevel or
        GmEnum.Other_Settlement or
        GmEnum.Other_Debug or
        GmEnum.Other_Collect_MeshFaces => true,
        _ => false,
    };

    private void RunLocal(GmEnum cmd, int start, int end)
    {
        switch (cmd)
        {
            case GmEnum.Screen_Terrain_ShowRadius:
                SetTerrainRadius(ReadFloat(start));
                break;
            case GmEnum.Screen_Terrain_ToggleTerrain:
                ToggleTerrain();
                break;
            case GmEnum.Screen_Fog:
                ToggleFog();
                break;
            case GmEnum.Save_PlayerLevel_AddExp:
                ModifyPlayerExp(ReadInt(start));
                break;
            case GmEnum.Save_PlayerLevel_ReduceExp:
                ModifyPlayerExp(-ReadInt(start));
                break;
            case GmEnum.Save_PlayerLevel_AddLevel:
                ModifyPlayerLevel(ReadInt(start));
                break;
            case GmEnum.Save_PlayerLevel_ReduceLevel:
                ModifyPlayerLevel(-ReadInt(start));
                break;
            case GmEnum.Save_CharacterLevel:
                SetCharacterLevel(ReadDropdownValue(start, typeof(GmCharacter)), ReadInt(start + 1));
                break;
            case GmEnum.Other_Debug:
                RefreshDataRows(start, end); // 重算 FPS / 实体数量 / 延迟 / 客户端时间
                break;
            case GmEnum.Other_Collect_MeshFaces:
                meshFacesText = CountSceneMeshFaces().ToString();
                RefreshDataRows(start, end);
                break;
            case GmEnum.Other_Settlement:
                if (Tool.UIManager != null) Tool.UIManager.Settlement.PlayDemo();
                break;
        }
    }

    // 开关类：客户端记本地状态（按钮标题随状态更新）；四个外挂与被动启用把目标状态发服务器，敌方对话纯本地
    private static bool IsToggle(GmEnum cmd) =>
        cmd is GmEnum.Other_EnemyDialog or GmEnum.Player_Skill_PassiveEnable
            or GmEnum.Player_Cheat_Flight or GmEnum.Player_Cheat_Invincible
            or GmEnum.Player_Cheat_Speed or GmEnum.Player_Cheat_IgnoreCd;

    private void HandleToggle(GmEnum cmd, int rowIndex, int start)
    {
        if (cmd == GmEnum.Other_EnemyDialog)
        {
            ChatPlugin.ShowEnemyDialog = !ChatPlugin.ShowEnemyDialog;
            RefreshButtonTitle(rowIndex);
            return;
        }

        if (cmd == GmEnum.Player_Skill_PassiveEnable)
        {
            int index = rowIndex - start; // 按钮顺序即防守方被动下标
            bool on = !passiveOn.Contains(index);
            if (on) passiveOn.Add(index); else passiveOn.Remove(index);
            if (Tool.NetworkManager != null)
                Tool.NetworkManager.SendGm(new CSGm { type = cmd, int1 = index, int2 = on ? 1 : 0 });
            RefreshButtonTitle(rowIndex);
            return;
        }

        bool turnOn = !toggleOn.Contains(cmd);
        if (turnOn) toggleOn.Add(cmd); else toggleOn.Remove(cmd);
        if (Tool.NetworkManager != null)
            Tool.NetworkManager.SendGm(new CSGm { type = cmd, int1 = turnOn ? 1 : 0 });
        RefreshButtonTitle(rowIndex);
    }

    // 只改按钮标题，不重挂事件（避免在按钮自己的回调里清掉 onClick）
    private void RefreshButtonTitle(int rowIndex)
    {
        var range = FindRange(rowIndex);
        if (range == null || rows[rowIndex] is not Button row || ItemAt(rowIndex) is not UI_GmItem item) return;
        item.m_content_btn.title = ButtonTitle(range.Value.cmd, rowIndex - range.Value.start, row.buttonTitle);
    }

    // 服务器回包（目前只有帧率/时间）：填缓存并重画该指令的数据行
    private void OnGmResult(SCGm gm)
    {
        if (gm == null) return;
        switch (gm.type)
        {
            case GmEnum.Other_Collect_ServerFps: serverFpsText = $"{gm.int1} FPS"; break;
            case GmEnum.Other_Collect_ServerTime: serverTimeText = gm.float1.ToString("0.0"); break;
            default: return;
        }
        if (!panel.visible) return;
        foreach (var r in cmdRanges)
        {
            if (r.cmd != gm.type) continue;
            RefreshDataRows(r.start, r.end);
            break;
        }
    }

    // 个别指令的行需要特殊渲染时在此返回 true 接管；index 是"该指令内"的行序号。
    private bool ApplySpecial(GmEnum cmd, int index, UI_GmItem item)
    {
        if (cmd == GmEnum.Other_Debug && index <= 3)
        {
            item.m_content.selectedPage = "文本";
            item.m_content_text.text = DebugText(index);
            return true;
        }
        if (cmd == GmEnum.Other_Collect_MeshFaces && index == 1)
        {
            item.m_content.selectedPage = "文本";
            item.m_content_text.text = meshFacesText;
            return true;
        }
        if (cmd == GmEnum.Other_Collect_ServerFps && index == 1)
        {
            item.m_content.selectedPage = "文本";
            item.m_content_text.text = serverFpsText;
            return true;
        }
        if (cmd == GmEnum.Other_Collect_ServerTime && index == 1)
        {
            item.m_content.selectedPage = "文本";
            item.m_content_text.text = serverTimeText;
            return true;
        }
        return false;
    }

    // 刷新按钮：只重画该指令里的数据行；不动按钮行本身（避免在按钮回调里重挂它的事件）
    private void RefreshDataRows(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (rows[i].LayoutType != LayoutType.Label) continue;
            if (ItemAt(i) is UI_GmItem item) RenderRow(i, item);
        }
    }

    private UI_GmItem ItemAt(int rowIndex) => panel.m_GmItemList.GetChildAt(rowIndex) as UI_GmItem;

    private int ReadInt(int rowIndex) => ItemAt(rowIndex) is UI_GmItem it ? ParseInt(it.m_content_inputField.text) : 0;
    private float ReadFloat(int rowIndex) => ItemAt(rowIndex) is UI_GmItem it ? ParseFloat(it.m_content_inputField.text) : 0f;
    private int ReadDropdownValue(int rowIndex, Type enumType) => ItemAt(rowIndex) is UI_GmItem it ? EnumValueOf(enumType, it.m_content_dropdown.selectedIndex) : 0;

    private static string DebugText(int index) => index switch
    {
        0 => (1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f)).ToString("0"),
        1 => CountEntities().ToString(),
        2 => $"{EnsInstance.LocalClientDelay} ms",
        3 => Time.time.ToString("0.0"),
        _ => string.Empty,
    };

    private static int CountEntities()
    {
        var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
        if (players == null) return 0;
        int count = 0;
        foreach (var _ in players.AllViews) count++;
        return count;
    }

    // 模型网格面数：遍历场景中所有网格物体累加三角面
    private static long CountSceneMeshFaces()
    {
        long faces = 0;
        foreach (var mf in UnityEngine.Object.FindObjectsOfType<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh != null) faces += mesh.triangles.Length / 3L;
        }
        foreach (var smr in UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>())
        {
            var mesh = smr.sharedMesh;
            if (mesh != null) faces += mesh.triangles.Length / 3L;
        }
        return faces;
    }

    private static void SetTerrainRadius(float radius)
    {
        var culler = UnityEngine.Object.FindObjectOfType<TerrainChunkCuller>();
        if (culler == null) { Debug.LogWarning("[GM] 场景内没有 TerrainChunkCuller，显示半径不生效"); return; }
        culler.visibleDistance = Mathf.Max(0f, radius);
    }

    // 显隐地形：全部可见↔全部隐藏；隐藏时先停掉分块剔除，否则它本帧的 LateUpdate 会把块又打开
    private static void ToggleTerrain()
    {
        var spawns = Tool.LandscapeSpawns;
        if (spawns == null) return;
        bool hide = false;
        foreach (var go in spawns.terrainChunks)
            if (go != null && go.activeSelf) { hide = true; break; }

        var culler = UnityEngine.Object.FindObjectOfType<TerrainChunkCuller>();
        if (culler != null) culler.enabled = !hide;
        foreach (var go in spawns.terrainChunks)
            if (go != null) go.SetActive(!hide);
    }

    private static void ToggleFog()
    {
        var env = Tool.EnvironmentManager;
        if (env != null) env.SetFogEnabled(!env.fogEnabled);
    }

    private static void ModifyPlayerExp(int delta)
    {
        var sm = Tool.SaveManager;
        if (sm == null || delta == 0) return;
        if (delta > 0) sm.AddPlayerExp(delta); // 正向走存档的升级结算
        else { sm.playerExp = Mathf.Max(0, sm.playerExp + delta); sm.Save(); }
    }

    private static void ModifyPlayerLevel(int delta)
    {
        var sm = Tool.SaveManager;
        if (sm == null || delta == 0) return;
        sm.playerLevel = Mathf.Clamp(sm.playerLevel + delta, 1, Config.player_max_level);
        sm.Save();
    }

    private static void SetCharacterLevel(int index, int level)
    {
        var sm = Tool.SaveManager;
        if (sm == null || index < 0 || index >= sm.characterLevels.Count) return;
        sm.characterLevels[index] = Mathf.Clamp(level, 1, Config.max_entity_level);
        sm.Save();
    }
    #endregion
}
