# FGUI 界面接线说明（已全部接线，2026-09-27）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（生成类，命名空间 **Ros.UI.Main**，引用需 `using Ros.UI.Main;`）。
> 本文档 = 字段含义/档位表/接线映射的参考。**逻辑已全部接线（UIManager + PageBase + 三页面 Logic），剩余可选项见第四节。**
> **2026-10-11 新增**：GM 面板、确认弹窗、全屏遮罩（UI 已建、代码未接），见第五节。
> **2026-10-12 新增**：聊天系统（ChatPanel + ChatItem，UI 已建、代码未接），见第七节。

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

- **确认面板**：UI 已建（ConfirmPanel + FullScreenMask，见第五节），**逻辑未接**——断开并返回前的"确定要断开吗"二次确认（当前点退出直接断开）
- **加载遮罩**：连接服务器期间的全屏提示（当前连接期间按钮无反馈界面，仅状态文本）
- **守护点减伤叠层**：按设计不在 UI 显示

## 五、GM 面板 / 确认弹窗 / 全屏遮罩（2026-10-11 新增，UI 已建·代码未接）

> 组件都在 Main 包里、`MainBinder` 已注册，能正常 `CreateInstance()`；但**代码侧暂无任何入口**（没有创建、没有协议、没有快捷键）。

### GmPanel（`Panels/Modules/GmPanel.xml`，800×600）

| 成员 | 类型 | 说明 |
|---|---|---|
| m_firstMenu / m_secondMenu | UI_GmButtonList | 一级 / 二级菜单列（各 125×500）。列内 GList 名 `button_list`，defaultItem = Button1（带 `selected` 选中态） |
| m_GmItemList | GList | 右侧参数行列表（448×467，单列，lineGap=15），defaultItem = **GmItem** |
| （底框，未导出） | UI_Panel_1 | 实例上 `hideTitle=1`（标题栏隐藏），且 Panel_1 本身没有关闭按钮 → **面板当前没有任何关闭入口** |

### GmItem（`Panels/Modules/GM/GmItem.xml`，500×50）——`content` 控制器五档

所有档位共用左侧 `m_title` 作为行名；`m_content` 是导出控制器，**按显示顺序（= `selectedIndex`）依次为**：

| selectedIndex | 档位 | 显示的元素 | 含义 / 用法 |
|---|---|---|---|
| 0 | **文本** | m_content_text（只读 GTextField，右对齐） | 只展示当前值，不可操作 |
| 1 | **按钮** | m_content_btn（UI_Button1） | 单次执行型指令（如"重置位置"），点一次执行一次 |
| 2 | **滑动条** | m_content_slider（GmSlider，`value` 0~max）+ m_content_slider_value（数值文本） | 连续调参：滑条值驱动右侧数值文本 |
| 3 | **输入框** | m_content_inputField（GTextInput，真输入框） | 手输数值 / 字符串 |
| 4 | **下拉框** | m_content_dropdown（ComboBoxGm） | 枚举选择。popup = GmComboBox_popup（内含名为 `list` 的 GList，条目 = GmComboBox_item） |

- ⚠ **页 id 与 index 不一致**：文本=0 / 按钮=**3** / 滑动条=**1** / 输入框=**2** / 下拉框=4（各元素 gearDisplay 也是按这套 id 挂的）。代码里请用 `selectedIndex`（即上表 index）或 `selectedPage = "按钮"`，**不要**把档位 id 当 index 用。
- 组件默认页 `selected=4`（下拉框）——只影响编辑器预览和新建实例，列表里的行由代码逐行设档。

### ConfirmPanel（`Panels/Modules/ConfirmPanel.xml`，400×300）

| 成员 | 类型 | 说明 |
|---|---|---|
| m_content | GTextField | 正文提示语（代码写入） |
| m_btn_confirm / m_btn_cancel | GButton（Button4） | 确认 / 取消 |

### FullScreenMask（`Panels/Modules/FullScreenMask.xml`）

5000×5000 全屏半透明遮罩（`#66000000`），弹窗时用来挡下层点击；空文本子节点 `empty_text_merely_make_sure_this_component_can_be_published` 只是为了让该组件能生成代码，无实际用途。遮罩与弹窗都需代码手动创建后 AddChild。

### 本节待确认

- GmPanel 无关闭入口（标题栏被隐藏、Panel_1 无关闭按钮）：待定用快捷键开关还是补关闭按钮。
- ConfirmPanel 无标题（当前只有正文）：若需要"断开连接"这类标题需补 title。

## 六、协议依赖（已实现）

