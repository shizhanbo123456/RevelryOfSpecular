# FGUI 界面清单：已确认 / 待确认（2026-09-27 第四版）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（28 个生成类）。
> 逻辑接线以本文为准。**UIManager 仍是旧版（引用已删类型编译不过），接逻辑时重写为 FGUI 版。**

## 一、页面根（✅ 齐了）

| 页面根 | 组成 |
|---|---|
| **UI_HomePanel** | m_page 控制器（**0=玩家信息页，1=角色列表页**）+ m_playerInfo(UI_PlayerInfo) + m_characterPanel(HomeCharacterList) + m_attributePanel(HomeAttrList) + m_connectPanel(HomeConnect) |
| **UI_LobbyPanel** | m_mainView(LobbyMemberList) + m_btn_exit（断开并返回） |
| **UI_BattlePanel** | m_skillList + m_PlayerBar + m_EventList + m_Minimap + m_btn_exit + 复活进度（m_showRegenerationBar + m_regeneration_progressbar）+ 守护点血量面板 ×4（m_progressMain / m_progressSub1~3） |

## 二、全部已确认的组件规则

| 组件 | 规则 |
|---|---|
| **UI_PlayerInfo**（玩家信息页） | m_input_playerName（GTextInput，玩家名可编辑）、m_label_level（玩家等级）、m_selectedAttacker/m_selectedDefenser（UI_RoleHead，展示两阵营当前选中角色）、m_btn_editSelectedCharacter（跳转角色列表页编辑） |
| UI_HomeCharacterList | m_characterList（←UI_RoleHead）+ m_btn_attacker/m_btn_defenser（页签切阵营）+ **m_btn_finish（❓完成按钮，语义见待确认 #1）** |
| UI_RoleHead | m_headIcon(GLoader)、m_roleName、m_level 控制器（index = level-1）、m_lock（未解锁）；**头像统一用 RoleHead：角色条目/大厅成员（文本=玩家名、头像=所选角色）** |
| UI_HomeAttrList + UI_AttrItem | m_attrValue 用 **UBB 语法**：基础属性白色 + 已取得的全部升级加成绿色 `[color=#6BD98C]+150[/color]` + 仅下一级将加成的属性橙色 `[color=#FF9E47]（+50）[/color]`；升级路线外或 0 增益不显示对应段 |
| UI_HomeConnect + UI_InputField | m_input_ipaddress.m_content(GTextInput) 读 IP + m_btn_connect 连接；**不需要连接状态文本**（未连接就在 Home） |
| UI_LobbyMemberList | 攻/守成员列表（条目 = RoleHead：玩家名+所选角色头像）+ m_btn_joinAttacker/m_btn_joinDefenser（选队）+ AI 编辑（m_attackerAI_minus/m_label_attackerAI_count/m_attackerAI_add + 防守方同组）+ m_btn_battleStart |
| UI_BattlePanel | m_icon_day_night 绕 Z 旋转（0°=正午，180°=午夜，Time01 线性插值）+ m_label_time_left（本地推演）+ m_PlayerBar（左上角固定，本地玩家等级+血量数字+血条）+ m_skillList + m_EventList + m_Minimap |
| UI_BattlePanel 复活进度 | m_showRegenerationBar 控制器显隐 + m_regeneration_progressbar 填充比例；文案内容并入（"复活中/可复活"） |
| **UI_DefensivePointBar**（×4：m_progressMain + m_progressSub1~3） | m_fill 填充比例 = 血量比；**m_destroyed 控制器 = 被摧毁标识**；不在 UI 显示减伤 |
| UI_EntityBar（世界空间血条） | m_fill + m_label；场景中所有血条用它，屏幕跟随定位 |
| UI_SkillListItem | m_loader_iconBase（底图，随技能一并设置）+ m_loader_icon + m_store + m_key + **m_starList（技能经验图形化=星级）**；CD = 图标填充比例（0→100 一轮冷却）；**无选中态、无经验/CD 文本** |
| UI_DamageLabel | value 0=无效（灰）/ >0=普通（白）/ <0=暴击（橙大号），受击实体头顶 |
| UI_EventItem + UI_EventIcon | type2 = 文字+图标+文字（A 击杀 B）；**type0/type1 按需使用**；EventIcon 档位：0 无源死亡 / 1 瘟疫树被击败 / 2 玩家复活 / 3 瘟疫树刷新 / 4 天黑 / 5 天亮 / 6 玩家间击败 / 7 其它 |
| UI_Minimap + UI_MinimapItem | m_mapBase 上放事件/单位；**MinimapItem 档位：0 自己 / 1 队友玩家 / 2 敌人玩家 / 3 瘟疫树 / 4 水晶 / 5 防御塔 / 6 僵尸 / 7 精英僵尸 / 8 主守护点 / 9 次守护点** |
| UI_BattleResult | m_title/m_content + m_t0 转场；**无关闭按钮——显示数秒后自动关闭（时长定 5s，代码常量可调）** |
| UI_Button1 / UI_NoticePanel | m_selected（选中态）；m_title（ShowNotice） |

