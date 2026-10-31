using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 大厅主视图：三阵营块（加入 / AI 数量 / 成员列表）、准备按钮、退出按钮
    public partial class UI_LobbyMemberList
    {
        // 大厅成员：clientId < 0 = AI，不进真人列表也不参与名额判定
        public readonly struct Member
        {
            public readonly int clientId;
            public readonly int camp;
            public readonly int characterIndex;
            public readonly bool ready;

            public Member(int clientId, int camp, int characterIndex, bool ready)
            {
                this.clientId = clientId;
                this.camp = camp;
                this.characterIndex = characterIndex;
                this.ready = ready;
            }
        }

        private readonly List<Member> members = new();
        private Func<int, string> nameOf;
        private Action<int, bool, int, int> onStateChanged; // camp / ready / 攻方AI / 守方AI
        private Action onExit;

        private int localClientId = -1;
        private int myCamp = -1;      // -1 未同步 / 0 进攻 / 1 防守 / 2 观战
        private bool myReady;         // 以服务器回显为准
        private int attackAICount;
        private int defenseAICount;
        private bool syncingFromServer; // 回显期间屏蔽本地回调，避免回环上报

        // 构造期一次：阵营档位、按钮、文案；发包与退出都回调给调用方
        public void Build(Func<int, string> memberNameOf, Action<int, bool, int, int> stateChanged, Action exitRequested)
        {
            nameOf = memberNameOf;
            onStateChanged = stateChanged;
            onExit = exitRequested;

            // 三个阵营块的图标档位（PlayerCampIcon.m_player_camp：0 进攻 / 1 防守 / 2 观战 / 3 中立）
            m_camp_attack.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Attack;
            m_camp_defense.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Defense;
            m_camp_spectate.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Spectator;

            m_camp_attack.m_camp_name.text = "进攻方";
            m_camp_defense.m_camp_name.text = "防守方";
            m_camp_spectate.m_camp_name.text = "观战";

            m_camp_attack.m_btn_join.onClick.Add(() => JoinCamp((int)PlayerCampType.Attack));
            m_camp_defense.m_btn_join.onClick.Add(() => JoinCamp((int)PlayerCampType.Defense));
            m_camp_spectate.m_btn_join.onClick.Add(() => JoinCamp((int)PlayerCampType.Spectator));
            m_camp_attack.m_add_ai.onClick.Add(() => ChangeAICount(false, +1));
            m_camp_attack.m_remove_ai.onClick.Add(() => ChangeAICount(false, -1));
            m_camp_defense.m_add_ai.onClick.Add(() => ChangeAICount(true, +1));
            m_camp_defense.m_remove_ai.onClick.Add(() => ChangeAICount(true, -1));
            m_btn_prepare.onClick.Add(ToggleReady);
            m_btn_exit.onClick.Add(OnExitClicked);

            RefreshCampMark();
        }

        // 服务器回显：成员表 + AI 数量以这里为准，回显期间屏蔽本地交互回调
        public void Apply(IReadOnlyList<Member> roomMembers, int attackAI, int defenseAI, int myClientId)
        {
            syncingFromServer = true;
            localClientId = myClientId;
            members.Clear();
            if (roomMembers != null)
                for (int i = 0; i < roomMembers.Count; i++) members.Add(roomMembers[i]);

            attackAICount = attackAI;
            defenseAICount = defenseAI;
            myCamp = -1;
            myReady = false;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].clientId != myClientId) continue;
                myCamp = members[i].camp;
                myReady = members[i].ready;
                break;
            }

            RefreshAICountLabels();
            RefreshPrepareButton();
            RefreshCampMark();
            RenderMemberList(m_camp_attack.m_attackerPlayers, (int)PlayerCampType.Attack);
            RenderMemberList(m_camp_defense.m_attackerPlayers, (int)PlayerCampType.Defense);
            RenderSpectateCount();
            syncingFromServer = false;
        }

        private void JoinCamp(int camp)
        {
            if (syncingFromServer || BlockedByReady()) return;
            if (camp != myCamp && !HasHumanSeat(camp))
            {
                if (Tool.UIManager != null)
                    Tool.UIManager.ShowFlyText(camp == (int)PlayerCampType.Defense ? "防守方人数已满" : "进攻方人数已满");
                return;
            }
            myCamp = camp;
            RaiseState();
        }

        // 真人名额（不含 AI）：观战不限，按服务器上次广播的成员列表预判，最终以服务器为准
        private bool HasHumanSeat(int camp)
        {
            if (camp == (int)PlayerCampType.Spectator) return true;
            int limit = camp == (int)PlayerCampType.Attack
                ? ConfigManager.GlobalVariableConfig.player_limit_attacker
                : ConfigManager.GlobalVariableConfig.player_limit_defenser;
            int count = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i].clientId >= 0 && members[i].camp == camp) count++;
            return count < limit;
        }

        private void ChangeAICount(bool defense, int delta)
        {
            if (syncingFromServer || BlockedByReady()) return;
            int limit = defense
                ? ConfigManager.GlobalVariableConfig.player_limit_defenser_AI
                : ConfigManager.GlobalVariableConfig.player_limit_attacker_AI;
            int current = defense ? defenseAICount : attackAICount;
            if (delta > 0 && current >= limit)
            {
                if (Tool.UIManager != null) Tool.UIManager.ShowFlyText(defense ? "防守方 AI 已达上限" : "进攻方 AI 已达上限");
                return;
            }
            int next = Mathf.Clamp(current + delta, 0, limit);
            if (defense) defenseAICount = next;
            else attackAICount = next;
            RefreshAICountLabels();
            RaiseState();
        }

        // 已准备（含开战倒计时中）不允许改阵营与 AI 数量：服务器只在准备状态变化时重算开局条件
        private bool BlockedByReady()
        {
            if (!myReady) return false;
            if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("请先取消准备");
            return true;
        }

        private void ToggleReady()
        {
            if (syncingFromServer) return;
            myReady = !myReady;
            RefreshPrepareButton();
            RaiseState(); // 服务器广播回显后，所有成员头像同步勾选状态
        }

        private void OnExitClicked()
        {
            // 断开不可逆，先弹二次确认
            if (Tool.UIManager != null) Tool.UIManager.ShowConfirm("确定断开与服务器的连接？", () => onExit?.Invoke());
            else onExit?.Invoke();
        }

        private void RaiseState() => onStateChanged?.Invoke(myCamp, myReady, attackAICount, defenseAICount);

        private void RefreshAICountLabels()
        {
            if (m_camp_attack.m_label_AI_count != null) m_camp_attack.m_label_AI_count.text = $"x{attackAICount}";
            if (m_camp_defense.m_label_AI_count != null) m_camp_defense.m_label_AI_count.text = $"x{defenseAICount}";
        }

        // 当前所在阵营标记：0 = 不在 / 1 = 在（myCamp 以服务器回显为准）
        private void RefreshCampMark()
        {
            m_camp_attack.m_is_in_this_camp.selectedIndex = myCamp == (int)PlayerCampType.Attack ? 1 : 0;
            m_camp_defense.m_is_in_this_camp.selectedIndex = myCamp == (int)PlayerCampType.Defense ? 1 : 0;
            m_camp_spectate.m_is_in_this_camp.selectedIndex = myCamp == (int)PlayerCampType.Spectator ? 1 : 0;
        }

        private void RefreshPrepareButton()
        {
            if (m_btn_prepare != null) m_btn_prepare.title = myReady ? "取消准备" : "准备";
        }

        // 观战方只显示人数，不列具体人员
        private void RenderSpectateCount()
        {
            int count = 0;
            for (int i = 0; i < members.Count; i++)
                if (members[i].camp == (int)PlayerCampType.Spectator) count++;
            if (m_camp_spectate.m_spectate_current_member_count != null)
                m_camp_spectate.m_spectate_current_member_count.text = count.ToString();
            m_camp_spectate.m_attackerPlayers.numItems = 0;
            m_camp_spectate.m_attackerPlayers.visible = false;
        }

        private void RenderMemberList(GList list, int camp)
        {
            if (list == null) return;
            var seats = new List<Member>();
            for (int i = 0; i < members.Count; i++)
                if (members[i].clientId >= 0 && members[i].camp == camp) seats.Add(members[i]); // 负数 = AI，不进真人列表

            var icons = Tool.AssetsManager == null ? null
                : camp == (int)PlayerCampType.Attack ? Tool.AssetsManager.AttackCharacterIcons : Tool.AssetsManager.DefenseCharacterIcons;
            list.itemRenderer = (i, obj) =>
            {
                var head = (UI_RoleHead)obj;
                var member = seats[i];
                head.m_roleName.text = nameOf != null ? nameOf(member.clientId) : $"玩家{member.clientId}";
                head.m_level.selectedIndex = 0; // 成员条目不展示等级
                head.m_lock.selectedIndex = 0;
                head.m_confirm.selectedIndex = member.ready ? 1 : 0; // 准备勾选
                head.m_quality.selectedIndex = MemberQuality(camp, member.characterIndex);
                bool hasIcon = member.characterIndex >= 0 && icons != null
                    && member.characterIndex < icons.Count && icons[member.characterIndex] != null;
                head.m_headIcon.texture = hasIcon ? new NTexture(icons[member.characterIndex]) : null;
            };
            list.numItems = seats.Count;
        }

        // 成员品质档：未选角色（characterIndex < 0）按 0 档，不读表
        private static int MemberQuality(int camp, int characterIndex)
        {
            if (characterIndex < 0) return 0;
            var type = camp == (int)PlayerCampType.Attack ? EntityType.Attack(characterIndex) : EntityType.Defense(characterIndex);
            return ConfigManager.EntityConfig.Get(type).quality;
        }
    }
}
