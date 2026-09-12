namespace Ros.Skill
{
    /// <summary>
    /// 非玩家单位与空手攻击技能池（id 100~199，分配见策划案 21.6）。
    ///
    /// 【统一技能模型】与玩家技能共用同一套 SkillBase 逻辑（store / CD / 轨迹 / 特效 / 伤害），
    /// 差异**仅在施法来源**：玩家 = 技能槽快捷键；非玩家实体 = AI 自动索敌触发。
    /// 因此这些技能同样通过 <see cref="SkillManager"/> 注册，客户端也能按 id 取到（悬浮武器等表现需要）。
    ///
    /// store 统一为 -1（无限制）：非玩家单位没有水晶补充渠道，若按策划案 13.1「store 用尽即停止施放」
    /// 会让塔/僵尸打一会儿就哑掉，故非玩家单位不受 store 限制（待策划确认；要限制就改这里的第 3 个参数）。
    /// 数值与效果为占位初值，待策划定稿；效果实现与其它技能包一样留空（继承 SkillStub）。
    /// </summary>
    public static class SkillPoolNonPlayer
    {
        public static void RegisterAll()
        {
            // ---- 空手攻击 100（徒手两连段 / 跃起砸地；AttackType 1/2 与 12）----
            SkillManager.Register(new SkillUnarmedStrike());

            // ---- 普通僵尸 101~119（爪击右/左、嘶吼；AttackType 41/42/43）----
            SkillManager.Register(new SkillZombieClawR());
            SkillManager.Register(new SkillZombieClawL());
            SkillManager.Register(new SkillZombieScream());

            // ---- 精英僵尸 120~139 ----
            SkillManager.Register(new SkillEliteZombieClawR());
            SkillManager.Register(new SkillEliteZombieClawL());
            SkillManager.Register(new SkillEliteZombieScream());

            // ---- 防御塔（瘟疫孢子）140~159 ----
            SkillManager.Register(new SkillTowerSporeShot());

            // ---- 瘟疫树 160~179 ----
            SkillManager.Register(new SkillPlagueTreeLash());
        }
    }

    /// <summary>非玩家单位·近战占位基类（徒手 / 爪击 / 藤鞭）：非远程，不参与右键触发判定。</summary>
    public abstract class NonPlayerMeleeStub : SkillStub
    {
        private readonly EntityAnim.AttackType castAnim;

        protected NonPlayerMeleeStub(int id, float cd, int store = -1, EntityAnim.AttackType castAnim = 0)
            : base(id, cd, store)
        {
            this.castAnim = castAnim;
        }

        /// <summary>释放动作 = 该攻击对应的 AttackType（与 AnimEvent 推送给客户端的 animId 同源）。</summary>
        public override EntityAnim.AttackType CastAnim => castAnim;
        /// <summary>近战/直接接触。</summary>
        public override bool Ranged => false;
    }

    /// <summary>非玩家单位·远程占位基类（塔的孢子喷射等）。</summary>
    public abstract class NonPlayerRangedStub : SkillStub
    {
        private readonly EntityAnim.AttackType castAnim;

        protected NonPlayerRangedStub(int id, float cd, int store = -1, EntityAnim.AttackType castAnim = 0)
            : base(id, cd, store)
        {
            this.castAnim = castAnim;
        }

        /// <summary>释放动作 = 该攻击对应的 AttackType。</summary>
        public override EntityAnim.AttackType CastAnim => castAnim;
    }

    #region 空手攻击（id 100）
    /// <summary>空手攻击（id 100）：与玩家 J 键空手攻击同源 —— 移动时 = 出拳两连段，静止时 = 跃起砸地（AttackType 12）。</summary>
    public class SkillUnarmedStrike : NonPlayerMeleeStub { public SkillUnarmedStrike() : base(100, 0.8f, -1, EntityAnim.AttackType.Attack_Hand_R) { } }
    #endregion

    #region 普通僵尸（id 101~119）
    /// <summary>僵尸爪击·右（id 101）。</summary>
    public class SkillZombieClawR : NonPlayerMeleeStub { public SkillZombieClawR() : base(101, 1.5f, -1, EntityAnim.AttackType.Zombie_Hand_Attack_R) { } }
    /// <summary>僵尸爪击·左（id 102）。</summary>
    public class SkillZombieClawL : NonPlayerMeleeStub { public SkillZombieClawL() : base(102, 1.5f, -1, EntityAnim.AttackType.Zombie_Hand_Attack_L) { } }
    /// <summary>僵尸嘶吼（id 103）：范围惊扰类，效果待实现。</summary>
    public class SkillZombieScream : NonPlayerMeleeStub { public SkillZombieScream() : base(103, 8f, -1, EntityAnim.AttackType.Zombie_Scream) { } }
    #endregion

    #region 精英僵尸（id 120~139）
    /// <summary>精英僵尸爪击·右（id 120）。</summary>
    public class SkillEliteZombieClawR : NonPlayerMeleeStub { public SkillEliteZombieClawR() : base(120, 1.5f, -1, EntityAnim.AttackType.Zombie_Hand_Attack_R) { } }
    /// <summary>精英僵尸爪击·左（id 121）。</summary>
    public class SkillEliteZombieClawL : NonPlayerMeleeStub { public SkillEliteZombieClawL() : base(121, 1.5f, -1, EntityAnim.AttackType.Zombie_Hand_Attack_L) { } }
    /// <summary>精英僵尸嘶吼（id 122）。</summary>
    public class SkillEliteZombieScream : NonPlayerMeleeStub { public SkillEliteZombieScream() : base(122, 8f, -1, EntityAnim.AttackType.Zombie_Scream) { } }
    #endregion

    #region 防御塔（瘟疫孢子，id 140~159）
    /// <summary>塔攻击·孢子喷射（id 140）：对攻击范围内任一单位自动索敌施放；塔攻击无预警 = 正常释放技能即可。
    /// 特效：子弹 BulletVFX 22（能量球4·绿），见《特效清单与分配表》三·4。</summary>
    public class SkillTowerSporeShot : NonPlayerRangedStub { public SkillTowerSporeShot() : base(140, 2f, -1) { } }
    #endregion

    #region 瘟疫树（id 160~179）
    /// <summary>瘟疫藤鞭（id 160）：瘟疫树主动攻击，对进入攻击范围的**任何**单位（进攻方与防守方均敌对，中立无友方）自动索敌施放。</summary>
    public class SkillPlagueTreeLash : NonPlayerRangedStub { public SkillPlagueTreeLash() : base(160, 2.5f, -1) { } }
    #endregion
}
