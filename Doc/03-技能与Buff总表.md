# 03 技能与 Buff 总表（唯一权威）

> 技能 id、名称、行为、发射点/落点、特效、Buff 数值口径**只在本文件维护**，其它文档只放指针。
> 代码实现细节见《02-代码架构》；特效资产编号见《04-特效资产》。

## 一、技能总表（80 个）

### 1.1 武器技能总览（0~49）

> 技能与 id 的对应由 `SkillTypes` 枚举顺序决定（段长：刀 11 / 长枪 8 / 枪械 15 / 魔法球 16），措辞取《特效清单分别表1019》的定稿描述。
> **发射点**：全部走实体骨骼锚点（`UpCenter` / `UpFront`；抛出武器、回旋武器用 `RightHand`），不用悬浮武器槽位。**落点**：不索敌 = 与起点同高水平射出；标 `[索敌]` 的 9 个 = 朝目标体中心水平锁定。

#### 近战刀（0~10）

| id | 名称 | 技能类 | 本体特效 | 状态表现（buff 由客户端统一播） |
| --- | --- | --- | --- | --- |
| 0 | 扇形三发冰锥 | SkillFanIceCone | Bullet[18] | 命中挂 1 级减速 5s |
| 1 | 连续三发雷锥 | SkillTripleThunderCone | Bullet[19] | 命中挂麻痹 2s |
| 2 | 一发冰冻弹 | SkillFreezeShot | Bullet[21] | 附加冰冻 2s |
| 3 | 力量侵蚀弹 | SkillStrengthBreakShot | Bullet[22] | 2 级力量降低 20s |
| 4 | 先一发后扇形两发 | SkillDoubleFanShot | Bullet[39] | 命中挂 1 级减速 5s |
| 5 | 绕身剑气 | SkillOrbitBlades | Bullet[40] ×4（绕身） | 无 |
| 6 | 后撤闪避+一发 | SkillBackstepShot | Bullet[41] | 短暂闪避 buff |
| 7 | 跃起随机散射 | SkillLeapScatter | Bullet[57] ×N（随机角） | 无 |
| 8 | 抛出武器 | SkillThrowWeapon | 武器模型本体 | 无 |
| 9 | 五发扇形武器 | SkillFanWeaponThrow | 武器模型本体 ×5 | 无 |
| 10 | 天降武器 | SkillSkyfallWeapon | 武器模型本体（天降） | 无 |

#### 长枪（11~18）

| id | 名称 | 技能类 | 本体特效 | 状态表现（buff 由客户端统一播） |
| --- | --- | --- | --- | --- |
| 11 | 回旋武器 | SkillBoomerangWeapon | 武器模型本体（去-回） | 无 |
| 12 | 背后扎入武器 | SkillBackstabWeapon | 武器模型本体 + 悬停 | 命中挂麻痹 2s |
| 13 | 速度提升 | SkillSwiftAura | 无 | 1 级加速 20s |
| 14 | 绕身武器击飞 | SkillOrbitWeaponKnockback | 武器模型本体 ×4（绕身） | 无 |
| 15 | 短距冲刺 | SkillShortDash | 无 | 无 |
| 16 | 10 级护盾 | SkillShieldTen | 无（护盾 buff 自带） | 10 级护盾 60s |
| 17 | 飞踢 | SkillFlyingKick | 无 | 无 |
| 18 | 小夹角冰扇 | SkillIceFanTiny | Bullet[9] | 命中挂冰冻 |

#### 枪械（19~33）

