using System;
using System.Collections.Generic;
using FairyGUI;

namespace Ros.UI.Main
{
    // 技能选择面板：持有技能列表、勾选集合、上限拦截；应用结果由调用方在关闭回调里做
    public partial class UI_SkillSelectPanel
    {
        private readonly List<int> held = new();
        private readonly List<int> selection = new();
        private Action onCommit;
        private int capacity;

        public bool IsOpen { get; private set; }

        // 当前勾选（按勾选顺序）；关闭回调里读取并交给技能栏
        public List<int> Selection => selection;

        // 构造期一次：标题与关闭按钮（关闭 = 收起 + 回调）
        public void Build(Action commit)
        {
            onCommit = commit;
            if (m_panel != null)
            {
                m_panel.title = "技能选择";
                if (m_panel.m_close != null) m_panel.m_close.onClick.Set(Commit);
            }
        }

        // 打开：默认勾选当前技能栏，列表按服务器下发的持有顺序排（空手三连不参与）
        public void Open(IReadOnlyList<int> heldSkills, IReadOnlyList<int> currentSelection, int slotCapacity)
        {
            capacity = slotCapacity;
            selection.Clear();
            if (currentSelection != null)
                for (int i = 0; i < currentSelection.Count; i++) selection.Add(currentSelection[i]);

            IsOpen = true;
            visible = true;
            touchable = true;
            RefreshHeld(heldSkills);
        }

        // 关闭按钮与外部关闭共用：先收起再回调，应用逻辑留在调用方
        public void Commit()
        {
            bool wasOpen = IsOpen;
            Close();
            if (wasOpen) onCommit?.Invoke();
        }

        // 只收起（进/出战斗重置用）
        public void Close()
        {
            IsOpen = false;
            selection.Clear();
            visible = false;
            touchable = false;
        }

        // 持有表变化时重建列表（打开期间新获得技能）
        public void RefreshHeld(IReadOnlyList<int> heldSkills)
        {
            if (!IsOpen) return;
            held.Clear();
            if (heldSkills != null)
                for (int i = 0; i < heldSkills.Count; i++) held.Add(heldSkills[i]);
            if (m_list == null) return;
            m_list.itemRenderer = RenderItem;
            m_list.numItems = held.Count;
        }

        private void RenderItem(int index, GObject obj)
        {
            if (obj is not UI_SkillSelectButton item || index >= held.Count) return;
            int skillId = held[index];
            Texture icon = UI_SkillList.ResolveIcon(skillId);
            if (item.m_loader_icon != null)
            {
                item.m_loader_icon.texture = icon != null ? new NTexture(icon) : null;
                item.m_loader_icon.visible = icon != null;
            }
            item.selected = selection.Contains(skillId);
            // GList 复用条目，先清后加以免叠加
            item.onClick.Set(() => ToggleItem(skillId));
        }

        // 勾选/取消：超过槽位数只提示，不改当前选择
        private void ToggleItem(int skillId)
        {
            if (selection.Contains(skillId))
            {
                selection.Remove(skillId);
            }
            else if (selection.Count >= capacity)
            {
                if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("已达到技能选择上限");
                return;
            }
            else
            {
                selection.Add(skillId);
            }
            if (m_list != null) m_list.numItems = held.Count;
        }
    }
}
