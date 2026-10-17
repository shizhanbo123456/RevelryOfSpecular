using UnityEngine;

namespace Ros.Skill.Utils
{
    // 技能冷却状态的唯一表达 = (skillId, cd 根时间戳)：根 = now − CD × (已恢复次数 + 当前轮进度)
    // 未满时根固定不动、满库存后根与时间同速前进，所以「用一次」只需把根前移一个 CD
    public static class SkillRecharge
    {
        // 已恢复的轮数：整数部分 = 库存次数，小数部分 = 下一轮进度；无限库存返回 -1，满库存固定为上限
        public static float GetCycles(int skillId, float rootTime, float now)
        {
            int max = SkillManager.GetSkillStore(skillId);
            if (max < 0) return -1f;

            float cd = SkillManager.GetSkillCD(skillId);
            if (cd <= 0f) return max;

            // 夹到 [0, max]：满库存后 cycles 会超过上限，不夹的话小数部分会被误当成「正在恢复」
            return Mathf.Clamp((now - rootTime) / cd, 0f, max);
        }

        public static int GetStore(int skillId, float rootTime, float now)
        {
            return (int)GetCycles(skillId, rootTime, now);
        }

        // 当前这一轮的恢复进度 0~1；满库存与无限库存都算 1（可直接喂冷却环）
        public static float GetCycleProgress(int skillId, float rootTime, float now)
        {
            float cycles = GetCycles(skillId, rootTime, now);
            if (cycles < 0f) return 1f;
            if (cycles >= SkillManager.GetSkillStore(skillId)) return 1f;
            return cycles - (int)cycles;
        }

        // 满库存时的根：与时间同速前进，任何时刻要用都重新取，不能缓存
        public static float FullRoot(int skillId, float now)
        {
            int max = SkillManager.GetSkillStore(skillId);
            float cd = SkillManager.GetSkillCD(skillId);
            if (max < 0 || cd <= 0f) return now;
            return now - cd * max;
        }

        // 每帧推进：只负责把「早就满了」的根顶到满库存位置，未满时原样返回
        public static float Advance(int skillId, float rootTime, float now)
        {
            if (SkillManager.GetSkillStore(skillId) < 0) return rootTime; // 无限库存没有充能概念
            return Mathf.Max(rootTime, FullRoot(skillId, now));
        }

        // 使用一次：把根前移一个 CD；无库存或无限库存时原样返回（不消耗）
        public static float Consume(int skillId, float rootTime, float now)
        {
            float root = Advance(skillId, rootTime, now); // 先对齐满库存的根，否则消耗会多算一段恢复量
            if (GetStore(skillId, root, now) <= 0) return rootTime;
            return root + SkillManager.GetSkillCD(skillId);
        }
    }
}