| id | 名称 | 技能类 | 本体特效 | 状态表现（buff 由客户端统一播） |
| --- | --- | --- | --- | --- |
| 19 | 后方随机汇聚 | SkillBackVolley | Bullet[11] ×6 | 无 |
| 20 | 连续扫射冰锥 | SkillIceSweep | Bullet[20] | 附加冰冻 3s |
| 21 | 定身弹 | SkillRootShot | Bullet[28] | 命中挂定身 3s |
| 22 | 缓慢漂浮弹 | SkillDriftOrb | Bullet[29] | 命中击飞 |
| 23 | 3/5/7 递增冰扇 | SkillTripleWaveIce | Bullet[45] | 命中挂冰冻 2s |
| 24 | 连续向前散射 | SkillForwardSpray | Bullet[46] | 无 |
| 25 | 高速沉默弹 | SkillSilenceBolt | Bullet[47] | 1 级沉默 12s |
| 26 | 周围天降弹幕 | SkillSkyfallVolley | RangeMagic[2] ×5（自身周围落下） | 无 |
| 27 | 周围射出武器 | SkillOrbitWeaponVolley | 武器模型本体 ×N（环身飞出） | 无 |
| 28 | 周围创造武器落下 | SkillOrbitWeaponRain | 武器模型本体 ×N（自身周围落下） | 无 |
| 29 | 牵引砸击 | SkillPullSmash | RangeMagic[7] 起手 + RangeMagic[4] 落点 | 无 |
| 30 | 前方连续落下 | SkillRainBarrage | Bullet[44] ×N | 无 |
| 31 | 冲刺 + 速度提升 | SkillDashSpeed | 无 | 2 级加速 10s |
| 32 | 8 级群体护盾 | SkillAllyShieldEight | 无（护盾 buff 自带） | 8 级护盾 60s（自身+附近友方） |
| 33 | 4 级自身护盾 | SkillShieldFour | 无（护盾 buff 自带） | 4 级护盾 10s |

#### 魔法球（34~49）

| id | 名称 | 技能类 | 本体特效 | 状态表现（buff 由客户端统一播） |
| --- | --- | --- | --- | --- |
| 34 | 终点分裂 5 发 | SkillSplitShot | Bullet[0] 母弹 + Bullet[36] ×5 分裂 | 无 |
| 35 | 牵引弹 | SkillPullBolt | Bullet[1] | 命中挂 MotionAttract 牵引 |
| 36 | 12 发静止弹幕 | SkillStaticBarrage | Bullet[2] ×12（定点） | 命中挂定身 10s |
| 37 | 扇形冰冻 | SkillFanFreeze | Bullet[3] | 命中挂冰冻 4s |
| 38 | 来回扫射中毒 | SkillSweepPoison | Bullet[4] ×N | 3 级中毒 5s |
| 39 | 八次弹跳 | SkillBounceShot | Bullet[5]（9 点折线） | 无 |
| 40 | 连续扫射燃烧 | SkillSweepBurn | Bullet[8] | 5 级燃烧 5s |
| 41 | 法阵 + 六向连射 | SkillCircleVolley | MagicCircle[7] 法阵 + Bullet[12] ×6 | 无 |
| 42 | 钩人扇形 | SkillHookFan | Bullet[14] ×5 | 命中挂 MotionAttract 牵引 |
| 43 | 漩涡延迟爆炸 | SkillVortexBomb | RangeMagic[1] | 命中挂 MotionAttract 牵引 |
| 44 | 6 定点持续伤害 | SkillSixOrbs | RangeMagic[3] ×6（定点） | 无 |
| 45 | 头顶沉默 | SkillSkyfallSilence | RangeMagic[4]（头顶落下） | 命中挂沉默 10s |
| 46 | 自身爆炸击飞 | SkillBlastKnockback | Bullet[7] ×6（扇形） | 灼烧 3 级 10s |
| 47 | 背后贝塞尔武器 | SkillBackArcWeapons | 武器模型本体 ×N（贝塞尔） | 无 |
| 48 | 群体暴击暴伤 | SkillAllyCritBoost | 无 | 5 级暴击率 + 5 级暴伤 20s（自身+附近友方） |
| 49 | 全体 25 级护盾 | SkillAllyShieldTwentyFive | 无（护盾 buff 自带） | 25 级护盾 30s（全体友方） |

### 1.2 防守方 6 角色（50~67）

> 这 18 个技能**都不播技能本体特效**：状态表现由对应的 Buff 承担，客户端按 `Config.buff_vfx` 统一播（技能里禁止播 buff 特效）。

