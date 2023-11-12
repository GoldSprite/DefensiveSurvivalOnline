using System;
using Unity.Sync.Relay.Model;
using UnityEngine;

namespace Unity.Sync.Relay
{
    public enum RelayCallback
    {
        ConnectToRelayServer = 0,
        MasterClientMigrate,
        PlayerInfoUpdate,
        RoomInfoUpdate,
        PlayerKicked,
        PlayerEnterRoom,
        PlayerLeaveRoom,
        HeartbeatTimeout,
        SetHeartbeat,
    }
    
    public class RelayCallbacks
    {
        // uint code, 表示连接的结果，可参考RelayCode，RelayCode.OK 表示成功
        // RelayRoom room, 连接成功返回当前房间信息，失败返回null
        public Action<uint, RelayRoom> OnConnectToRelayServerCallback { get; private set; }
        
        // uint newMasterClientID, 新的MasterClient的TransportId
        // 在MasterClient在退出房间或掉线时会触发，如果未勾选DisableDisconnectRemoteClient，则仅会在被动掉线时触发
        // 注册该回调函数后，如果MasterClient离开，当前玩家不会断开连接，后续流程会由OnMasterClientMigrateCallback处理
        public Action<uint> OnMasterClientMigrateCallback { get; private set; }
        
        // RelayPlayer player, 表示更新的玩家信息
        public Action<RelayPlayer> OnPlayerInfoUpdateCallback { get; private set; }

        // RelayRoom room, 表示更新后的房间信息
        public Action<RelayRoom> OnRoomInfoUpdateCallback { get; private set; }

        // uint code, 表示玩家被踢掉的原因，可参考RelayCode
        // string reason, 表示玩家被踢掉的原因
        public Action<uint, string> OnPlayerKickedCallback { get; private set; }
        
        // RelayPlayer player, 表示加入房间的玩家信息
        public Action<RelayPlayer> OnPlayerEnterRoom { get; private set; }
        
        // RelayPlayer player, 表示离开房间的玩家信息
        public Action<RelayPlayer> OnPlayerLeaveRoom { get; private set; }

        // 当客户端到Relay Server的心跳超时时会触发
        public Action OnHeartbeatTimeout { get; private set; }
        
        // uint code, 表示设置的结果，可参考RelayCode
        // uint timeout, 心跳超时时间，单位为s
        public Action<uint, uint> OnSetHeartbeat { get; private set; }
        
        public void RegisterConnectToRelayServer(Action<uint, RelayRoom> callback)
        {
            this.OnConnectToRelayServerCallback = callback;
        }

        public void RegisterMasterClientMigrate(Action<uint> callback)
        {
            this.OnMasterClientMigrateCallback = callback;
        }
        
        public void RegisterPlayerInfoUpdate(Action<RelayPlayer> callback)
        {
            this.OnPlayerInfoUpdateCallback = callback;
        }
        
        public void RegisterRoomInfoUpdate(Action<RelayRoom> callback)
        {
            this.OnRoomInfoUpdateCallback = callback;
        }

        public void RegisterPlayerKicked(Action<uint, string> callback)
        {
            this.OnPlayerKickedCallback = callback;
        }
        
        public void RegisterPlayerEnterRoom(Action<RelayPlayer> callback)
        {
            this.OnPlayerEnterRoom = callback;
        }
        
        public void RegisterPlayerLeaveRoom(Action<RelayPlayer> callback)
        {
            this.OnPlayerLeaveRoom = callback;
        }
        
        public void RegisterHeartbeatTimout(Action callback)
        {
            this.OnHeartbeatTimeout = callback;
        }
        
        public void RegisterSetHeartbeat(Action<uint, uint> callback)
        {
            this.OnSetHeartbeat = callback;
        }

        public void Remove(RelayCallback code)
        {
            switch (code)
            {
                case RelayCallback.ConnectToRelayServer:
                    this.OnConnectToRelayServerCallback = null;
                    break;
                case RelayCallback.MasterClientMigrate:
                    this.OnMasterClientMigrateCallback = null;
                    break;
                case RelayCallback.PlayerInfoUpdate:
                    this.OnPlayerInfoUpdateCallback = null;
                    break;
                case RelayCallback.RoomInfoUpdate:
                    this.OnRoomInfoUpdateCallback = null;
                    break;
                case RelayCallback.PlayerKicked:
                    this.OnPlayerKickedCallback = null;
                    break;
                case RelayCallback.PlayerEnterRoom:
                    this.OnPlayerEnterRoom = null;
                    break;
                case RelayCallback.PlayerLeaveRoom:
                    this.OnPlayerLeaveRoom = null;
                    break; 
                case RelayCallback.HeartbeatTimeout:
                    this.OnHeartbeatTimeout = null;
                    break;
                case RelayCallback.SetHeartbeat:
                    this.OnSetHeartbeat = null;
                    break;
                default:
                    Debug.Log("Unsupported callback");
                    break;
            }
        }
    }
}