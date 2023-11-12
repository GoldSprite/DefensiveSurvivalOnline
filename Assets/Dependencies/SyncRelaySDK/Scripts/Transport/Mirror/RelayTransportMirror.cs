#if MIRROR
using System;
using System.Collections.Generic;
using Google.Protobuf;
using kcp2k;
using Mirror;
using UnityEngine;

using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Message;
using Unity.Sync.Relay.Model;
using RelayPlayer = Unity.Sync.Relay.Model.RelayPlayer;
using RelayRoom = Unity.Sync.Relay.Model.RelayRoom;
using MirrorTransport = Mirror.Transport;

namespace Unity.Sync.Relay.Transport.Mirror
{
    
    public class RelayTransportMirror : MirrorTransport
    {
        private const uint DefaultHeartbeat = 15;
        
        private RelayLayer _activeRelayLayer;
        
        private RelayRoom m_Room;
        private RelayPlayer m_Player;

        public RelayRoom Room => m_Room;
        public RelayPlayer Player => m_Player;

        public string UosAppId;
        public string UosAppSecret;
        public string RoomProfileUUID;
        public RelayTransportType m_transportType = RelayTransportType.KCP;
        
        private RelayRole m_role = RelayRole.UNKNOWN_ROLE;
        
        private RelayState m_state = RelayState.INIT;

        private uint MasterClientTransportId = 0;

        private uint m_transportId = 0;
        
        private string m_scheme;
        
        private RttUpdater m_RttUpdater = new RttUpdater();
        private double m_RttUpdateInterval = 2;
        
        private Dictionary<string, object> m_internalCallbacks = new Dictionary<string, object>();
        
        private RelayCallbacks m_callbacks;

        void Awake()
        {
            RelaySettings.UosAppId = string.IsNullOrEmpty(UosAppId) ? "" : UosAppId;
            RelaySettings.UosAppSecret = string.IsNullOrEmpty(UosAppSecret) ? "" : UosAppSecret;
            RelaySettings.RoomProfileUUID = string.IsNullOrEmpty(RoomProfileUUID) ? "" : RoomProfileUUID;
            RelaySettings.TransportType = m_transportType;

            m_Player = new RelayPlayer();
            m_Room = new RelayRoom();
            m_callbacks = new RelayCallbacks();
            
            if (m_transportType == RelayTransportType.KCP)
            {
                _activeRelayLayer = new RelayKcpLayer(
                    OnTransportConnected, OnDispatchEvent, OnTransportDisconnected);
                m_scheme = "kcp";
            }
            else if (m_transportType == RelayTransportType.Websocket || m_transportType == RelayTransportType.WebSocketSecure)
            {
                // _activeRelayLayer = new RelayWebsocketLayer();
                _activeRelayLayer = new RelayWebsocketLayer(
                    OnTransportConnected, OnDispatchEvent, OnTransportDisconnected);
                m_scheme = "websocket";
            }
            else if (m_transportType == RelayTransportType.UTP)
            {
                Debug.LogError("Relay for Mirror does not support UTP currently");
                return;
            }
            
            Debug.Log("Relay Transport Initialized!");
        }
        
        public bool CheckRequirement()
        {
            if (string.IsNullOrEmpty(RelaySettings.UosAppId))
            {
                Debug.LogError("uos app id can not be null. please set uos app id in Inspector of NetworkManager");
                return false;
            }
            
            if (string.IsNullOrEmpty(RelaySettings.UosAppSecret))
            {
                Debug.LogError("uos app secret can not be null. please set uos app secret in Inspector of NetworkManager");
                return false;
            }
            
            if (string.IsNullOrEmpty(m_Player.ID))
            {
                Debug.LogError("player id can not be null. please call RelayTransportNetcode.SetPlayerData() first");
                return false;
            }
            
            if (string.IsNullOrEmpty(m_Player.Name))
            {
                Debug.LogWarning("player name is null");
            }
            
            if (string.IsNullOrEmpty(m_Room.IP))
            {
                Debug.LogError("room ip can not be null. please call RelayTransportNetcode.SetRoomData() first");
                return false;
            }
            
            if (string.IsNullOrEmpty(m_Room.ID))
            {
                Debug.LogError("room id can not be null. please call RelayTransportNetcode.SetRoomData() first");
                return false;
            }

            return true;
        }
        