| id | 名称 | 技能类 | 本体特效 | 状态表现 |
| --- | --- | --- | --- | --- |
| 50 | 岩石护盾 | SkillRockShield | 无（护盾 buff 自带） | 附近防御塔挂 TowerShield |
| 51 | 蘑菇感染（效果已移除，待重做） | SkillMushroomInfect | 无 | 无 |
| 52 | 灵火 | SkillTowerBlazeCast | 无 | 全场塔挂 TowerBlaze |
| 53 | 白眼标记 | SkillEyeMark | 无 | EyeMark（小地图常显） |
| 54 | 无限视野 | SkillInfiniteVision | 无 | 10 级可见距离提升 |
| 55 | 立即入夜 | SkillForceNight | 无 | 无 |
| 56 | 召唤僵尸 | SkillSummonZombies | 无 | 无 |
| 57 | 死灵漫步 | SkillDeathStrollCast | 无 | DeathStroll（霸体 + 光环 DoT） |
| 58 | 召唤精英 | SkillSummonElites | 无 | 无 |
| 59 | 沉默 | SkillSilenceCast | 无 | 范围内敌人挂 Silence |
| 60 | 泥沼 | SkillMireCast | 无 | 全场敌人挂 Mire |
| 61 | 反伤 | SkillReflectCast | 无 | 守护点挂 Reflect |
| 62 | 瘟疫标记 | SkillPlagueMarkCast | 无 | 敌人挂 PlagueMark |
| 63 | 吸收矿石 | SkillAbsorbOre | 无 | 无 |
| 64 | 引爆瘟疫 | SkillDetonatePlague | 无 | 移除 PlagueMark 并结算伤害 |
| 65 | 苍白之光 | SkillPaleLightCast | Bullet[2] ×5 | 命中挂 PaleLight |
| 66 | 苍白之暗 | SkillPaleDarkCast | Bullet[1] ×5 | 命中挂 PaleDark |
| 67 | 迷雾 | SkillFogCast | 无 | 敌人挂 Fog |

**被动（不是技能、不占 id）**：暴击麻痹（`BattleManagerPassive.OnHitPassive`）、光暗转化（`BattleManagerPassive.CheckPaleConversion`）、教皇守护 / 僵尸刷新等级提升 / 进攻方复活减速等全局被动见策划案第二十章。

### 1.3 非玩家单位与空手（68~79）

| id | 名称 | 技能类 | 本体特效 | 状态表现 |
| --- | --- | --- | --- | --- |
| 68 | 僵尸·爪击右 | SkillZombieClawR | 无（手部骨骼判定球） | 无 |
| 69 | 僵尸·爪击左 | SkillZombieClawL | 无（手部骨骼判定球） | 无 |
| 70 | 精英僵尸·爪击右 | SkillEliteZombieClawR | 无（手部骨骼判定球） | 无 |
| 71 | 精英僵尸·爪击左 | SkillEliteZombieClawL | 无（手部骨骼判定球） | 无 |
| 72 | 精英僵尸·嘶吼 | SkillEliteZombieScream | 无 | ZombieScreamBoost（增益 buff 自带特效） |
| 73 | 防御塔·孢子喷射1 | SkillTowerSporeShotA | Bullet[25] | 无 |
| 74 | 防御塔·孢子喷射2 | SkillTowerSporeShotB | Bullet[24] | 无 |
| 75 | 防御塔·孢子喷射3 | SkillTowerSporeShotC | Bullet[26] | 无 |
| 76 | 瘟疫树·孢子喷发 | SkillPlagueTreeSpore | RangeMagic[9] | 命中挂 Poison |
| 77 | 空手·左拳 | SkillPunchLeft | 无（手部骨骼判定球） | 无 |
| 78 | 空手·右拳 | SkillPunchRight | 无（手部骨骼判定球） | 无 |
| 79 | 空手·原地砸击 | SkillPunchSmash | RangeMagic[6] | 无 |

