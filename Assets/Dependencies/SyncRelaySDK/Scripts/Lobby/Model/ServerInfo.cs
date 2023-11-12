using System;
using System.Collections.Generic;

namespace Unity.Sync.Relay.Lobby
{
    
    [Serializable]
    public class ServerPortItem
    {
        
        public ushort Port;
        
        public string Protocol;
        
        public string Name;
        
    }
    
    [Serializable]
    public class ServerInfo
    {
        // 分配的uuid
        public string AllocationUuid;
        
        // 服务端ip
        public string Ip;
        
        // 服务节点，开放的端口列表
        public List<ServerPortItem> ServerPorts;
        
    }
}