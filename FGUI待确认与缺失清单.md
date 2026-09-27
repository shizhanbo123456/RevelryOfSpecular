# FGUI 界面清单：待确认 / 待补充（2026-09-27）

> 配套：`Assets/Resources/GUI/Main_fui.bytes`（包名 Main）+ `ScriptsGenerated/Main`（22 个生成类）。
> 本文供补齐 UI 与讲解字段含义用；逻辑接线时以本文为准。**当前 UIManager 仍是旧 uGUI 版（引用已删除的 RosPage/HomePage 等），编译不过，接逻辑时我会重写为 FGUI 版。**

## 一、生成组件与我的字段理解（✅=已确认 ❓=需要你讲解）

| 组件 | 字段 | 我的理解 |
|---|---|---|
| UI_HomeConnect | m_input_ipaddress / m_btn_connect | IP 输入 + 连接按钮 ✅；❓ ipaddress 是 GComponent 而非文本框——内部文本字段名？ |
| UI_HomeCharacterList | m_panel / m_characterList / **m_btn_attacker / m_btn_defenser** | ✅ 两按钮 = 页签，切换 characterList 显示哪个阵营 |
| UI_RoleHead | **m_level / m_lock** 控制器、m_headIcon(GLoader)、m_roleName | 角色条目：❓ m_level 有几个档位、显示什么（数字等级还是徽标）；m_lock=未解锁态 ✅；头像由代码 SetIcon ✅ |
| UI_HomeAttrList + UI_AttrItem | m_attrList ← m_attrName/m_attrValue | ✅ 属性列表；m_attrValue 会填 richtext（当前值+绿增益+橙下级），FGUI 文本需支持富文本 |
| UI_LobbyMemberList | m_panel / m_defenserPlayers / m_attackerPlayers / m_btn_battleStart | ✅ 攻守成员两列表 + 准备按钮；❓ **成员条目组件是什么**（需要玩家名+角色头像，生成代码里没有对应条目类） |
| UI_BattlePanel | **m_icon_day_night**(GImage) / m_label_time_left | ✅ 昼夜图标旋转（0°=正午/180°=午夜）+ 剩余时间；❓ GetChildAt(5)(8) 说明还有不少子元素未导出字段——它们是占位还是待接？ |
| UI_SkillListItem | m_loader_iconBase / m_loader_icon / m_store / m_key | 技能槽：底框/技能图标/库存/键位 ✅ |
| UI_PlayerHealth | **m_bar_fill**(GGraph) / m_label_level / m_label_health | ❓ 血条：GGraph 填充是代码改宽度还是改色？名牌上要显示等级和血量数字吗（旧版只有名字+比例条）？ |
| UI_PlayerName | m_num | ✅ 玩家名（"玩家 N"） |
| UI_DamageLabel | m_num | ✅ 伤害数字：0=无效 / >0=普通 / <0=暴击，样式由代码切（灰/白/橙+大号） |
| UI_EventItem | **m_type 控制器** + type0_label + type1_loader+label + type2_loader+label1+label2 | ❓ 三种形态分别对应什么事件？（猜测：纯文字 / 图标+文字 / 双行？） |
| UI_EventIcon | m_type 控制器 | ❓ 图标档位有哪些（击杀/水晶/守护点/瘟疫树…） |
| UI_NoticePanel | m_title | ✅ 消息提示（ShowNotice） |
| UI_BattleResult | m_title / m_content / **m_t0(转场)** | ✅ 结算：胜负标题+比分经验详情，显示时播 m_t0 |
| UI_BattleResultDetail(Item) | m_resultList；Item 只有 m_t0 转场 | ✅ 结算明细列表；❓ 明细条目上显示什么、怎么摆（无导出字段） |
| UI_Minimap + UI_MinimapItem | m_mapBase(GGraph)；m_type 控制器 | 小地图；❓ 底图是代码绘制还是贴图；❓ MinimapItem 的 type 档位（自己/攻/守/中立？）；❓ 点击展开全图还要吗 |
| UI_Button1 | m_selected 控制器 | ✅ 通用按钮，选中态切换（页签高亮） |
| UI_Panel_1 | m_hideTitle 控制器 | 通用卡片底板；❓ 面板标题文字是预置的吗，m_hideTitle 隐藏的是哪部分 |

## 二、缺失的 UI 元素（旧功能无对应物，补齐后我再接）

