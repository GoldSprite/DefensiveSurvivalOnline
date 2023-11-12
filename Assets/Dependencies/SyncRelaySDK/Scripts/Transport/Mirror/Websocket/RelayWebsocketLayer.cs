#if MIRROR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using Mirror;
using NativeWebSocket;
using Unity.Sync.Relay.Message;
using Unity.Sync.Relay.Model;
using UnityEngine;

namespace Unity.Sync.Relay.Transport.Mirror
{
    
    public class RelayWebsocketLayer : RelayLayer
    {
        public const string Scheme = "websocekt";
        
        public Action OnConnectedCallback = () => Debug.LogWarning("OnClientDisconnected called with no handler");

        public Action<RelayEvent> OnDispatchEvent = (e) => Debug.LogWarning("OnDispatchEvent called with no handler");
        
        /// <summary>Called by Transport when the client disconnected from the server.</summary>
        public Action OnDisconnectedCallback = () => Debug.LogWarning("OnClientDisconnected called with no handler");
        
        private WebSocket client;
        
        private IOBuffer _ioBuffer;
        
        public RelayWebsocketLayer(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            this.OnConnectedCallback = OnConnectedCallback;
            this.OnDispatchEvent = OnDispatchEvent;
            this.OnDisconnectedCallback = OnDisconnectedCallback;
        }
        
        public bool Available()
        {
            return true;
        }

        public void Connect(string ip, ushort port)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            string uri = string.Format("ws://{0}:{1}/room/ws", ip, port);
            
            if (RelaySettings.TransportType == RelayTransportType.WebSocketSecure)
            {
                uri = $"{RelaySettings.WebsocketProxy}/room/ws?ip={ip}&port={port}" ;
            }
            
            Debug.Log("connect to uri: " + uri);

            client = new WebSocket(uri, headers);
            _ioBuffer = new IOBuffer();
            
            client.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                OnConnectedCallback.Invoke();
            };

            client.OnError += (e) =>
            {
                // 这边需要加相关的操作
                Debug.Log("Websocket Error! " + e);
                OnDisconnectedCallback.Invoke();
            };

            client.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                OnDisconnectedCallback.Invoke();
            };

            client.OnMessage += (bytes) =>
            {
                if (client.State == WebSocketState.Open)
                {
                    List<RelayEvent> evts = _ioBuffer.read(bytes, bytes.Length);
                    foreach (RelayEvent e in evts)
                    {
                        OnDispatchEvent?.Invoke(e);
                    }
                }
            };
            
            client.Connect();
        }
        
        public void Disconnect()
        {
            client?.Close();
            _ioBuffer?.Dispose();
        }
        
        public void Send(byte[] data)
        { 
            byte[] seg = RelayBaseHeader.SendMessageWithoutLimit(data);
            if (client != null && client.State == WebSocketState.Open)
            {
                client.Send(seg);
            }
        }
        
        public bool IsConnected()
        {
            return client != null;
        }
        
        public string ServerGetClientAddress(int connectionId)
        {
            Debug.Log("call ServerGetClientAddress");
            return "0.0.0.0:0000";
        }
        
        public int GetMaxPacketSize(int channelId = Channels.Reliable)
        {
            return 16 * 1024;
        }
        
        public void EarlyUpdate()
        {
            // Debug.Log("client early update");
            client?.DispatchMessageQueue();
        }
        
        public void LateUpdate()
        {
            // Debug.Log("client early update");
        }
        
        public void Pause()
        {
            // throw new NotImplementedException();
        }
        
        public void UnPause()
        {
            // throw new NotImplementedException();
        }
        
        public void Shutdown()
        {
            Debug.Log("websocket transport shutdown");
            client?.Close();
            client = null;
            _ioBuffer?.Dispose();
            _ioBuffer = null;
        }

    }
}
#endif
