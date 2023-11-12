# Sync Relay SDK
### 相关接口
------------------
    RelayRoom 是描述房间信息的类

    namespace Unity.Sync.Relay.Model
    {
        public class RelayRoom
        {
            public string Name;

            public string NameSpace;

            public string ID;

            public ulong MasterClientID;

            public Dictionary<uint, RelayPlayer> Players;

            public LobbyRoomStatus Status;

            public string IP;

            public ushort Port;

            public string JoinCode;

            public Dictionary<string, string> CustomProperties;
        }
    }

------------------

    RelayPlayer 是描述玩家信息的类

    namespace Unity.Sync.Relay.Model
    {
        public class RelayPlayer
        {
            public string ID;

            public string Name;

            public uint TransportId;

            public Dictionary<string, string> Properties;
        }
    }

------------------

    Lobby 是 Sync Relay 为客户端提供的异步创建房间/异步查询房间列表/异步查询房间信息的类

    namespace Unity.Sync.Relay.Lobby
    {
        public class CreateRoomRequest;
        public class CreateRoomResponse;

        public class ListRoomRequest;
        public class ListRoomResponse;

        public class QueryRoomResponse;
        public class ChangeRoomStatusResponse;

        public class LobbyService
        {
            // 异步创建房间
            public static IEnumerator AsyncCreateRoom(CreateRoomRequest req, Action<CreateRoomResponse> callback);
            // 异步查询房间列表
            public static IEnumerator AsyncListRoom(ListRoomRequest request, Action<ListRoomResponse> callback);
            // 异步查询房间信息
            public static IEnumerator AsyncQueryRoom(String roomId, Action<QueryRoomResponse> callback);
            // 改变房间状态，仅支持在Ready和Running之间切换
            public static IEnumerator ChangeRoomStatus(String roomUuid, LobbyRoomStatus status, Action<ChangeRoomStatusResponse> callback)
        }
    }

------------------

    RelayCallbacks 是 Sync Relay 提供的用户可自定义的回调类

    namespace Unity.Sync.Relay
    {
        // 目前支持的回调函数类型
        public enum RelayCallback
        {
            ConnectToRelayServer = 0,
            MasterClientMigrate,
            PlayerInfoUpdate,
            RoomInfoUpdate,
            PlayerKicked,
            PlayerEnterRoom,
            PlayerLeaveRoom,
            SetHeartbeat
        }

        public class RelayCallbacks
        {
            // 目前支持的回调函数接口定义

            // uint code, 表示连接的结果，可参考RelayCode
            // RelayRoom room, 连接成功返回当前房间信息，失败返回null
            public Action<uint, RelayRoom> OnConnectToRelayServerCallback;

            // uint newMasterClientID, 新的MasterClient的TransportId
            // 在MasterClient在退出房间或掉线时会触发，(对于Netcode，如果未勾选DisableDisconnectRemoteClient，则仅会在被动掉线时触发）
            // 注册该回调函数后，如果MasterClient离开，当前玩家不会断开连接，后续流程会由OnMasterClientMigrateCallback处理
            public Action<uint> OnMasterClientMigrateCallback;

            // RelayPlayer player, 表示更新的玩家信息
            public Action<RelayPlayer> OnPlayerInfoUpdateCallback;

            // RelayRoom room, 表示更新后的房间信息
            public Action<RelayRoom> OnRoomInfoUpdateCallback;

            // uint code, 表示玩家被踢掉的原因，可参考RelayCode
            // string reason, 表示玩家被踢掉的原因
            public Action<uint, string> OnPlayerKickedCallback;

            // RelayPlayer player, 表示加入房间的玩家信息
            public Action<RelayPlayer> OnPlayerEnterRoom;
        
            // RelayPlayer player, 表示离开房间的玩家信息
            public Action<RelayPlayer> OnPlayerLeaveRoom;
        
            // 当客户端到Relay Server的心跳超时时会触发
            public Action OnHeartbeatTimeout;

            // 调用SetHeartbeat完成后触发
            // uint code, 表示设置的结果，可参考RelayCode
            // uint timeout, 心跳超时时间，单位为s
            public Action<uint, uint> OnSetHeartbeat;

            // 注册回调函数，重复调用会覆盖之前的记录（确保回调函数的类型和定义保持一致）
            public void RegisterConnectToRelayServer(Action<uint, RelayRoom> callback);
            
            public void RegisterMasterClientMigrate(Action<uint> callback);

            public void RegisterPlayerInfoUpdate(Action<RelayPlayer> callback);

            public void RegisterRoomInfoUpdate(Action<RelayRoom> callback);

            public void RegisterPlayerKicked(Action<uint, string> callback);

            public void RegisterPlayerEnterRoom(Action<RelayPlayer> callback);

            public void RegisterPlayerLeaveRoom(Action<RelayPlayer> callback);

            public void RegisterHeartbeatTimout(Action callback);

            public void RegisterSetHeartbeat(Action<uint, uint> callback);

            // 删除回调函数
            public void Remove(RelayCallback code);
        }
    }

