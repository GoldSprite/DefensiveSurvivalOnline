#if NETCODE_GAMEOBJECTS_1_3_1
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Sync.Relay.Transport;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Message;
using UnityEngine.Serialization;
using RaiseEventTarget = Unity.Sync.Relay.Message.RaiseEventTarget;
using RelayPlayer = Unity.Sync.Relay.Model.RelayPlayer;
using RelayRoom = Unity.Sync.Relay.Model.RelayRoom;

using Unity.Netcode;
using Unity.Sync.Relay.Model;
using NetworkTransport = Unity.Netcode.NetworkTransport;

namespace Unity.Sync.Relay.Transport.Netcode
{
    [AddComponentMenu("Netcode/Relay Transport (Netcode)")]
    public class RelayTransportNetcode : NetworkTransport
    {
        private ulong m_ServerTransportId;
        internal NetworkManager NetworkManager;
        private RelayTransportClient m_client;
        private RelayHeartbeatTimer m_relayHeartbeatTimer = new RelayHeartbeatTimer();
        private double m_timeout;

        private RelayRoom m_Room;
        private RelayPlayer m_Player;
        
        private RttUpdater m_RttUpdater = new RttUpdater();
        private double m_RttUpdateInterval;
        
        private RelayCallbacks m_callbacks;
        private Dictionary<string, object> m_internalCallbacks = new Dictionary<string, object>();

        public string UosAppId;
        public string UosAppSecret;
        public string RoomProfileUUID;
        public RelayTransportType m_transportType = RelayTransportType.UTP;
        
        [Tooltip("客户端到relay server的心跳超时值(秒)")]
        public float HeartbeatTimeout = 15;
        
        [Tooltip("勾选将会禁用掉Netcode里NetworkTransport的DisconnectRemoteClient()方法，有下述影响\n" +
                 "1.主机退出后不会踢掉其他玩家\n" +
                 "2.不能通过NetworkManager.DisconnectClient()踢掉其他玩家\n" +
                 "3.处理ConnectionApproval相关逻辑时，遇到ConnectionRequestMessage格式不合法/Approve没通过/Approve超时，不会踢掉发起的客户端")]
        public bool DisableDisconnectRemoteClient = false;
        
        public override ulong ServerClientId => m_ServerTransportId;
        
        private void Awake()
        {
            RelaySettings.UosAppId = string.IsNullOrEmpty(UosAppId) ? "" : UosAppId;
            RelaySettings.UosAppSecret = string.IsNullOrEmpty(UosAppSecret) ? "" : UosAppSecret;
            RelaySettings.RoomProfileUUID = string.IsNullOrEmpty(RoomProfileUUID) ? "" : RoomProfileUUID;
            RelaySettings.TransportType = m_transportType;
            
            m_Room = new RelayRoom();
            m_Player = new RelayPlayer();
            m_callbacks = new RelayCallbacks();
            
            m_RttUpdateInterval = 2;
            m_timeout = HeartbeatTimeout;
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
        
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            var e = new RelayEvent()
            {
                SentMessage = new SentMessage()
                {
                    RaiseEventTarget = RaiseEventTarget.ToPlayers,
                    ReceiverIds = { (uint)clientId },
                    Data = ByteString.CopyFrom(payload),
                }
            };
            
            m_client.SendEvent(e);
        }

        private void Update()
        {
            if (m_client == null)
            {
                return;
            }
            if (m_client.IsConnected())
            {
                if (m_relayHeartbeatTimer.IsTimeout())
                {
                    Debug.Log("usync client heartbeat timeout");
                    TriggerEvent(NetworkEvent.TransportFailure, m_Player.TransportId);
                    m_callbacks.OnHeartbeatTimeout?.Invoke();
                    return;
                }
                m_RttUpdater.Refresh(NetworkManager.IsServer);
            }
            m_client?.Update();
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = new ArraySegment<byte>(new byte[2], 0, 2);
            receiveTime = Time.realtimeSinceStartup;
            return NetworkEvent.Nothing;
        }

        public override bool StartClient()
        {
            if (!CheckRequirement())
            {
                return false;
            }
            
            m_client =  RelayTransportFactoryRegistry.Create(
                m_transportType, OnConnectedTransport, OnDispatchEvent, OnDisconnectedTransport
            );
            
            m_client.Init(m_Room.IP, m_Room.Port);
            
            return true;
        }

        public override bool StartServer()
        {
            if (!CheckRequirement())
            {
                return false;
            }
            m_client =  RelayTransportFactoryRegistry.Create(
                m_transportType, OnConnectedTransport, OnDispatchEvent, OnDisconnectedTransport
            );
            
            m_client.Init(m_Room.IP, m_Room.Port);
        
            return true;
        }

        /// <summary>
        /// Disconnects a remote client from the server
        /// </summary>
        /// <param name="transportId">The client to disconnect</param>
        public override void DisconnectRemoteClient(ulong transportId)
        {
            // Debug.Log("Calling DisconnectRemoteClient");
            if (!DisableDisconnectRemoteClient)
            {
                KickPlayer((uint)transportId);
            }
        }