> 防御塔 / 瘟疫树的孢子同样从**体中心 `UpCenter`** 发出（不走塔顶 / 树冠），落点取目标体中心。

## 二、Buff 总表（35 条持续表现）

| Buff / 状态（EffectType） | 持续特效 | 主要来源 |
| --- | --- | --- |
| BeaconReduce 副守护点减伤 | Shield[0] 红覆盖 | 守护点机制（存活外围点） |
| TowerShield 岩石护盾 | MC[8] 红色防御增益 | 鹿铠怪人 主动1 |
| PopeGuard 教皇守护 | Shield[12] 红熔岩 | 蒙面教皇 被动 |
| Shield 护盾 | Shield[2] 白厚实 | 光盾 / 枪阵屏障 / 奥术屏障 |
| Stun 麻痹 | Buff[20] 麻痹黄 | 破霸重斩 / 麻痹弹 / 落雷术 / 鹿铠被动 |
| TowerBlaze 灵火 | MC[3] 深红护盾 | 鹿铠怪人 大招 |
| Burning 燃烧 | Buff[11] 火焰 | 燃烧弹 |
| EyeMark 白眼标记 | Buff[23] 宇宙 | 白眼伯爵 主动1 |
| PaleLight 苍白之光 | Buff[15] 黄色泛光 | 苍白舞者 主动1 |
| PaleDark 苍白之暗 | Buff[4] 黑色缠绕 | 苍白舞者 主动2 |
| PaleFire 苍白之火 | Buff[3] 血色缠绕 | 苍白舞者被动：光暗双 8 层转化 |
| PaleIce 苍白之冰 | Buff[14] 绿色治愈 | 苍白舞者被动：光满 10 层转化 |
| PaleThunder 苍白之雷 | Buff[20] 麻痹黄 | 苍白舞者被动：暗满 10 层转化 |
| DeathStroll 死灵漫步 | Buff[29] 橙色祝福 | 死灵漫步者 主动2 |
| DeathCurse 死灵诅咒 | Buff[0] 紫色雾团 | 死灵漫步者 主动2（尚未施加） |
| Silence 沉默 | Buff[2] 科技紫 | 蒙面教皇 主动1 |
| Mire 泥沼 | Buff[17] 黑绿喷发 | 蒙面教皇 主动2 |
| Reflect 反伤 | MC[6] 深紫色 | 蒙面教皇 大招 |
| Root 定身 | Buff[19] 麻痹蓝 | 捕获投掷 / 束缚钉 |
| PlagueMark 瘟疫标记 | Buff[30] 紫色祝福 | 瘟疫使者 主动1 |
| Poison 中毒 | Buff[13] 绿色治愈 | 毒珠 / 毒气弹 / 引爆 |
| Freeze 冰冻 | Buff[12] 冻结 | 冻结弹 / 冰锥术 / 冰霜新星 |
| AnimSlowDown 减速 | Buff[17] 黑绿喷发 | 破甲重弹 |
| AnimSpeedUp 加速 | Buff[27] 环绕风 | 激励弹 |
| PlagueBless 瘟疫祝福 | MC[9] 蓝色攻击增益 | 攻占瘟疫树 |
| ZombieScreamBoost 僵尸嘶吼增益 | MC[9] 蓝色攻击增益 | 僵尸·嘶吼（尚未施加） |
| AttrStrengthBoost / AttrMagicBoost | MC[0] 黄色力场 | 激励法阵等属性增益 |
| AttrCritRateBoost / AttrCritDamageBoost | MC[1] 治愈绿 | 暴击 / 暴伤增益 |
| AttrStrengthDecrease / AttrMagicDecrease / AttrCritRateDecrease / AttrCritDamageDecrease | MC[5] 深绿护盾 | 属性削弱类 |
| AttrKnockbackBoost 击退抗性提升 | MC[8] 红色防御增益 | 复活后的击退抗性提升等 |

### 2.1 明确无特效 / 非特效的状态（8）

