using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 技能栏表现：条目骨架、键位、图标、恢复进度与库存次数。冷却推演在 ClientSkillManager
    public partial class UI_SkillList
    {
        // 单个槽位的表现数据，skillId < 0 = 空槽
        public readonly struct Slot
        {
            public readonly int skillId;
            public readonly float recharge01; // 本轮恢复进度，1 = 满
            public readonly int store;
            public readonly bool unlimited;   // 无限库存：不显示次数、进度恒满

            public Slot(int skillId, float recharge01, int store, bool unlimited)
            {
                this.skillId = skillId;
                this.recharge01 = recharge01;
                this.store = store;
                this.unlimited = unlimited;
            }
        }

        private readonly List<Slot> slots = new();
        private Action<int, GObject> hoverEnter;
        private Action hoverExit;

        public bool IsBuilt { get; private set; }

        public int SlotCount { get; private set; }

        // 搭骨架：条目数 = 角色技能槽位数；悬浮提示由调用方提供，提示面板不在这边
        public void Build(int count, Action<int, GObject> onHoverEnter, Action onHoverExit)
        {
            var list = m_content;
            if (list == null) return;
            SlotCount = count;
            hoverEnter = onHoverEnter;
            hoverExit = onHoverExit;
            list.itemRenderer = RenderSlot;
            list.numItems = count; // 非虚拟列表会同步建出全部条目并回调渲染器
            RefreshWidth(list);
            IsBuilt = true;
        }

        // 重画内容：条目数不变，按新数据逐个刷新（恢复进度靠这条周期性推进）
        public void Refresh(IReadOnlyList<Slot> data)
        {
            if (!IsBuilt || m_content == null) return;
            slots.Clear();
            if (data != null)
                for (int i = 0; i < data.Count; i++) slots.Add(data[i]);
            m_content.numItems = SlotCount;
        }

        private void RenderSlot(int index, GObject obj)
        {
            if (obj is not UI_SkillListItem slot) return;
            // 键位：槽位顺序即 Config.skill_slot_keys（U I O L H Y）
            if (slot.m_key != null)
                slot.m_key.text = index < Config.skill_slot_keys.Length ? Config.skill_slot_keys[index].ToString() : "";

            Slot data = index < slots.Count ? slots[index] : new Slot(-1, 1f, 0, true);
            bool empty = data.skillId < 0;
            if (slot.m_empty != null) slot.m_empty.selectedIndex = empty ? 1 : 0;
            if (empty) ClearSlot(slot);
            else FillSlot(slot, data);

            // 空槽不弹提示，但离开时照常收起
            slot.onRollOver.Set(() => { if (data.skillId >= 0) hoverEnter?.Invoke(data.skillId, slot); });
            slot.onRollOut.Set(() => hoverExit?.Invoke());
        }

        private static void ClearSlot(UI_SkillListItem slot)
        {
            if (slot.m_loader_icon != null)
            {
                slot.m_loader_icon.texture = null;
                slot.m_loader_icon.visible = false;
                slot.m_loader_icon.fillAmount = 1f;
            }
            if (slot.m_loader_iconBase != null)
            {
                slot.m_loader_iconBase.texture = null;
                slot.m_loader_iconBase.visible = false;
            }
            if (slot.m_storeList != null) slot.m_storeList.numItems = 0;
        }

        private static void FillSlot(UI_SkillListItem slot, in Slot data)
        {
            Texture icon = ResolveIcon(data.skillId);
            if (slot.m_loader_icon != null)
            {
                slot.m_loader_icon.texture = icon != null ? new NTexture(icon) : null;
                slot.m_loader_icon.visible = icon != null;
                slot.m_loader_icon.fillAmount = data.unlimited ? 1f : data.recharge01;
            }
            // 底图与图标同一张图，前后层级由 FGUI 保证
            if (slot.m_loader_iconBase != null)
            {
                slot.m_loader_iconBase.texture = icon != null ? new NTexture(icon) : null;
                slot.m_loader_iconBase.visible = icon != null;
            }
            if (slot.m_storeList != null) slot.m_storeList.numItems = data.unlimited ? 0 : data.store;
        }

        // 技能图标：技能栏与技能选择面板共用同一张表
        internal static Texture ResolveIcon(int skillId)
        {
            if (skillId < 0) return null;
            var map = Tool.AssetsManager != null ? Tool.AssetsManager.SkillIconMap : null;
            if (map == null) return null;
            return map.TryGetValue(skillId, out var icon) ? icon : null; // 没配图标 → 不显示
        }

        // 列表宽度 = 全部条目宽度之和（FGUI 里是横向单行、不会自己撑开）
        private static void RefreshWidth(GList list)
        {
            float width = 0f;
            for (int i = 0; i < list.numChildren; i++)
            {
                var child = list.GetChildAt(i);
                if (child != null) width += child.width;
            }
            list.width = width;
        }
    }
}