## 三、待确认（❓ 只剩 3 个）

| # | 问题 |
|---|---|
| 1 | **UI_HomeCharacterList.m_btn_finish 的语义**：是"角色列表页编辑完成→切回玩家信息页（m_page 切 0）"的按钮吗？ |
| 2 | **玩家名同步**：UI_PlayerInfo 的玩家名可编辑——要不要上报服务器（CSPlayerInfo/RoomMemberInfo 加 name 字段）让大厅成员列表和头顶名字显示自定义名？不上报的话，其它玩家处只能显示"玩家{clientId}"，自己输入的名字只有本地可见 |
| 3 | **starList 的经验→星级映射规则**：技能 exp 如何换算星星数量（按 Config.level_up_exp？按固定阈值？） |

## 四、已不再缺失的旧清单项

结算自动关闭（✅ 定时关闭，替代按钮）、组队/战斗返回按钮（✅ m_btn_exit ×2）、玩家等级/选中角色展示（✅ UI_PlayerInfo）、守护点颜色/摧毁（✅ m_destroyed）、下一级提升提示（✅ 并入属性值橙段）、连接状态文本（✅ 不需要）、技能经验（✅ starList）、AI 编辑/选队按钮（✅）。

## 五、可选未做（默认不做，需要再说）

- 确认面板：**断开并返回前的"确定要断开吗"二次确认弹窗**（旧 ConfirmUnit）——如果不需要确认就直接断开，忽略此项
- 加载遮罩：**连接服务器期间的全屏提示**（旧 LoadingUnit）——连接耗时短，可直接用按钮禁用代替

## 六、逻辑接线规划（同第三版，微调）

| 页面 | 数据/事件 | 写入目标 |
|---|---|---|
| Home | m_page 控制器 | 0=玩家信息（playerName/level/两 RoleHead）/ 1=角色列表（页签+条目） |
| Home | 攻/守角色列表 + SaveManager + AssetsManager 图标 | m_characterList ← UI_RoleHead |
| Home | ClientSelection + GetAttribute(level/1/level+1) | m_attrList ← UBB 三段式 |
| Home | m_btn_finish（语义确认后） | 编辑完成 → 切回玩家信息页 |
| Lobby | SCRoomInfo | 两成员列表（RoleHead）+ AI 数量 + 开始按钮可用性 |
| Lobby | join / AI ± / exit | CSRoomUpdate 上报 / ExitWorld → Home |
| Battle | OnEntityDisplayUpdate | 技能栏（CD=icon 填充）、m_PlayerBar、守护点 4 条（m_destroyed 控制器） |
| Battle | SCBattleEvent.Damage | DamageLabel（受击实体头顶，GRoot 坐标换算） |
| Battle | OnMinimapUpdate | MinimapItem 按 10 档位显示 |
| Battle | OnScoreUpdate（终局一次） | BattleResult.Show + m_t0.Play + **定时自动关闭回大厅** |
| Battle | Tick | 时间推演 + 昼夜图标旋转 + EntityBar/名牌跟随 |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页 |

## 七、现状备忘

- FGUI 运行时 `Assets/FairyGUI-unity-master`；`UIPackage.AddPackage("GUI/Main")` + `MainBinder.BindAll()`。
- 协议已就绪：SCBattleEvent.Damage、RoomMemberInfo.characterIndex、OnDamaged isCrit、TryGetEntityHeadPos。
- FGUI 生成类是 partial——页面逻辑写成 partial 类；UBB 颜色：绿 `#6BD98C`、橙 `#FF9E47`。