| 状态 | 处理 |
| --- | --- |
| AttrKnockbackDecrease 击退抗性降低 | 无特效（1019 表未分配） |
| AttrViewDistanceBoost / AttrViewDistanceDecrease 可见距离增减 | 无特效 |
| DodgeBoost 闪避提升 | 无特效（属性侧暂无闪避字段） |
| YzCy 愈战愈勇 | 无特效（策划案明确无外观） |
| MinimapLost 小地图失联 | 小地图 UI 逻辑，非特效 |
| GmSpeedUp GM 极速 | 无特效 |
| Fog 迷雾 | 由技能本体挂 Buff[6] 紫雾喷发（不在 `buff_vfx` 表内） |

---

## 三、实现口径

Skills文件夹：
**当前状态（2026-10-20 重写）**：旧技能包与旧图标工具已删除（历史见 git），现行代码只有新技能。新技能 80 个，按拥有者分文件：`SkillPoolWeapon`（武器 50）、`SkillPoolDefense`（防守方 18）、`SkillPoolNpc`（普通僵尸 2 + 精英僵尸 3 + 塔孢子 3 + 树孢子 1）、`SkillPoolUnarmed`（空手 3），统一由 `SkillPools.RegisterAll()` 注册。`SkillType` 的值由枚举顺序自动分配（**禁止手写数字**），段长固定：刀 11 / 长枪 8 / 枪械 15 / 魔法球 16 / 防守方 18 / 非玩家 9 / 空手 3，**段长与顺序不可调整**（`Config.weapon_id_*` 与 `WeaponRef` 依赖段连续）；技能与武器槽位的对应是临时顺序分配（槽位 = 武器段内偏移），调整时同步改池文件里的 `WeaponRef` 与枚举顺序。技能图标分配表（`Editor/SkillIconAssigner.cs`）也已删除，需按新技能重建。
[E]外部调用只向SkillManager传入技能id即可。
技能双端执行模型：EntitySkillController.TryUseSkill是全部释放路径（技能槽/J键空手/AI/非玩家）的唯一收口——释放瞬间调 skill.BeginCast(entity, skillId)：按下时先摇攻击动画（动画类型由技能的 `CastAnim` 声明；`HasCastAnim=false` 的技能无动画则按下即施放），等动画到**攻击帧**（`EntityAnim.onAttack`）时才执行 `SkillLogic(entity)`（此刻索敌/起点基于位移后的实时位置）→ `OnCast(context)`（真实子弹结算）→ `SendSkillCast` 下发——**攻击帧回调不一次性清空**：多段攻击动画的每个攻击帧都各结算一次。客户端收到 RPC 即经 SkillManager.PlayVFX 调同一技能的 `PlayVFX(context)` 播放（**不等自己的 onAttack**），用同一个轨迹构建函数重建轨迹播放特效。技能 `SkillLogic` 只生成上下文，`CastAnim`/`HasCastAnim` 在技能上声明。技能内禁止直接 Instantiate 特效或引用 AssetsManager（下标映射集中在 VfxManager）。

**技能上下文（SkillContext）**：`ints` / `vectors` / `floats` 三个可变长列表，语义 = **能复原技能表现的最小可变数据包**。
① 只放运行时才确定、两端无法推导的东西：施法者 id、随机种子、瞄准点/落点、目标 id、档位、时间（放 floats）。发数、散布角、波次表、间隔、半径、时长、轨迹形状、特效下标一律写死在技能类常量里，读包时用「变参 + 技能常量」复原——包体不随发数膨胀。
② **包内容与布局由每个技能自己决定**：`ShotSkillBase` **不实现 `SkillLogic`**（`SkillBase.SkillLogic` 是 abstract），只提供可选 helper `ShotContext(entity, seed, aim)`——写不写种子、写不写落点由子类在自己那行 `SkillLogic => ShotContext(...)` 里显式声明；需要别的布局就直接 `new SkillContext()` 自己拼。读取同样由技能自己按下标解析（同一技能的内部约定：`ints[0]=施法者`、`vectors[0]=起点`、`vectors[1]=落点`，只有真写了才存在）。
③ **跨端随机一律伪随机**：服务器施放时生成种子写进包里，两端用技能自己的 `System.Random(seed)` 按固定调用顺序取数（禁止 `UnityEngine.Random`：全局状态，两端消耗顺序不同必然发散）。目前只有「跃起随机散射」「后方随机汇聚」写种子。
④ **位置一律 entityId + Vector3**：客户端有该实体就按锚点取实时位置（跟随身体），取不到实体数据才用 Vector3 兜底。
⑤ **禁止给子弹加跨端回调**：分裂 / 弹跳 / 延时等二次效果，施放时就算好时刻与点位，两端到点各自创建——弹跳 = 起点 + 8 跳共 9 个点整串下发；分裂 = 母弹终点 + 按弹速与距离动态算出的时刻。
包体参考（`SkillContextSerializer`：1B 空标记 + 每列表 4B 计数，int 4B / Vector3 12B）：普通射击 = 29B，不写落点 = 17B，写种子 = 33B。

