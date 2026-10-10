# FGUI 界面接线说明（已全部接线，2026-09-27）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（生成类，命名空间 **Ros.UI.Main**，引用需 `using Ros.UI.Main;`）。
> 本文档 = 字段含义/档位表/接线映射的参考。**逻辑已全部接线（UIManager + PageBase + 三页面 Logic），剩余可选项见第四节。**

## 一、页面根与结构

| 页面根 | 组成 |
|---|---|
| **UI_HomePanel** | m_page 控制器（**0=玩家信息页，1=角色列表页**）+ m_playerInfo(UI_PlayerInfo) + m_characterPanel(HomeCharacterList) + m_attributePanel(HomeAttrList) + m_connectPanel(HomeConnect) |
| **UI_LobbyPanel** | m_mainView(LobbyMemberList) + m_btn_exit（断开并返回） |
| **UI_BattlePanel** | m_skillList + m_PlayerBar(PlayerHealth) + m_EventList(UI_EventList) + m_Minimap + m_icon_day_night + m_label_time_left + 复活进度（m_showRegenerationBar + m_regeneration_progressbar）+ 守护点血量面板 ×4（m_progressMain + m_progressSub1~3） |

## 二、组件规则（全部按你的讲解落实）

| 组件 | 规则 |
|---|---|
| **UI_PlayerInfo**（玩家信息页） | m_input_playerName（GTextInput，玩家名可编辑，**已上报**：随 CSPlayerInfo 发给服务器并进 RoomMemberInfo.name）、m_label_level（玩家等级）、m_selectedAttacker/m_selectedDefenser（UI_RoleHead 展示两阵营当前选中角色）、m_btn_editSelectedCharacter（跳角色列表页） |
| UI_HomeCharacterList | m_characterList（←UI_RoleHead）+ m_btn_attacker/m_btn_defenser（页签切阵营）+ m_btn_finish（编辑完成切回玩家信息页，**已接线**） |
| UI_RoleHead | m_headIcon(GLoader)、m_roleName、m_level 控制器（index = level-1）、m_lock（未解锁）；**角色头像统一用它：角色条目/大厅成员（文本=玩家名、头像=所选角色）** |
| UI_HomeAttrList + UI_AttrItem | m_attrValue 用 **UBB**：基础属性白 + 已取得的全部升级加成绿 `[color=#6BD98C]+150[/color]` + 仅下一级将加成的属性橙 `[color=#FF9E47]（+50）[/color]` |
| UI_HomeConnect + UI_InputField | m_input_ipaddress.m_content(GTextInput) 读 IP + m_btn_connect；无连接状态文本（未连接就在 Home） |
| UI_LobbyMemberList | 攻/守成员列表（条目 = RoleHead：玩家名+所选角色头像）+ m_btn_joinAttacker/m_btn_joinDefenser（选队）+ AI 编辑（m_attackerAI_minus/m_label_attackerAI_count/m_attackerAI_add + 防守方同组）+ m_btn_battleStart |
| UI_BattlePanel | m_icon_day_night 绕 Z 旋转（0°=正午，180°=午夜，Time01 线性插值）+ m_label_time_left（本地推演）+ m_PlayerBar（左上角固定，本地玩家等级+血量数字+血条）+ m_skillList + m_EventList（UI_EventList）+ m_Minimap |
| UI_BattlePanel 复活进度 | m_showRegenerationBar 控制器显隐 + m_regeneration_progressbar 填充比例 |
| **UI_DefensivePointBar**（×4：m_progressMain + m_progressSub1~3） | m_fill 填充比例 = 血量比；**m_destroyed 控制器 = 被摧毁标识**；减伤不在 UI 显示 |
| UI_EntityBar（世界空间血条） | m_fill + m_label（血量数字）；名牌（PlayerName 名字 + EntityBar 血条）屏幕跟随实体头顶，锚点 = TryGetAnchorPos(Bar)（人形 Head 骨骼+0.5m / 非人形包围盒顶+0.5m）；**轴心为左上角（未勾作为锚点）→ 代码设位置时减半个宽度做水平居中**（血条 160 宽、名牌 20 宽） |
| **UI_SkillList**（BattlePanel.m_skillList） | m_content（**GList，横向单行**，defaultItem = SkillListItem，溢出可见无滚动）：条目数 = 本地角色**技能槽位数**（`EntityAttribute.weaponSlotCount`，默认 3、可被升级抬高）；**代码额外设列表宽度 = 所有条目宽度之和**（白名单内唯一允许的尺寸设置），位置/高度仍由界面决定 |
| UI_SkillListItem | m_loader_iconBase（底图）+ m_loader_icon（图标，**CD = 填充比例 0→100 一轮冷却**）+ m_store + m_key（键位按 `Config.skill_slot_keys` = U I O L H Y）+ **m_starList（技能经验，exp 与星星 1:1）** + **m_empty 控制器（0 有技能 / 1 空槽）** + **m_randomOutline 控制器（0~4，进入战斗时每个槽位随机一次）**；无选中态、无经验/CD 文本 |
| UI_DamageLabel | value 0=无效（灰）/ >0=普通（白）/ <0=暴击（橙大号），受击实体头顶（**轴心左上角 → 代码减半个宽度让数字居中于头顶**，上浮+渐隐后销毁；随机散布在**屏幕空间**水平±50px / 垂直±10px，创建时定死不再每帧重掷；无命中点时锚点 = 受击实体的 `EntityAnchor.LabelFallBack`） |
| **UI_EventList**（BattlePanel.m_EventList） | m_EventItemContainer（**GList，纵向单列**，defaultItem = EventItem）：代码只设 `itemRenderer` + `numItems`，条目组件与排布全由界面决定；数据是 `BattlePage.eventEntries`（3.5s 到期从表头移除） |
| UI_EventItem + UI_EventIcon | type2 = 文字+图标+文字；type1 = 图标+文字；type0 = 纯文字（无图标）。**EventIcon 档位 0~7 已全部接入**：0 无源死亡（玩家死亡无归属）/ 1 瘟疫树被击败 / 2 玩家复活 / 3 瘟疫树刷新 / 4 天黑 / 5 天亮 / 6 玩家间击败 / 7 其它（守护点被摧毁）。条目文本统一数字 id 传输（`SCBattleEvent.textId`：<10000 = NoticeMessageMap，≥10000 = 玩家名，clientId = id − 10000） |
| UI_Minimap + UI_MinimapItem | 档位：0 自己 / 1 队友玩家 / 2 敌人玩家 / 3 瘟疫树 / 4 水晶 / 5 防御塔 / 6 僵尸 / 7 精英僵尸 / 8 主守护点 / 9 次守护点；**雷达式**：本地玩家图标固定在 mapBase 正中心并随朝向旋转，上方=世界Z+、右侧=世界X+，**当前雷达显示半径**铺满 mapBase（F 键循环 100/200/300，服务器回应 `SCMinimapRadius`），其它点按相对本地玩家的偏移绘制；点位图标创建时代码设 `scale` = 父链上层元素缩放的倒数（minimapItemScale，界面初始化时算，不含页面根 UiScale 适配），抵消上层缩放使图标保持 FGUI 设计尺寸 |
| UI_BattleResult | m_title/m_content + m_t0 转场；**显示 5 秒后自动关闭回组队大厅** |
| UI_Button1 / UI_Panel_1 / UI_NoticePanel | m_selected（选中态）；m_hideTitle=1 隐藏标题栏；m_title（ShowNotice） |
| UI_Button1 回调 | `onClick.Set(...)` 为覆盖语义（列表复用用它），`onClick.Add(...)` 为追加 |