- `SCBattleEvent`（战斗事件列表唯一来源）：type（事件语义唯一判别，icon 档位与条目版式由客户端按类型映射）+ textId/textId2（数字文本：<10000 = NoticeMessageMap，≥10000 = 玩家名，clientId = id − 10000）。事件源：玩家击杀（PlayerKill，type2 双名）/玩家死亡无归属（DeathUnattributed）、复活（PlayerRespawn）、攻占瘟疫树（PlagueTreeCaptured）、瘟疫树刷新（PlagueTreeRespawn）、天黑/天亮（Nightfall/Daybreak）、守护点摧毁（BeaconDestroyed，textId 区分哪一座：19 中心 / 24~26 外围，驱动对应血条 destroyed）、防御塔摧毁（TowerDestroyed）、武器提示 13/14/15（Notice，纯文字，定向）
- `SCDamage`（value：0=无效，>0=普通，<0=暴击取绝对值；targetId=受击实体；hasHitPos+hitPos 命中点可选）+ `NetworkManager.SendDamage` 定向（按可见性）/广播重载，不可靠通道
- `SCPrompt`（messageId = NoticeMessageMap 消息 id）+ `NetworkManager.SendPrompt` 定向：仅开局校验 17/18（战斗事件列表条目一律走 SCBattleEvent，不走此通道）
- `SCRoomInfo.RoomMemberInfo.characterIndex / name`；`CSPlayerInfo.name`（玩家名上报）
- `EntityPlayerManager.TryGetAnchorPos(id, EntityAnchor, out pos)`（任意锚点取世界位置，见《代码架构说明》「锚点」节；名牌/血条走 `Bar`、伤害飘字兜底走 `LabelFallBack`）
- `EntityData.OnDamaged(..., bool isCrit)` 在终伤处广播伤害事件（ProcessHit 传 isCrit）

## 七、聊天系统（2026-10-12 新增，UI 已建·代码未接）

> 组件在 Main 包内：`Panels/Modules/ChatPanel.xml`（421×300）+ `Panels/Modules/Chat/ChatItem.xml`（400×60）。
> `MainBinder` 已注册两者 URL，`UI_ChatPanel.CreateInstance()` / `UI_ChatItem.CreateInstance()` 可直接用；**代码侧目前 0 行**（无创建入口、无协议、无渲染、无输入处理），本节即后续纯代码开发的对接说明。

### 7.1 需求确认（2026-10-12 与策划确认）

| 项 | 结论 |
|---|---|
| 频道 | 只有两种：**全频道**（所有人可见）+ **阵营频道**（仅同阵营可见） |
| 可用场景 | **联网状态下均可**：组队大厅（LobbyPage）+ 战斗（BattlePage）；未联网的首页不显示 |
| 自己的消息 | **本地立即回显**（不等服务端回包），同时照常上报服务端 |
| 面板入口 | **进入网络状态后自动显示**：无标题栏、无关闭按钮、无快捷键，靠联网/断线状态自动显隐 |

### 7.2 ChatPanel 成员

| 成员 | 类型 | 说明 |
|---|---|---|
| m_show_channel_menu | Controller（2 档） | 频道菜单显隐，档位见 7.3 |
| m_input | GTextInput | 单行输入框（字体 20，ubb=true）；读值用 `.text` |
| m_btn_send | GButton | 标题"发送"（334,256，81×37） |
| m_content | GList | 400×229(11,11)，`overflow=scroll`，defaultItem = ChatItem |
| m_btn_allChannel | GButton | 标题"全频道"（面板**上方** y=-28，100×30） |
| m_btn_campChannel | GButton | 标题"阵营频道"（面板**上方** y=-28，100×30） |

### 7.3 控制器档位

**ChatPanel.m_show_channel_menu**（控制的是 `btn_allChannel` + `btn_campChannel` 所在组的显隐，组位于面板上方 y=-28，即面板外）

| index | 效果 |
|---|---|
| 0 | 收起：只剩列表 + 输入行，频道按钮不可见 |
| 1 | 展开：额外显示"全频道 / 阵营频道"两个按钮（**组件当前默认档 = 1**） |

**ChatItem.m_camp**（控制的是两个阵营 image 的互斥显示）

| index | 效果 |
|---|---|
| 0 | 显示攻击方图标 `Images/Common/icon_camp_attacker.png`（28×28，位于头像右下角） |
| 1 | 显示防守方图标 `Images/Common/icon_camp_defenser.png` |

**ChatItem.m_side**（控制的是「头像框组」与「阵营图标组」两个组的 XY，文本/名字不参与）

| index | 头像框组（底框+headIcon） | 阵营图标组 | 正文/名字 |
|---|---|---|---|
| 0 = 他人（默认） | (5,6) | (33,33) | 固定 (62,29) / (62,5) |
| 1 = 自己 | (345,5) | (340,33) | **不变** |

### 7.4 ChatItem 成员

