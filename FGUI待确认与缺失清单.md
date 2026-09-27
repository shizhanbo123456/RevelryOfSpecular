# FGUI 界面清单：已确认 / 待确认 / 待补充（2026-09-27 第三版）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（27 个生成类）。
> 逻辑接线以本文为准。**UIManager 仍是旧版（引用已删类型编译不过），接逻辑时重写为 FGUI 版。**

## 一、页面根（✅ 三个都齐了）

| 页面根 | 组成 |
|---|---|
| **UI_HomePanel** | m_characterPanel（HomeCharacterList）+ m_attributePanel（HomeAttrList）+ m_connectPanel（HomeConnect） |
| **UI_LobbyPanel** | m_mainView（LobbyMemberList） |
| **UI_BattlePanel** | m_skillList + m_PlayerBar(PlayerHealth) + m_EventList + m_Minimap + m_icon_day_night + m_label_time_left + **复活进度（m_showRegenerationBar 控制器 + m_regeneration_progressbar）** + **守护点血量面板 ×4（m_progressMain + m_progressSub1~3，UI_DefensivePointBar）** |

## 二、本轮新增 / 已确认的组件

| 组件 | 字段与用法 |
|---|---|
| **UI_DefensivePointBar**（守护点血条） | m_fill(GImage 填充比例)；中心守护点用 m_progressMain，外围 3 个用 m_progressSub1~3 |
| **UI_EntityBar**（世界空间血条） | m_fill(填充比例) + m_label(文本)——场景中所有血条都用它 |
| **UI_LobbyMemberList 补齐 AI 编辑** | m_attackerAI_minus/m_label_attackerAI_count/m_attackerAI_add + 防守方同组 + m_btn_joinAttacker/m_btn_joinDefenser（选队）+ m_btn_battleStart |
| UI_MinimapItem | **m_type 档位：0 自己，1 队友玩家，2 敌人玩家，3 瘟疫树，4 水晶，5 防御塔，6 僵尸，7 精英僵尸，8 主守护点，9 次守护点** |
| UI_EventItem | type2 = 文字+图标+文字（"玩家A (枪) 玩家B"= A 击杀 B）；**type0/type1 按需使用** |
| UI_BattlePanel.m_PlayerBar | **左上角固定显示**的本地玩家大血条（等级+血量数字+血条），不是头顶跟随 |
| UI_RoleHead | **角色头像统一用它**：角色列表条目、大厅成员条目都用 RoleHead（玩家成员 = 文本填玩家名、头像填所选角色）；m_level index = level-1；m_lock = 未解锁 |
| UI_BattlePanel 坐标系 | 三个 Panel 的内容都是**屏幕空间**；场景中的血条用 UI_EntityBar |
| 技能槽 | **不需要选中态**；技能经验与 CD 文本都不要，直接图形化（CD = 图标填充比例 0→100 一轮冷却） |
| UI_BattleResult | m_title/m_content + m_t0 转场 |
| UI_InputField | m_content(GTextInput)——HomeConnect 的 IP 输入 |
| UI_Panel_1 | m_hideTitle=1 隐藏顶部标题栏 |
| UI_EventIcon | 档位：0 无源死亡，1 瘟疫树被击败，2 玩家复活，3 瘟疫树刷新，4 天黑，5 天亮，6 玩家间击败，7 其它 |

## 三、仍缺失 / 待确认（只剩这些）

| # | 项 | 说明 | 优先级 |
|---|---|---|---|
| 1 | 结算：关闭按钮 | UI_BattleResult 没有按钮字段，结算后回组队大厅需要出口 | 高 |
| 2 | 大厅：断开并返回按钮 | UI_LobbyPanel/LobbyMemberList 上没有 | 高 |
| 3 | 首页：玩家等级文本 | "玩家等级 Lv N"（SaveManager.playerLevel） | 中 |
| 4 | 首页：选中角色标题 + 下一级提升提示 | "名字 Lv N" + "下一级：生命值上限 +25%"（属性列表标题行） | 中 |
| 5 | 首页：连接状态反馈 | "正在连接.../连接失败" 文本 | 中 |
| 6 | 守护点：减伤叠层/已摧毁的视觉表达 | UI_DefensivePointBar 只有 fill，减伤叠层怎么显示（文字/换色）？已摧毁怎么表达？ | 中 |
| 7 | 技能槽：技能经验的图形表达 | 无对应元件——是否暂不做经验显示？ | 低 |
| 8 | 确认面板 / 加载遮罩 | 断开二次确认、连接期间遮罩 | 低 |

## 四、逻辑接线规划（接逻辑时的映射，供核对）

| 页面 | 数据/事件 | 写入目标 |
|---|---|---|
| Home | 攻/守角色列表 + SaveManager 等级/解锁 + AssetsManager 图标 | m_characterList ← UI_RoleHead（m_lock 禁点、m_level=index、m_headIcon、m_roleName） |
| Home | 页签点击 | m_btn_attacker/m_btn_defenser 的 m_selected 互斥 + m_characterList 刷新 |
| Home | ClientSelection + GetAttribute(level/1/level+1) | m_attrList ← UI_AttrItem（m_attrValue richtext：当前值+绿累计增益+橙下级） |
| Home | TryConnect + OnConnect 事件 | m_btn_connect → 连接 → 大厅（UIManager 统一切页） |
| Lobby | SCRoomInfo | 两成员列表（RoleHead：玩家名+所选角色头像）+ AI 数量文本 + 开始按钮可用性 |
| Lobby | join 按钮 / AI -/+ | CSRoomUpdate 上报（camp / attackAICount / defenseAICount） |
| Battle | OnEntityDisplayUpdate | 技能栏整表重渲染（CD=图标填充比例）；m_PlayerBar（等级/血量数字/血条）；守护点 4 条血量（m_progressMain/Sub1~3） |
| Battle | SCBattleEvent.Damage（0=无效/>0=普通/<0=暴击，targetId=受击实体） | DamageLabel 在受击实体头顶（世界→屏幕→GRoot 坐标换算） |
| Battle | OnMinimapUpdate | MinimapItem 按 type 档位显示/移除，坐标映射到 m_mapBase |
| Battle | OnScoreUpdate（仅终局一次） | BattleResult.Show + m_t0.Play |
| Battle | Tick 逐帧 | 时间推演 + 昼夜图标旋转（0°=正午/180°=午夜）+ EntityBar/名牌屏幕跟随（TryGetEntityHeadPos） |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页（HomePanel/LobbyPanel/BattlePanel 显隐） |
| 全局 | SCBattleEvent（Kill/BeaconDestroyed/CrystalCollected/CrystalBroken/PlagueTreeCaptured/ShowText） | EventList ← UI_EventItem（type 控制器选形态、EventIcon 选档位） |

## 五、现状备忘

- FGUI 运行时 `Assets/FairyGUI-unity-master`；加载 `UIPackage.AddPackage("GUI/Main")` + `MainBinder.BindAll()`。
- 旧 uGUI 系统已删；`UIManager.cs` 待重写为 FGUI 版（保留 TurnPage/切页事件/ShowFloating 调用形态）。
- 协议已就绪：SCBattleEvent.Damage（value 0=无效/>0=普通/<0=暴击，targetId）、RoomMemberInfo.characterIndex、OnDamaged isCrit、EntityPlayerManager.TryGetEntityHeadPos。
- FGUI 生成类都是 partial——页面逻辑写成 partial 类（沿用旧 View/Logic 习惯）。