        /// <summary>
        /// Disconnects the local client from the remote
        /// </summary>
        public override void DisconnectLocalClient()
        {
            // Debug.Log("On DisconnectLocalClient");
            if (m_client == null || !m_client.IsConnected())
            {
                return;
            }
            
            RelayEvent e = new RelayEvent()
            {
                LeaveRoomRequest = new LeaveRoomRequest()
            };

            m_client.SendEvent(e);
            m_client.Disconnect();
            m_Room = new RelayRoom();
            // TriggerEvent(NetworkEvent.Disconnect, m_Player.TransportId);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            // Debug.Log("GetCurrentRtt : " + m_RttUpdater.Rtt);
            return m_RttUpdater.Rtt;
        }

        public override void Shutdown()
        {
            Debug.Log("Shutdown Transport");

            m_ServerTransportId = 0;
            m_Player.TransportId = 0;
            if (m_client != null)
            {
                m_client.OnDestroy();
            }
            m_client = null;
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            NetworkManager = networkManager;
            m_ServerTransportId = 0;
            m_Player.TransportId = 0;
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

        public void OnConnectedTransport()
        {
            Debug.Log("On Connected Transport");
            m_relayHeartbeatTimer.Start(m_timeout);
            
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
                    GameType = GameType.Netcode
                }
            };

            m_client.SendEvent(e);
        }

        public void OnDisconnectedTransport()
        {
            Debug.Log("On Disconnected!");
            TriggerEvent(NetworkEvent.Disconnect, m_ServerTransportId);
        }

