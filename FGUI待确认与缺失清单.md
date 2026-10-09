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
| UI_EntityBar（世界空间血条） | m_fill + m_label（血量数字）；名牌（PlayerName 名字 + EntityBar 血条）屏幕跟随实体头顶，锚点 = TryGetEntityHeadPos（EntityModelInfo 顶点）；**轴心为左上角（未勾作为锚点）→ 代码设位置时减半个宽度做水平居中**（血条 160 宽、名牌 20 宽） |
| **UI_SkillList**（BattlePanel.m_skillList） | m_content（**GList，横向单行**，defaultItem = SkillListItem，溢出可见无滚动）：条目数 = 本地角色**技能槽位数**（`EntityAttribute.weaponSlotCount`，默认 3、可被升级抬高）；**代码额外设列表宽度 = 所有条目宽度之和**（白名单内唯一允许的尺寸设置），位置/高度仍由界面决定 |
| UI_SkillListItem | m_loader_iconBase（底图）+ m_loader_icon（图标，**CD = 填充比例 0→100 一轮冷却**）+ m_store + m_key（键位按 `Config.skill_slot_keys` = U I O L H Y）+ **m_starList（技能经验，exp 与星星 1:1）** + **m_empty 控制器（0 有技能 / 1 空槽）** + **m_randomOutline 控制器（0~4，进入战斗时每个槽位随机一次）**；无选中态、无经验/CD 文本 |
| UI_DamageLabel | value 0=无效（灰）/ >0=普通（白）/ <0=暴击（橙大号），受击实体头顶（**轴心左上角 → 代码减半个宽度让数字居中于头顶**，上浮+渐隐后销毁） |
| **UI_EventList**（BattlePanel.m_EventList） | m_EventItemContainer（**GList，纵向单列**，defaultItem = EventItem）：代码只设 `itemRenderer` + `numItems`，条目组件与排布全由界面决定；数据是 `BattlePage.eventEntries`（3.5s 到期从表头移除） |
| UI_EventItem + UI_EventIcon | type2 = 文字+图标+文字（"玩家A (图标) 玩家B"= A 击杀 B，图标档位 6=玩家间击败）；type0/type1 按需；EventIcon 档位：0 无源死亡 / 1 瘟疫树被击败 / 2 玩家复活 / 3 瘟疫树刷新 / 4 天黑 / 5 天亮 / 6 玩家间击败 / 7 其它 |
| UI_Minimap + UI_MinimapItem | 档位：0 自己 / 1 队友玩家 / 2 敌人玩家 / 3 瘟疫树 / 4 水晶 / 5 防御塔 / 6 僵尸 / 7 精英僵尸 / 8 主守护点 / 9 次守护点；**雷达式**：本地玩家图标固定在 mapBase 正中心并随朝向旋转，上方=世界Z+、右侧=世界X+，`minimap_view_radius`(100m) 铺满 mapBase，其它点按相对本地玩家的偏移绘制 |
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
| Battle | SCBattleEvent.Damage | DamageLabel（受击实体头顶，世界→屏幕→GRoot 坐标） |
| Battle | OnMinimapUpdate | MinimapItem 按 10 档位显示；超时未更新（离屏/夜间停传）或超显示半径时由客户端移除，不每帧清空 |
| Battle | OnScoreUpdate（仅终局一次） | BattleResult.Show + m_t0.Play + 5s 自动关闭 |
| Battle | Tick 逐帧 | 时间推演 + 昼夜图标旋转 + EntityBar/名牌跟随 |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页 |
| 全局 | SCBattleEvent.ShowText / OnRightClickBlocked / OnScoreUpdate 终局 | 全局飘字（UIManager.ShowFloating） |

## 四、可选未做（默认不做，需要再说）

- **确认面板**：断开并返回前的"确定要断开吗"二次确认弹窗（当前直接断开）
- **加载遮罩**：连接服务器期间的全屏提示（当前连接期间按钮无反馈界面，仅状态文本）
- **守护点减伤叠层**：按设计不在 UI 显示

## 五、协议依赖（已实现）

- `SCBattleEvent.Type.Damage`（value：0=无效，>0=普通，<0=暴击取绝对值；targetId=受击实体）+ `NetworkManager.SendBattleEvent(SCBattleEvent)` 广播重载
- `SCBattleEvent` Kill 事件：value = 击杀者客户端 id（-1 无归属），targetId = 受害实体 id
- `SCRoomInfo.RoomMemberInfo.characterIndex / name`；`CSPlayerInfo.name`（玩家名上报）
- `EntityPlayerManager.TryGetEntityHeadPos`（名牌/伤害飘字锚点，EntityModelInfo 顶点懒缓存）
- `EntityData.OnDamaged(..., bool isCrit)` 在终伤处广播伤害事件（ProcessHit 传 isCrit）
