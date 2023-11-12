#if FISHNET
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using Google.Protobuf;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Message;
using Unity.Sync.Relay.Model;
using UnityEngine;
using FishNetTransport = FishNet.Transporting.Transport;
using RelayPlayer = Unity.Sync.Relay.Model.RelayPlayer;
using RelayRoom = Unity.Sync.Relay.Model.RelayRoom;

namespace Unity.Sync.Relay.Transport.FishNet
{
    [AddComponentMenu("FishNet/Transport/Sync Relay(FishNet)")]
    public class RelayTransportFishNet : FishNetTransport
    {
        ~RelayTransportFishNet()
        {
            Shutdown();
        }
        
        #region Serialized.
        public string UosAppId;
        public string UosAppSecret;
        public string RoomProfileUUID;
        public RelayTransportType m_transportType = RelayTransportType.UTP;
        
        private int m_unreliableMTU = 1023;
        private int m_maximumClients = 4095;
        private ushort m_timeout = 15;
        private ushort m_port = 7770;
        private string m_clientAddress = "localhost";
        private string m_ipv4BindAddress;
        private string m_ipv6BindAddress;
        
        #endregion
        
        
        #region Private.
        private uint m_ServerTransportId;
        private RelayLayer m_relayLayer;
        
        private RelayRoom m_Room;
        private RelayPlayer m_Player;
        private RelayCallbacks m_callbacks;
        
        private RelayRole m_role = RelayRole.UNKNOWN_ROLE;
        private LocalConnectionState m_state = LocalConnectionState.Starting;

        private string m_scheme;

        private bool IsHost = false;
        
        private Dictionary<string, object> m_internalCallbacks = new Dictionary<string, object>();
        
        #endregion
        
        
        #region Initialization and unity.
        
        private void Awake()
        {
            Debug.Log("Relay Transport FishNet Awake");
        }

        private void Start()
        {
            Debug.Log("Relay Transport FishNet Start");
        }

        public override void Initialize(NetworkManager networkManager, int transportIndex)
        {
            Debug.Log("Relay Transport FishNet Initialize");
            base.Initialize(networkManager, transportIndex);
            
            RelaySettings.UosAppId = string.IsNullOrEmpty(UosAppId) ? "" : UosAppId;
            RelaySettings.UosAppSecret = string.IsNullOrEmpty(UosAppSecret) ? "" : UosAppSecret;
            RelaySettings.RoomProfileUUID = string.IsNullOrEmpty(RoomProfileUUID) ? "" : RoomProfileUUID;
            RelaySettings.TransportType = m_transportType;
            
            if (m_transportType == RelayTransportType.UTP)
            {
                m_relayLayer = new RelayUtpLayer(
                    OnTransportConnected, OnDispatchEvent, OnTransportDisconnected);
                m_scheme = "utp";
            }
            else if (m_transportType == RelayTransportType.KCP)
            {
                m_relayLayer = new RelayKcpLayer(
                    OnTransportConnected, OnDispatchEvent, OnTransportDisconnected);
                m_scheme = "kcp";
            }
            else if ( m_transportType == RelayTransportType.Websocket || m_transportType == RelayTransportType.WebSocketSecure)
            {
                Debug.LogError("Sync Relay for FishNet Only Support UTP and KCP Currently.");
                return;
            }
            else 
            {
                Debug.LogError("Unknown Transport Type.");
                return;
            }

            IsHost = false;
            m_Room = new RelayRoom();
            m_Player = new RelayPlayer();
            m_callbacks = new RelayCallbacks();
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
        
        protected void OnDestroy()
        {
            Shutdown();
        }
        #endregion
        
        
        #region ConnectionStates.
        /// <summary>
        /// Gets the address of a remote connection Id.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public override string GetConnectionAddress(int connectionId)
        {
            Debug.Log("server get client address");
            return "0.0.0.0:0000";
        }
        /// <summary>
        /// Called when a connection state changes for the local client.
        /// </summary>
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        /// <summary>
        /// Called when a connection state changes for the local server.
        /// </summary>
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        /// <summary>
        /// Gets the current local ConnectionState.
        /// </summary>
        /// <param name="server">True if getting ConnectionState for the server.</param>
        public override LocalConnectionState GetConnectionState(bool server)
        {
            return m_state;
        }
        /// <summary>
        /// Gets the current ConnectionState of a remote client on the server.
        /// </summary>
        /// <param name="connectionId">ConnectionId to get ConnectionState for.</param>
        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            if (m_Room.Players.ContainsKey((uint)connectionId))
            {
                return RemoteConnectionState.Started;
            }
            else
            {
                return RemoteConnectionState.Stopped;
            }
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            OnClientConnectionState?.Invoke(connectionStateArgs);
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local server.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            OnServerConnectionState?.Invoke(connectionStateArgs);
            UpdateTimeout();
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for a remote client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            OnRemoteConnectionState?.Invoke(connectionStateArgs);
        }
        #endregion
        