------------------
### Netcode 相关接口

    RelayTransportNetcode 是 Sync Relay 提供的实现Netcode Transport的类

    namespace Unity.Sync.Relay.Transport.Netcode
    {
        public class RelayTransportNetcode : NetworkTransport
        {
            // 默认为false
            // 设为true将会禁用掉Netcode里NetworkTransport的DisconnectRemoteClient()方法
            public bool DisableDisconnectRemoteClient = false;

            // 设置房间信息
            public void SetRoomData(CreateRoomResponse resp);
            public void SetRoomData(QueryRoomResponse resp);

            // 设置私有房间的Join Code
            public void SetJoinCode(string joinCode);

            // 设置玩家信息
            public void SetPlayerData(string Id, string Name);
            public void SetPlayerData(string Id, string Name, Dictionary<string, string> Properties);

            // 设置回调函数
            public void SetCallbacks(RelayCallbacks callbacks);

            // 获取房间/玩家的信息
            public RelayRoom GetRoomInfo();
            public RelayPlayer GetPlayerInfo(uint transportId);
            public RelayPlayer GetCurrentPlayer();

            // 更新玩家信息/房间属性
            // 玩家信息是根据 TransportId 更新 Name/Properties ( ID和TransportId不支持更新 )
            public void UpdatePlayerInfo(RelayPlayer player);
            public void UpdateRoomCustomProperties(Dictionary<string, string> properties);
            
            // 踢掉玩家
            // reason可不填，默认为空
            public void KickPlayer(uint transportId, string reason);

            // 获取客户端到Relay Server的往返时延
            public ulong GetRelayServerRtt();
        }
    }