        public void SetRoomData(CreateRoomResponse resp)
        {
            m_Room.Name = resp.Name;
            m_Room.Status = resp.Status;
            m_Room.ID = resp.RoomUuid;
            m_Room.Port = LobbyUtility.ParsePort(resp.ServerInfo);
            m_Room.IP = LobbyUtility.ParseIP(resp.ServerInfo);
            // m_Room.JoinCode = resp.JoinCode;
            if (resp.CustomProperties != null)
            {
                m_Room.CustomProperties = resp.CustomProperties;
            }
        }
        
        public void SetRoomData(QueryRoomResponse resp)
        {
            m_Room.Name = resp.Name;
            m_Room.Status = resp.Status;
            m_Room.ID = resp.RoomUuid;
            m_Room.Port = LobbyUtility.ParsePort(resp.ServerInfo);
            m_Room.IP = LobbyUtility.ParseIP(resp.ServerInfo);
            // m_Room.JoinCode = resp.JoinCode;
            if (resp.CustomProperties != null)
            {
                m_Room.CustomProperties = resp.CustomProperties;
            }
        }
        
        public void SetJoinCode(string joinCode)
        {
            m_Room.JoinCode = joinCode;
        }

        public void SetPlayerData(string Id, string Name)
        {
            m_Player.ID = Id;
            m_Player.Name = Name;
            m_Player.Properties = new Dictionary<string, string>();
        }
        
        public void SetPlayerData(string Id, string Name, Dictionary<string, string> Properties)
        {
            m_Player.ID = Id;
            m_Player.Name = Name;
            m_Player.Properties = Properties;
        }
        
        public void SetCallbacks(RelayCallbacks callbacks)
        {
            m_callbacks = callbacks;
        }
        
        public override bool Available()
        {
            return _activeRelayLayer.Available();
        }

        public override bool ClientConnected()
        {
            Debug.Log("call ClientConnected");
            return IsConnected();
        }

        public override void ClientConnect(string address)
        {
            if (!CheckRequirement())
            {
                return;
            }
            // throw new NotImplementedException();
            Debug.Log("client connect start to handle");
            m_role = RelayRole.CLIENT_ROLE;

            _activeRelayLayer.Connect(m_Room.IP, m_Room.Port);
        }

