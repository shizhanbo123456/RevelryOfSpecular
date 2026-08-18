using Ros.Transport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

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
    #region//Client
    private static bool connecting;
    private static bool rejected;
    private static bool connected;
    public static LevelInfo levelInfo;
    public static SCPlayerInfo roomInfo;
    public static CSPlayerInfo playerInfo;
    private static bool tryingEnterWorld;
    private static bool hasRoomInfo;
    private static bool intentionalExitWorld;
    public static bool CanSendWorldCommand => playerInfo != null && hasRoomInfo;

    private void ResetClientNetworkInfo()
    {
        connecting = false;
        rejected = false;
        connected = false;
        levelInfo = null;
        playerInfo = null;
        hasRoomInfo = false;
        tryingEnterWorld = false;
        intentionalExitWorld = false;
    }
    private void ClientBindEvents()
    {
        EnsInstance.OnConnectionRejected += () => rejected = true;
        EnsInstance.OnServerConnect += () => connected = true;
        ClientRoomManagerEventCenter.Register(0, data =>
        {
            levelInfo = new LevelInfo(data);
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
        connecting=true;
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
        while (levelInfo==null && Time.time < t + 5f)
        {
            await Task.Delay(100);
        }
        if (levelInfo == null)
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
        playerInfo = CreatePlayerInfoFromHomeSelection();
        tryingEnterWorld = true;
        StartCoroutine(EnterWorldFallBack());
        Ens.Request.Client.JoinRoom.SendRequest(EnsRoomManager.roomIdStart);
    }
    public void ExitWorld()
    {
        intentionalExitWorld = true;
        if (EnsInstance.LocalClientId >= 0 && EnsInstance.PresentRoomId != 0)
        {
            CallFuncRpc(ServerReceiveExitWorld, SendTo.RoomOwner, Delivery.Reliable, EnsInstance.LocalClientId);
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
        CallFuncRpc(ServerReceivePlayerInfo, SendTo.RoomOwner, Delivery.Reliable, playerInfo, EnsInstance.LocalClientId);
    }
    private CSPlayerInfo CreatePlayerInfoFromHomeSelection()
    {
        var index = HomePage.currentSelectedCharacter;
        var imprintList = HomePage.SelectedImprints.ToList();
        if (imprintList.Count > Config.max_selected_imprint_count)
        {
            imprintList = imprintList.GetRange(0, Config.max_selected_imprint_count);
        }
        Dictionary<int, int> imprints=new Dictionary<int, int>();
        foreach (var imprint in imprintList) imprints.Add(imprint, Tool.SaveManager.imprintLevels[imprint]);
        List<int> noteCount = new();
        for(int i = 0; i < Config.note_count; i++)
        {
            noteCount.Add(Tool.SaveManager.noteCounts[i]);
        }
        var skillRunes = new Dictionary<int, int>();
        foreach (var skill in HomePage.SelectedSkillCounts.OrderBy(i => i.Key))
        {
            int skillId = skill.Key;
            int remainCount = skill.Value;
            if (skillId < 0) continue;
            if (remainCount <= 0) continue;
            skillRunes[skillId] = remainCount;
        }
        if (skillRunes.Count > Config.skill_slot_count)
        {
            skillRunes = skillRunes.Take(Config.skill_slot_count).ToDictionary(i => i.Key, i => i.Value);
        }
        return new CSPlayerInfo()
        {
            type = EntityType.Character(index),
            level = Tool.SaveManager.characterLevels[index],
            imprints=imprints,
            collectionLevels=noteCount,
            skills= skillRunes,
        };
    }
    [Rpc]
    private void ClientRecvRoomInfo(SCPlayerInfo roomInfo)
    {
        if (!tryingEnterWorld) return;
        NetworkManager.roomInfo= roomInfo;
        if (!Tool.SaveManager.TryConsumeCarriedRunes(playerInfo) ||
            !Tool.SaveManager.TryConsumeCharacterToken(HomePage.currentSelectedCharacter))
        {
            tryingEnterWorld = false;
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "入场资源消耗失败");
            ExitWorld();
            return;
        }
        hasRoomInfo = true;
        tryingEnterWorld = false;
        EventManager.TrigEvent(ClientEvent.OnEnterWorld);
    }

    public void SendMoveCommand(CSInputCommand command)
    {
        if (Application.platform == RuntimePlatform.WindowsServer) return;
        CallFuncRpc(ReceiveInputCommand, SendTo.RoomOwner, Delivery.Strive, EnsInstance.LocalClientId, command.moving, command.yaw);
    }
    public void SendUseSkillRequest(int skillId)
    {
        if (Application.platform == RuntimePlatform.WindowsServer) return;
        CallFuncRpc(ReceiveUseSkillRequest, SendTo.RoomOwner, Delivery.Reliable, EnsInstance.LocalClientId, skillId);
    }
    public void SendUseSkillRequest(int skillId, Vector3 dest)
    {
        if (Application.platform == RuntimePlatform.WindowsServer) return;
        CallFuncRpc(ReceiveUseSkillRequestWithDest, SendTo.RoomOwner, Delivery.Reliable, EnsInstance.LocalClientId, skillId, dest);
    }

    [Rpc]
    private void UpdateInfoLocal(SCEntityDisplayInfo info)
    {
        EventManager.TrigEvent(ClientEvent.UpdateEntityDisplayInfo, info);
    }
    [Rpc]
    private void RemoveInfoLocal(int id)
    {
        EventManager.TrigEvent(ClientEvent.RemoveEntityDisplayInfo, id);
    }
    [Rpc]
    private void ShowTextLocal(TextValueInfo info)
    {
        EventManager.TrigEvent<(string, Vector3, TextColor)>(ClientEvent.ShowSceneLabel, (info.value.ToString(), info.pos, info.color));
    }
    [Rpc]
    private void ShowTextLocal(TextLabelInfo info)
    {
        EventManager.TrigEvent<(string, Vector3, TextColor)>(ClientEvent.ShowSceneLabel, (info.label, info.pos, info.color));
    }
    [Rpc]
    private void UseSkillLocal(UseSkillInfo info)
    {
        //SkillManager.GetSkill(info.id).PlayVFX(info.pos, info.dest);
        SkillManager.PlayVFX(info.id, info.pos, info.dest);
    }
    [Rpc]
    private void SyncNetworkEventLocal(NetworkEvent e)
    {
        foreach(var i in e.type)
        {
            EventManager.TrigEvent(i.Key, i.Value);
        }
    }
    [Rpc]
    private void PvpKillRewardLocal(SCPvpKillRewardInfo info)
    {
        EventManager.TrigEvent(ClientEvent.PvpKillReward, info);
    }
    [Rpc]
    public void SyncTripLocal(InteractablePropInfo info)
    {
        EventManager.TrigEvent(ClientEvent.UpdateInteractablePropInfo, info);
    }
    #endregion


    #region//Server
    [Rpc]
    private void ServerReceivePlayerInfo(CSPlayerInfo info,short id)
    {
        var res=Tool.BattleManager.AddPlayerInfo(info,id);
        CallFuncRpc(ClientRecvRoomInfo, SendTo.To(id), Delivery.Reliable, res);
        CallFuncRpc(SyncTripLocal, SendTo.To(id), Delivery.Reliable, Tool.BattleManager.GetInteractablePropInfo());
    }

    [Rpc]
    private void ServerReceiveExitWorld(short id)
    {
        Tool.BattleManager.RemovePlayerInfo(id);
    }

    [Rpc]
    private void ReceiveInputCommand(short id, bool moving, float yaw)
    {
        Tool.BattleManager.ReceiveCommand(id, new CSInputCommand(moving, yaw));
    }
    [Rpc]
    private void ReceiveUseSkillRequest(short id, int skillId)
    {
        Tool.BattleManager.ReceiveUseSkill(id, skillId);
    }
    [Rpc]
    private void ReceiveUseSkillRequestWithDest(short id, int skillId, Vector3 dest)
    {
        Tool.BattleManager.ReceiveUseSkill(id, skillId, dest);
    }
    public void UpdateInfoRpc(short cid,SCEntityDisplayInfo info)
    {
        CallFuncRpc(UpdateInfoLocal, SendTo.To(cid), Delivery.Unreliable, info);
    }
    public void RemoveInfoRpc(short cid, int id)
    {
        CallFuncRpc(RemoveInfoLocal, SendTo.To(cid),Delivery.Strive, id);
    }
    public void ShowTextRpc(short cid, TextValueInfo info)
    {
        CallFuncRpc(ShowTextLocal,SendTo.To(cid),Delivery.Unreliable,info);
    }
    public void ShowTextRpc(short cid, TextLabelInfo info)
    {
        CallFuncRpc(ShowTextLocal, SendTo.To(cid), Delivery.Unreliable, info);
    }
    public void UseSkillRpc(short cid, UseSkillInfo info)
    {
        CallFuncRpc(UseSkillLocal, SendTo.To(cid), Delivery.Reliable, info);
    }
    public void SyncNetworkEventRpc(short cid, NetworkEvent e)
    {
        CallFuncRpc(SyncNetworkEventLocal, SendTo.To(cid), Delivery.Reliable, e);
    }
    public void PvpKillRewardRpc(short cid, SCPvpKillRewardInfo info)
    {
        CallFuncRpc(PvpKillRewardLocal, SendTo.To(cid), Delivery.Reliable, info);
    }
    public void SyncTripRpc(InteractablePropInfo info)
    {
        CallFuncRpc(SyncTripLocal,SendTo.ExcludeSender,Delivery.Reliable, info);
    }
    #endregion
}
