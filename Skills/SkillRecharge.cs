using UnityEngine;

namespace Ros.Skill.Utils
{
    // 技能冷却状态的唯一表达 = (skillId, cd 根时间戳)，根 = now − CD × (已恢复次数 + 当前轮进度)
    // 未满时根固定不动、满库存后根与时间同速前进，所以「用一次」只需把根前移一个 CD
    public static class SkillRecharge
    {
        public struct State
        {
            public int store;           // 剩余可用次数，-1 = 无限制
            public float cycleProgress; // 当前这一轮的恢复进度 0~1，满库存 = 1
            public float nextRemain;    // 距下一点恢复的秒数，满库存 = 0
        }

        public static State GetState(int skillId, float rootTime, float now)
        {
            int max = SkillManager.GetSkillStore(skillId);
            if (max < 0) return new State { store = -1, cycleProgress = 1f, nextRemain = 0f };

            float cd = SkillManager.GetSkillCD(skillId);
            if (cd <= 0f) return new State { store = max, cycleProgress = 1f, nextRemain = 0f };

            float elapsed = Mathf.Max(0f, now - rootTime); // 双端时钟有偏差时按「刚满」处理，不出现负数次数
            float cycles = elapsed / cd;
            if (cycles >= max) return new State { store = max, cycleProgress = 1f, nextRemain = 0f };

            int store = (int)cycles;
            float progress = cycles - store;
            return new State
            {
                store = store,
                cycleProgress = progress,
                nextRemain = cd * (1f - progress),
            };
        }

        public static int GetStore(int skillId, float rootTime, float now)
        {
            return GetState(skillId, rootTime, now).store;
        }

        public static float GetCycleProgress(int skillId, float rootTime, float now)
        {
            return GetState(skillId, rootTime, now).cycleProgress;
        }

        public static float GetNextStoreRemain(int skillId, float rootTime, float now)
        {
            return GetState(skillId, rootTime, now).nextRemain;
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
