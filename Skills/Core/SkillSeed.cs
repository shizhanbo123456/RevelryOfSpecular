namespace Ros.Skill
{
    // 伪随机：种子由服务器在施放时生成并随包下发，两端按同一序列取数（禁止 UnityEngine.Random）
    public static class SkillSeed
    {
        public static int New() => UnityEngine.Random.Range(1, int.MaxValue);

        public static System.Random Rng(int seed) => new System.Random(seed);
    }
}