### Netcode 集成示例
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using Unity.Sync.Relay;
    using Unity.Sync.Relay.Lobby;
    using Unity.Sync.Relay.Model;
    using Unity.Sync.Relay.Transport.Netcode;

    public class Demo : MonoBehaviour
    {

        private string playerName;
        private string playerUuid;

        // Start()会在第一帧Update()之前被调用
        private void Start()
        {
            // 需要在创建或加入房间之前，设置好玩家信息 
            // playerUuid是Unique ID，用于表明用户的身份
            // playerName是用户名
            playerUuid = Guid.NewGuid().ToString();
            playerName = "Player-" + playerUuid;
            NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetPlayerData(playerUuid, playerName);

            // 需要在创建或加入房间之前，配置好回调函数
            var callbacks = new RelayCallbacks();
            callbacks.RegisterConnectToRelayServer(OnConnectToRelayServer);
            NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetCallbacks(callbacks);
        }

        public void OnConnectToRelayServer(uint code, RelayRoom room)
        {
            Debug.Log("OnConnectToRelayServer Called");
            if (code == (uint)RelayCode.OK)
            {
                Debug.LogFormat("Connect To Relay Server Succeed. ( Room : {0} )", room.Name);
            }
            else
            {
                Debug.LogFormat("Connect To Relay Server Failed with Code {0}.", code);
            }
        }

        // 以Server身份加入游戏
        public void OnStartServerButton()
        {
            // 异步创建房间
            StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
            {
                Name = "Demo",
                MaxPlayers = 4, // 选填项，默认值为0，表示不设上限
                OwnerId = PlayerUuid
            },( resp) =>
            {
                if ( resp.Code == (uint)RelayCode.OK )
                {
                    Debug.Log("Create Room succeed.");
                    if (resp.Status == LobbyRoomStatus.ServerAllocated)
                    {
                        // 需要在连接到Relay服务器之前，设置好房间信息
                        NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetRoomData(resp);
                        // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                        // NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetJoinCode(JoinCode);
                        StartServer();
                    }
                    else
                    {
                        Debug.Log("Room Status Exception : " + resp.Status.ToString());
                    }
                }
                else
                {
                    Debug.Log("Create Room Fail By Lobby Service");
                }
            }));
        }

        private void StartServer()
        {
            NetworkManager.Singleton.StartServer();
        }

        // 以Host身份加入游戏
        public void OnStartHostButton()
        {
            // 异步创建房间
            StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
            {
                Name = "Demo",
                MaxPlayers = 4, // 选填项，默认值为0，表示不设上限
                OwnerId = PlayerUuid,
                Visibility = LobbyRoomVisibility.Public, // 选填项，默认值为Public，如果选择Private，则必须带上JoinCode
                JoinCode = "U", // 选填项，仅在Visibility值为Private时带上
            }, ( resp) =>
            {
                if (resp.Code == (uint)RelayCode.OK)
                {
                    Debug.Log("Create Room succeed.");
                    if (resp.Status == LobbyRoomStatus.ServerAllocated)
                    {
                        // 需要在连接到Relay服务器之前，设置好房间信息
                        NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetRoomData(resp);
                        // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                        // NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetJoinCode(JoinCode);
                        StartHost();
                    }
                    else
                    {
                        Debug.Log("Room Status Exception : " + resp.Status.ToString());
                    }
                }
                else
                {
                    Debug.Log("Create Room Fail By Lobby Service");
                }
            }));
        }

        private void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        // 以Client身份加入游戏
        public void OnStartClientButton()
        {
            // 异步查询房间列表
            StartCoroutine(LobbyService.AsyncListRoom(new ListRoomRequest()
            {
                Namespace = "Unity",
                Start = 0,
                Count = 10,
                Name = "U", // 选填项，可用于房间名的模糊搜索
            }, ( resp) =>
            {
                if (resp.Code == (uint)RelayCode.OK)
                {
                    Debug.Log("List Room succeed.");
                    if (resp.Items.Count > 0)
                    {
                        foreach (var item in resp.Items)
                        {
                            if (item.Status == LobbyRoomStatus.Ready)
                            {
                                // 异步查询房间信息
                                StartCoroutine(LobbyService.AsyncQueryRoom(item.RoomUuid,
                                    ( _resp) =>
                                    {
                                        if (_resp.Code == (uint)RelayCode.OK)
                                        {
                                            // 需要在连接到Relay服务器之前，设置好房间信息
                                            NetworkManager.Singleton.GetComponent<RelayTransportNetcode>()
                                                .SetRoomData(_resp);
                                            // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                                            // NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().SetJoinCode(JoinCode);

                                            StartClient();
                                        }
                                        else
                                        {
                                            Debug.Log("Query Room Fail By Lobby Service");
                                        }
                                    }));
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("List Room Fail By Lobby Service");
                }
            }));
        }

        private void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("Update Player Info");
                var p = NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().GetCurrentPlayer();
                p.Properties.Add("logo", "unity");
                // 更新玩家信息
                NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().UpdatePlayerInfo(p);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Update Room Custom Properties");
                var p = new Dictionary<string, string>();
                p.Add("logo", "unity");
                // 更新房间属性
                NetworkManager.Singleton.GetComponent<RelayTransportNetcode>().UpdateRoomCustomProperties(p);
            }
        }
    }