        public override void ClientConnect(Uri uri)
        {
            if (!CheckRequirement())
            {
                return;
            }
            Debug.Log("client conneced(uri)");
            m_role = RelayRole.CLIENT_ROLE;

            _activeRelayLayer.Connect(m_Room.IP, m_Room.Port);
        }
        
        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            RelayEvent e = new RelayEvent()
            {
                SentMessage = new SentMessage()
                {
                    RaiseEventTarget = RaiseEventTarget.ToPlayers,
                    ReceiverIds = { MasterClientTransportId },
                    Data = ByteString.CopyFrom(segment),
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }

        public override void ClientDisconnect()
        {
            // throw new NotImplementedException();
            Debug.Log("client disconnect");
            _activeRelayLayer?.Disconnect();
            m_state = RelayState.CLOSED;
            m_role = RelayRole.UNKNOWN_ROLE;
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder();
            
            builder.Scheme = m_scheme;
            builder.Host = m_Room.IP;
            builder.Port = m_Room.Port;
            return builder.Uri;
        }

        public override bool ServerActive()
        {
            Debug.Log("serverActive");
            return IsConnected();
        }

        public override void ServerStart()
        {
            if (!CheckRequirement())
            {
                return;
            }
            // throw new NotImplementedException();
            Debug.Log("Server Start");
            m_role = RelayRole.SERVER_ROLE;
            _activeRelayLayer.Connect(m_Room.IP, m_Room.Port);
        }

        public override void ServerSend(int transportId, ArraySegment<byte> segment, int channelId)
        {
            // Debug.LogFormat("ServerSend2 connectionId: {0}, channelId:{1}", transportId, channelId);
            RelayEvent e = new RelayEvent()
            {
                SentMessage = new SentMessage()
                {
                    RaiseEventTarget = RaiseEventTarget.ToPlayers,
                    ReceiverIds = { (uint)transportId },
                    Data = ByteString.CopyFrom(segment),
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }

        public override void ServerDisconnect(int connectionId)
        {
            // 不知道什么时候被调用
            Debug.Log("call ServerDisconnect");
            RelayEvent e = new RelayEvent()
            {
                KickPlayerRequest = new KickPlayerRequest()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    PlayerId = (uint)connectionId,
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            Debug.Log("server get client address");
            return "0.0.0.0:0000";
        }

        public override void ServerStop()
        {
            Debug.Log("server stop");
            _activeRelayLayer.Disconnect();
            
            m_state = RelayState.CLOSED;
            m_role = RelayRole.UNKNOWN_ROLE;
        }

        public override int GetMaxPacketSize(int channelId = Channels.Reliable)
        {
            return _activeRelayLayer.GetMaxPacketSize(channelId);
        }

        public override void ClientEarlyUpdate()
        {
            _activeRelayLayer?.EarlyUpdate();
            if (m_role == RelayRole.CLIENT_ROLE) {
                m_RttUpdater.Refresh();
            }
        }

        public override void ServerEarlyUpdate()
        {
            // Debug.Log("server early update");
            _activeRelayLayer?.EarlyUpdate();
        }

        public override void ClientLateUpdate()
        {
            // Debug.Log("client late update");
            _activeRelayLayer?.LateUpdate();
        }

        public override void ServerLateUpdate()
        {
            // Debug.Log("server late update");
            _activeRelayLayer?.LateUpdate();
        }
        
        public void Pause()
        { 
            Debug.Log("PAUSE");
            _activeRelayLayer?.Pause();
        }
    
        public void UnPause()
        {
            Debug.Log("UNPAUSE");  
            _activeRelayLayer?.UnPause();
        }

        public override void Shutdown()
        {
            Debug.Log("called shutdown");
            if (_activeRelayLayer != null)
            {
                _activeRelayLayer.Shutdown();
                _activeRelayLayer = null;
            }
            m_state = RelayState.CLOSED;
            m_role = RelayRole.UNKNOWN_ROLE;
        }
        
        // public override void OnApplicationQuit()
        // {
        //     base.OnApplicationQuit();
        // }

        public void OnTransportConnected()
        {
            RelayEvent e = new RelayEvent()
            {
                JoinRoomRequest = new JoinRoomRequest()
                {
                    RoomId = m_Room.ID,
                    UniqueId = m_Player.ID,
                    Name = m_Player.Name,
                    UosAppId = RelaySettings.UosAppId,
                    Properties = { m_Player.Properties },
                    JoinCode = String.IsNullOrEmpty(m_Room.JoinCode) ? "" : m_Room.JoinCode,
                    GameType = GameType.Mirror
                }
            };
            
            _activeRelayLayer.Send(e.ToByteArray());
        }
        
        public void OnTransportDisconnected()
        {
            // Debug.Log("On Transport Disconnect");
            if (m_role == RelayRole.CLIENT_ROLE)
            {
                OnClientDisconnected();
            }
            else if (m_role == RelayRole.SERVER_ROLE)
            {
            }
        }
        
        public void OnDispatchEvent(RelayEvent e)
        {
            if (e != null)
            {
                if (m_role == RelayRole.SERVER_ROLE)
                {
                    OnServerEvent(e);
                }
                else if (m_role == RelayRole.CLIENT_ROLE)
                {
                    OnClientEvent(e);
                }
            }
        }

        public void OnServerEvent(RelayEvent e)
        {
            // OnDispatchEvent?.Invoke(eEvent);
            // 做dispatch处理
            // eEvent.ContentCase
            switch (e.ContentCase)
            {
                case RelayEvent.ContentOneofCase.Ping:
                {
                    // 对端发送ping包
                    // Debug.Log("receive a ping");
                    PongRelay();
                    break;
                }
                case RelayEvent.ContentOneofCase.Pong:
                {
                    // 理论上不会收到
                    Debug.Log("receive a pong");
                    break;
                }
                case RelayEvent.ContentOneofCase.JoinRoomResponse:
                {
                    JoinRoomResponse r = e.JoinRoomResponse;

                    if (r.Code == 0)
                    {
                        // TODO
                        // 表示连接成功
                        Debug.LogFormat("Connect Successfully As Server");
                        m_Room.UpdateWithJoinRoomResponse(r);

                        //  m_transportId = r.Id;
                        MasterClientTransportId = r.MasterClientId;
                        m_Player.TransportId = r.Id;

                        if (r.MasterClientId != r.Id)
                        {
                            Debug.LogWarning("Not Master ?");
                        }
                        m_state = RelayState.CONNECTED;
                        
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        
                        m_RttUpdater.Start(_activeRelayLayer, m_RttUpdateInterval);
                    }
                    else
                    {
                        Debug.LogFormat("Connect Fail:{0} ", r.Code);
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, null);
                        ServerStop();
                    }

                    break;
                }
                case RelayEvent.ContentOneofCase.LeaveRoomResponse:
                { 
                    LeaveRoomResponse r = e.LeaveRoomResponse;
                    Debug.Log("Server Leave Room Response");
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerEnteredRoomEvent:
                {
                    PlayerEnteredRoomEvent r = e.PlayerEnteredRoomEvent;
                    m_Room.AddPlayer(r.Player);
                    this.OnServerConnected((int) r.Player.Id);
                    m_callbacks.OnPlayerEnterRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    // Debug.LogFormat("senderId: {0}, connect", r.Player.Id);
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerLeftRoomEvent:
                {
                    PlayerLeftRoomEvent r = e.PlayerLeftRoomEvent;
                    m_callbacks.OnPlayerLeaveRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    m_Room.RemovePlayer(r.Player);
                    this.OnServerDisconnected((int) r.Player.Id);
                    // Debug.LogFormat("senderId: {0}, disconnected", r.Player.Id);
                    break;
                }
                case RelayEvent.ContentOneofCase.ReceivedMessage:
                {
                    ReceivedMessage r = e.ReceivedMessage;

                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    uint senderId = r.SenderId;

                    this.OnServerDataReceived((int) senderId, seg, Channels.Reliable);
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerKickedEvent:
                {
                    PlayerKickedEvent r = e.PlayerKickedEvent;
                    if (r.Code == (int)RelayCode.RoomClosed)
                    {
                        Debug.Log("Room Closed");
                    }
                    else if (r.Code == (int)RelayCode.UserReLoggedIn)
                    {
                        Debug.Log("Same Account Login In Another Place Or Device");
                    }
                    else if (r.Code == (int)RelayCode.KickPlayerByMasterClient)
                    {
                        Debug.LogFormat("Be Disconnected By Self(??) For {0}", r.CustomMessage);
                    }
                    else
                    {
                        Debug.Log("Receive Kicked Event for Unknown Reason");
                    }
                    
                    m_callbacks.OnPlayerKickedCallback?.Invoke(r.Code, r.CustomMessage);
                    ServerStop();
                    break;
                }
                case RelayEvent.ContentOneofCase.TellRttRequest:
                {
                    TellRttRequest r = e.TellRttRequest;
                    var ee = new RelayEvent()
                    {
                        TellRttResponse = new TellRttResponse()
                        {
                            ReceiverId = r.SenderId,
                            Timestamp = r.Timestamp
                        }
                    };
                    
                    _activeRelayLayer?.Send(ee.ToByteArray());
                    break;
                }
                case RelayEvent.ContentOneofCase.KickPlayerResponse:
                {
                    KickPlayerResponse r = e.KickPlayerResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Disconnect Player " + r.PlayerId + " Succeed.");
                    }
                    else
                    {
                        Debug.Log("Disconnect Player " + r.PlayerId + " Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerUpdatedEvent:
                {
                    PlayerUpdatedEvent r = e.PlayerUpdatedEvent;
                    Debug.LogFormat("Update Player {0} : {1}", r.Player.Id, r.Player);
                    m_Room.UpdatePlayer(r.Player);
                    m_callbacks.OnPlayerInfoUpdateCallback?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    break;
                }
                case RelayEvent.ContentOneofCase.UpdatePlayerResponse:
                {
                    UpdatePlayerResponse r = e.UpdatePlayerResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Update Player Info " + r.Player.Id + " Succeed.");
                    }
                    else
                    {
                        Debug.Log("Update Player Info " + r.Player.Id + " Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.RoomCustomPropertiesUpdatedEvent:
                {
                    RoomCustomPropertiesUpdatedEvent r = e.RoomCustomPropertiesUpdatedEvent;
                    Debug.LogFormat("Update Room Custom Properties Event from {0}", r.SenderId);
                    /*
                    foreach (var item in r.Properties)
                    {
                        Debug.LogFormat("{0} - {1}", item.Key, item.Value);
                    }
                    */
                    m_Room.UpdateCustomProperties(r.Properties);
                    m_callbacks.OnRoomInfoUpdateCallback?.Invoke(m_Room);
                    break;
                }
                case RelayEvent.ContentOneofCase.UpdateRoomCustomPropertiesResponse:
                {
                    UpdateRoomCustomPropertiesResponse r = e.UpdateRoomCustomPropertiesResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Update Room Custom Properties Succeed.");
                    }
                    else
                    {
                        Debug.Log("Update Room Custom Properties Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.SetHeartbeatResponse:
                {
                    SetHeartbeatResponse r = e.SetHeartbeatResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.LogFormat("Set Heartbeat Timeout to {0}s Succeed.", r.Timeout);
                    }
                    else
                    {
                        Debug.LogFormat("Set Heartbeat Timeout to {0}s Fail. ( {1} )", r.Timeout, r.Code);
                    }

                    if (m_internalCallbacks.ContainsKey(r.TraceId))
                    {
                        Action<uint> callback = (Action<uint>)m_internalCallbacks[r.TraceId];
                        callback?.Invoke(r.Code);
                        m_internalCallbacks.Remove(r.TraceId);
                    }
                    m_callbacks.OnSetHeartbeat?.Invoke(r.Code, r.Timeout);
                    break;
                }
                case RelayEvent.ContentOneofCase.FetchTimestampResponse:
                {
                    break;
                }
                default:
                {
                    Debug.LogFormat("receive a unknonw msg: {0}", e.ContentCase);
                    break;
                }
            }
        }
        
        
        public void OnClientEvent(RelayEvent e)
        {
            // OnDispatchEvent?.Invoke(eEvent);
            switch (e.ContentCase)
            {
                case RelayEvent.ContentOneofCase.Ping:
                {
                    // 对端发送ping包
                    PongRelay();
                    break;
                }
                case RelayEvent.ContentOneofCase.Pong:
                {
                    // 理论上不会收到
                    Debug.Log("receive a pong");
                    break;
                }
                case RelayEvent.ContentOneofCase.JoinRoomResponse:
                {
                    JoinRoomResponse r = e.JoinRoomResponse;

                    if (r.Code == 0)
                    { 
                        // 表示连接成功
                        m_Room.UpdateWithJoinRoomResponse(r);
                        MasterClientTransportId = r.MasterClientId;
                        m_Player.TransportId = r.Id;
                        // m_transportId = r.Id;
                        m_state = RelayState.CONNECTED;
                        Debug.LogFormat("Connect Successfully As Client");
                        m_RttUpdater.Start(_activeRelayLayer, m_RttUpdateInterval);
                        
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        this.OnClientConnected();
                    }
                    else
                    {
                        Debug.LogFormat("Connect Fail:{0} As Client", r.Code); 
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        this.OnClientDisconnected();
                    }
                    break;
                }
                case RelayEvent.ContentOneofCase.LeaveRoomResponse:
                { 
                    LeaveRoomResponse r = e.LeaveRoomResponse;
                    Debug.Log("Client Leave Room Response");
                    break;
                }
                case RelayEvent.ContentOneofCase.ReceivedMessage:
                {
                    ReceivedMessage r = e.ReceivedMessage;
                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    this.OnClientDataReceived(seg, Channels.Reliable);
                    // Debug.LogFormat("client receive a message, data-size: {0}", r.Data.Length);
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerKickedEvent:
                {
                    PlayerKickedEvent r = e.PlayerKickedEvent;
                    if (r.Code == (int)RelayCode.RoomClosed)
                    {
                        Debug.Log("Room Closed");
                    }
                    else if (r.Code == (int)RelayCode.UserReLoggedIn)
                    {
                        Debug.Log("Same Account Login In Another Place Or Device");
                    }
                    else if (r.Code == (int)RelayCode.KickPlayerByMasterClient)
                    {
                        Debug.LogFormat("Be Disconnected By MasterClient For {0}", r.CustomMessage);
                    }
                    else
                    {
                        Debug.Log("Receive Kicked Event for Unknown Reason");
                    }

                    m_callbacks.OnPlayerKickedCallback?.Invoke(r.Code, r.CustomMessage);
                    this.OnClientDisconnected();
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerEnteredRoomEvent:
                {

                    PlayerEnteredRoomEvent r = e.PlayerEnteredRoomEvent;
                    m_Room.AddPlayer(r.Player);
                    m_callbacks.OnPlayerEnterRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerLeftRoomEvent:
                {
                    PlayerLeftRoomEvent r = e.PlayerLeftRoomEvent;
                    m_callbacks.OnPlayerLeaveRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    m_Room.RemovePlayer(r.Player);

                    if (r.Player.Id == MasterClientTransportId)
                    {
                        // Debug.LogFormat("server closed");
                        if (m_callbacks.OnMasterClientMigrateCallback == null)
                        {
                            this.OnClientDisconnected();
                        }
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.MigrateMasterClientEvent:
                {
                    MigrateMasterClientEvent r = e.MigrateMasterClientEvent;
                    Debug.Log("Master Client Migrate To " + r.MasterClientId);
                    if (m_callbacks.OnMasterClientMigrateCallback != null)
                    {
                        MasterClientTransportId = r.MasterClientId;
                        m_Room.MasterClientID = r.MasterClientId;
                        m_callbacks.OnMasterClientMigrateCallback?.Invoke(r.MasterClientId);
                    }
                    break;
                }
                case RelayEvent.ContentOneofCase.TellRttResponse:
                {
                    TellRttResponse r = e.TellRttResponse;
                    m_RttUpdater.Update(r.Timestamp);
                    break;
                }
                case RelayEvent.ContentOneofCase.KickPlayerResponse:
                {
                    KickPlayerResponse r = e.KickPlayerResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Disconnect Player " + r.PlayerId + " Succeed.");
                    }
                    else
                    {
                        Debug.Log("Disconnect Player " + r.PlayerId + " Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerUpdatedEvent:
                {
                    PlayerUpdatedEvent r = e.PlayerUpdatedEvent;
                    Debug.LogFormat("Update Player {0} : {1}", r.Player.Id, r.Player);
                    m_Room.UpdatePlayer(r.Player);
                    m_callbacks.OnPlayerInfoUpdateCallback?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    break;
                }
                case RelayEvent.ContentOneofCase.UpdatePlayerResponse:
                {
                    UpdatePlayerResponse r = e.UpdatePlayerResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Update Player Info " + r.Player.Id + " Succeed.");
                    }
                    else
                    {
                        Debug.Log("Update Player Info " + r.Player.Id + " Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.RoomCustomPropertiesUpdatedEvent:
                {
                    RoomCustomPropertiesUpdatedEvent r = e.RoomCustomPropertiesUpdatedEvent;
                    Debug.LogFormat("Update Room Custom Properties Event from {0}", r.SenderId);
                    /*
                    foreach (var item in r.Properties)
                    {
                        Debug.LogFormat("{0} - {1}", item.Key, item.Value);
                    }
                    */
                    m_Room.UpdateCustomProperties(r.Properties);
                    m_callbacks.OnRoomInfoUpdateCallback?.Invoke(m_Room);
                    break;
                }
                case RelayEvent.ContentOneofCase.UpdateRoomCustomPropertiesResponse:
                {
                    UpdateRoomCustomPropertiesResponse r = e.UpdateRoomCustomPropertiesResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("Update Room Custom Properties Succeed.");
                    }
                    else
                    {
                        Debug.Log("Update Room Custom Properties Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.SetHeartbeatResponse:
                {
                    SetHeartbeatResponse r = e.SetHeartbeatResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        Debug.LogFormat("Set Heartbeat Timeout to {0}s Succeed.", r.Timeout);
                    }
                    else
                    {
                        Debug.LogFormat("Set Heartbeat Timeout to {0}s Fail. ( {1} )", r.Timeout, r.Code);
                    }
                    
                    if (m_internalCallbacks.ContainsKey(r.TraceId))
                    {
                        Action<uint> callback = (Action<uint>)m_internalCallbacks[r.TraceId];
                        callback?.Invoke(r.Code);
                        m_internalCallbacks.Remove(r.TraceId);
                    }
                    
                    m_callbacks.OnSetHeartbeat?.Invoke(r.Code, r.Timeout);
                    break;
                }
                case RelayEvent.ContentOneofCase.FetchTimestampResponse:
                {
                    break;
                }
                default:
                {
                    Debug.LogFormat("receive a unknonw msg: {0}", e.ContentCase);
                    break;
                }
            }
        }
        
        public void PongRelay()
        {
            // relay层面的心跳维护
            RelayEvent e = new RelayEvent()
            {
                Pong = new RelayPong()
                {
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }
        
        public void PingRelay()
        {
            // relay层面的心跳维护
            RelayEvent e = new RelayEvent()
            {
                Ping = new RelayPing()
                {
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }

        public bool IsConnected()
        {
            return _activeRelayLayer.IsConnected() && m_state == RelayState.CONNECTED;
        }
        
        // timeout范围为 5s - 600s
        public void SetHeartbeat(uint seconds, Action<uint> callback)
        {
            var traceId = Guid.NewGuid().ToString();
            RelayEvent e = new RelayEvent()
            {
                SetHeartbeatRequest = new SetHeartbeatRequest()
                {
                    TraceId = traceId,
                    Timeout = seconds
                }
            };
            
            _activeRelayLayer.Send(e.ToByteArray());
            m_internalCallbacks.Add(traceId, callback);
        }
        
        public void SetHeartbeat(uint seconds)
        {
            var traceId = Guid.NewGuid().ToString();
            RelayEvent e = new RelayEvent()
            {
                SetHeartbeatRequest = new SetHeartbeatRequest()
                {
                    TraceId = traceId,
                    Timeout = seconds
                }
            };
            
            _activeRelayLayer.Send(e.ToByteArray());
        }
        
        public uint GetDefaultHeartbeat()
        {
            return DefaultHeartbeat;
        }

        
        public RelayRoom GetRoomInfo()
        {
            return m_Room;
        }
        
        public RelayPlayer GetPlayerInfo(uint transportId)
        {
            return m_Room.GetPlayer(transportId);
        }
        
        public RelayPlayer GetCurrentPlayer()
        {
            return m_Room.GetPlayer(m_Player.TransportId);
        }
        
        // 更新玩家信息
        // 仅支持通过 TransportId 更新 Name/Properties ( ID和TransportId不支持更新 )
        public void UpdatePlayerInfo(RelayPlayer player)
        {
            Debug.LogFormat("Update Player {0}", player.TransportId);
            m_Room.UpdatePlayer(player);
            var e = new RelayEvent()
            {
                UpdatePlayerRequest = new UpdatePlayerRequest()
                {
                    UniqueId = player.ID,
                    Name = player.Name,
                    PlayerId = player.TransportId,
                    TraceId = Guid.NewGuid().ToString(),
                    Properties = { player.Properties }
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }
        
        // 更新房间属性
        // 仅支持更新房间的自定义属性，可将需要修改的属性在 Custom Properties 里维护一份
        public void UpdateRoomCustomProperties(Dictionary<string, string> properties)
        {
            Debug.LogFormat("Update Room Custom Properties");
            m_Room.UpdateCustomProperties(properties);
            var e = new RelayEvent()
            {
                UpdateRoomCustomPropertiesRequest = new UpdateRoomCustomPropertiesRequest()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    Properties = { properties }
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }
        
        public void KickPlayer(uint transportId, string reason = "")
        {
            RelayEvent e = new RelayEvent()
            {
                KickPlayerRequest = new Message.KickPlayerRequest()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    PlayerId = transportId,
                    CustomMessage = reason
                }
            };
            _activeRelayLayer.Send(e.ToByteArray());
        }
    }
    
    public class RttUpdater
    {
        private bool _active;
        
        private double _interval { get; set; } 
        
        private DateTime _lastPing { get; set; }
        
        private RelayLayer _client { get; set; }
        
        private ulong _rtt { get; set; }

        public ulong Rtt => _active ? _rtt : 0;
        
        private DateTime _centuryBegin = new DateTime(2001, 1, 1);
        
        public void Start(RelayLayer client, double interval)
        {
            _active = true;
            _interval = interval;
            _client = client;
            _rtt = 0;
            _lastPing = DateTime.Now;
        }

        public void Refresh()
        {
            if (_active)
            {
                var pingInterval = DateTime.Now - _lastPing;
                if (pingInterval.TotalSeconds > _interval)
                {
                    _lastPing = DateTime.Now;
                    // Debug.Log("Refresh MS : " + GetCurrentMs());
                    var e = new RelayEvent()
                    {
                        TellRttRequest = new TellRttRequest()
                        {
                            Timestamp = GetCurrentMs()
                        }
                    };
                    
                    _client.Send(e.ToByteArray());
                }
            }
        }
        
        public void Update(ulong timestamp)
        {
            _rtt = GetCurrentMs() - timestamp;
            // Debug.Log("Update Rtt : " + _rtt);
        }

        private ulong GetCurrentMs()
        {
            return (ulong)((DateTime.Now.Ticks - _centuryBegin.Ticks) / 10000);
        }
    }
}
#endif