        #region Iterating.
        /// <summary>
        /// Processes data received by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateIncoming(bool server)
        {
            m_relayLayer.IterateIncoming();
        }

        /// <summary>
        /// Processes data to be sent by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateOutgoing(bool server)
        {
            m_relayLayer.IterateOutgoing();
        }
        #endregion
        
        #region ReceivedData.
        /// <summary>
        /// Called when client receives data.
        /// </summary>
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            OnClientReceivedData?.Invoke(receivedDataArgs);
        }
        /// <summary>
        /// Called when server receives data.
        /// </summary>
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            OnServerReceivedData?.Invoke(receivedDataArgs);
        }
        #endregion
        
        #region Sending.
        /// <summary>
        /// Sends to the server or all clients.
        /// </summary>
        /// <param name="channelId">Channel to use.</param>
        /// <param name="segment">Data to send.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            Channel channel = channelId == (byte)Channel.Reliable ? Channel.Reliable : Channel.Unreliable;
            RelayEvent e;
            if (channel == Channel.Reliable)
            {
                e = new RelayEvent()
                {
                    SentMessage = new SentMessage()
                    {
                        RaiseEventTarget = RaiseEventTarget.ToPlayers,
                        ReceiverIds = { m_ServerTransportId },
                        Data = ByteString.CopyFrom(segment),
                    }
                };
            }
            else
            {
                e = new RelayEvent()
                {
                    UnreliableSentMessage = new UnreliableSentMessage()
                    {
                        Data = ByteString.CopyFrom(segment),
                    }
                };
            }

            if (IsHost)
            {
                var args = new ServerReceivedDataArgs(segment, channel, (int)m_Player.TransportId, base.Index);
                this.HandleServerReceivedDataArgs(args);
            }
            else
            {
                m_relayLayer.Send(e.ToByteArray(), channel);
            }
        }
        /// <summary>
        /// Sends data to a client.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="segment"></param>
        /// <param name="connectionId"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            Channel channel = channelId == (byte)Channel.Reliable ? Channel.Reliable : Channel.Unreliable;
            RelayEvent e;
            
            if (channel == Channel.Reliable)
            {
                e = new RelayEvent()
                {
                    SentMessage = new SentMessage()
                    {
                        RaiseEventTarget = RaiseEventTarget.ToPlayers,
                        ReceiverIds = { (uint)connectionId },
                        Data = ByteString.CopyFrom(segment),
                    }
                };
            }
            else
            {
                e = new RelayEvent()
                {
                    UnreliableSentMessage = new UnreliableSentMessage()
                    {
                        ReceiverId = (uint)connectionId,
                        Data = ByteString.CopyFrom(segment),
                    }
                };
            }
            
            if (IsHost && connectionId == m_ServerTransportId)
            {                
                var args = new ClientReceivedDataArgs(segment, channel, base.Index);
                this.HandleClientReceivedDataArgs(args);
            }
            else
            {
                m_relayLayer.Send(e.ToByteArray(), channel);
            }
        }
        #endregion
        
        #region Configuration.
        /// <summary>
        /// Sets which PacketLayer to use with LiteNetLib.
        /// </summary>
        /// <param name="packetLayer"></param>
        public void SetPacketLayer()
        {
        }
        /// <summary>
        /// How long in seconds until either the server or client socket must go without data before being timed out.
        /// </summary>
        /// <param name="asServer">True to get the timeout for the server socket, false for the client socket.</param>
        /// <returns></returns>
        public override float GetTimeout(bool asServer)
        {
            //Server and client uses the same timeout.
            return (float)m_timeout;
        }
        /// <summary>
        /// Sets how long in seconds until either the server or client socket must go without data before being timed out.
        /// </summary>
        /// <param name="asServer">True to set the timeout for the server socket, false for the client socket.</param>
        public override void SetTimeout(float value, bool asServer)
        {
            m_timeout = (ushort)value;
        }
        /// <summary>
        /// Returns the maximum number of clients allowed to connect to the server. If the transport does not support this method the value -1 is returned.
        /// </summary>
        /// <returns></returns>
        public override int GetMaximumClients()
        {
            return m_maximumClients;
            // return _server.GetMaximumClients();
        }
        /// <summary>
        /// Sets maximum number of clients allowed to connect to the server. If applied at runtime and clients exceed this value existing clients will stay connected but new clients may not connect.
        /// </summary>
        /// <param name="value"></param>
        public override void SetMaximumClients(int value)
        {
            m_maximumClients = value;
            // _server.SetMaximumClients(value);
        }
        /// <summary>
        /// Sets which address the client will connect to.
        /// </summary>
        /// <param name="address"></param>
        public override void SetClientAddress(string address)
        {
            m_clientAddress = address;
        }
        /// <summary>
        /// Gets which address the client will connect to.
        /// </summary>
        public override string GetClientAddress()
        {
            return m_clientAddress;
        }

        /// <summary>
        /// Sets which address the server will bind to.
        /// </summary>
        /// <param name="address"></param>
        public override void SetServerBindAddress(string address, IPAddressType addressType)
        {
            if (addressType == IPAddressType.IPv4)
                m_ipv4BindAddress = address;
            else
                m_ipv6BindAddress = address;
        }
        /// <summary>
        /// Gets which address the server will bind to.
        /// </summary>
        /// <param name="address"></param>
        public override string GetServerBindAddress(IPAddressType addressType)
        {
            if (addressType == IPAddressType.IPv4)
                return m_ipv4BindAddress;
            else
                return m_ipv6BindAddress;
        }
        /// <summary>
        /// Sets which port to use.
        /// </summary>
        /// <param name="port"></param>
        public override void SetPort(ushort port)
        {
            m_port = port;
        }
        /// <summary>
        /// Gets which port to use.
        /// </summary>
        /// <param name="port"></param>
        public override ushort GetPort()
        {
            return m_port;
        }
        #endregion
        
        #region Start and stop.

        public void SetHost(bool host)
        {
            IsHost = host;
        }
        
        /// <summary>
        /// Starts the local server or client using configured settings.
        /// </summary>
        /// <param name="server">True to start server.</param>

        public override bool StartConnection(bool server)
        {
            if (!CheckRequirement())
            {
                return false;
            }
            
            if (server)
            {
                m_role = RelayRole.SERVER_ROLE;
                
                m_state = LocalConnectionState.Starting;
                var args = new ServerConnectionStateArgs(LocalConnectionState.Starting,
                    base.Index);
                this.HandleServerConnectionState(args);
            }
            else
            {
                m_role = RelayRole.CLIENT_ROLE;
                
                m_state = LocalConnectionState.Starting;
                var args = new ClientConnectionStateArgs(LocalConnectionState.Starting,
                    base.Index);
                this.HandleClientConnectionState(args);
            }
            
            m_relayLayer.Connect(m_Room.IP, m_Room.Port);
            // UpdateTimeout(); ???????
            return true;
        }

        /// <summary>
        /// Stops the local server or client.
        /// </summary>
        /// <param name="server">True to stop server.</param>
        public override bool StopConnection(bool server)
        {
            if (m_relayLayer == null || !m_relayLayer.IsConnected())
            {
                return true;
            }

            if (server)
            {
                var args = new ServerConnectionStateArgs(LocalConnectionState.Stopped,
                    base.Index);
                this.HandleServerConnectionState(args);
            }
            else
            {
                var args = new ClientConnectionStateArgs(LocalConnectionState.Stopped,
                    base.Index);
                this.HandleClientConnectionState(args);
            }
            
            RelayEvent e = new RelayEvent()
            {
                LeaveRoomRequest = new LeaveRoomRequest()
            };

            m_relayLayer.Send(e.ToByteArray());
            
            m_relayLayer?.Disconnect();
            m_state = LocalConnectionState.Stopped;
            m_role = RelayRole.UNKNOWN_ROLE;
            m_Room = new RelayRoom();
            
            return true;
        }

        /// <summary>
        /// Stops a remote client from the server, disconnecting the client.
        /// </summary>
        /// <param name="connectionId">ConnectionId of the client to disconnect.</param>
        /// <param name="immediately">True to abrutly stop the client socket. The technique used to accomplish immediate disconnects may vary depending on the transport.
        /// When not using immediate disconnects it's recommended to perform disconnects using the ServerManager rather than accessing the transport directly.
        /// </param>
        public override bool StopConnection(int connectionId, bool immediately)
        {
            Debug.LogFormat("Disconnect Player {0}", connectionId);
            RelayEvent e = new RelayEvent()
            {
                KickPlayerRequest = new KickPlayerRequest()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    PlayerId = (uint)connectionId,
                }
            };
            m_relayLayer.Send(e.ToByteArray());
            return true;
        }

        /// <summary>
        /// Stops both client and server.
        /// </summary>
        public override void Shutdown()
        {
            // Debug.Log("Shutdown");
            OnTransportDisconnected();
            
            if (m_relayLayer != null)
            {
                m_relayLayer.Shutdown();
                m_relayLayer = null;
            }
            m_state = LocalConnectionState.Stopped;
            m_role = RelayRole.UNKNOWN_ROLE;
            
            //Stops client then server connections.
            // StopConnection(false);
            // StopConnection(true);
        }
        
        public void Disconnect()
        {
            // xxxx
        }
        
        #region Privates.
        
        /// <summary>
        /// Updates clients timeout values.
        /// </summary>
        private void UpdateTimeout()
        {
            //If server is running set timeout to max. This is for host only.
            //int timeout = (GetConnectionState(true) != LocalConnectionState.Stopped) ? MAX_TIMEOUT_SECONDS : _timeout;
            // int timeout = (Application.isEditor) ? MAX_TIMEOUT_SECONDS : _timeout;
            // _client.UpdateTimeout(timeout);
            // _server.UpdateTimeout(timeout);
        }
        #endregion
        
        #endregion
        
        #region Channels.
        /// <summary>
        /// If channelId is invalid then channelId becomes forced to reliable.
        /// </summary>
        /// <param name="channelId"></param>
        private void SanitizeChannel(ref byte channelId)
        {
            if (channelId < 0 || channelId >= TransportManager.CHANNEL_COUNT)
            {
                NetworkManager.LogWarning($"Channel of {channelId} is out of range of supported channels. Channel will be defaulted to reliable.");
                channelId = 0;
            }
        }
        /// <summary>
        /// Gets the MTU for a channel. This should take header size into consideration.
        /// For example, if MTU is 1200 and a packet header for this channel is 10 in size, this method should return 1190.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public override int GetMTU(byte channel)
        {
            return m_unreliableMTU;
        }
        #endregion

        #region Editor.
