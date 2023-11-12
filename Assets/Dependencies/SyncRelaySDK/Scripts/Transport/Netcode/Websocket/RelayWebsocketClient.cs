#if NETCODE_GAMEOBJECTS_1_3_1
using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using NativeWebSocket;
using Unity.Sync.Relay.Message;
using UnityEngine;

namespace Unity.Sync.Relay.Transport.Netcode
{
    public class RelayWebsocketClient : RelayTransportClient
    {

        private WebSocket websocket;

        public event Action<RelayEvent> OnDispatchEvent;
        
        // 客户端在传输层，connected, 默认需要调用
        public event Action OnConnectedCallback;
        
        // 客户端在传输层，disconnected消息
        public event Action OnDisconnectedCallback;
        
        private RelayConnectionStatus _status = RelayConnectionStatus.Init;
        
        private IOBuffer _ioBuffer = new IOBuffer();
        
        public RelayWebsocketClient(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            this.OnConnectedCallback = OnConnectedCallback;
            this.OnDispatchEvent = OnDispatchEvent;
            this.OnDisconnectedCallback = OnDisconnectedCallback;
        }
    
        async void Start(string serverIp, int serverPort)
        {
            // websocket = new WebSocket("ws://echo.websocket.org");

            Dictionary<string, string> headers = new Dictionary<string, string>();
            string uri = string.Format("ws://{0}:{1}/room/ws", serverIp, serverPort);
            if (RelaySettings.TransportType == RelayTransportType.WebSocketSecure)
            {
                // uri = "wss://wsp.unity.cn:443/room/ws";
                uri = $"{RelaySettings.WebsocketProxy}/room/ws?ip={serverIp}&port={serverPort}" ;
                // headers["x-instance-ip"] = serverIp;
                // headers["x-instance-port"] = $"{serverPort}";
            }

            Debug.Log("connect to uri: " + uri);
            
            websocket = new WebSocket(uri, headers);
            _ioBuffer = new IOBuffer();

            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                _status = RelayConnectionStatus.Connected;
                OnConnectedCallback?.Invoke();
            };

            websocket.OnError += (e) =>
            {
                // 这边需要加相关的操作
                 Debug.Log("Websocket Error! " + e);
                this.Disconnect();
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                _status = RelayConnectionStatus.Closed;
                OnDisconnectedCallback?.Invoke();
                this.CleanUp();
            };

            websocket.OnMessage += (bytes) =>
            {
                if (_status != RelayConnectionStatus.Connected) return;
                List<RelayEvent> evts = _ioBuffer.read(bytes, bytes.Length);
                foreach (RelayEvent e in evts)
                {
                    OnDispatchEvent?.Invoke(e);
                }
            };
            await websocket.Connect();
        }

        public override void Init(string socketIP, ushort socketPort)
        {
            Start(socketIP, socketPort);
        }

        public override void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            // if (websocket != null)
            if (websocket != null)
            {
                websocket.DispatchMessageQueue();
            }
#endif
        }

        public override void SendEvent(RelayEvent eEvent)
        {
            if (IsConnected())
            {
                byte[] data = eEvent.ToByteArray();
                byte[] res = RelayBaseHeader.SendMessageWithoutLimit(data);
                SendWebSocketMessage(res);
            }
        }

        public override bool IsConnected()
        {
            return _status == RelayConnectionStatus.Connected;
        }

        async void SendWebSocketMessage(byte[] data)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                // Sending bytes
                await websocket.Send(data);
            }
        }

        public override void Disconnect()
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                websocket.Close();
                Debug.Log("Disconnect in websocket");
            }
        }

        public override void OnDestroy()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            _status = RelayConnectionStatus.Closed;
            this.websocket = null;
                
            if (_ioBuffer != null)
            {
                _ioBuffer.Dispose();
                _ioBuffer = null;
            }
        }
        
        public override void Pause()
        {
            
        }

        public override void UnPause()
        {
            
        }

        // 永远都行
        public override bool Available() => true;

    }
    
    // *) 然后使用
    public class RelayWebsocketTransportFactory : RelayTransportFactory
    {
        // 工厂方法
        public override RelayTransportClient Create(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            return new RelayWebsocketClient(OnConnectedCallback, OnDispatchEvent, OnDisconnectedCallback);
        }

    }
}
#endif