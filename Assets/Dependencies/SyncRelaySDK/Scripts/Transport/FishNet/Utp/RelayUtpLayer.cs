#if FISHNET
using System;
using System.Collections.Generic;
using FishNet.Transporting;
using Google.Protobuf;
using kcp2k;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Sync.Relay.Message;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Sync.Relay.Transport.FishNet
{
    public class RelayUtpLayer : RelayLayer
    {
        public const string Scheme = "utp";

        private NetworkSettings m_networkSetting;
        private NetworkDriver m_Driver;
        private NetworkPipeline m_reliablePipeline;
        private NetworkPipeline m_unreliablePipeline;
        private NetworkEndPoint m_networkEndpoint;
        private NetworkConnection m_Connection;

        public event Action<RelayEvent, Channel> OnDispatchEvent = null;

        // 客户端在传输层，connected, 默认需要调用
        public event Action OnConnectedCallback;

        // 客户端在传输层，disconnected消息
        public event Action OnDisconnectedCallback;

        private RelayUtpIoBuffer _utpIoBuffer = new RelayUtpIoBuffer();

        // private RelayConnectionStatus _status = RelayConnectionStatus.Init;
        
        private Queue<byte[]> m_Data = new Queue<byte[]>();

        public RelayUtpLayer(
            Action OnConnectedCallback,
            Action<RelayEvent, Channel> OnDispatchEvent,
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

            m_reliablePipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            m_unreliablePipeline = m_Driver.CreatePipeline(typeof(NullPipelineStage));

            _utpIoBuffer = new RelayUtpIoBuffer();
        }

        public bool Available()
        {
            // 平台和协议之间的支持程度, support
            Debug.Log("call available");
            // 如果这边支持kcp
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public void Connect(string socketIP, ushort socketPort)
        {
            CreateNetworkSettings();
            m_Connection = default(NetworkConnection);
            m_networkEndpoint = ParseNetworkEndpoint(socketIP, socketPort);

            m_Connection = m_Driver.Connect(m_networkEndpoint);
        }

        public void Disconnect()
        {
            if (m_Driver.IsCreated)
            {
                ProcessSend();
                m_Driver.ScheduleUpdate().Complete();
                m_Driver.Disconnect(m_Connection);
            }
            
            m_Connection = default(NetworkConnection);
            m_Driver.Dispose();
            _utpIoBuffer.Dispose();

            // this._status = RelayConnectionStatus.Closed;
            OnDisconnectedCallback?.Invoke();
        }
        
        public void Update()
        {
            m_Driver.ScheduleUpdate().Complete();
            if (!IsConnected())
            {
                return;
            }

            NetworkPipeline pipeline;
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            
            while ((cmd = m_Connection.PopEvent(m_Driver, out stream, out pipeline)) != NetworkEvent.Type.Empty)
            {
                var channel = pipeline == m_reliablePipeline ? Channel.Reliable : Channel.Unreliable;
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");
                    // _status = RelayConnectionStatus.Connected;
                    OnConnectedCallback?.Invoke();
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    // *) 这边需要回调
                    List<RelayEvent> events = _utpIoBuffer.read(stream, channel);
                    for (int i = 0; i < events.Count; i++)
                    {
                        RelayEvent e = events[i];
                        OnDispatchEvent?.Invoke(e, channel);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    // _status = RelayConnectionStatus.Closed;
                    m_Connection = default(NetworkConnection);
                    // 这边也需要回调下
                    OnDisconnectedCallback?.Invoke();
                }
                
                if (!IsConnected())
                {
                    break;
                }
            }
            
            ProcessSend();
        }
        
        private void ProcessSend()
        {
            while (m_Data.Count > 0)
            {
                var result = m_Driver.BeginSend(m_reliablePipeline, m_Connection, out var writer);
                
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

        public void Send(byte[] bytes, Channel channel)
        {
            if (m_Connection == null)
            {
                Debug.Log("connection not create");
                return;
            }

            if (!m_Connection.IsCreated)
            {
                // Debug.Log("connection not create");
                return;
            }
            
            var pipeline = channel == Channel.Reliable ? m_reliablePipeline : m_unreliablePipeline;
            /*
            if (_status != RelayConnectionStatus.Connected)
            {
                return;
            }
            */

            // *) 主要调用这个, 不过这个要分段写, 
            RelayBaseHeader.SendMessage(bytes, (byte)channel, delegate(byte[] buf)
            {
                if (channel == Channel.Reliable)
                {
                    m_Data.Enqueue(buf);
                    return;
                }
                
                NativeArray<byte> nativeArray = new NativeArray<byte>(buf, Allocator.Temp);
                m_Driver.BeginSend(pipeline, m_Connection, out var writer);
                writer.WriteBytes(nativeArray);
                m_Driver.EndSend(writer);
                nativeArray.Dispose();
            });
        }

        public bool IsConnected()
        {
            return m_Driver.IsCreated && m_Connection.IsCreated;
            // return _status == RelayConnectionStatus.Connected;
        }

        public int GetMaxPacketSize(Channel channel = Channel.Reliable)
        {
            switch (channel)
            {
                case Channel.Unreliable:
                    return 1023;
                default:
                    return 1023;
            }
        }

        public void IterateIncoming()
        {
            if (IsConnected())
            {
                this.Update();
            }
        }

        public void IterateOutgoing()
        {
        }
        
        public void Pause()
        {
            
        }

        public void UnPause()
        {
            
        }

        public void Shutdown()
        {
            // throw new NotImplementedException();
            Debug.Log("utp transport shutdown");
            if (m_Driver.IsCreated)
            {
                ProcessSend();
                m_Driver.ScheduleUpdate().Complete();
            }
            m_Driver.Dispose();
            _utpIoBuffer.Dispose();
        }
    }
}
#endif