#if UNITY_EDITOR
        private void OnValidate()
        {
            int MINIMUM_UDP_MTU = 576;
            var MAXIMUM_UDP_MTU = 1023;
            
            if (m_unreliableMTU < 0)
                m_unreliableMTU = MINIMUM_UDP_MTU; 
            else if (m_unreliableMTU > MAXIMUM_UDP_MTU)
                m_unreliableMTU = MAXIMUM_UDP_MTU;
        }
#endif
        #endregion

        
        #region RelayProperties.
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
        
        public void OnTransportConnected()
        {
            Debug.Log("On Transport Connected");
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
                    GameType = GameType.Fishnet
                }
            };
            
            m_relayLayer.Send(e.ToByteArray());
        }
        
        public void OnTransportDisconnected()
        {
            StopConnection(m_role == RelayRole.SERVER_ROLE ? true : false);
        }
        
        public void OnDispatchEvent(RelayEvent e, Channel channel)
        {
            if (e != null)
            {
                if (m_role == RelayRole.SERVER_ROLE)
                {
                    OnServerEvent(e, channel);
                }
                else if (m_role == RelayRole.CLIENT_ROLE)
                {
                    OnClientEvent(e, channel);
                }
            }
        }

        public void OnServerEvent(RelayEvent e, Channel channel)
        {
            // 做dispatch处理
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
                        m_ServerTransportId = r.MasterClientId;
                        m_Player.TransportId = r.Id;

                        Debug.LogFormat("Server Transport ID {0}", m_ServerTransportId);

                        if (r.MasterClientId != r.Id)
                        {
                            Debug.LogWarning("Not Master ?");
                        }
                        
                        m_state = LocalConnectionState.Started;
                        var args = new ServerConnectionStateArgs(LocalConnectionState.Started,
                            base.Index);
                            
                        this.HandleServerConnectionState(args);

                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);

                        if (IsHost)
                        {
                            // Debug.LogFormat("Host Client Transport ID {0}", m_ServerTransportId);
                            var args1 = new RemoteConnectionStateArgs(RemoteConnectionState.Started,
                                (int)m_ServerTransportId,
                                base.Index);
                            this.HandleRemoteConnectionState(args1);

                            var args2 = new ClientConnectionStateArgs(LocalConnectionState.Started,
                                base.Index);
                            this.HandleClientConnectionState(args2);
                        }
                        // m_RttUpdater.Start(_activeRelayLayer, m_RttUpdateInterval);
                    }
                    else
                    {
                        Debug.LogFormat("Connect Fail:{0} ", r.Code);
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, null);
                        StopConnection(true);
                    }

                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerEnteredRoomEvent:
                {
                    PlayerEnteredRoomEvent r = e.PlayerEnteredRoomEvent;
                    m_Room.AddPlayer(r.Player);

                    Debug.LogFormat("Player Entered Room - Transport ID {0}", r.Player.Id);
                    var args = new RemoteConnectionStateArgs(RemoteConnectionState.Started, (int)r.Player.Id,
                        base.Index);
                    this.HandleRemoteConnectionState(args);
                    m_callbacks.OnPlayerEnterRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    break;
                }
                case RelayEvent.ContentOneofCase.PlayerLeftRoomEvent:
                {
                    PlayerLeftRoomEvent r = e.PlayerLeftRoomEvent;
                    m_callbacks.OnPlayerLeaveRoom?.Invoke(m_Room.GetPlayer(r.Player.Id));
                    m_Room.RemovePlayer(r.Player);
                    
                    var args = new RemoteConnectionStateArgs(RemoteConnectionState.Stopped, (int)r.Player.Id,
                        base.Index);
                    this.HandleRemoteConnectionState(args);
                    // Debug.LogFormat("senderId: {0}, disconnected", r.Player.Id);
                    break;
                }
                case RelayEvent.ContentOneofCase.ReceivedMessage:
                {
                    ReceivedMessage r = e.ReceivedMessage;

                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    uint senderId = r.SenderId;

                    // Debug.LogFormat("Server Receive Message from {0} with Offset {1} and Length {2} with Channel {3}", senderId, seg.Offset, seg.Count, channel);
                    var args = new ServerReceivedDataArgs(seg, Channel.Reliable, (int)senderId, base.Index);
                    this.HandleServerReceivedDataArgs(args);
                    break;
                }
                case RelayEvent.ContentOneofCase.UnreliableReceivedMessage:
                {
                    UnreliableReceivedMessage r = e.UnreliableReceivedMessage;

                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    uint senderId = r.SenderId;

                    // Debug.LogFormat("Server Receive Unreliable Message from {0} with Channel {1}", senderId, channel);
                    var args = new ServerReceivedDataArgs(seg, Channel.Unreliable, (int)senderId, base.Index);
                    this.HandleServerReceivedDataArgs(args);
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
                    StopConnection(true);
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
                    
                    m_relayLayer?.Send(ee.ToByteArray());
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
                case RelayEvent.ContentOneofCase.LeaveRoomResponse:
                {
                    Debug.Log("receive a leave room response ack");
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
        
        public void OnClientEvent(RelayEvent e, Channel channel)
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
                        m_ServerTransportId = r.MasterClientId;
                        m_Player.TransportId = r.Id;
                        // m_transportId = r.Id;
                        m_state = LocalConnectionState.Started;
                        // Debug.LogFormat("Connect Successfully As Client");
                        // m_RttUpdater.Start(_activeRelayLayer, m_RttUpdateInterval);
                        Debug.LogFormat("Client Transport ID - {0}", r.Id);

                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        var args = new ClientConnectionStateArgs(LocalConnectionState.Started,
                            base.Index);
                        this.HandleClientConnectionState(args);
                    }
                    else
                    {
                        Debug.LogFormat("Connect Fail:{0} As Client", r.Code); 
                        m_callbacks.OnConnectToRelayServerCallback?.Invoke(r.Code, m_Room);
                        StopConnection(false);

                        // this.OnClientDisconnected();
                    }
                    break;
                }
                case RelayEvent.ContentOneofCase.ReceivedMessage:
                {
                    ReceivedMessage r = e.ReceivedMessage;
                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    
                    var args = new ClientReceivedDataArgs(seg, Channel.Reliable, base.Index);
                    this.HandleClientReceivedDataArgs(args);
                    
                    // Debug.LogFormat("Client Receive Reliable Message from {0} with Channel {1}", r.SenderId, channel);
                    // Debug.LogFormat("client receive a message, data-size: {0}", r.Data.Length);
                    break;
                }
                case RelayEvent.ContentOneofCase.UnreliableReceivedMessage:
                {
                    UnreliableReceivedMessage r = e.UnreliableReceivedMessage;

                    ArraySegment<byte> seg = new ArraySegment<byte>(r.Data.ToByteArray());
                    uint senderId = r.SenderId;

                    // Debug.LogFormat("Client Receive Unreliable Message from {0} with Channel {1}", senderId, channel);
                    var args = new ClientReceivedDataArgs(seg, Channel.Unreliable, base.Index);
                    this.HandleClientReceivedDataArgs(args);
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
                    StopConnection(false);
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

                    if (r.Player.Id == m_ServerTransportId)
                    {
                        // Debug.LogFormat("server closed");
                        if (m_callbacks.OnMasterClientMigrateCallback == null)
                        {
                            // this.OnClientDisconnected();
                            var args = new ClientConnectionStateArgs(LocalConnectionState.Stopped,
                                base.Index);
                            this.HandleClientConnectionState(args);
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
                case RelayEvent.ContentOneofCase.TellRttResponse:
                {
                    TellRttResponse r = e.TellRttResponse;
                    // m_RttUpdater.Update(r.Timestamp);
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
                case RelayEvent.ContentOneofCase.LeaveRoomResponse:
                {
                    Debug.Log("receive a leave room response ack");
                    break;
                }
                case RelayEvent.ContentOneofCase.FetchTimestampResponse:
                {
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
            m_relayLayer.Send(e.ToByteArray());
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
            m_relayLayer.Send(e.ToByteArray());
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
            m_relayLayer.Send(e.ToByteArray());
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
            m_relayLayer.Send(e.ToByteArray());
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
            m_relayLayer.Send(e.ToByteArray());
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
            m_relayLayer.Send(e.ToByteArray());

            if (callback != null)
            {
                m_internalCallbacks.Add(traceId, callback);
            }
        }
        
        public void Pause()
        { 
            m_relayLayer?.Pause();
        }
    
        public void UnPause()
        {
            m_relayLayer?.UnPause();
        }
        
        #endregion

    }
}
#endif