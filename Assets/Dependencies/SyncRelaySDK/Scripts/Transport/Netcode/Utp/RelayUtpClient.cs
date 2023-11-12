#if NETCODE_GAMEOBJECTS_1_3_1
using System;
using System.Collections.Generic;
using Google.Protobuf;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Sync.Relay.Message;
using UnityEngine;

namespace Unity.Sync.Relay.Transport.Netcode
{
    public class RelayUtpClient : RelayTransportClient
    {
        private NetworkSettings m_networkSetting;
        private NetworkDriver m_Driver;
        private NetworkPipeline m_reliableSequencedPipeline;
        private NetworkPipeline m_unreliableFragmentedPipeline;
        private NetworkPipeline m_unreliableSequencedFragmentedPipeline;
        private NetworkEndPoint m_networkEndpoint;
        private NetworkConnection m_Connection;

        public event Action<RelayEvent> OnDispatchEvent = null;

        // 客户端在传输层，connected, 默认需要调用
        public event Action OnConnectedCallback;

        // 客户端在传输层，disconnected消息
        public event Action OnDisconnectedCallback;
        
        private RelayUtpIoBuffer _mRelayUtpIoBuffer = new RelayUtpIoBuffer();
        
        private RelayConnectionStatus _status = RelayConnectionStatus.Init;

        private Queue<byte[]> m_Data = new Queue<byte[]>();
        
        public RelayUtpClient(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            this.OnConnectedCallback = OnConnectedCallback;
            this.OnDispatchEvent = OnDispatchEvent;
            this.OnDisconnectedCallback = OnDisconnectedCallback;
        }
        
        public void CreateNetworkSettings()
        {
            m_networkSetting = new NetworkSettings();
            m_networkSetting.WithNetworkConfigParameters(
                maxConnectAttempts: NetworkParameterConstants.MaxConnectAttempts,
                connectTimeoutMS: NetworkParameterConstants.ConnectTimeoutMS,
                disconnectTimeoutMS: NetworkParameterConstants.DisconnectTimeoutMS,
                heartbeatTimeoutMS: NetworkParameterConstants.HeartbeatTimeoutMS,
                maxFrameTimeMS: 100);
        
            m_Driver = NetworkDriver.Create(m_networkSetting);
        
            m_reliableSequencedPipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

            _mRelayUtpIoBuffer = new RelayUtpIoBuffer();
        }
        
        public override void Init(string socketIP, ushort socketPort)
        {
            CreateNetworkSettings();
            m_Connection = default(NetworkConnection);
            m_networkEndpoint = ParseNetworkEndpoint(socketIP, socketPort);
            
            m_Connection = m_Driver.Connect(m_networkEndpoint);
        }

        public override void Disconnect()
        {
            if (m_Driver.IsCreated)
            {
                ProcessSend();
                m_Driver.ScheduleUpdate().Complete();
                m_Driver.Disconnect(m_Connection);
            }
            m_Connection = default(NetworkConnection);
            // m_Driver.Dispose();
            // _mRelayUtpIoBuffer.Dispose();

            this._status = RelayConnectionStatus.Closed;
            OnDisconnectedCallback?.Invoke();
        }
        
        public override void OnDestroy()
        {
            ProcessSend();
            m_Driver.ScheduleUpdate().Complete();
            m_Driver.Dispose();
            _mRelayUtpIoBuffer.Dispose();
        }
        
        public override void Update()
        {
            if (!m_Driver.IsCreated)
            {
                return;
            }
            
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                return;
            }
            
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");
                    _status = RelayConnectionStatus.Connected;
                    OnConnectedCallback?.Invoke();
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    // *) 这边需要回调
                    List<RelayEvent> events = _mRelayUtpIoBuffer.read(stream);
                    for (int i = 0; i < events.Count; i++)
                    {
                        RelayEvent e = events[i];
                        OnDispatchEvent?.Invoke(e);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    _status = RelayConnectionStatus.Closed;
                    m_Connection = default(NetworkConnection);
                    // 这边也需要回调下
                    OnDisconnectedCallback?.Invoke();
                }
            }
            
            ProcessSend();
        }

        private void ProcessSend()
        {
            while (m_Data.Count > 0)
            {
                var result = m_Driver.BeginSend(m_reliableSequencedPipeline, m_Connection, out var writer);
                if (result != (int)StatusCode.Success)
                {
                    Debug.LogError($"Error begin sending message: {result}");
                    return;
                }

                var data = m_Data.Peek();
                NativeArray<byte> nativeArray = new NativeArray<byte>(data, Allocator.Temp);
                writer.WriteBytes(nativeArray);
                result = m_Driver.EndSend(writer);
                nativeArray.Dispose();
                
                if (result == data.Length)
                {
                    m_Data.Dequeue();
                }
                else
                {
                    if (result != (int)StatusCode.NetworkSendQueueFull)
                    {
                        m_Data.Dequeue();
                        Debug.LogError($"Error sending the message: {result}");
                    }
                    else
                    {
                        // Debug.LogError($"Network Send Queue Full");
                    }

                    return;
                }
            }
        }

        private static NetworkEndPoint ParseNetworkEndpoint(string ip, ushort port)
        {
            if (!NetworkEndPoint.TryParse(ip, port, out var endpoint))
            {
                Debug.LogError($"Invalid network endpoint: {ip}:{port}.");
                return default;
            }
            return endpoint;
        }
        
        public override void SendEvent(RelayEvent eEvent)
        {
            if (m_Connection == null)
            {
                Debug.Log("connection not create");
                return;
            }
            if (!m_Connection.IsCreated)
            { 
                Debug.Log("connection not create");
                return;
            }

            if (_status != RelayConnectionStatus.Connected)
            {
                // 这边需要把魔数，尽可能的去掉
                return;
            }

            byte[] bytes = eEvent.ToByteArray();
            // *) 主要调用这个, 不过这个要分段写, 
            RelayBaseHeader.SendMessage(bytes, delegate (byte[] buf) {
                m_Data.Enqueue(buf);
            });
        }

        public override bool IsConnected()
        {
            return _status == RelayConnectionStatus.Connected;
        }
        
        public override void Pause()
        {
            
        }

        public override void UnPause()
        {
            
        }
        
        // all except WebGL
        public override bool Available() =>
            Application.platform != RuntimePlatform.WebGLPlayer;
        
    }
    
    
    public class RelayUtpTransportFactory : RelayTransportFactory
    {
        // 工厂方法
        public override RelayTransportClient Create(
            Action OnConnectedCallback,
            Action<RelayEvent> OnDispatchEvent,
            Action OnDisconnectedCallback
        )
        {
            return new RelayUtpClient(OnConnectedCallback, OnDispatchEvent, OnDisconnectedCallback);
        }

    }
}
#endif