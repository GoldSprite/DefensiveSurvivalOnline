#if FISHNET
using System;
using FishNet.Transporting;
using UnityEngine;

namespace Unity.Sync.Relay.Transport.FishNet
{
    // *) 
    public interface RelayLayer
    {

        // /// <summary>Called by Transport when the client connected to the server.</summary>
        // public Action OnClientConnected = () => Debug.LogWarning("OnClientConnected called with no handler");
        //
        // /// <summary>Called by Transport when the client received a message from the server.</summary>
        // public Action<ArraySegment<byte>, int> OnClientDataReceived = (data, channel) => Debug.LogWarning("OnClientDataReceived called with no handler");
        //
        // /// <summary>Called by Transport when the client encountered an error.</summary>
        // public Action<Exception> OnClientError = (error) => Debug.LogWarning("OnClientError called with no handler");
        //
        // /// <summary>Called by Transport when the client disconnected from the server.</summary>
        // public Action OnClientDisconnected = () => Debug.LogWarning("OnClientDisconnected called with no handler");
        //
        // // 这属于服务端的回调
        // /// <summary>Called by Transport when a new client connected to the server.</summary>
        // public Action<int> OnServerConnected = (connId) => Debug.LogWarning("OnServerConnected called with no handler");
        //
        // /// <summary>Called by Transport when the server received a message from a client.</summary>
        // public Action<int, ArraySegment<byte>, int> OnServerDataReceived = (connId, data, channel) => Debug.LogWarning("OnServerDataReceived called with no handler");
        //
        // /// <summary>Called by Transport when a server's connection encountered a problem.</summary>
        // public Action<int, Exception> OnServerError = (connId, error) => Debug.LogWarning("OnServerError called with no handler");
        //
        // /// <summary>Called by Transport when a client disconnected from the server.</summary>
        // public Action<int> OnServerDisconnected = (connId) => Debug.LogWarning("OnServerDisconnected called with no handler");
        
        // public void setup(RelayTransportMirror transport);

        public bool Available();

        public void Connect(string ip, ushort port);

        public void Send(byte[] data, Channel channelId = Channel.Reliable);

        public void Disconnect();
        
        public bool IsConnected();
        
        public void IterateIncoming();

        public void IterateOutgoing();
        
        public int GetMaxPacketSize(Channel channelId = Channel.Reliable);
        
        public void Pause();
        
        public void UnPause();
        
        public void Shutdown();

        // public override void OnApplicationQuit()
        // {
        //     base.OnApplicationQuit();
        // }
    }
}
#endif