**发射点（2026-10-20 起）**：只用实体骨骼锚点，**没有任何「漂浮武器槽位」发射点**（`GetWeaponFloatPosition` / `ShotOrigin` / 客户端 `TryGetShotOrigin` 已删除）。每个技能用 `OriginAnchor` + `OriginOffset` 声明：`UpCenter`（26 个）/ `UpFront`（12 个）/ `RightHand`（抛出武器、回旋武器 2 个）；防御塔与瘟疫树也是 `UpCenter`（不走塔顶）。两端都用「实体实时位姿 + 锚点 + 固定偏移」现算，包里的起点只作兜底。
**索敌与落点（2026-10-20 起）**：文档 `[索敌]` 标记 = 覆盖 `AutoAim => true`，其余默认 `false`（朝自身面向）。落点统一走 `AimPoint(entity, origin)`：
- 不索敌：`origin + forward × ForwardAimDistance`，**高度 = 起点高度**（水平射出）；
- 索敌：水平方向朝目标体中心（`TargetSelect.BodyPos`，即 `UpCenter`），**落点高度 = 目标体中心**（不锁 `transform.position` / 脚部），距离取技能 `CastRange`；
- 专门做上下变化的技能自己覆盖 `AimPoint`（「周围子弹天降」「周围创造武器落下」→ 自身脚下）或整段覆写 `TrajectoryFor`。
`TargetSelect.AimPos` 保留给非射击类索敌（防守方 / 塔 / 树），内部同样按体中心取点。
**轨迹构建**：射击类写 `TrajectoryFor(context, wave, shotIndex, shotsInWave, spread)`（默认 = 扇形 `FanDest` + `TrialConstructor.Line(origin, dest, BulletLife)`），非射击类写 `CreateTrajectory(context, index)`；也可直接 new 对应 `BulletTrajectory` 子类。
**多目标**：需要锁多个目标时由技能自己在 `SkillLogic` 里写（`ints[2..]` = 目标 id、`vectors[2..]` = 位置兜底，上限与排序规则写进技能常量）；纯范围效果优先只传「中心 + 半径 + 阵营」，由服务器自己取，不逐个下发。
**命中附加效果**：`AttackData.addEffectEvent`（命中挂 Buff）与 `AttackData.onHit`（命中额外逻辑）；子弹路径由 `BattleManagerCombat.TryHitBullet` 消费、近战球判定由 `SkillBase.StrikeSphere` 消费。`SkillBase` 提供 `ApplyEffect`（命中挂 Buff 回调）、`GiveEffect`（直接挂 Buff）、`PullTo`（命中拉拽）。**击退/击飞不是回调**：力度写在 `AttackData.knockbackPower` 上，由命中管线统一处理（见速度节⑥）。
**持续型 Buff 表现**：客户端按同步的 `SCEntityDisplayInfo.buffs` 增删特效（仅 `includeRuntime || hasBuffs` 的包触发），映射表见 `Config.buff_vfx`（清单即本文件二章）。**技能里禁止再为 Buff 播持续特效**（不写 `PlayFollow`）——否则同一状态叠两份、且技能那份不随 Buff 消失而清理；因此 `PlayVFX` 允许空实现。
**位移**：统一走 `MotionBase`（`Entity/Motion/`），由 `EntityData.TickVelocity` 逐帧驱动（Enter / Update / 到期 Exit 三态都在 TickVelocity 内，见速度节⑤），禁止直接改 position 以免穿模；现有 `MotionDash`（恒定朝前）、`MotionToPoint`（固定点）、`MotionAttract`（持续被目标吸引，距离越远速度越大、上限封顶；目标可传 `Transform` / 固定点 / 位置来源——服务器子弹没有 Transform，用「轨迹 + 出生时间」算实时位置）。
**延时 / 多段 / 重复结算：统一用 `Timer.AddTimer`**（见 Utils 节），不要在技能里自己写计时字段——多段命中、延时爆炸、引导间隔、姿态持续期结束等都在 `SkillLogic`/`OnCast` 里注册即可，服务端与客户端（`PlayVFX` 内）都能调。`AddTimer` 没有取消句柄，注册的闭包请自行判空（实体可能已销毁）；需要中途取消的过渡动画用 `Timer.AddTransition`（有句柄）。