| # | 缺失项 | 说明 | 优先级 |
|---|---|---|---|
| 1 | 大厅：选队控件 | 「加入进攻方/防守方」入口（Toggle 或点列表加入均可，定一种） | 高 |
| 2 | 大厅：AI 数量编辑 | -/+ 按钮 + 数量文本（双方各一组） | 高 |
| 3 | 大厅：成员条目组件 | 玩家名 + 所选角色头像（攻/守列表的条目） | 高 |
| 4 | 守护点血量面板 | 右侧：中心守护点固定单槽 + 外围守护点 3 项（血条/减伤叠层/已摧毁） | 高 |
| 5 | 技能槽：CD 表现 | CD 遮罩（填充）+ 剩余秒数 + 选中高亮 + 武器经验条 | 高 |
| 6 | 复活进度 | 遮罩 + 进度条 + 文案（"复活中 X%（愈战愈勇 ×N）"/"可复活！"） | 高 |
| 7 | 结算：关闭按钮 | "回到组队大厅"按钮（结算面板没有出口） | 高 |
| 8 | 首页：玩家等级文本 | 顶栏"玩家等级 Lv N"（SaveManager.playerLevel） | 中 |
| 9 | 首页：选中角色标题 | "角色名 Lv N"（属性列表的标题行） | 中 |
| 10 | 首页：下一级提升提示 | "下一级：生命值上限 +25%" 一行小字 | 中 |
| 11 | 首页：连接状态文本 | "正在连接.../连接失败" 反馈 | 中 |
| 12 | 确认面板 | 断开并返回的二次确认（旧 ConfirmUnit） | 中 |
| 13 | 加载遮罩 | 连接服务器期间的自旋/文字遮罩（旧 LoadingUnit） | 中 |
| 14 | 伤害飘字的暴击/无效样式 | 若模板内含两种以上外观（底框/描边）需要说明；纯代码改色则不用 | 低 |

## 三、逻辑接线规划（我接逻辑时的数据/事件映射，供核对）

| 页面 | 数据/事件 | 写入目标 |
|---|---|---|
| Home | InfoManager 攻/守角色列表 + SaveManager 等级/解锁 + AssetsManager 图标 | HomeCharacterList.m_characterList ← UI_RoleHead（m_lock/m_headIcon/m_roleName，未解锁禁点） |
| Home | ClientSelection 攻/守选中 + GetAttribute 三份差值 | 页签切换列表；HomeAttrList.m_attrList ← UI_AttrItem（attrValue = 当前值+绿增益+橙下级 richtext） |
| Home | NetworkManager.TryConnect + OnConnect 事件 | HomeConnect 按钮点击 → 连接 → 大厅（切页由 UIManager 统一接线） |
| Lobby | SCRoomInfo（OnRoomInfoUpdate） | 两成员列表（名字+头像，characterIndex 已在协议里）+ 开始按钮可用性 |
| Lobby | CSRoomUpdate 上报 | 选队/AI 编辑（控件补齐后接） |
| Battle | OnEntityDisplayUpdate | 守护点面板、技能栏（SkillListItem 整表重渲染）、名牌刷新 |
| Battle | Tick 逐帧 | 剩余时间推演、昼夜图标旋转（0°=正午/180°=午夜）、名牌/伤害飘字屏幕跟随（GRoot 坐标换算） |
| Battle | OnBattleEvent（含新增 Damage 事件：value 0=无效/>0=普通/<0=暴击） | 伤害飘字（受击实体头顶）+ 事件飘字 |
| Battle | OnScoreUpdate（仅终局一次）+ OnReviveProgressUpdate | 结算面板（m_t0 播放）+ 复活进度（控件补齐后接） |
| 全局 | OnConnect/OnBattleStart/OnRestartGame | UIManager 统一切页（FGUI 版：GComponent 显隐 + TurnPage 形态不变） |
| 全局 | SCBattleEvent.ShowText / ShowNotice | NoticePanel / UI_EventItem |

## 四、现状备忘

- FGUI 运行时在 `Assets/FairyGUI-unity-master`；包加载路径 `UIPackage.AddPackage("GUI/Main")`（Resources/GUI/Main_fui.bytes）。
- 旧 uGUI 系统（View/Logic/Components/Items）已删除；`UIManager.cs` 待重写为 FGUI 版（页面切换/飘字 API 形态保留，调用方不用改）。
- `SCRoomInfo.RoomMemberInfo.characterIndex` 已在协议里（大厅头像用）；`SCBattleEvent` 已有 Damage 事件（value 0=无效/>0=普通/<0=暴击，targetId=受击实体）。
