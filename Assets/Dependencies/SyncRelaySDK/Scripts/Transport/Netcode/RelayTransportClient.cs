#if NETCODE_GAMEOBJECTS_1_3_1
using System;
using System.Collections.Generic;
using Unity.Sync.Relay.Message;
using UnityEngine;

namespace Unity.Sync.Relay.Transport.Netcode
{
    public enum RelayConnectionStatus
    {
        Init,
        Connected,
        Closed,
    }

    public abstract class RelayTransportClient
    {
        public abstract void Init(string socketIP, ushort socketPort);

        public abstract void Disconnect();

        public abstract void OnDestroy();

        public abstract void Update();

        public abstract void SendEvent(RelayEvent eEvent);

        public abstract bool IsConnected();
        
        public abstract void Pause();
        
        public abstract void UnPause();

        // 针对平台进行判定
        public abstract bool Available();
        
    }
    
    public abstract class RelayTransportFactory
    {
        
        // 工厂方法
        public abstract RelayTransportClient Create(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent, 
            Action OnDisconnectedCallback
            );
    }

    public class RelayTransportFactoryRegistry
    {
        private static Dictionary<RelayTransportType, RelayTransportFactory> gFactory =
            new Dictionary<RelayTransportType, RelayTransportFactory>();

        static RelayTransportFactoryRegistry()
        {
            Register(RelayTransportType.WebSocketSecure, new RelayWebsocketTransportFactory());
            Register(RelayTransportType.Websocket, new RelayWebsocketTransportFactory());
            Register(RelayTransportType.KCP, new RelayKcp2KTransportFactory());
            Register(RelayTransportType.UTP, new RelayUtpTransportFactory());
        }

        public static void Register(RelayTransportType type, RelayTransportFactory factory)
        {
            gFactory.Add(type, factory);
        }

        public static RelayTransportClient Create(
            RelayTransportType type,
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
            )
        {
            if (!gFactory.ContainsKey(type))
            {
                Debug.LogWarningFormat("RelayTranportType: {0} not exists!", type);
                return null;
            }

            RelayTransportFactory factory = gFactory[type];
            return factory.Create(OnConnectedCallback, OnDispatchEvent, OnDisconnectedCallback);
        }

    }
}
#endif