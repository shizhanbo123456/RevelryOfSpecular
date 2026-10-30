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

    public static bool CanSendWorldCommand => connected && hasLobbyInfo;

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

        EnsInstance.OnServerDisconnect += () =>
        {
            if (intentionalExitWorld)
            {
                intentionalExitWorld = false;
                return;
            }
            EventManager.TrigEvent(ClientEvent.OnRestartGame);
        };
    }

    public async Task<ConnectResult> TryConnect(string ipAddress)
    {
        if (connecting) return ConnectResult.TooFrequent;
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
            ClientRoomManagerEventCenter.TrigRpcEvent(Delivery.Reliable, 0, EnsInstance.LocalClientId.ToString());
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
            CallFuncRpc(ServerReceiveExitWorldLocal, SendTo.RoomOwner, Delivery.Reliable, EnsInstance.LocalClientId);
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
        CallFuncRpc(ServerReceivePlayerInfoLocal, SendTo.RoomOwner, Delivery.Reliable, info, EnsInstance.LocalClientId);
    }
    #endregion

    #region//发送封装：客户端 → 服务器
    public void SendInput(CSPlayerInput input)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveInputLocal, SendTo.RoomOwner, Delivery.Strive, input, EnsInstance.LocalClientId);
    }

    // 技能键：客户端已把按键解析成技能 id，服务器只做持有/沉默/强控校验
    public void SendSkillCastInput(int skillId)
    {
        if (!CanSendWorldCommand || skillId < 0) return;
        CallFuncRpc(ServerReceiveSkillCastInputLocal, SendTo.RoomOwner, Delivery.Strive, new CSSkillCast() { skillId = skillId }, EnsInstance.LocalClientId);
    }

    public void SendLobbyUpdate(CSLobbyUpdate update)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLobbyUpdateLocal, SendTo.RoomOwner, Delivery.Reliable, update, EnsInstance.LocalClientId);
    }

    #region 加载期请求（客户端 → 服务器，逐步发送）
    public void SendLoadingAttackPlayerRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingAttackPlayerRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingAttackPlayerRequest(), EnsInstance.LocalClientId);
    }

    public void SendLoadingDefensePlayerRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingDefensePlayerRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingDefensePlayerRequest(), EnsInstance.LocalClientId);
    }

    public void SendLoadingAiUnitRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingAiUnitRequestLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingAiUnitRequest(), EnsInstance.LocalClientId);
    }

    public void SendLoadingComplete()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveLoadingCompleteLocal, SendTo.RoomOwner, Delivery.Reliable,
            new CSLoadingComplete(), EnsInstance.LocalClientId);
    }
    #endregion

    public void SendMinimapRadiusSwitch()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveMinimapRadiusSwitchLocal, SendTo.RoomOwner, Delivery.Reliable, new CSMinimapRadiusSwitch(), EnsInstance.LocalClientId);
    }

    // 相机模式属于移动语义的一部分（俯视 = 四向），切模式即同步一次
    public void SendCameraMode(bool topDown)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveCameraModeLocal, SendTo.RoomOwner, Delivery.Reliable, new CSCameraMode() { topDown = topDown }, EnsInstance.LocalClientId);
    }

    // 观战自由视角：位置上报（仅纯观战方），由 CameraController 按 1s / 1m 阈值节流
    public void SendFreeCamera(CSFreeCamera info)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveFreeCameraLocal, SendTo.RoomOwner, Delivery.Reliable, info, EnsInstance.LocalClientId);
    }

    public void SendChat(CSChat chat)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveChatLocal, SendTo.RoomOwner, Delivery.Reliable, chat, EnsInstance.LocalClientId);
    }

    public void SendGm(CSGm gm)
    {
        if (gm == null || !CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveGmLocal, SendTo.RoomOwner, Delivery.Reliable, gm, EnsInstance.LocalClientId);
    }
    #endregion

    #region//发送封装：服务器 → 客户端
    private static bool HasClient(short clientId) => clientId >= 0;

    #region 加载期下发（服务器 → 客户端）
    public void SendLoadingStart(short clientId, SCLoadingStart info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLoadingStartLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendLoadingPrepared(short clientId, SCLoadingPrepared info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLoadingPreparedLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendLoadingAttackPlayerResponse(short clientId, SCLoadingAttackPlayerResponse info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLoadingAttackPlayerResponseLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendLoadingDefensePlayerResponse(short clientId, SCLoadingDefensePlayerResponse info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLoadingDefensePlayerResponseLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendLoadingAiUnitResponse(short clientId, SCLoadingAiUnitResponse info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLoadingAiUnitResponseLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendBattleStart(short clientId)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveBattleStartLocal, SendTo.To(clientId), Delivery.Reliable, new SCBattleStart());
    }
    #endregion

    public void SendEntityDisplay(short clientId, SCEntityDisplayInfo info)
    {
        if (!HasClient(clientId)) return;
        // 含 buff 信息的包走尽力传输（丢一包下轮脏标记还会补）；纯位姿包走不可靠（丢了等下个快包）
        var delivery = info.includeRuntime || info.hasBuffs ? Delivery.Strive : Delivery.Unreliable;
        CallFuncRpc(ClientReceiveEntityDisplayLocal, SendTo.To(clientId), delivery, info);
    }

    public void SendEntityAnim(short clientId, SCEntityAnimInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveEntityAnimLocal, SendTo.To(clientId), Delivery.Strive, info);
    }

    public void SendRemoveEntity(short clientId, int entityId)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientRemoveEntityLocal, SendTo.To(clientId), Delivery.Reliable, entityId);
    }

    public void SendMinimapEntity(short clientId, SCMinimapEntity e)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveMinimapEntityLocal, SendTo.To(clientId), Delivery.Unreliable, e);
    }

    public void SendMinimapRadius(short clientId, float radius)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveMinimapRadiusLocal, SendTo.To(clientId), Delivery.Reliable, new SCMinimapRadius() { radius = radius });
    }

    public void SendBattleEvent(short clientId, SCBattleEvent e)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.To(clientId), Delivery.Strive, e);
    }

    public void SendBattleEvent(SCBattleEvent e)
    {
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.ExcludeSender, Delivery.Strive, e);
    }

    public void SendPrompt(short clientId, int messageId)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceivePromptLocal, SendTo.To(clientId), Delivery.Reliable, new SCPrompt() { messageId = messageId });
    }

    // 技能受理成功回执：只回施法者，客户端收到才扣一次可用次数
    public void SendSkillUsed(short clientId, int skillId)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveSkillUsedLocal, SendTo.To(clientId), Delivery.Reliable, new SCSkillUsed() { skillId = skillId });
    }

    public void SendCombatLabel(short clientId, SCCombatLabel label)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveCombatLabelLocal, SendTo.To(clientId), Delivery.Unreliable, label);
    }

    public void SendCombatLabel(SCCombatLabel label)
    {
        CallFuncRpc(ClientReceiveCombatLabelLocal, SendTo.ExcludeSender, Delivery.Unreliable, label);
    }

    public void SendScoreInfo(short clientId, SCScoreInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveScoreInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendReviveInfo(short clientId, SCReviveInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveReviveInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendLobbyInfo(SCLobbyInfo info)
    {
        CallFuncRpc(ClientReceiveLobbyInfoLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    // 名字广播（新玩家进房时发给所有人；组队期专用）
    public void SendLobbyPlayerName(SCLobbyPlayerName info)
    {
        CallFuncRpc(ClientReceiveLobbyPlayerNameLocal, SendTo.ExcludeSender, Delivery.Reliable, info);
    }

    // 名字定向（给新玩家补发房间里每个玩家的名字；组队期专用）
    public void SendLobbyPlayerName(short clientId, SCLobbyPlayerName info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveLobbyPlayerNameLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendPlayerEntity(short clientId, int entityId)
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
        CallFuncRpc(ServerReceiveSpectateLocal, SendTo.RoomOwner, Delivery.Reliable, request, EnsInstance.LocalClientId);
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

    public void SendDayNightInfo(short clientId, SCDayNightInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveDayNightInfoLocal, SendTo.To(clientId), Delivery.Strive, info);
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
    public void SendGm(short clientId, SCGm gm)
    {
        if (gm == null || !HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveGmLocal, SendTo.To(clientId), Delivery.Reliable, gm);
    }
    #endregion

    #region//[Rpc] 服务器侧接收（客户端 → 服务器）
    [Rpc]
    private void ServerReceivePlayerInfoLocal(CSLobbyPlayerInfo info, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.AddPlayer(clientId, info);
    }

    [Rpc]
    private void ServerReceiveInputLocal(CSPlayerInput input, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveInput(clientId, input);
    }

    [Rpc]
    private void ServerReceiveSkillCastInputLocal(CSSkillCast cast, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveSkillCast(clientId, cast.skillId);
    }

    [Rpc]
    private void ServerReceiveExitWorldLocal(short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.RemovePlayer(clientId);
    }

    [Rpc]
    private void ServerReceiveLobbyUpdateLocal(CSLobbyUpdate update, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveLobbyUpdate(clientId, update);
    }

    [Rpc]
    private void ServerReceiveLoadingAttackPlayerRequestLocal(CSLoadingAttackPlayerRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestAttackPlayers(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingDefensePlayerRequestLocal(CSLoadingDefensePlayerRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestDefensePlayers(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingAiUnitRequestLocal(CSLoadingAiUnitRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnLoadingRequestAiUnits(clientId);
    }

    [Rpc]
    private void ServerReceiveLoadingCompleteLocal(CSLoadingComplete complete, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.OnClientLoadingComplete(clientId);
    }

    [Rpc]
    private void ServerReceiveSpectateLocal(CSSpectate request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SpectateRequest(clientId, request != null ? request.targetClientId : -1);
    }

    [Rpc]
    private void ServerReceiveMinimapRadiusSwitchLocal(CSMinimapRadiusSwitch request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SwitchMinimapRadius(clientId);
    }

    [Rpc]
    private void ServerReceiveCameraModeLocal(CSCameraMode request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SetCameraMode(clientId, request != null && request.topDown);
    }

    [Rpc]
    private void ServerReceiveFreeCameraLocal(CSFreeCamera request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveFreeCamera(clientId, request);
    }

    [Rpc]
    private void ServerReceiveChatLocal(CSChat chat, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveChat(clientId, chat);
    }

    [Rpc]
    private void ServerReceiveGmLocal(CSGm gm, short clientId)
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
        LatestLobbyInfo = info; // 缓存最新组队状态（成员/阵营/AI 数量；组队期专用，战斗期不读）
        EventManager.TrigEvent(ClientEvent.OnLobbyInfoUpdate, info);
    }

    public static SCLobbyInfo LatestLobbyInfo { get; private set; }

    // clientId → 名字：组队期专用（战斗期名字来自加载期请求的数据）
    private static readonly Dictionary<short, string> lobbyPlayerNames = new();

    [Rpc]
    private void ClientReceiveLobbyPlayerNameLocal(SCLobbyPlayerName info)
    {
        if (info == null) return;
        lobbyPlayerNames[info.clientId] = info.name;
    }

    public static bool TryGetLobbyPlayerName(int clientId, out string name)
    {
        return lobbyPlayerNames.TryGetValue((short)clientId, out name) && !string.IsNullOrEmpty(name);
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
