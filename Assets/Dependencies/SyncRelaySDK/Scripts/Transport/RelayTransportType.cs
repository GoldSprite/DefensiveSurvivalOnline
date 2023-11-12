using System.ComponentModel;

namespace Unity.Sync.Relay.Transport
{
    
    public enum RelayTransportType
    {
        [Description("Unity Transport Protocol")]
        UTP,
        
        [Description("Kcp2k Base On KCP Protocol")]
        KCP,
        
        [Description("Websocket")]
        Websocket,
        
        // Websocket With SSL
        [Description("WebSocketSecure")]
        WebSocketSecure,
    }

}