Buff管理器（EntityEffectController）设计要点（对应策划案 11.3；**2026-10-17 重构后口径**）：
1. **统一添加入口 `AddEffect(EffectType type, int level = 1, float duration = -1f, EntityData source = null)`**：外部只给类型 / 等级 / 持续时间（<0 = 永久）+ 施加者实体，**不传任何数值**——强度全部由控制器内部定死（系数进 Config 通用 Buff region）。`source` 非空时克隆其 `floatingAttribute` 存为**施加者属性快照**（独立实例，施加者后续被减益不影响已挂 Buff），`sourceId` 由此自动取（DoT 击杀归属）；无施加者的强加（守护点减伤/愈战愈勇/小地图失联等）传 null。叠加语义：同类型重复添加 = **等级累加 + 时间刷新**，施加者与快照以最新一次为准。
2. 双轨属性，事件驱动：只有属性修改类 Buff（`IsAttribute` 白名单，**6 对 Boost/Decrease**：力量 / 魔法 / 暴击率 / 暴击伤害 / 击飞抗性 / 可见距离）允许改属性。幅度内部定死 = **被施加者对应属性初始值 × 20% × 等级**（`Config.buff_attr_ratio`，方向由 Boost/Decrease 类型表达）；不再有固定数值特例（白眼伯爵「无限视野」= 施加 10 级 `AttrViewDistanceBoost`）。`EntityAttributeDelta.Field` 里的武器槽位分支已删除——**武器槽位数不可被 Buff 改变**，生命值上限也只能由升级路线改变。
   **`EntityAttribute.health` 一个字段承载两种语义**：在 `baseAttribute` 里是**生命值上限**（配置项，所以配置 SO 里只出现一个"生命值"项，由升级路线抬高），在 `floatingAttribute` 里是**当前生命值**（受击扣减、生成/复活克隆即满血、死亡清零）。→ **读上限读 `baseAttribute.health`，读当前生命读 `floatingAttribute.health`。**
   Add/Remove 时统一重算运行时属性（base 克隆 + Σ修改量，再把当前生命**夹回上限**——夹取不平移 = 不自动回血）；其它 Buff 一律不得触碰属性