--------------
### Mirror 相关接口

    RelayTransportMirror 是 Sync Relay 提供的实现Mirror Transport的类

    namespace Unity.Sync.Relay.Transport.Mirror
    {
        public class RelayTransportMirror : Transport
        {
            // 设置房间信息
            public void SetRoomData(CreateRoomResponse resp);
            public void SetRoomData(QueryRoomResponse resp);

            // 设置私有房间的Join Code
            public void SetJoinCode(string joinCode);

            // 设置玩家信息
            public void SetPlayerData(string Id, string Name);
            public void SetPlayerData(string Id, string Name, Dictionary<string, string> Properties);

            // 设置回调函数
            public void SetCallbacks(RelayCallbacks callbacks);

            // 获取房间/玩家的信息
            public RelayRoom GetRoomInfo();
            public RelayPlayer GetPlayerInfo(uint transportId);
            public RelayPlayer GetCurrentPlayer();

            // 更新玩家信息/房间属性
            // 玩家信息是根据 TransportId 更新 Name/Properties ( ID和TransportId不支持更新 )
            public void UpdatePlayerInfo(RelayPlayer player);
            public void UpdateRoomCustomProperties(Dictionary<string, string> properties);
            
            // 踢掉玩家
            // reason可不填，默认为空
            public void KickPlayer(uint transportId， string reason);

            // 设置Relay Server的心跳超时时间，单位s，范围为5 - 600
            // 如果不传入callback，可以通过RegisterSetHeartbeat注册回调函数接收结果
            public void SetHeartbeat(uint seconds, Action<uint> callback);
            public void SetHeartbeat(uint seconds);

            // 暂停/重启传输层协议的心跳
            // 如果有需要临时延长心跳超时时间，先调用SetHeartbeat()，再调用Pause()
            public void Pause();
            public void UnPause();

            // 获取Relay Server的默认心跳超时时间，单位s
            public uint GetDefaultHeartbeat();
        }
    }

