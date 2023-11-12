#if MIRROR
using System;
using System.Collections.Generic;
using System.Net;
using Google.Protobuf;
using kcp2k;
using Mirror;
using Unity.Collections;
using Unity.Sync.Relay.Message;
using UnityEngine;
using Unity.Sync.Relay.Model;
using UnityEngine.Serialization;

namespace Unity.Sync.Relay.Transport.Mirror
{
    public class RelayKcpLayer : RelayLayer
    { 
        public const string Scheme = "kcp";
        
        public bool NoDelay = true;

        public uint Interval = 10;

        public int Timeout = 30000;
        
        public int FastResend = 2;

        public bool CongestionWindow = false; // KCP 'NoCongestionWindow' is false by default. here we negate it for ease of use.

        public uint SendWindowSize = 4096; //Kcp.WND_SND; 32 by default. Mirror sends a lot, so we need a lot more.

        public uint ReceiveWindowSize = 4096; //Kcp.WND_RCV; 128 by default. Mirror sends a lot, so we need a lot more.

        KcpClient client;
        
        public Action OnConnectedCallback = () => Debug.LogWarning("OnClientDisconnected called with no handler");

        public Action<RelayEvent> OnDispatchEvent = (e) => Debug.LogWarning("OnDispatchEvent called with no handler");

        //// <summary>Called by Transport when the client encountered an error.</summary>
        // public Action<Exception> OnClientError = (error) => Debug.LogWarning("OnClientError called with no handler");

        /// <summary>Called by Transport when the client disconnected from the server.</summary>
        public Action OnDisconnectedCallback = () => Debug.LogWarning("OnClientDisconnected called with no handler");
        
        private IOBuffer _ioBuffer;

        public RelayKcpLayer(
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
            // 平台和协议之间的支持程度, support
            Debug.Log("call available");
            // 如果这边支持kcp
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public void Connect(string ip, ushort port)
        {
            _ioBuffer = new IOBuffer();

            client = new KcpClient(
                () =>
                {
                    Debug.Log("connected to relay server");
                    OnConnectedCallback.Invoke();
                },
                (message) =>
                {
                    byte[] bytes = message.ToArray();
                    List<RelayEvent> evts = _ioBuffer.read(bytes, bytes.Length);
                    foreach (RelayEvent e in evts)
                    {
                        OnDispatchEvent?.Invoke(e);
                    }
                },
                () =>
                {
                    // TODO，需要再次确认逻辑
                    Debug.Log("connection layer, ack close!");
                    OnDisconnectedCallback.Invoke();
                });
            
            client.Connect(ip, port, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize,
            ReceiveWindowSize, Timeout);
            // role = RelayRole.CLIENT_ROLE;
        }
        
        public void Disconnect()
        {
            client?.Disconnect();
            _ioBuffer?.Dispose();
        }

        public void Send(byte[] data)
        { 
            byte[] res = RelayBaseHeader.SendMessageWithoutLimit(data);
            var seg = new ArraySegment<byte>(res);
            client.Send(seg, KcpChannel.Reliable);
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
            switch (channelId)
            {
                case Channels.Unreliable:
                    return KcpConnection.UnreliableMaxMessageSize;
                default:
                    return KcpConnection.ReliableMaxMessageSize;
            }
        }
        
        public void EarlyUpdate()
        {
            // Debug.Log("client early update");
            if (client != null)
            {
                client.TickIncoming();
            }
        }
        
        public void LateUpdate()
        {
            // Debug.Log("client early update");
            if (client != null)
            {
                client.TickOutgoing();
            }
        }
        
        public void Pause()
        {
            // throw new NotImplementedException();
            client?.Pause();
        }
        
        public void UnPause()
        {
            // throw new NotImplementedException();
            client?.Unpause();
        }
        
        public void Shutdown()
        {
            // throw new NotImplementedException();
            Debug.Log("kcp transport shutdown");
            client?.Disconnect();
            client = null;
            _ioBuffer?.Dispose();
            _ioBuffer = null;
        }

        // public override void OnApplicationQuit()
        // {
        //     base.OnApplicationQuit();
        // }
    }
}
#endif