using Ros.Transport;
using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 网络行为脚本（继承 EnsBehaviour，partial 供代码生成）。
/// 本项目唯一进行服务器、客户端调用的位置（架构说明）。
/// 【注意】运行前需在 Unity 菜单执行 Ens - Generate Code 重新生成 EnsNetcode/Gen/NetworkManager.Generated.cs。
/// </summary>
public partial class NetworkManager : EnsBehaviour
{
    private void Awake()
    {
        Tool.NetworkManager = this;
        if (Application.platform != RuntimePlatform.WindowsServer && GetComponent<InputManager>() == null)
        {
            gameObject.AddComponent<InputManager>();
        }
        ResetClientNetworkInfo();
        ClientBindEvents();
    }

    public enum ConnectResult
    {
        Success,
        Failed,
        TooFrequent
    }

    /// <summary>服务器返回的握手信息（房间事件 0）。</summary>
    public static string serverHello;

    /// <summary>本地玩家开局信息（服务器下发）。</summary>
    public static SCBattleInfo battleInfo;

    #region//Client 连接流程
    private static bool connecting;
    private static bool rejected;
    private static bool connected;
    private static bool tryingEnterWorld;
    private static bool hasRoomInfo;
    private static bool intentionalExitWorld;

    /// <summary>是否可以发送世界内命令（已进入房间）。</summary>
    public static bool CanSendWorldCommand => connected && hasRoomInfo;

    private void ResetClientNetworkInfo()
    {
        connecting = false;
        rejected = false;
        connected = false;
        serverHello = null;
        battleInfo = null;
        hasRoomInfo = false;
        tryingEnterWorld = false;
        intentionalExitWorld = false;
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

    /// <summary>尝试连接服务器（ipAddress 为空则连接本机）。</summary>
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
            connecting = false;
            Debug.LogError("Ens层连接失败");
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

    /// <summary>进入世界（加入房间并上报选角信息）。</summary>
    public void EnterWorld()
    {
        if (tryingEnterWorld) return;
        tryingEnterWorld = true;
        StartCoroutine(EnterWorldFallBack());
        Ens.Request.Client.JoinRoom.SendRequest(EnsRoomManager.roomIdStart);
    }

    /// <summary>退出世界。</summary>
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
        // 双方角色各自选择并随 CSPlayerInfo 上报；阵营完全在房间内确定，服务器按最终阵营取对应一侧
        var info = new CSPlayerInfo()
        {
            attackCharacter = EntityType.Attack(Mathf.Clamp(ClientSelection.selectedAttackIndex, 0, Config.attack_character_count - 1)),
            attackLevel = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(ClientSelection.selectedAttackIndex) : 1,
            defenseCharacter = EntityType.Defense(Mathf.Clamp(ClientSelection.selectedDefenseIndex, 0, Config.defense_character_count - 1)),
            defenseLevel = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(Config.attack_character_count + ClientSelection.selectedDefenseIndex) : 1,
        };
        CallFuncRpc(ServerReceivePlayerInfoLocal, SendTo.RoomOwner, Delivery.Reliable, info, EnsInstance.LocalClientId);
    }
    #endregion