### Mirror 集成示例
    using System;
    using Mirror;
    using Unity.Sync.Relay;
    using Unity.Sync.Relay.Lobby;
    using Unity.Sync.Relay.Model;
    using UnityEngine;
    
    namespace Unity.Sync.Relay
    {
        public class RelayNetworkManagerHUD : MonoBehaviour
        {
            NetworkManager manager;
    
            private string playerName;
            private string playerUuid;
    
            void Awake()
            {
                manager = GetComponent<NetworkManager>();
            }
    
            private void Start()
            {
                // 需要在创建或加入房间之前，设置好玩家信息
                // playerUuid是Unique ID，用于表明用户的身份
                // playerName是用户名
                playerUuid = Guid.NewGuid().ToString();
                playerName = "Player-" + playerUuid;
                manager.GetComponent<RelayTransportMirror>().SetPlayerData(playerUuid, playerName);
            }
    
            // 以Server身份加入游戏
            private void StartServer()
            {
                // 异步创建房间
                StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
                {
                    Name = "Demo",
                    Namespace = "Unity",
                    MaxPlayers = 4, // 选填项，默认值为0，表示不设上限
                    OwnerId = uid,
                },(resp) =>
                {
                    if ( resp.Code == (uint)RelayCode.OK )
                    {
                        Debug.Log("Create Room succeed.");
                        if (resp.Status == LobbyRoomStatus.ServerAllocated)
                        {
                            // 需要在连接到Relay服务器之前，设置好房间信息
                            manager.GetComponent<RelayTransportMirror>().SetRoomData(resp);
                            // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                            // manager.GetComponent<RelayTransportMirror>().SetJoinCode(JoinCode);
                            manager.StartServer();
                        }
                        else
                        {
                            Debug.Log("Room Status Exception : " + resp.Status.ToString());
                        }
                    }
                    else
                    {
                        Debug.Log("Create Room Fail By Lobby Service");
                    }
                }));
            }
    
            // 以Host身份加入游戏
            private void StartHost()
            {
                // 异步创建房间
                StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
                {
                    Name = "Demo",
                    Namespace = "Unity",
                    MaxPlayers = 4, // 选填项，默认值为0，表示不设上限
                    OwnerId = uid,
                    Visibility = LobbyRoomVisibility.Public, // 选填项，默认值为Public，如果选择Private，则必须带上JoinCode
                    JoinCode = "U", // 选填项，仅在Visibility值为Private时带上
                },(resp) =>
                {
                    if ( resp.Code == (uint)RelayCode.OK )
                    {
                        Debug.Log("Create Room succeed.");
                        if (resp.Status == LobbyRoomStatus.ServerAllocated)
                        {
                            // 需要在连接到Relay服务器之前，设置好房间信息
                            manager.GetComponent<RelayTransportMirror>().SetRoomData(resp);
                            // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                            // manager.GetComponent<RelayTransportMirror>().SetJoinCode(JoinCode);
                            manager.StartHost();
                        }
                        else
                        {
                            Debug.Log("Room Status Exception : " + resp.Status.ToString());
                        }
                    }
                    else
                    {
                        Debug.Log("Create Room Fail By Lobby Service");
                    }
                }));
            }
    
            // 以Client身份加入游戏
            private void StartClient()
            {
                // 异步查询房间列表
                StartCoroutine(LobbyService.AsyncListRoom(new ListRoomRequest()
                {
                    Namespace = "Unity",
                    Start = 0,
                    Count = 10,
                    Name = "U", // 选填项，可用于房间名的模糊搜索
                }, (resp) =>
                {
                    if (resp.Code == (uint)RelayCode.OK)
                    {
                        Debug.Log("List Room succeed.");
                        if (resp.Items.Count > 0)
                        {
                            foreach (var item in resp.Items)
                            {
                                if (item.Status == LobbyRoomStatus.Ready)
                                {
                                    // 异步查询房间信息
                                    StartCoroutine(LobbyService.AsyncQueryRoom(item.RoomUuid,
                                        ( _resp) =>
                                        {
                                            if (_resp.Code == (uint)RelayCode.OK)
                                            {
                                                // 需要在连接到Relay服务器之前，设置好房间信息
                                                manager.GetComponent<RelayTransportMirror>().SetRoomData(_resp);
                                                // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                                                // manager.GetComponent<RelayTransportMirror>().SetJoinCode(JoinCode);
                                                manager.StartClient();
                                            }
                                            else
                                            {
                                                Debug.Log("Query Room Fail By Lobby Service");
                                            }
                                        }));
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("List Room Fail By Lobby Service");
                    }
                }));
            }
        }
    }

------------------
### FishNet 相关接口

    RelayTransportFishNet 是 Sync Relay 提供的实现FishNet Transport的类
    * 目前仅支持 UTP 和 KCP 作为传输协议

    using FishNetTransport = FishNet.Transporting.Transport;
    namespace Unity.Sync.Relay.Transport.FishNet
    {
        public class RelayTransportFishNet : FishNetTransport
        {
            // 设置是否以Host身份开启游戏，默认为False
            // 需要在调用FishNet的ServerManager.StartConnection()方法之前调用
            public void SetHost(bool host)

            // 设置房间信息
            public void SetRoomData(CreateRoomResponse resp);
            public void SetRoomData(QueryRoomResponse resp);

            // 设置私有房间的Join Code
            public void SetJoinCode(string joinCode);

            // 设置玩家信息
            public void SetPlayerData(string Id, string Name);
            public void SetPlayerData(string Id, string Name, Dictionary<string, string> Properties);

            // 设置回调函数
            public void SetCallbacks(RelayCallbacks callbacks);

            // 获取房间/玩家的信息
            public RelayRoom GetRoomInfo();
            public RelayPlayer GetPlayerInfo(uint transportId);
            public RelayPlayer GetCurrentPlayer();

            // 更新玩家信息/房间属性
            // 玩家信息是根据 TransportId 更新 Name/Properties ( ID和TransportId不支持更新 )
            public void UpdatePlayerInfo(RelayPlayer player);
            public void UpdateRoomCustomProperties(Dictionary<string, string> properties);
            
            // 踢掉玩家
            // reason可不填，默认为空
            public void KickPlayer(uint transportId, string reason);

            // 获取客户端到Relay Server的往返时延
            public ulong GetRelayServerRtt();
        }
    }

### FishNet 集成示例
    using System;
    using FishNet.Connection;
    using FishNet.Managing;
    using FishNet.Managing.Server;
    using FishNet.Transporting;
    using Unity.Sync.Relay;
    using Unity.Sync.Relay.Lobby;
    using Unity.Sync.Relay.Model;
    using Unity.Sync.Relay.Transport.FishNet;
    using UnityEngine;

    public class NetworkHudCanvases : MonoBehaviour
    {
        private NetworkManager _networkManager;

        private string playerName;
        private string playerUuid;
    
        private void Start()
        {
            _networkManager = FindObjectOfType<NetworkManager>();

            // playerUuid是Unique ID，用于表明用户的身份
            // playerName是用户名
            playerUuid = Guid.NewGuid().ToString();
            playerName = "Player-" + playerUuid;
            _networkManager.GetComponent<RelayTransportFishNet>().SetPlayerData(playerUuid, playerName);
        }
    
        public void StartHost()
        {
            if (_networkManager == null)
                return;
    
            StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
            {
                Name = "Demo",
                Namespace = "Unity",
                MaxPlayers = 20,
                OwnerId = uid
            }, (resp) =>
            {
                if (resp.Code == (uint)RelayCode.OK)
                {
                    Debug.Log("Create Room succeed.");
                    if (resp.Status == LobbyRoomStatus.ServerAllocated)
                    {
                        // 如果仅作为Server启动，不调用SetHost()
                        _networkManager.GetComponent<RelayTransportFishNet>().SetHost(true);
                        _networkManager.GetComponent<RelayTransportFishNet>().SetRoomData(resp);
                        // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                        // _networkManager.GetComponent<RelayTransportFishNet>().SetJoinCode(JoinCode);
                        _networkManager.ServerManager.StartConnection();
                    }
                    else
                    {
                        Debug.Log("Room Status Exception : " + resp.Status.ToString());
                    }
                }
                else
                {
                    Debug.Log("Create Room Fail By Lobby Service");
                }
            }));
        }

        public void StartClient()
        {
            if (_networkManager == null)
                return;
    
            StartCoroutine(LobbyService.AsyncListRoom(new ListRoomRequest()
            {
                Namespace = "Unity",
                Start = 0,
                Count = 10,
            }, ( resp) =>
            {
                if (resp.Code == (uint)RelayCode.OK)
                {
                    Debug.Log("List Room succeed.");
                    if (resp.Items.Count > 0)
                    {
                        foreach (var item in resp.Items)
                        {
                            if (item.Status == LobbyRoomStatus.Ready)
                            {
                                StartCoroutine(LobbyService.AsyncQueryRoom(item.RoomUuid,
                                    ( _resp) =>
                                    {
                                        if (_resp.Code == (uint)RelayCode.OK)
                                        {
                                            _networkManager.GetComponent<RelayTransportFishNet>()
                                                .SetRoomData(_resp);
                                            // 如果是Private类型的房间，需要开发者自行获取JoinCode，并调用以下方法设置好
                                            // _networkManager.GetComponent<RelayTransportFishNet>().SetJoinCode(JoinCode);
                                            _networkManager.ClientManager.StartConnection();
                                        }
                                        else
                                        {
                                            Debug.Log("Query Room Fail By Lobby Service");
                                        }
                                    }));
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("List Room Fail By Lobby Service");
                }
            }));
        }
    }