using System;
using Unity.Sync.Relay.Transport;

namespace Unity.Sync.Relay.Lobby
{
    // 静态工具类
    //  1. convert
    //  2. 提取有效的信息
    public class LobbyUtility
    {
        // 从这提取，相关的ip:port
        public static string ParseIP(ServerInfo serverInfo)
        {
            if (serverInfo == null) return null;
            return serverInfo.Ip;
        }

        public static ushort ParsePort(ServerInfo serverInfo)
        {
            if (serverInfo == null) return 0;
            if (serverInfo.ServerPorts == null || serverInfo.ServerPorts.Count == 0) return 0;

            foreach (var portItem in serverInfo.ServerPorts)
            {
                if (RelaySettings.TransportType == RelayTransportType.UTP)
                {
                    if ("UDP".Equals(portItem.Protocol, StringComparison.OrdinalIgnoreCase)
                        && "utp".Equals(portItem.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return portItem.Port;
                    }
                }
                else if (RelaySettings.TransportType == RelayTransportType.Websocket)
                {
                    if ("TCP".Equals(portItem.Protocol, StringComparison.OrdinalIgnoreCase)
                        && "ws".Equals(portItem.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return portItem.Port;
                    }
                }
                else if (RelaySettings.TransportType == RelayTransportType.WebSocketSecure)
                {
                    if ("TCP".Equals(portItem.Protocol, StringComparison.OrdinalIgnoreCase)
                        && "ws".Equals(portItem.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return portItem.Port;
                    }
                }
                else if (RelaySettings.TransportType == RelayTransportType.KCP)
                {
                    if ("UDP".Equals(portItem.Protocol, StringComparison.OrdinalIgnoreCase)
                        && "kcp2k".Equals(portItem.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return portItem.Port;
                    }
                }
            }

            return 0;
        }
    }
    
    public enum LobbyRoomStatus
    {
        // SERVER_ALLOCATED, ALLOCATION_FAILED, CLOSED, CREATED
        Unknown,
        Created,
        ServerAllocated,
        Ready,
        Running,
        AllocatedFailed,
        Closed
    }
    
    public class LobbyRoomStatusHelper
    {
        public static LobbyRoomStatus Resolve(string v)
        {
            if (string.IsNullOrEmpty(v)) return LobbyRoomStatus.Unknown;
            if ("SERVER_ALLOCATED".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.ServerAllocated;
            }
            else if ("READY".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.Ready;
            }
            else if ("RUNNING".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.Running;
            }
            else if ("ALLOCATION_FAILED".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.AllocatedFailed;
            }
            else if ("CLOSED".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.Closed;
            }
            else if ("CREATED".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomStatus.Created;
            }

            return LobbyRoomStatus.Unknown;
        }

        public static string ValueOf(LobbyRoomStatus s)
        {
            switch (s)
            {
                case LobbyRoomStatus.Created:
                    return "CREATED";
                case LobbyRoomStatus.Closed:
                    return "CLOSED";
                case LobbyRoomStatus.ServerAllocated:
                    return "SERVER_ALLOCATED";
                case LobbyRoomStatus.Ready:
                    return "READY";
                case LobbyRoomStatus.Running:
                    return "RUNNING";
                case LobbyRoomStatus.AllocatedFailed:
                    return "ALLOCATION_FAILED";
            }

            return "UNKNOWN";
        }
    }
    
    public enum LobbyRoomVisibility
    {
        Unknown,
        Public,
        Private
    }
    
    public class LobbyRoomVisibilityHelper
    {
        public static LobbyRoomVisibility Resolve(string v)
        {
            if (string.IsNullOrEmpty(v)) return LobbyRoomVisibility.Unknown;
            if ("PUBLIC".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomVisibility.Public;
            }
            else if ("PRIVATE".Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return LobbyRoomVisibility.Private;
            }

            return LobbyRoomVisibility.Unknown;
        }

        public static string ValueOf(LobbyRoomVisibility s)
        {
            // if (s == null) return "UNKNOWN";
            switch (s)
            {
                case LobbyRoomVisibility.Public:
                    return "PUBLIC";
                case LobbyRoomVisibility.Private:
                    return "PRIVATE";
            }

            return "UNKNOWN";
        }
    }

}