        public void OnDispatchEvent(RelayEvent e)
        {
            if (e == null) return;
            m_relayHeartbeatTimer.Refresh();

            switch (e.ContentCase)
            {
                case RelayEvent.ContentOneofCase.JoinRoomResponse:
                {
                    JoinRoomResponse r = e.JoinRoomResponse;

                    if (r.Code == 0)
                    {
                        Debug.Log(String.Format("join room response, response id ( current user sender id ) : {0}", r.Id));

                        m_Room.UpdateWithJoinRoomResponse(r);
                        m_ServerTransportId = r.MasterClientId;
                        m_Player.TransportId = r.Id;

                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        if (!NetworkManager.IsServer)
                        {
                            TriggerEvent(NetworkEvent.Connect, r.MasterClientId);
                        }
                        m_RttUpdater.Start(m_client, m_RttUpdateInterval);
                    }
                    else
                    {
                        Debug.Log(String.Format("join room failed. error code : {0}, msg : {1}", r.Code, RelayStatusCodeHelper.Convert(r.Code).Description));
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, null);
                        TriggerEvent(NetworkEvent.TransportFailure, 0);
                    }
                    break;
                }
                case RelayEvent.ContentOneofCase.LeaveRoomResponse:
                {
                    // Debug.Log("receive a leave room response ack");
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
                    if (!NetworkManager.IsServer)
                    {
                        OnDisconnectedTransport();
                    }
                    else
                    {
                        TriggerEvent(NetworkEvent.TransportFailure, 0);
                    }

                    break;
                }
                case RelayEvent.ContentOneofCase.Ping:
                {
                    RelayEvent ee = new RelayEvent()
                    {
                        Pong = new RelayPong()
                    };
                    m_client?.SendEvent(ee);
                    break;
                }
                case RelayEvent.ContentOneofCase.Pong:
                {
                    // 更新自身的状态信息
                    break;
                }
                case RelayEvent.ContentOneofCase.ReceivedMessage:
                {
                    ReceivedMessage r = e.ReceivedMessage;
                    var payload = new ArraySegment<byte>(r.Data.ToByteArray());
                    // Debug.LogFormat("Receive Message from {0} with Size {1}", r.SenderId, payload.Count);

                    InvokeOnTransportEvent(NetworkEvent.Data, r.SenderId, payload, Time.realtimeSinceStartup);
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerEnteredRoomEvent:
                {

                    PlayerEnteredRoomEvent r = e.PlayerEnteredRoomEvent;
                    m_Room.AddPlayer(r.Player);

                    if (NetworkManager.IsServer)
                    {
                        TriggerEvent(NetworkEvent.Connect, r.Player.Id);
                    }
                    m_callbacks.OnPlayerEnterRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));

                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerLeftRoomEvent:
                {
                    PlayerLeftRoomEvent r = e.PlayerLeftRoomEvent;
                    m_callbacks.OnPlayerLeaveRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    
                    m_Room.RemovePlayer(r.Player);
                    if (NetworkManager.IsServer)
                    {
                        TriggerEvent(NetworkEvent.Disconnect, r.Player.Id);
                    }
                    else if (m_ServerTransportId == r.Player.Id)
                    {
                        if (m_callbacks.OnMasterClientMigrateCallback == null)
                        {
                            TriggerEvent(NetworkEvent.Disconnect, r.Player.Id);
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
                        m_ServerTransportId = r.MasterClientId;
                        m_Room.MasterClientID = r.MasterClientId;
                        m_callbacks.OnMasterClientMigrateCallback?.Invoke(r.MasterClientId);
                    }
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
                    
                    m_client?.SendEvent(ee);
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
                    // Debug.LogFormat("Update Player {0} : {1}", r.Player.Id, r.Player);
                    m_Room.UpdatePlayer(r.Player);
                    m_callbacks.OnPlayerInfoUpdateCallback?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    break;
                }
                case RelayEvent.ContentOneofCase.UpdatePlayerResponse:
                {
                    UpdatePlayerResponse r = e.UpdatePlayerResponse;
                    if (r.Code == (uint)RelayCode.OK)
                    {
                        // Debug.Log("Update Player Info " + r.Player.Id + " Succeed.");
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
                    // Debug.LogFormat("Update Room Custom Properties Event from {0}", r.SenderId);
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
                        // Debug.Log("Update Room Custom Properties Succeed.");
                    }
                    else
                    {
                        Debug.Log("Update Room Custom Properties Fail. ( " + r.Code + " )");
                    }
                    
                    break;
                }
                case RelayEvent.ContentOneofCase.TellRttOnlyServerResponse:
                {
                    TellRttOnlyServerResponse r = e.TellRttOnlyServerResponse;
                    m_RttUpdater.UpdateRelayRtt(r.Timestamp);
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
        
        private void TriggerEvent(NetworkEvent e, ulong clientId)
        {
            InvokeOnTransportEvent(e,
                clientId,
                default,
                Time.realtimeSinceStartup);
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
        // 传入的player要包含完整的玩家信息
        public void UpdatePlayerInfo(RelayPlayer player)
        {
            // Debug.LogFormat("Update Player {0}", player.TransportId);
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
            m_client.SendEvent(e);
        }
        
        // 更新房间属性
        // 仅支持更新房间的自定义属性，可将需要修改的属性在 Custom Properties 里维护一份
        public void UpdateRoomCustomProperties(Dictionary<string, string> properties)
        {
            // Debug.LogFormat("Update Room Custom Properties");
            m_Room.UpdateCustomProperties(properties);
            var e = new RelayEvent()
            {
                UpdateRoomCustomPropertiesRequest = new UpdateRoomCustomPropertiesRequest()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    Properties = { properties }
                }
            };
            m_client.SendEvent(e);
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
            m_client.SendEvent(e);
        }
        
        public ulong GetRelayServerRtt()
        {
            return m_RttUpdater.RelayRtt;
        }
        
        public void Pause()
        { 
            m_client?.Pause();
        }
    
        public void UnPause()
        {
            m_client?.UnPause();
        }
        
        public void SetHeartbeat(uint seconds, Action<uint> callback = null)
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
            m_client.SendEvent(e);
            m_relayHeartbeatTimer.SetTimeout(seconds);

            if (callback != null)
            {
                m_internalCallbacks.Add(traceId, callback);
            }
        }
    }

    public class RttUpdater
    {
        private bool _active;
        
        private double _interval { get; set; } 
        
        private DateTime _lastPing { get; set; }
        
        private RelayTransportClient _client { get; set; }
        
        private ulong _rtt { get; set; }
        
        private ulong _relayRtt { get; set; }

        // Client到Host之间的Rtt
        public ulong Rtt => _active ? _rtt : 0;
        
        // Client到Relay Server之间的Rtt
        public ulong RelayRtt => _active ? _relayRtt : 0;

        private DateTime _centuryBegin = new DateTime(2001, 1, 1);
        
        public void Start(RelayTransportClient client, double interval)
        {
            _active = true;
            _interval = interval;
            _client = client;
            _rtt = 0;
            _lastPing = DateTime.Now;
        }

        public void Refresh(bool isServer)
        {
            if (_active)
            {
                var pingInterval = DateTime.Now - _lastPing;
                if (pingInterval.TotalSeconds > _interval)
                {
                    _lastPing = DateTime.Now;
                    // Debug.Log("Refresh MS : " + GetCurrentMs());
                    if (!isServer)
                    {
                        var e = new RelayEvent()
                        {
                            TellRttRequest = new TellRttRequest()
                            {
                                Timestamp = GetCurrentMs()
                            }
                        };
                        _client.SendEvent(e);
                    }
                    
                    var ee = new RelayEvent()
                    {
                        TellRttOnlyServerRequest = new TellRttOnlyServerRequest()
                        {
                            Timestamp = GetCurrentMs()
                        }
                    };
                    _client.SendEvent(ee);
                }
            }
        }
        
        public void Update(ulong timestamp)
        {
            _rtt = GetCurrentMs() - timestamp;
            // Debug.LogFormat("Current Rtt : {0}", _rtt);
        }
        
        public void UpdateRelayRtt(ulong timestamp)
        {
            _relayRtt = GetCurrentMs() - timestamp;
            // Debug.LogFormat("Current Relay Rtt : {0}", _relayRtt);
        }

        private ulong GetCurrentMs()
        {
            return (ulong)((DateTime.Now.Ticks - _centuryBegin.Ticks) / 10000);
        }
    }
}
#endif