    #region//发送封装：客户端 → 服务器
    /// <summary>发送输入命令（高频，不可靠）。</summary>
    public void SendInputCommand(CSInputCommand command)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveInputCommandLocal, SendTo.RoomOwner, Delivery.Unreliable, command, EnsInstance.LocalClientId);
    }

    /// <summary>发送技能释放请求。</summary>
    public void SendUseSkill(CSUseSkillRequest request)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveUseSkillLocal, SendTo.RoomOwner, Delivery.Strive, request, EnsInstance.LocalClientId);
    }

    /// <summary>发送组队大厅状态更新（选队 / AI 数量）。</summary>
    public void SendRoomUpdate(CSRoomUpdate update)
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveRoomUpdateLocal, SendTo.RoomOwner, Delivery.Reliable, update, EnsInstance.LocalClientId);
    }

    /// <summary>发送开始对局请求。</summary>
    public void SendStartRequest()
    {
        if (!CanSendWorldCommand) return;
        CallFuncRpc(ServerReceiveStartRequestLocal, SendTo.RoomOwner, Delivery.Reliable, new CSStartRequest(), EnsInstance.LocalClientId);
    }
    #endregion

    #region//发送封装：服务器 → 客户端
    /// <summary>发送开局信息（定向）。</summary>
    public void SendBattleInfo(short clientId, SCBattleInfo info)
    {
        CallFuncRpc(ClientReceiveBattleInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    /// <summary>发送实体表现（定向）。</summary>
    public void SendEntityDisplay(short clientId, SCEntityDisplayInfo info)
    {
        CallFuncRpc(ClientReceiveEntityDisplayLocal, SendTo.To(clientId), Delivery.Unreliable, info);
    }

    /// <summary>移除实体（定向）。</summary>
    public void SendRemoveEntity(short clientId, int entityId)
    {
        CallFuncRpc(ClientRemoveEntityLocal, SendTo.To(clientId), Delivery.Reliable, entityId);
    }

    /// <summary>发送战斗事件（定向或广播）。</summary>
    public void SendBattleEvent(short clientId, SCBattleEvent e)
    {
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.To(clientId), Delivery.Reliable, e);
    }

    /// <summary>发送战斗事件（广播，含消息 id 飘字）。</summary>
    public void SendBattleEvent(byte type, ushort targetId = 0, byte messageId = 0)
    {
        var e = new SCBattleEvent() { type = type, targetId = targetId, value = messageId };
        CallFuncRpc(ClientReceiveBattleEventLocal, SendTo.Everyone, Delivery.Reliable, e);
    }

    /// <summary>发送分数（定向）。</summary>
    public void SendScoreInfo(short clientId, SCScoreInfo info)
    {
        CallFuncRpc(ClientReceiveScoreInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    /// <summary>发送复活进度（定向）。</summary>
    public void SendReviveInfo(short clientId, SCReviveInfo info)
    {
        CallFuncRpc(ClientReceiveReviveInfoLocal, SendTo.To(clientId), Delivery.Reliable, info);
    }

    /// <summary>发送房间状态（全房间广播）。</summary>
    public void SendRoomInfo(SCRoomInfo info)
    {
        CallFuncRpc(ClientReceiveRoomInfoLocal, SendTo.Everyone, Delivery.Reliable, info);
    }
    #endregion

    #region//[Rpc] 服务器侧接收（客户端 → 服务器）
    /// <summary>服务器：接收玩家进场选角。</summary>
    [Rpc]
    private void ServerReceivePlayerInfoLocal(CSPlayerInfo info, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.AddPlayer(clientId, info);
    }

    /// <summary>服务器：接收输入命令。</summary>
    [Rpc]
    private void ServerReceiveInputCommandLocal(CSInputCommand command, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveInputCommand(clientId, command);
    }

    /// <summary>服务器：接收技能释放请求。</summary>
    [Rpc]
    private void ServerReceiveUseSkillLocal(CSUseSkillRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveUseSkill(clientId, request);
    }

    /// <summary>服务器：接收退出世界。</summary>
    [Rpc]
    private void ServerReceiveExitWorldLocal(short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.RemovePlayer(clientId);
    }

    /// <summary>服务器：接收组队大厅状态更新（选队 / AI 数量）。</summary>
    [Rpc]
    private void ServerReceiveRoomUpdateLocal(CSRoomUpdate update, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveRoomUpdate(clientId, update);
    }

    /// <summary>服务器：接收开始对局请求。</summary>
    [Rpc]
    private void ServerReceiveStartRequestLocal(CSStartRequest request, short clientId)
    {
        if (Tool.BattleManager != null) Tool.BattleManager.ReceiveStartRequest(clientId, request);
    }
    #endregion

    #region//[Rpc] 客户端侧接收（服务器 → 客户端）
    /// <summary>客户端：接收开局信息。</summary>
    [Rpc]
    private void ClientReceiveBattleInfoLocal(SCBattleInfo info)
    {
        if (info == null) return;
        battleInfo = info;
        EventManager.TrigEvent(ClientEvent.OnBattleStart, (int)info.camp);
    }

    /// <summary>客户端：接收实体表现。</summary>
    [Rpc]
    private void ClientReceiveEntityDisplayLocal(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (Tool.ClientDisplayManager != null) Tool.ClientDisplayManager.OnEntityDisplay(info);
        else EventManager.TrigEvent(ClientEvent.OnEntityDisplayUpdate, info);
    }

    /// <summary>客户端：移除实体。</summary>
    [Rpc]
    private void ClientRemoveEntityLocal(int entityId)
    {
        if (Tool.ClientDisplayManager != null) Tool.ClientDisplayManager.OnRemoveEntity(entityId);
        else EventManager.TrigEvent(ClientEvent.OnEntityDisplayRemove, entityId);
    }

    /// <summary>客户端：接收战斗事件。</summary>
    [Rpc]
    private void ClientReceiveBattleEventLocal(SCBattleEvent e)
    {
        if (e == null) return;
        EventManager.TrigEvent(ClientEvent.OnBattleEvent, e);
    }

    /// <summary>客户端：接收分数。</summary>
    [Rpc]
    private void ClientReceiveScoreInfoLocal(SCScoreInfo info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnScoreUpdate, info);
    }

    /// <summary>客户端：接收复活进度。</summary>
    [Rpc]
    private void ClientReceiveReviveInfoLocal(SCReviveInfo info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnReviveProgressUpdate, info);
    }

    /// <summary>客户端：接收房间状态（组队大厅）。</summary>
    [Rpc]
    private void ClientReceiveRoomInfoLocal(SCRoomInfo info)
    {
        if (info == null) return;
        EventManager.TrigEvent(ClientEvent.OnRoomInfoUpdate, info);
    }
    #endregion
}
