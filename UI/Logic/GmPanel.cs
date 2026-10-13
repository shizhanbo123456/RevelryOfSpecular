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
    }

    public void Shutdown()
    {
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnNetChanged);
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, OnNetChanged);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnNetChanged);
    }

    // 与页面同规则的整体缩放（设计高度 1080）
    public void OnResize(float uiScale) => panel.scale = new Vector2(uiScale, uiScale);

    public void Open()
    {
        panel.visible = true;
        BuildGroups();
    }

    public void Close() => panel.visible = false;

    private static bool Online => Tool.NetworkManager != null && NetworkManager.CanSendWorldCommand;

    private void OnNetChanged() { if (panel.visible) BuildGroups(); }

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

    // 未联网时只显示可离线项
    private static bool Visible(GmEnum t) => Online || IsOffline(t);

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
                item.m_content_btn.title = ((Button)row).buttonTitle;
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
    #endregion

    #region//默认行为：收集 → 组包 → 发包
    private void OnAction(int rowIndex)
    {
        if (!Online) return; // 未联网不发包（本地项后续在特殊处理里直接改）
        var range = FindRange(rowIndex);
        if (range == null) return;
        var (cmd, start, end) = range.Value;
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
    // 个别指令的行需要特殊渲染时在此返回 true 接管；index 是"该指令内"的行序号。
    private bool ApplySpecial(GmEnum cmd, int index, UI_GmItem item)
    {
        if (cmd == GmEnum.Other_Debug)
        {
            // FPS / 客户端时间为本地数据，直接刷文本
            if (index == 0)
            {
                item.m_content.selectedPage = "文本";
                item.m_content_text.text = (1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f)).ToString("0");
                return true;
            }
            if (index == 3)
            {
                item.m_content.selectedPage = "文本";
                item.m_content_text.text = Time.time.ToString("0.0");
                return true;
            }
        }
        return false;
    }
    #endregion
}
