using Ros.Transport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public partial class NetworkManager : EnsBehaviour
{
    private void Awake()
    {
        Tool.NetworkManager = this;
        ResetClientNetworkInfo();
        ClientBindEvents();
    }

    public enum ConnectResult
    {
        Success,
        Failed,
        TooFrequent
    }

    public static string serverHello;

    public static bool BattleRunning { get; private set; }

    #region//Client 连接流程
    private static bool connecting;
    private static bool connected;
    private static bool tryingEnterWorld;
    private static bool hasLobbyInfo;
    private static bool intentionalExitWorld;
    private static bool kicked;      // 被同令牌的新连接顶掉：提示语与普通断线区分

    public static bool CanSendWorldCommand => connected && hasLobbyInfo;

    // 本机玩家令牌（存档持有，跨断线/跨进程不变）：协议里的玩家标识一律是它，连接 id 只用于寻址
    public static int LocalToken { get; private set; }

    private void ResetClientNetworkInfo()
    {
        connecting = false;
        connected = false;
        serverHello = null;
        BattleRunning = false;
        hasLobbyInfo = false;
        tryingEnterWorld = false;
        intentionalExitWorld = false;
        LatestLobbyInfo = null; // 跨连接清空，防止重连后大厅显示上一局成员
        lobbyMembers.Clear();
        lobbyPlayerNames.Clear();
        serverTimeOffset = 0f;
    }

    #region 服务器时间换算
    private static float serverTimeOffset; // 服务器时间 − 本地时间，收到带服务器时间的包时校准

    // 包里的时间戳是发包时刻，补上单程延迟才是到达时刻的服务器时间
    public static void SyncServerTime(float packetServerTime)
    {
        if (packetServerTime <= 0f) return;
        serverTimeOffset = packetServerTime + EnsInstance.LocalClientDelay / 1000f - Time.time;
    }

    public static float ClientToServerTime(float localTime)
    {
        return localTime + serverTimeOffset;
    }

    public static float ServerToClientTime(float serverTime)
    {
        return serverTime - serverTimeOffset;
    }
    #endregion

    private void ClientBindEvents()
    {
        EnsInstance.OnServerConnect += () => connected = true;
        // 房间事件 0：服务器握手（服务器信息可达性确认）
        ClientRoomManagerEventCenter.Register(0, data =>
        {
            serverHello = data;
        });
        EnsInstance.OnJoinRoom += ClientSendInfo;
        // 房间事件 2：服务器顶号通知（同一令牌在别处登录），本端自行断开且不再自动重连
        ClientRoomManagerEventCenter.Register(2, data =>
        {
            kicked = true;
            EnsInstance.Corr.ShutDown();
        });

        EnsInstance.OnServerDisconnect += () =>
        {
            if (intentionalExitWorld)
            {
                intentionalExitWorld = false;
                return;
            }
            if (kicked)
            {
                kicked = false;
                if (Tool.UIManager != null)
                {
                    Tool.UIManager.HideLoading();
                    Tool.UIManager.ShowFlyText("账号已在别处登录");
                }
                EventManager.TrigEvent(ClientEvent.OnRestartGame);
                return;
            }
            // 任何断开都直接回初始界面：会话在服务器端按令牌保留，由玩家自己重新连接
            if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("与服务器断开连接");
            EventManager.TrigEvent(ClientEvent.OnRestartGame);
        };
    }

    public async Task<ConnectResult> TryConnect(string ipAddress)
    {
        if (connecting) return ConnectResult.TooFrequent;
        if (Tool.SaveManager == null)
        {
            Debug.LogError("未找到存档管理器，无法取得玩家令牌");
            return ConnectResult.Failed;
        }
        LocalToken = Tool.SaveManager.token;
        kicked = false;
        ResetClientNetworkInfo();
        connecting = true;
        try
        {
            if (ipAddress == string.Empty)
            {
                ipAddress = IPAddress.Loopback.ToString();
            }
            else if (!IPAddress.TryParse(ipAddress, out IPAddress _))
            {
                Debug.LogError("输入IP地址错误");
                return ConnectResult.Failed;
            }
            EnsInstance.Corr.IP = ipAddress;
            // ShutDown 会拿上次连接残留的未握手标记补发一次 OnConnectionRejected，先关再挂监听，避免被假拒绝打断
            EnsInstance.Corr.ShutDown();
            bool rejected = false;
            void OnRejected() => rejected = true;
            EnsInstance.OnConnectionRejected += OnRejected;
            try
            {
                EnsInstance.Corr.StartClient();
                float deadline = Time.time + 5f;
                while (!rejected && !connected && Time.time < deadline)
                {
                    await Task.Delay(100);
                }
            }
            finally
            {
                EnsInstance.OnConnectionRejected -= OnRejected;
            }
            if (!connected)
            {
                Debug.LogError($"Ens层连接失败 rejected={rejected}");
                return ConnectResult.Failed;
            }
            ClientRoomManagerEventCenter.TrigRpcEvent(Delivery.Reliable, 0, LocalToken.ToString());
            float helloDeadline = Time.time + 5f;
            while (serverHello == null && Time.time < helloDeadline)
            {
                await Task.Delay(100);
            }
            if (serverHello == null)
            {
                Debug.LogError("服务器信息获取失败");
                return ConnectResult.Failed;
            }
            EventManager.TrigEvent(ClientEvent.OnConnect);
            return ConnectResult.Success;
        }
        finally
        {
            connecting = false;
        }
    }

    public void EnterWorld()
    {
        if (tryingEnterWorld) return;
        tryingEnterWorld = true;
        StartCoroutine(EnterWorldFallBack());
        EnsCorrespondent.JoinRoom(EnsRoomManager.roomIdStart);
    }

    public void ExitWorld()
    {
        intentionalExitWorld = true;
        if (EnsInstance.LocalClientId >= 0 && EnsInstance.PresentRoomId != 0)
        {
            CallFuncRpc(ServerReceiveExitWorldLocal, SendTo.RoomOwner, Delivery.Reliable, LocalToken);
            EnsInstance.Corr.FlushSendBufferNow();
        }
        hasLobbyInfo = false;
        EnsInstance.Corr.ShutDown();
        // 主动退出：通知 UI 返回主界面。断开回调 OnServerDisconnect 仅用于抑制 OnRestartGame，避免重复重启流程。
        EventManager.TrigEvent(ClientEvent.OnExitWorld);
    }

    private IEnumerator EnterWorldFallBack()
    {
        yield return new WaitForSeconds(2f);
        if (!tryingEnterWorld) yield break;
        tryingEnterWorld = false;
        EventManager.TrigEvent(ClientEvent.OnRestartGame);
    }

    private void ClientSendInfo()
    {
        if (!tryingEnterWorld) return;
        //加入房间失败时保持 tryingEnterWorld，交由 EnterWorldFallBack 兜底重连
        if (EnsInstance.PresentRoomId <= 0) return;
        tryingEnterWorld = false;
        // 双方角色各自选择并随 CSLobbyPlayerInfo 上报；阵营完全在房间内确定，服务器按最终阵营取对应一侧
        var info = new CSLobbyPlayerInfo()
        {
            attackCharacter = EntityType.Attack(Mathf.Clamp(ClientSelection.selectedAttackIndex, 0, Config.attack_character_count - 1)),
            attackLevel = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(ClientSelection.selectedAttackIndex) : 1,
            defenseCharacter = EntityType.Defense(Mathf.Clamp(ClientSelection.selectedDefenseIndex, 0, Config.defense_character_count - 1)),
            defenseLevel = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(Config.attack_character_count + ClientSelection.selectedDefenseIndex) : 1,
            name = ClientSelection.playerName,
        };
        CallFuncRpc(ServerReceivePlayerInfoLocal, SendTo.RoomOwner, Delivery.Reliable, info, LocalToken);
    }
    #endregion

    #region//发送封装：客户端 → 服务器
    public void SendInput(CSPlayerInput input)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveInputLocal, SendTo.RoomOwner, Delivery.Strive, input, LocalToken);
    }

    // 技能键：客户端已把按键解析成技能 id，服务器只做持有/沉默/强控校验
    public void SendSkillCastInput(int skillId)
    {
        if (!CanSendWorldCommand || skillId < 0) return;
        CallFuncRpc(ServerReceiveSkillCastInputLocal, SendTo.RoomOwner, Delivery.Strive, new CSSkillCast() { skillId = skillId }, LocalToken);
    }

    public void SendLobbyUpdate(CSLobbyUpdate update)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLobbyUpdateLocal, SendTo.RoomOwner, Delivery.Reliable, update, LocalToken);
    }

    #region 加载期请求（客户端 → 服务器，逐步发送）
    public void SendLoadingAttackPlayerRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingAttackPlayerRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingAttackPlayerRequest(), LocalToken);
    }

    public void SendLoadingDefensePlayerRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingDefensePlayerRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingDefensePlayerRequest(), LocalToken);
    }

    public void SendLoadingAiUnitRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingAiUnitRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingAiUnitRequest(), LocalToken);
    }

    public void SendLoadingComplete()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingCompleteLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingComplete(), LocalToken);
    }
    #endregion

    public void SendMinimapRadiusSwitch()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveMinimapRadiusSwitchLocal, SendTo.RoomOwner, Delivery.Reliable, new CSMinimapRadiusSwitch(), LocalToken);
    }

    // 相机模式属于移动语义的一部分（俯视 = 四向），切模式即同步一次
    public void SendCameraMode(bool topDown)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveCameraModeLocal, SendTo.RoomOwner, Delivery.Reliable, new CSCameraMode() { topDown = topDown }, LocalToken);
    }

    // 观战自由视角：位置上报（仅纯观战方），由 CameraController 按 1s / 1m 阈值节流
    public void SendFreeCamera(CSFreeCamera info)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveFreeCameraLocal, SendTo.RoomOwner, Delivery.Reliable, info, LocalToken);
    }

    public void SendChat(CSChat chat)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveChatLocal, SendTo.RoomOwner, Delivery.Reliable, chat, LocalToken);
    }

    public void SendGm(CSGm gm)
    {
        if (gm == null || !CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveGmLocal, SendTo.RoomOwner, Delivery.Reliable, gm, LocalToken);
    }
    #endregion

    #region//发送封装：服务器 → 客户端
    // 令牌 → 当前连接 id：查不到说明该玩家没有连接（AI 玩家 / 已断线），直接不发
    private static bool TryGetSendTo(int clientId, out SendTo to)
    {
        to = default;
        if (!RSPlayerRegistry.TryGetPhys(clientId, out var physId)) return false;
        to = SendTo.To(physId);
        return true;
    }

    #region 加载期下发（服务器 → 客户端）
    public void SendLoadingStart(int clientId, SCLoadingStart info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLoadingStartLocal, to, Delivery.Reliable, info);
    }

    public void SendLoadingPrepared(int clientId, SCLoadingPrepared info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLoadingPreparedLocal, to, Delivery.Reliable, info);
    }

    public void SendLoadingAttackPlayerResponse(int clientId, SCLoadingAttackPlayerResponse info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLoadingAttackPlayerResponseLocal, to, Delivery.Reliable, info);
    }

    public void SendLoadingDefensePlayerResponse(int clientId, SCLoadingDefensePlayerResponse info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLoadingDefensePlayerResponseLocal, to, Delivery.Reliable, info);
    }

    public void SendLoadingAiUnitResponse(int clientId, SCLoadingAiUnitResponse info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLoadingAiUnitResponseLocal, to, Delivery.Reliable, info);
    }

    public void SendBattleStart(int clientId)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveBattleStartLocal, to, Delivery.Reliable, new SCBattleStart());
    }
    #endregion

    public void SendEntityDisplay(int clientId, SCEntityDisplayInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        // 含 buff 信息的包走尽力传输（丢一包下轮脏标记还会补）；纯位姿包走不可靠（丢了等下个快包）
        var delivery = info.includeRuntime || info.hasBuffs ? Delivery.Strive : Delivery.Unreliable;
        CallFuncRpc(ClientReceiveEntityDisplayLocal, to, delivery, info);
    }

    public void SendEntityAnim(int clientId, SCEntityAnimInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveEntityAnimLocal, to, Delivery.Strive, info);
    }

    public void SendRemoveEntity(int clientId, int entityId)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientRemoveEntityLocal, to, Delivery.Reliable, entityId);
    }

    public void SendMinimapEntity(int clientId, SCMinimapEntity e)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveMinimapEntityLocal, to, Delivery.Unreliable, e);
    }

    public void SendMinimapRadius(int clientId, float radius)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveMinimapRadiusLocal, to, Delivery.Reliable, new SCMinimapRadius() { radius = radius });
    }

    public void SendBattleEvent(int clientId, SCBattleEvent e)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveBattleEventLocal, to, Delivery.Strive, e);
    }

    public void SendBattleEvent(SCBattleEvent e)
    {
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.ExcludeSender, Delivery.Strive, e);
    }

    public void SendPrompt(int clientId, int messageId)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceivePromptLocal, to, Delivery.Reliable, new SCPrompt() { messageId = messageId });
    }

    // 技能受理成功回执：只回施法者，客户端收到才扣一次可用次数
    public void SendSkillUsed(int clientId, int skillId)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveSkillUsedLocal, to, Delivery.Reliable, new SCSkillUsed() { skillId = skillId });
    }

    public void SendSkillSync(int clientId, SCSkillSync info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveSkillSyncLocal, to, Delivery.Reliable, info);
    }

    public void SendCombatLabel(int clientId, SCCombatLabel label)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveCombatLabelLocal, to, Delivery.Unreliable, label);
    }

    public void SendCombatLabel(SCCombatLabel label)
    {
        CallFuncRpc(ClientReceiveCombatLabelLocal, SendTo.ExcludeSender, Delivery.Unreliable, label);
    }

    public void SendScoreInfo(int clientId, SCScoreInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveScoreInfoLocal, to, Delivery.Reliable, info);
    }

    public void SendReviveInfo(int clientId, SCReviveInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveReviveInfoLocal, to, Delivery.Reliable, info);
    }

    public void SendLobbyInfo(SCLobbyInfo info)
    {
        CallFuncRpc(ClientReceiveLobbyInfoLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    // 定向全量：新玩家进房 / 重连时建立自己的成员表
    public void SendLobbyInfo(int clientId, SCLobbyInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLobbyInfoLocal, to, Delivery.Reliable, info);
    }

    // 名字广播（新玩家进房时发给所有人；组队期专用）
    public void SendLobbyPlayerName(SCLobbyPlayerName info)
    {
        CallFuncRpc(ClientReceiveLobbyPlayerNameLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    // 名字定向（给新玩家补发房间里每个玩家的名字；组队期专用）
    public void SendLobbyPlayerName(int clientId, SCLobbyPlayerName info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveLobbyPlayerNameLocal, to, Delivery.Reliable, info);
    }

    public void SendPlayerEntity(int clientId, int entityId)
    {
        CallFuncRpc(ClientReceivePlayerEntityLocal, SendTo.ExcludeSender, Delivery.Reliable,
            new SCPlayerEntity() { clientId = clientId, entityId = entityId });
    }

    [Rpc]
    private void ClientReceivePlayerEntityLocal(SCPlayerEntity info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        // 直连常驻的加载会话：开战关系包早于战斗界面创建到达，走事件会整批丢失
        Tool.ClientLogicManager.Loading.OnPlayerEntity(info);
    }

    public void SendSpectate(CSSpectate request)
    {
        CallFuncRpc(ServerReceiveSpectateLocal, SendTo.RoomOwner, Delivery.Reliable, request, LocalToken);
    }

    // 观战接口：传目标玩家 clientId（客户端可从 SCPlayerEntity 关系包 / 房间成员表取得）；
    // 传非法 / 死亡 / 异阵营目标 = 退出观战
    public static void RequestSpectate(int targetClientId)
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendSpectate(new CSSpectate() { targetClientId = targetClientId });
    }

    public void SendCampAliveCount(SCCampAliveCount info)
    {
        CallFuncRpc(ClientReceiveCampAliveCountLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    // 守护点开采进度：与视野无关，广播给房间内所有客户端（含观战方）
    public void SendDefensePointProgress(SCDefensePointProgress info)
    {
        CallFuncRpc(ClientReceiveDefensePointProgressLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    public void SendDayNightInfo(int clientId, SCDayNightInfo info)
    {
        if (!TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveDayNightInfoLocal, to, Delivery.Strive, info);
    }

    public void SendDayNightInfo(SCDayNightInfo info)
    {
        CallFuncRpc(ClientReceiveDayNightInfoLocal, SendTo.ExcludeSender, Delivery.Strive, info);
    }

    public void SendSkillCast(int skillId, SkillContext context)
    {
        CallFuncRpc(ClientUseSkillLocal, SendTo.ExcludeSender, Delivery.Strive, skillId, context);
    }

    //聊天广播：发送者本地已回显，不回送
    public void SendChat(SCChat chat)
    {
        CallFuncRpc(ClientReceiveChatLocal, SendTo.ExcludeSender, Delivery.Reliable, chat);
    }

    //GM 指令回包（服务器帧率 / 服务器时间等）
    public void SendGm(int clientId, SCGm gm)
    {
        if (gm == null || !TryGetSendTo(clientId, out var to)) return;
        CallFuncRpc(ClientReceiveGmLocal, to, Delivery.Reliable, gm);
    }
    #endregion

    #region//[Rpc] 服务器侧接收（客户端 → 服务器）
    [Rpc]
    private void ServerReceivePlayerInfoLocal(CSLobbyPlayerInfo info, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.AddPlayer(clientId, info);
    }

    [Rpc]
    private void ServerReceiveInputLocal(CSPlayerInput input, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveInput(clientId, input);
    }

    [Rpc]
    private void ServerReceiveSkillCastInputLocal(CSSkillCast cast, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveSkillCast(clientId, cast.skillId);
    }

    [Rpc]
    private void ServerReceiveExitWorldLocal(int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.RemovePlayer(clientId);
    }

    [Rpc]
    private void ServerReceiveLobbyUpdateLocal(CSLobbyUpdate update, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveLobbyUpdate(clientId, update);
    }

    [Rpc]
    private void ServerReceiveLoadingAttackPlayerRequestLocal(CSLoadingAttackPlayerRequest request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestAttackPlayers(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingDefensePlayerRequestLocal(CSLoadingDefensePlayerRequest request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestDefensePlayers(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingAiUnitRequestLocal(CSLoadingAiUnitRequest request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestAiUnits(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingCompleteLocal(CSLoadingComplete complete, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnClientLoadingComplete(clientId);
    }

    [Rpc]
    private void ServerReceiveSpectateLocal(CSSpectate request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SpectateRequest(clientId, request != null ? request.targetClientId : -1);
    }

    [Rpc]
    private void ServerReceiveMinimapRadiusSwitchLocal(CSMinimapRadiusSwitch request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SwitchMinimapRadius(clientId);
    }

    [Rpc]
    private void ServerReceiveCameraModeLocal(CSCameraMode request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SetCameraMode(clientId, request != null && request.topDown);
    }

    [Rpc]
    private void ServerReceiveFreeCameraLocal(CSFreeCamera request, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveFreeCamera(clientId, request);
    }

    [Rpc]
    private void ServerReceiveChatLocal(CSChat chat, int clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveChat(clientId, chat);
    }

    [Rpc]
    private void ServerReceiveGmLocal(CSGm gm, int clientId)
    {
        if (gm == null) return;
        if (Tool.BattleManager != null) Tool.BattleManager.ExecuteGm(clientId, gm);
    }
    #endregion

    #region//[Rpc] 客户端侧接收（服务器 → 客户端）
    #region 加载期
    [Rpc]
    private void ClientReceiveLoadingStartLocal(SCLoadingStart info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnLoadingStart(info);
    }

    [Rpc]
    private void ClientReceiveLoadingPreparedLocal(SCLoadingPrepared info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnLoadingPrepared(info);
    }

    [Rpc]
    private void ClientReceiveLoadingAttackPlayerResponseLocal(SCLoadingAttackPlayerResponse info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnAttackPlayerResponse(info);
    }

    [Rpc]
    private void ClientReceiveLoadingDefensePlayerResponseLocal(SCLoadingDefensePlayerResponse info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnDefensePlayerResponse(info);
    }

    [Rpc]
    private void ClientReceiveLoadingAiUnitResponseLocal(SCLoadingAiUnitResponse info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnAiUnitResponse(info);
    }

    [Rpc]
    private void ClientReceiveBattleStartLocal(SCBattleStart info)
    {
        if (info == null || Tool.ClientLogicManager == null) return;
        Tool.ClientLogicManager.Loading.OnBattleStart();
    }
    #endregion

    [Rpc]
    private void ClientReceiveEntityDisplayLocal(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        var logic = Tool.ClientLogicManager;
        if (logic != null) logic.EntityPlayers.OnEntityDisplay(info);
        else EventManager.TrigEvent(ClientEvent.OnEntityDisplayUpdate, info);
    }

    [Rpc]
    private void ClientReceiveEntityAnimLocal(SCEntityAnimInfo info)
    {
        if (info == null) return;
        if (Tool.ClientLogicManager != null) Tool.ClientLogicManager.EntityPlayers.OnEntityAnim(info);
    }

    [Rpc]
    private void ClientRemoveEntityLocal(int entityId)
    {
        var logic = Tool.ClientLogicManager;
        if (logic != null) logic.EntityPlayers.OnRemoveEntity(entityId);
        else EventManager.TrigEvent(ClientEvent.OnEntityDisplayRemove, entityId);
    }

    [Rpc]
    private void ClientReceiveMinimapEntityLocal(SCMinimapEntity e)
    {
        if (e == null) return;
        EventManager.TrigEvent(ClientEvent.OnMinimapUpdate, e);
    }

    [Rpc]
    private void ClientReceiveMinimapRadiusLocal(SCMinimapRadius info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnMinimapRadiusUpdate, info.radius);
    }

    [Rpc]
    private void ClientReceiveBattleEventLocal(SCBattleEvent e)
    {
        if (e == null) return;
        EventManager.TrigEvent(ClientEvent.OnBattleEvent, e);
    }

    [Rpc]
    private void ClientReceivePromptLocal(SCPrompt p)
    {
        if (p == null) return;
        EventManager.TrigEvent(ClientEvent.OnShowPrompt, p.messageId);
    }

    [Rpc]
    private void ClientReceiveSkillUsedLocal(SCSkillUsed info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnSkillUsed, info.skillId);
    }

    [Rpc]
    private void ClientReceiveSkillSyncLocal(SCSkillSync info)
    {
        if (info == null) return;
        if (Tool.ClientLogicManager == null) return;
        var skills = Tool.ClientLogicManager.SkillVfx;
        if (skills == null) return;
        if (info.full) skills.SetHeldSkills(info.skills);
        else
        {
            foreach (var skillId in info.skills) skills.AddHeldSkill(skillId);
        }
    }

    [Rpc]
    private void ClientReceiveCombatLabelLocal(SCCombatLabel label)
    {
        if (label == null) return;
        EventManager.TrigEvent(ClientEvent.OnCombatLabelDisplay, label);
    }

    [Rpc]
    private void ClientReceiveScoreInfoLocal(SCScoreInfo info)
    {
        if (info == null) return;
        var logic = Tool.ClientLogicManager;
        if (logic != null) logic.BattleTime.OnScoreUpdate(info);
        else EventManager.TrigEvent(ClientEvent.OnScoreUpdate, info);
    }

    [Rpc]
    private void ClientReceiveReviveInfoLocal(SCReviveInfo info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnReviveProgressUpdate, info);
    }

    [Rpc]
    private void ClientReceiveLobbyInfoLocal(SCLobbyInfo info)
    {
        if (info == null) return;
        hasLobbyInfo = true; // 已收到服务器房间状态，允许发送大厅命令（选队/开始/输入）
        BattleRunning = info.battleStarted;
        MergeLobbyMembers(info); // 增量合并成完整成员表，UI 与聊天仍按全量快照消费
        LatestLobbyInfo = info; // 缓存最新组队状态（成员/阵营/AI 数量；组队期专用，战斗期不读）
        EventManager.TrigEvent(ClientEvent.OnLobbyInfoUpdate, info);
    }

    public static SCLobbyInfo LatestLobbyInfo { get; private set; }

    // 组队期成员表：服务器只发增量，这里合并成完整快照
    private static readonly Dictionary<int, SCLobbyInfo.LobbyMemberInfo> lobbyMembers = new();

    private static void MergeLobbyMembers(SCLobbyInfo info)
    {
        if (info.full) lobbyMembers.Clear();
        if (info.members != null)
        {
            foreach (var member in info.members)
            {
                if (member == null) continue;
                if (member.removed) lobbyMembers.Remove(member.clientId);
                else lobbyMembers[member.clientId] = member;
            }
        }
        info.members.Clear();
        foreach (var member in lobbyMembers.Values) info.members.Add(member);
        info.members.Sort((a, b) => a.clientId.CompareTo(b.clientId));
    }

    // clientId → 名字：组队期专用（战斗期名字来自加载期请求的数据）
    private static readonly Dictionary<int, string> lobbyPlayerNames = new();

    [Rpc]
    private void ClientReceiveLobbyPlayerNameLocal(SCLobbyPlayerName info)
    {
        if (info == null) return;
        lobbyPlayerNames[info.clientId] = info.name;
    }

    public static bool TryGetLobbyPlayerName(int clientId, out string name)
    {
        return lobbyPlayerNames.TryGetValue(clientId, out name) && !string.IsNullOrEmpty(name);
    }

    [Rpc]
    private void ClientReceiveCampAliveCountLocal(SCCampAliveCount info)
    {
        if (info == null) return;
        LatestCampAliveCount = info; // 缓存最新双方人数（战斗页进入时取初值）
        EventManager.TrigEvent(ClientEvent.OnCampAliveCount, info);
    }

    public static SCCampAliveCount LatestCampAliveCount { get; private set; }

    [Rpc]
    private void ClientReceiveDefensePointProgressLocal(SCDefensePointProgress info)
    {
        if (info == null) return;
        LatestDefensePointProgress = info; // 缓存最新进度（战斗页进入时取初值）
        EventManager.TrigEvent(ClientEvent.OnDefensePointProgress, info);
    }

    public static SCDefensePointProgress LatestDefensePointProgress { get; private set; }

    // 组队期的成员名字（战斗期请走 ClientLoadingManager，不要读这里的组队缓存）
    public static string GetLobbyMemberName(int clientId)
    {
        if (TryGetLobbyPlayerName(clientId, out var name)) return name;
        return $"玩家{clientId}";
    }

    [Rpc]
    private void ClientReceiveDayNightInfoLocal(SCDayNightInfo info)
    {
        if (info == null) return;
        var logic = Tool.ClientLogicManager;
        if (logic != null) logic.BattleTime.OnDayNightSync(info);
        else if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.ApplyServerSync(info.cycleTime, info.dayDuration, info.nightDuration);
    }

    [Rpc]
    private void ClientUseSkillLocal(int skillId, SkillContext context)
    {
        if (context == null) return;
        var logic = Tool.ClientLogicManager;
        if (logic != null) logic.SkillVfx.OnSkillCast(skillId, context);
        else SkillManager.PlayVFX(skillId, context);
    }

    [Rpc]
    private void ClientReceiveChatLocal(SCChat chat)
    {
        if (chat == null) return;
        EventManager.TrigEvent(ClientEvent.OnChatMessage, chat);
    }

    [Rpc]
    private void ClientReceiveGmLocal(SCGm gm)
    {
        if (gm == null) return;
        EventManager.TrigEvent(ClientEvent.OnGmResult, gm);
    }
    #endregion
}
