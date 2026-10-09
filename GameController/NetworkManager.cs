using Ros.Transport;
using System;
using System.Collections;
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

    public static SCBattleInfo battleInfo;

    public static bool BattleRunning { get; private set; }

    #region//Client 连接流程
    private static bool connecting;
    private static bool rejected;
    private static bool connected;
    private static bool tryingEnterWorld;
    private static bool hasRoomInfo;
    private static bool intentionalExitWorld;

    public static bool CanSendWorldCommand => connected && hasRoomInfo;

    private void ResetClientNetworkInfo()
    {
        connecting = false;
        rejected = false;
        connected = false;
        serverHello = null;
        battleInfo = null;
        BattleRunning = false;
        hasRoomInfo = false;
        tryingEnterWorld = false;
        intentionalExitWorld = false;
        LatestRoomInfo = null; // 跨连接清空，防止重连后大厅显示上一局成员
    }

    private void ClientBindEvents()
    {
        EnsInstance.OnConnectionRejected += () => rejected = true;
        EnsInstance.OnServerConnect += () => connected = true;
        // 房间事件 0：服务器握手（服务器信息可达性确认）
        ClientRoomManagerEventCenter.Register(0, data =>
        {
            serverHello = data;
        });
        Ens.Request.Client.JoinRoom.OnRecvReply += ClientSendInfo;

        EnsInstance.OnServerDisconnect += () =>
        {
            if (intentionalExitWorld)
            {
                intentionalExitWorld = false;
                return;
            }
            EventManager.TrigEvent(ClientEvent.OnRestartGame);
        };
        Ens.Request.Client.JoinRoom.OnTimeOut += () => EventManager.TrigEvent(ClientEvent.OnRestartGame);
    }

    public async Task<ConnectResult> TryConnect(string ipAddress)
    {
        if (connecting) return ConnectResult.TooFrequent;
        ResetClientNetworkInfo();
        connecting = true;
        if (ipAddress == string.Empty)
        {
            ipAddress = IPAddress.Loopback.ToString();
        }
        else if (!IPAddress.TryParse(ipAddress, out IPAddress _))
        {
            connecting = false;
            Debug.LogError("输入IP地址错误");
            return ConnectResult.Failed;
        }
        EnsInstance.Corr.IP = ipAddress;
        EnsInstance.Corr.ShutDown();
        EnsInstance.Corr.StartClient();
        float t = Time.time;
        while (!rejected && !connected && Time.time < t + 5f)
        {
            await Task.Delay(100);
        }
        if (!connected)
        {
            Debug.LogError($"Ens层连接失败 rejected={rejected}");
            return ConnectResult.Failed;
        }
        ClientRoomManagerEventCenter.TrigEvent(Delivery.Reliable, 0, EnsInstance.LocalClientId.ToString());
        t = Time.time;
        while (serverHello == null && Time.time < t + 5f)
        {
            await Task.Delay(100);
        }
        if (serverHello == null)
        {
            connecting = false;
            Debug.LogError("服务器信息获取失败");
            return ConnectResult.Failed;
        }
        EventManager.TrigEvent(ClientEvent.OnConnect);
        connecting = false;
        return ConnectResult.Success;
    }

    public void EnterWorld()
    {
        if (tryingEnterWorld) return;
        tryingEnterWorld = true;
        StartCoroutine(EnterWorldFallBack());
        Ens.Request.Client.JoinRoom.SendRequest(EnsRoomManager.roomIdStart);
    }

    public void ExitWorld()
    {
        intentionalExitWorld = true;
        if (EnsInstance.LocalClientId >= 0 && EnsInstance.PresentRoomId != 0)
        {
            CallFuncRpc(ServerReceiveExitWorldLocal, SendTo.RoomOwner, Delivery.Reliable, EnsInstance.LocalClientId);
            EnsInstance.Corr.FlushSendBufferNow();
        }
        hasRoomInfo = false;
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
        tryingEnterWorld = false;
        // 双方角色各自选择并随 CSPlayerInfo 上报；阵营完全在房间内确定，服务器按最终阵营取对应一侧
        var info = new CSPlayerInfo()
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
        CallFuncRpc(ServerReceiveInputLocal, SendTo.RoomOwner, Delivery.Reliable, input, EnsInstance.LocalClientId);
    }

    public void SendRoomUpdate(CSRoomUpdate update)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveRoomUpdateLocal, SendTo.RoomOwner, Delivery.Reliable, update, EnsInstance.LocalClientId);
    }

    public void SendStartRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveStartRequestLocal, SendTo.RoomOwner, Delivery.Reliable, new CSStartRequest(), EnsInstance.LocalClientId);
    }

    public void SendMinimapRadiusSwitch()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveMinimapRadiusSwitchLocal, SendTo.RoomOwner, Delivery.Reliable, new CSMinimapRadiusSwitch(), EnsInstance.LocalClientId);
    }
    #endregion

    #region//发送封装：服务器 → 客户端
    private static bool HasClient(short clientId) => clientId >= 0;

    public void SendBattleInfo(short clientId, SCBattleInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveBattleInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendEntityDisplay(short clientId, SCEntityDisplayInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveEntityDisplayLocal, SendTo.To(clientId), Delivery.Unreliable, info);
    }

    public void SendEntityAnim(short clientId, SCEntityAnimInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveEntityAnimLocal, SendTo.To(clientId), Delivery.Reliable, info);
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
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.To(clientId), Delivery.Reliable, e);
    }

    public void SendBattleEvent(byte type, byte messageId = 0)
    {
        var e = new SCBattleEvent() { type = type, value = messageId };
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.Everyone, Delivery.Reliable, e);
    }

    public void SendBattleEvent(SCBattleEvent e)
    {
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.Everyone, Delivery.Reliable, e);
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

    public void SendRoomInfo(SCRoomInfo info)
    {
        CallFuncRpc(ClientReceiveRoomInfoLocal, SendTo.Everyone, Delivery.Reliable, info);
    }

    public void SendDayNightInfo(short clientId, SCDayNightInfo info)
    {
        if (!HasClient(clientId)) return;
        CallFuncRpc(ClientReceiveDayNightInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    public void SendDayNightInfo(SCDayNightInfo info)
    {
        CallFuncRpc(ClientReceiveDayNightInfoLocal, SendTo.Everyone, Delivery.Reliable, info);
    }

    public void SendSkillCast(int skillId, SkillContext context)
    {
        CallFuncRpc(ClientUseSkillLocal, SendTo.Everyone, Delivery.Reliable, skillId, context);
    }
    #endregion

    #region//[Rpc] 服务器侧接收（客户端 → 服务器）
    [Rpc]
    private void ServerReceivePlayerInfoLocal(CSPlayerInfo info, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.AddPlayer(clientId, info);
    }

    [Rpc]
    private void ServerReceiveInputLocal(CSPlayerInput input, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveInput(clientId, input);
    }

    [Rpc]
    private void ServerReceiveExitWorldLocal(short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.RemovePlayer(clientId);
    }

    [Rpc]
    private void ServerReceiveRoomUpdateLocal(CSRoomUpdate update, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveRoomUpdate(clientId, update);
    }

    [Rpc]
    private void ServerReceiveStartRequestLocal(CSStartRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveStartRequest(clientId, request);
    }

    [Rpc]
    private void ServerReceiveMinimapRadiusSwitchLocal(CSMinimapRadiusSwitch request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.SwitchMinimapRadius(clientId);
    }
    #endregion

    #region//[Rpc] 客户端侧接收（服务器 → 客户端）
    [Rpc]
    private void ClientReceiveBattleInfoLocal(SCBattleInfo info)
    {
        if (info == null) return;
        battleInfo = info;
        EventManager.TrigEvent(ClientEvent.OnBattleStart, (int)info.camp);
    }

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
    private void ClientReceiveRoomInfoLocal(SCRoomInfo info)
    {
        if (info == null) return;
        hasRoomInfo = true; // 已收到服务器房间状态，允许发送大厅命令（选队/开始/输入）
        BattleRunning = info.battleStarted;
        LatestRoomInfo = info; // 缓存最新房间状态（战斗期头顶名字按 clientId 反查）
        EventManager.TrigEvent(ClientEvent.OnRoomInfoUpdate, info);
    }

    public static SCRoomInfo LatestRoomInfo { get; private set; }

    public static string GetMemberName(int clientId)
    {
        SCRoomInfo.RoomMemberInfo member = null;
        if (LatestRoomInfo != null) member = LatestRoomInfo.members.Find(m => m != null && m.clientId == clientId);
        if (member == null || string.IsNullOrEmpty(member.name)) return $"玩家{clientId}";
        return member.name;
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
    #endregion
}
