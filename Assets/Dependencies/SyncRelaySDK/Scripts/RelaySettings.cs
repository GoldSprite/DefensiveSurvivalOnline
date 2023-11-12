using Unity.Sync.Relay.Transport;

namespace Unity.Sync.Relay
{
    public class RelaySettings
    {
        // 环境变量
        public static string WebsocketProxy = "wss://wsp.unity.cn:443";
        public static string UosAppId = "";
        public static string UosAppSecret = "";
        public static string RoomProfileUUID = "";
#if MIRROR
        public static RelayTransportType TransportType = RelayTransportType.KCP;
#else
        public static RelayTransportType TransportType = RelayTransportType.UTP;
#endif

    }
}