using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ros.Skill.Utils
{
    // 技能冷却的唯一状态 = (skillId, cd 根时间戳)；库存与进度都是 (now − root) / CD 的纯函数，没有每帧推演
    // 卸下槽位的技能冻结在卸下瞬间的恢复量，重新选上只平移根戳，防止反复装卸刷次数
    public class SkillRecharge
    {
        private readonly Dictionary<int, float> cdRoots = new();
        private readonly Dictionary<int, float> frozenAt = new();

        private readonly List<int> keyBuffer = new();

        private readonly Func<float> timeSource;

        public SkillRecharge(Func<float> timeSource)
        {
            this.timeSource = timeSource;
        }

        public float GetServerTime() => timeSource();

        #region 记账
        // 首次获得：按满库存起算
        public void AddSkill(int skillId)
        {
            if (skillId < 0 || cdRoots.ContainsKey(skillId)) return;
            cdRoots[skillId] = FullRoot(skillId, GetServerTime());
        }

        public void RemoveSkill(int skillId)
        {
            cdRoots.Remove(skillId);
            frozenAt.Remove(skillId);
        }

        public void Clear()
        {
            cdRoots.Clear();
            frozenAt.Clear();
        }

        // 遍历用：把已登记的技能 id 填进外部 buffer，避免每次分配新列表
        public void FillSkillIds(List<int> buffer)
        {
            buffer.Clear();
            buffer.AddRange(cdRoots.Keys);
        }

        // 整体替换：服务器下发的槽位技能与根戳，未列出的技能一律丢弃
        public void SetRoots(IReadOnlyList<int> skillIds, IReadOnlyList<float> roots)
        {
            if (skillIds == null || roots == null) return;
            if (skillIds.Count != roots.Count)
                throw new ArgumentException($"技能 id 与根戳数量不一致：{skillIds.Count} / {roots.Count}");
            cdRoots.Clear();
            frozenAt.Clear();
            for (int i = 0; i < skillIds.Count; i++) cdRoots[skillIds[i]] = roots[i];
        }

        // 槽位变更时一次性结算：卸下的记下解冻基准，重新选上的把根戳平移回卸下那一刻
        public void SetAdvancing(IReadOnlyList<int> advancing)
        {
            if (cdRoots.Count == 0) return;
            float now = GetServerTime();
            keyBuffer.Clear();
            keyBuffer.AddRange(cdRoots.Keys);
            foreach (var skillId in keyBuffer)
            {
                if (Contains(advancing, skillId))
                {
                    if (!frozenAt.TryGetValue(skillId, out var frozenTime)) continue;
                    cdRoots[skillId] += now - frozenTime;
                    frozenAt.Remove(skillId);
                    continue;
                }
                if (frozenAt.ContainsKey(skillId)) continue;
                cdRoots[skillId] = Mathf.Max(cdRoots[skillId], FullRoot(skillId, now));
                frozenAt[skillId] = now;
            }
        }

        // 下发给客户端的根戳：按槽位顺序输出，未登记属错配
        public void ExportRoots(IReadOnlyList<int> slotSkillIds, List<float> roots)
        {
            roots.Clear();
            if (slotSkillIds == null) return;
            for (int i = 0; i < slotSkillIds.Count; i++)
            {
                if (!cdRoots.TryGetValue(slotSkillIds[i], out var root))
                    throw new InvalidOperationException($"技能 {slotSkillIds[i]} 未登记冷却，无法下发根戳");
                roots.Add(root);
            }
        }
        #endregion

        #region 推演
        // 使用一次：根前移一个 CD；无库存或无限库存时不动
        public void Consume(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return;
            // 冻结中的技能以解冻基准为准，否则这段冻结期会被当成恢复量
            float now = frozenAt.TryGetValue(skillId, out var frozenTime) ? frozenTime : GetServerTime();
            float aligned = Mathf.Max(root, FullRoot(skillId, now)); // 满库存先对齐根戳，否则这段恢复量会漏掉
            if (GetCycles(skillId, aligned, now) <= 0f) return;
            cdRoots[skillId] = aligned + SkillManager.GetSkillCD(skillId);
        }

        public int GetStore(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return 0;
            return (int)GetCycles(skillId, root, RefTime(skillId));
        }

        // 当前这一轮的恢复进度 0~1；满库存与无限库存都算 1（可直接喂冷却环）
        public float GetCycleProgress(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return 0f;
            float now = RefTime(skillId);
            float cycles = GetCycles(skillId, root, now);
            if (cycles < 0f) return 1f;
            if (cycles >= SkillManager.GetSkillStore(skillId)) return 1f;
            return cycles - (int)cycles;
        }
        #endregion

        #region//Local
        // 冻结中的技能以解冻基准当「当前时刻」，读到的是卸下瞬间的恢复量
        private float RefTime(int skillId)
        {
            return frozenAt.TryGetValue(skillId, out var frozenTime) ? frozenTime : GetServerTime();
        }

        private static bool Contains(IReadOnlyList<int> ids, int skillId)
        {
            if (ids == null) return false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == skillId) return true;
            }
            return false;
        }

        // 满库存时的根：与时间同速前进，任何时刻要用都重新取，不能缓存
        private static float FullRoot(int skillId, float now)
        {
            int max = SkillManager.GetSkillStore(skillId);
            float cd = SkillManager.GetSkillCD(skillId);
            if (max < 0 || cd <= 0f) return now;
            return now - cd * max;
        }

        // 已恢复的轮数：整数部分 = 库存次数，小数部分 = 下一轮进度；无限库存 -1，满库存固定为上限
        private static float GetCycles(int skillId, float rootTime, float now)
        {
            int max = SkillManager.GetSkillStore(skillId);
            if (max < 0) return -1f;

            float cd = SkillManager.GetSkillCD(skillId);
            if (cd <= 0f) return max;

            // 夹到 [0, max]：满库存后 cycles 会超过上限，不夹的话小数部分会被误当成「正在恢复」
            return Mathf.Clamp((now - rootTime) / cd, 0f, max);
        }
        #endregion
    }
}
