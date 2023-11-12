#if NETCODE_GAMEOBJECTS_1_3_1
using System;
using System.Collections.Generic;
using Google.Protobuf;
using kcp2k;
using Unity.Sync.Relay.Message;
using UnityEngine;
using System.Linq;

namespace Unity.Sync.Relay.Transport.Netcode
{
    public class RelayKcp2kClient : RelayTransportClient
    {
        
        public event Action<RelayEvent> OnDispatchEvent = null;

        // 客户端在传输层，connected, 默认需要调用
        public event Action OnConnectedCallback;

        // 客户端在传输层，disconnected消息
        public event Action OnDisconnectedCallback;

        private RelayConnectionStatus _status = RelayConnectionStatus.Init;
        
        private IOBuffer _ioBuffer = new IOBuffer();
        
        public RelayKcp2kClient(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            this.OnConnectedCallback = OnConnectedCallback;
            this.OnDispatchEvent = OnDispatchEvent;
            this.OnDisconnectedCallback = OnDisconnectedCallback;
        }
        
        void Start(string serverIp, ushort serverPort)
        {
            _ioBuffer = new IOBuffer();
            
            // 里面没有await操作，所以取消async修饰
            client = new KcpClient(
                () =>
                {
                    _status = RelayConnectionStatus.Connected;
                    OnConnectedCallback?.Invoke();
                },
                (message) =>
                {
                    if (_status != RelayConnectionStatus.Connected) return;
                    byte[] data = message.ToArray();
                    List<RelayEvent> evts = _ioBuffer.read(data, data.Length);
                    foreach (RelayEvent e in evts)
                    {
                        OnDispatchEvent?.Invoke(e);
                    }
                },
                () =>
                {
                    _status = RelayConnectionStatus.Closed;
                    OnDisconnectedCallback?.Invoke();
                });
            
            client.Connect(serverIp, serverPort, NoDelay, Interval, FastResend, CongestionWindow, SendWindowSize,
                ReceiveWindowSize, Timeout);
        }

        public override void Init(string socketIP, ushort socketPort)
        {
            Start(socketIP, socketPort);
        }

        public override void Update()
        {
            if (client != null)
            {
                client.Tick();
            }
        }

        public override void SendEvent(RelayEvent eEvent)
        {
            // Debug.LogFormat("sorry to send, ignore!");
            if (IsConnected())
            {
                /*
                byte[] data = eEvent.ToByteArray();
                ArraySegment<byte> seg = new ArraySegment<byte>(data);
                client.Send(seg, KcpChannel.Reliable);
                */
                byte[] data = eEvent.ToByteArray();
                byte[] res = RelayBaseHeader.SendMessageWithoutLimit(data);
                ArraySegment<byte> seg = new ArraySegment<byte>(res);
                client.Send(seg, KcpChannel.Reliable);
            }
        }

        public override bool IsConnected()
        {
            return _status == RelayConnectionStatus.Connected;
        }

        public override void Disconnect()
        {
            if (IsConnected())
            {
                client.Disconnect();
            }
            _status = RelayConnectionStatus.Closed;
        }

        public override void OnDestroy()
        {
            Disconnect();
            if (_ioBuffer != null)
            {
                _ioBuffer.Dispose();
                _ioBuffer = null;
            }
        }

        public override void Pause()
        {
            client?.Pause();
        }

        public override void UnPause()
        {
            client?.Unpause();
        }

        // // scheme used by this transport
        // public const string Scheme = "kcp";

        public bool NoDelay = true;

        public uint Interval = 10;

        public int Timeout = 30000;

        public int FastResend = 2;

        public bool CongestionWindow = false; // KCP 'NoCongestionWindow' is false by default. here we negate it for ease of use.

        public uint SendWindowSize = 4096; //Kcp.WND_SND; 32 by default. Mirror sends a lot, so we need a lot more.

        public uint ReceiveWindowSize = 4096; //Kcp.WND_RCV; 128 by default. Mirror sends a lot, so we need a lot more.

        KcpClient client;

        // all except WebGL
        public override bool Available() =>
            Application.platform != RuntimePlatform.WebGLPlayer;
        
    }
    
    
    public class RelayKcp2KTransportFactory : RelayTransportFactory
    {
        // 工厂方法
        public override RelayTransportClient Create(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            return new RelayKcp2kClient(OnConnectedCallback, OnDispatchEvent, OnDisconnectedCallback);
        }

    }
}
#endif