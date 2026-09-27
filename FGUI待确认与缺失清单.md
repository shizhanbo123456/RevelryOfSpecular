# FGUI 界面清单：已确认 / 待确认 / 待补充（2026-09-27 第二版）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（24 个生成类）。
> 逻辑接线以本文为准。**UIManager 仍是旧版（引用已删类型编译不过），接逻辑时重写为 FGUI 版。**

## 一、已确认的组件与字段（✅ 全部按你的讲解落实）

| 组件 | 字段与用法 |
|---|---|
| **UI_HomePanel**（首页根）✅ | m_characterPanel（角色列表）+ m_attributePanel（属性列表）+ m_connectPanel（连接区） |
| UI_HomeCharacterList | m_panel + m_characterList(GList←UI_RoleHead) + m_btn_attacker/m_btn_defenser = **页签，切换列表阵营** |
| UI_RoleHead | m_lock 控制器（未解锁态）、m_headIcon(GLoader)、m_roleName；**m_level 控制器 index=0 对应 level=1，直接设 index** |
| UI_HomeAttrList + UI_AttrItem | m_attrName/m_attrValue；attrValue 填 richtext（当前值+绿累计增益+橙下级增益） |
| **UI_InputField** ✅ | m_content(GTextInput)——HomeConnect 的 IP 输入框内部，读写 .text |
| UI_HomeConnect | m_input_ipaddress(UI_InputField) + m_btn_connect |
| **UI_LobbyMemberList** ✅ | m_defenserPlayers/m_attackerPlayers(GList) + **m_btn_joinAttacker/m_btn_joinDefenser（选队按钮）** + m_btn_battleStart |
| **UI_BattlePanel**（战斗页根）✅ | m_skillList(GComponent) + **m_PlayerBar(UI_PlayerHealth)** + m_EventList(GComponent) + m_Minimap(UI_Minimap) + m_icon_day_night(GImage，绕 Z 旋转 0°=正午/180°=午夜) + m_label_time_left |
| UI_SkillListItem | m_loader_iconBase/m_loader_icon/m_store/m_key；**CD = 调整技能图标填充比例（0→100 为一轮冷却）** |
| UI_PlayerHealth | m_bar_fill（**水平填充比例**）+ m_label_level + m_label_health = 显示玩家所操控角色的当前等级与血量 |
| UI_PlayerName | m_num = "玩家 N" |
| UI_DamageLabel | m_num；0=无效 / >0=普通 / <0=暴击，样式代码切 |
| UI_EventItem | m_type 控制器；**type2 = 文字+图标+文字单行**（如"玩家A (枪图标) 玩家B"表示 A 击杀 B） |
| UI_EventIcon | **m_type 档位：0 无源死亡，1 瘟疫树被击败，2 玩家复活，3 瘟疫树刷新，4 天黑，5 天亮，6 玩家间击败，7 其它事件** |
| UI_NoticePanel | m_title（ShowNotice 消息） |
| UI_BattleResult | m_title/m_content + m_t0 转场（显示时 Play） |
| UI_BattleResultDetail + Item | m_resultList ← Item.**m_content** + m_t0 ✅（明细内容文本） |
| UI_Minimap | m_mapBase(GGraph)：**各类小地图事件/单位显示在它上面** |
| UI_Button1 / UI_Panel_1 | m_selected（选中态）；**m_hideTitle=1 隐藏顶部标题栏** |

## 二、仍待确认（❓）

| # | 问题 |
|---|---|
| 1 | 大厅页根：Lobby 有没有独立整屏根（类似 HomePanel/BattlePanel），还是 LobbyMemberList 兼任大厅主体？「断开并返回」按钮放哪？ |
| 2 | 大厅成员条目：攻/守两个 GList 的条目组件是什么（需要玩家名+所选角色头像；生成代码里没有对应条目类）？ |
| 3 | 大厅 AI 数量编辑（-/+ 按钮与数量文本）是否要补？补在哪？ |
| 4 | UI_EventItem 的 type0（纯文字）与 type1（图标+文字）分别用于什么事件？ |
| 5 | UI_MinimapItem 的 m_type 档位含义（自己/攻/守/中立/水晶…？） |
| 6 | 头顶跟随名牌：本地玩家用 m_PlayerBar（大血条+等级+血量数字）；**其它玩家/敌方头顶用什么**——UI_PlayerName 单独跟随，还是所有人各一组 PlayerName+PlayerHealth？ |
| 7 | 技能槽：CD 用图标填充比例后，还需要 CD 秒数文本/选中高亮/武器经验条吗？ |

## 三、仍缺失的 UI 元素（补齐后接逻辑）

| # | 缺失项 | 优先级 |
|---|---|---|
| 1 | 守护点血量面板（中心守护点固定单槽 + 外围 3 项；血条/减伤叠层/已摧毁态） | 高 |
| 2 | 复活进度面板（进度条 + "复活中 X%（愈战愈勇 ×N）"/"可复活！"） | 高 |
| 3 | 大厅：AI 数量 -/+ 与数量文本 | 高 |
| 4 | 大厅：成员条目组件（玩家名+头像） | 高 |
| 5 | 结算：关闭按钮（回组队大厅） | 高 |
| 6 | 首页：玩家等级文本 + 选中角色标题（"名字 Lv N"）+ 下一级提升提示 + 连接状态反馈 | 中 |
| 7 | 确认面板（断开并返回二次确认）/ 加载遮罩 | 中 |
| 8 | 技能槽：选中高亮 / 武器经验条 /（可选）CD 秒数 | 中 |

## 四、逻辑接线规划（接逻辑时的映射，供核对）

| 页面 | 数据/事件 | 写入目标 |
|---|---|---|
| Home | 攻/守角色列表 + SaveManager 等级/解锁 + AssetsManager 图标 | m_characterList ← UI_RoleHead（m_lock/m_level index=level-1/m_headIcon/m_roleName） |
| Home | 页签点击 | m_btn_attacker/m_btn_defenser 的 m_selected 控制器互斥 + 列表刷新 |
| Home | ClientSelection + GetAttribute(level/1/level+1) | m_attrList ← UI_AttrItem（attrValue richtext 三段式） |
| Home | TryConnect + OnConnect | m_btn_connect → 连接 → 大厅（UIManager 统一切页） |
| Lobby | SCRoomInfo | 两成员列表 + 开始按钮可用性；joinAttacker/joinDefenser → CSRoomUpdate.camp |
| Battle | OnEntityDisplayUpdate | 技能栏整表重渲染（CD=图标填充比例）、m_PlayerBar、守护点面板（补齐后） |
| Battle | SCBattleEvent.Damage（0=无效/>0=普通/<0=暴击） | DamageLabel 在受击实体头顶（GRoot 坐标换算） |
| Battle | OnScoreUpdate（仅终局一次） | BattleResult.Show + m_t0.Play |
| Battle | Tick 逐帧 | 时间推演 + m_icon_day_night 旋转（0°=正午/180°=午夜）+ 小地图点位（OnMinimapUpdate） |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页（HomePanel/Lobby/BattlePanel 显隐） |

## 五、现状备忘

- FGUI 运行时 `Assets/FairyGUI-unity-master`；加载 `UIPackage.AddPackage("GUI/Main")` + `MainBinder.BindAll()`。
- 旧 uGUI 系统已删；`UIManager.cs` 待重写（编译当前不过）。服务器/协议层的 Damage 事件、RoomMemberInfo.characterIndex 均已就绪。
- FGUI 生成类都是 **partial**——页面逻辑建议写成同目录/Logic 目录的 partial 类（沿用旧 View/Logic 习惯）。