## 三、接线映射

| 页面 | 数据/事件 | 写入目标 |
|---|---|---|
| Home | 攻/守角色列表 + SaveManager 等级/解锁 + AssetsManager 图标 | m_characterList ← UI_RoleHead |
| Home | 页签点击 | m_selected 互斥 + 列表刷新 |
| Home | ClientSelection + GetAttribute(level/1/level+1) | m_attrList（UBB 三段式） |
| Home | TryConnect + OnConnect 事件 | m_btn_connect → 连接 → 大厅 |
| Lobby | SCRoomInfo | 两成员列表 + AI 数量 + 开始按钮可用性 |
| Lobby | join 按钮 / AI ± | CSRoomUpdate 上报 |
| Battle | OnEntityDisplayUpdate | 技能栏整表重渲染、m_PlayerBar、守护点 4 条 |
| Battle | SCDamage（OnDamageDisplay） | DamageLabel（受击实体头顶，世界→屏幕→GRoot 坐标） |
| Battle | OnMinimapUpdate | MinimapItem 按 10 档位显示；超时未更新（离屏/夜间停传）或超显示半径时由客户端移除，不每帧清空 |
| Battle | OnScoreUpdate（仅终局一次） | BattleResult.Show + m_t0.Play + 5s 自动关闭 |
| Battle | Tick 逐帧 | 时间推演 + 昼夜图标旋转 + EntityBar/名牌跟随 |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页 |
| 全局 | SCPrompt（OnShowPrompt）/ OnScoreUpdate 终局 | 全局提示（当前仅开局校验 17/18，战斗页外发、暂无显示方）/ 全局飘字（UIManager.ShowFloating） |

## 四、可选未做（默认不做，需要再说）

- **确认面板**：断开并返回前的"确定要断开吗"二次确认弹窗（当前直接断开）
- **加载遮罩**：连接服务器期间的全屏提示（当前连接期间按钮无反馈界面，仅状态文本）
- **守护点减伤叠层**：按设计不在 UI 显示

## 五、协议依赖（已实现）

- `SCBattleEvent`（战斗事件列表唯一来源）：type（事件语义唯一判别，icon 档位与条目版式由客户端按类型映射）+ textId/textId2（数字文本：<10000 = NoticeMessageMap，≥10000 = 玩家名，clientId = id − 10000）。事件源：玩家击杀（PlayerKill，type2 双名）/玩家死亡无归属（DeathUnattributed）、复活（PlayerRespawn）、攻占瘟疫树（PlagueTreeCaptured）、瘟疫树刷新（PlagueTreeRespawn）、天黑/天亮（Nightfall/Daybreak）、守护点摧毁（BeaconDestroyed，textId 区分哪一座：19 中心 / 24~26 外围，驱动对应血条 destroyed）、防御塔摧毁（TowerDestroyed）、武器提示 13/14/15（Notice，纯文字，定向）
- `SCDamage`（value：0=无效，>0=普通，<0=暴击取绝对值；targetId=受击实体；hasHitPos+hitPos 命中点可选）+ `NetworkManager.SendDamage` 定向（按可见性）/广播重载，不可靠通道
- `SCPrompt`（messageId = NoticeMessageMap 消息 id）+ `NetworkManager.SendPrompt` 定向：仅开局校验 17/18（战斗事件列表条目一律走 SCBattleEvent，不走此通道）
- `SCRoomInfo.RoomMemberInfo.characterIndex / name`；`CSPlayerInfo.name`（玩家名上报）
- `EntityPlayerManager.TryGetAnchorPos(id, EntityAnchor, out pos)`（任意锚点取世界位置，见《代码架构说明》「锚点」节；名牌/血条走 `Bar`、伤害飘字兜底走 `LabelFallBack`）
- `EntityData.OnDamaged(..., bool isCrit)` 在终伤处广播伤害事件（ProcessHit 传 isCrit）