| 成员 | 类型 | 说明 |
|---|---|---|
| m_content | GTextField | 正文，`autoSize=height` → **条目高度随文本增高** |
| m_headIcon | GLoader | 头像，url 由代码写（取角色图标，参考 HomePage 里 UI_RoleHead.m_headIcon 的取法） |
| m_player_name | GTextField | 发送者名字（固定宽 278，autoSize=none） |

### 7.5 数据流链路（按数据流顺序；括号内为落点）

1. **挂载**：联网后创建 ChatPanel（`UI_ChatPanel.CreateInstance()`），列表先设 `m_content.itemRenderer`，再设 `numItems`。（UIManager 或 LobbyPage/BattlePage，见 7.8）
2. **交互**：`m_btn_send.onClick` / `m_input.onSubmit` → 取 `m_input.text`，判空与长度；`m_btn_allChannel.onClick` / `m_btn_campChannel.onClick` → 切发送频道。
3. **本地回显**：先把条目加入本地列表（`m_side=1`、`m_camp`=自己的阵营），清空输入框，滚到最新。
4. **上报**：`NetworkManager.SendChat(channel, content)` → `CallFuncRpc(ServerReceiveChatLocal, SendTo.RoomOwner, Delivery.Reliable, new CSChat{...}, EnsInstance.LocalClientId)`（照 `SendStartRequest` 写法）。
5. **服务端**：`[Rpc] ServerReceiveChatLocal(CSChat c, short clientId)` → 取阵营（`BattleManager.PlayerCamp` / `SCRoomInfo.RoomMemberInfo.camp`）组装 `SCChat`（含 senderClientId、名字、阵营）→ 全频道用 `SendTo.ExcludeSender` 广播（**排除发送者，避免与自己回显重复**）；阵营频道按同阵营 clientId 逐个 `SendTo.To(clientId)`。
6. **下行**：`[Rpc] ClientReceiveChatLocal(SCChat e)` → `EventManager.TrigEvent(ClientEvent.OnChatMessage, e)`。
7. **渲染**：页面 `Enter` 订阅事件 → 条目入表 → `m_content.numItems = count` → `itemRenderer` 写 `m_camp / m_side / m_player_name / m_content`（照 BattlePage `RenderEventItem` 模式，`itemRenderer` 先设、`numItems` 后设）。

### 7.6 需新增/改动的位置

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Transport/CSChat.cs`、`SCChat.cs` | 新增：载荷类 + 同名 Serializer（照 `SCRoomInfo.cs` 结构）。SCChat 需带 senderClientId、名字、**阵营**（客户端不知道别人的阵营，必须由服务端下发） |
| `Assets/Scripts/GameController/NetworkManager.cs` | 新增 `SendChat(...)` + `[Rpc] ServerReceiveChatLocal` + `[Rpc] ClientReceiveChatLocal` |
| `Assets/Scripts/GlobalData/ClientEvent.cs` | 新增 10310 段常量 `OnChatMessage` |
| `Assets/Scripts/UI/UIManager.cs` 或 `UI/Logic/LobbyPage.cs`、`BattlePage.cs` | 面板挂载、事件订阅（Enter/Exit）、列表渲染 |
| （可选）`Assets/Scripts/GlobalData/NoticeMessageMap.cs` | 若发送失败/超长要走纯文字提示 |

**改完必须执行 Unity 菜单 Ens/GenerateCode**：消息 id 由代码生成器按类与方法声明顺序自动分配，不跑就没有协议 id。

### 7.7 开发完成后的界面行为

- 联网进入组队大厅即出现聊天面板；切到战斗界面仍在；断开/回首页时隐藏（或销毁，见 7.8）。
- 别人发言：条目左侧（`side=0`），头像+名字，`camp` 按发送方阵营显示攻/守图标，正文自增高。
- 自己发言：本地立即插入条目（`side=1`），头像与阵营图标移到右侧，输入框清空，列表滚到最新。
- 频道：默认发全频道；点"阵营频道"后只发给同阵营；点任一频道按钮后 `show_channel_menu.selectedIndex = 0` 收起菜单。
- 列表只保留最近 N 条（建议 50，超出从头部移除后重设 `numItems`）；滚动由列表自身的 `overflow=scroll` 负责。

### 7.8 本节待确认

- **面板挂载方式**：UIManager 联网后创建一次并常驻 GRoot（断线销毁），还是 LobbyPage/BattlePage 各自持有？影响重连时的表现。
- **自己消息的文本位置**：`m_side=1` 目前只把头像组与阵营图标组移到右侧，**正文与名字仍在左侧** (62)。若希望自己的消息整体右对齐，需要在 FGUI 里给正文组补 gearXY/gear 对齐。
- 消息历史上限条数、是否跨场景保留、断线重连是否清空。
- 发送频率限制、长度上限、空消息处理策略（本地忽略 或 提示）。
