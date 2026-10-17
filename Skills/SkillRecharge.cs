using System.Collections.Generic;
using UnityEngine;

namespace Ros.Skill.Utils
{
    // 技能冷却状态的唯一表达 = (skillId, cd 根时间戳)，根 = now − CD × (已恢复次数 + 当前轮进度)
    // 内部持有技能列表；未满时根不动、满库存后根与时间同速前进，用一次只把根前移一个 CD
    public class SkillRecharge
    {
        private readonly Dictionary<int, float> cdRoots = new();

        private readonly List<int> tickBuffer = new(); // Tick 的遍历缓冲，避免每帧新列表

        // 时间源：服务器返回 Time.time；客户端返回「本地时间 − 与服务器的时钟偏移」
        public virtual float GetServerTime()
        {
            return 0f;
        }

        #region 技能列表
        // 入列并按满库存起算；已持有则保持原有根戳（重复获得不补库存）
        public void AddSkill(int skillId)
        {
            if (skillId < 0 || cdRoots.ContainsKey(skillId)) return;
            cdRoots[skillId] = FullRoot(skillId, GetServerTime());
        }

        // 从服务器下发的二元组写入/刷新根戳（客户端用）
        public void SetRoot(int skillId, float rootTime)
        {
            if (skillId < 0) return;
            cdRoots[skillId] = rootTime;
        }

        public void RemoveSkill(int skillId)
        {
            cdRoots.Remove(skillId);
        }

        public void Clear()
        {
            cdRoots.Clear();
        }

        // 库存整体回满：复活重建实体等场景
        public void ResetAll()
        {
            if (cdRoots.Count == 0) return;
            float now = GetServerTime();
            tickBuffer.Clear();
            tickBuffer.AddRange(cdRoots.Keys);
            foreach (var skillId in tickBuffer) cdRoots[skillId] = FullRoot(skillId, now);
        }

        public List<int> GetSkillIds()
        {
            return new List<int>(cdRoots.Keys);
        }

        public bool HasSkill(int skillId)
        {
            return cdRoots.ContainsKey(skillId);
        }
        #endregion

        #region 推演
        // 每帧推进：满库存的根顶到「now − CD×max」，未满的原样保留
        public void Tick()
        {
            if (cdRoots.Count == 0) return;
            float now = GetServerTime();
            tickBuffer.Clear();
            tickBuffer.AddRange(cdRoots.Keys);
            foreach (var skillId in tickBuffer) cdRoots[skillId] = Advance(skillId, cdRoots[skillId], now);
        }

        // 使用一次：根前移一个 CD；无库存或无限库存时不动
        public void Consume(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return;
            float now = GetServerTime();
            float aligned = Advance(skillId, root, now); // 先对齐满库存的根，否则消耗会多算一段恢复量
            if (GetCycles(skillId, aligned, now) <= 0f) return;
            cdRoots[skillId] = aligned + SkillManager.GetSkillCD(skillId);
        }

        public int GetStore(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return 0;
            return (int)GetCycles(skillId, root, GetServerTime());
        }

        // 当前这一轮的恢复进度 0~1；满库存与无限库存都算 1（可直接喂冷却环）
        public float GetCycleProgress(int skillId)
        {
            if (!cdRoots.TryGetValue(skillId, out var root)) return 0f;
            float now = GetServerTime();
            float cycles = GetCycles(skillId, root, now);
            if (cycles < 0f) return 1f;
            if (cycles >= SkillManager.GetSkillStore(skillId)) return 1f;
            return cycles - (int)cycles;
        }

        // 下发给客户端的根戳；未持有返回 0
        public float GetRootTime(int skillId)
        {
            return cdRoots.TryGetValue(skillId, out var root) ? root : 0f;
        }
        #endregion

        #region//Local
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

        // 每帧推进：只负责把「早就满了」的根顶到满库存位置，未满时原样返回
        private static float Advance(int skillId, float rootTime, float now)
        {
            if (SkillManager.GetSkillStore(skillId) < 0) return rootTime; // 无限库存没有充能概念
            return Mathf.Max(rootTime, FullRoot(skillId, now));
        }
        #endregion
    }
}