3. 查询式战斗管线：功能 Buff 不做每帧计算，由战斗代码在明确时机调用查询接口——受伤管线顺序 = 护盾吸收 → 减伤 → 受伤乘区（愈战愈勇；固定数值伤害跳过）→ 扣血 → 反伤；出伤乘区由攻击方在伤害计算时调用
4. OnUpdate 只做两件事：到期移除（Buff 记录结束时间戳 endTime，不每帧刷新剩余时间）、固定间隔 tick（`IsTicking`：燃烧/苍白之火/中毒/死灵漫步，统一 1s 一跳，吃护盾/减伤、不吃增减伤）。**每跳伤害也内部定死**：燃烧与苍白之火 = `Config.burning_damage_per_tick`(30) × 等级；中毒 = `Config.poison_damage_ratio`(0.5) × max(施加者快照力/魔) × 等级；死灵漫步 = `Config.death_stroll_dot_ratio`(1.5) × max(快照力/魔) × 等级，光环半径 `Config.death_stroll_radius`。**叠层自动提高每跳伤害**
5. 护盾（`Shield`/`TowerShield`）：初始值 = 被施加者生命上限 × `Config.shield_health_ratio`(0.1) × 等级（重挂按累计层数重算），受击逐次扣减、扣完自动移除；反伤（`Reflect`）按比例制：**反弹 = 实际受伤（扣盾/减伤后）× 等级 × `Config.reflect_rate`(0.1)，向下取整 min=1**
6. 极性静态标注：`EffectType.IsNegative()` 扩展方法固定每种 Buff 的正/负（负面白名单 + 全部属性削弱 Decrease 类），`AddEffect` 不接收极性参数；净化（`RemoveAllNegative`）按此筛选
7. 强控（麻痹/冰冻/定身，苍白之冰/雷同属此类）施加：打断位移（MotionBase.Exit）、动画播放速度直接置 0（当前攻击随之被打断）；全部移除后恢复；期间霸体等级立即变为最低（GetEndureLevel 顺序：强控→None > 位移→Common > 强制霸体→Super > 动画状态）
8. 破霸体：攻击数据（AttackData.breakEndure）命中霸体等级 1 的目标时进入受击，且若目标在位移中则移除 MotionBase；命中判定入口 = EntityData.ProcessHit（**受击动作、打断位移、被击飞三者都在 enterHit 这一分支内**，见速度节⑥）
9. 沉默/强控期间无法释放技能（EntitySkillController.TryUseSkill 校验 CanCastSkill）
10. Buff 数据使用 struct；标记类 Buff（瘟疫标记/苍白之光/暗）无自身效果，供其它逻辑读层数（苍白转化由被动订阅 Buff 变化实现；**转化出的苍白之冰/之雷/之火归属原施加者**——`AddEffect` 的苍白钩子把标记的施加者传给 `CheckPaleConversion`，多人叠同一目标时归属最后叠层者）。苍白之冰/之雷是独立强控类型（行为同冰冻/麻痹），苍白之火是独立 DoT（每跳同燃烧），三者只是类型不同、可与通用冰冻/麻痹/燃烧并存各算各的层数与时间
11. 全局参数（僵尸刷新等级、进攻方复活减速等）不是 Buff，实现于游戏逻辑控制器/Config（策划案第二十章）；其中僵尸刷新等级按战斗剩余时间实时计算（`BattleManagerPassive.GetZombieSpawnLevel`：每经过 7.5% 战斗时间 +1，死亡漫步者被动再 +4，普通/精英共用）。等级上限拆两档：`save_max_level=10` 只管玩家存档升级，`battle_max_level=20` 管战斗期生成（`EntityData.OnCreate` 钳制，最高刷出 20 级）
12. 表现：持续型 Buff 特效**只由客户端播**——`EntityPlayerManager.ApplyBuffVfx` 按 `SCEntityDisplayInfo.buffs` 查 `Config.buff_vfx` 增删跟随特效（映射表按《特效清单分别表1019》定稿），**技能 `PlayVFX` 内禁止播 buff 特效**。
13. 瘟疫祝福（`PlagueBless`）：唯一的"出伤 / 受伤乘区"Buff，与愈战愈勇同通道（`GetOutDamageMultiplier` +75% / `GetInDamageMultiplier` −25%），攻击方在伤害计算时读取。由 `PlagueTreeEntityData.OnKilled` 挂给"把树血量清零那一次攻击的攻击者"；被 Buff/DoT（固定数值伤害）打死的没有归属。移速类单独一档，见"速度"节⑨
