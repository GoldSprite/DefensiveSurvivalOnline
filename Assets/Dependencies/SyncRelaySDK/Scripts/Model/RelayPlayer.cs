using System;
using System.Collections.Generic;
using TransportPlayer = Unity.Sync.Relay.Message.RelayPlayer;

namespace Unity.Sync.Relay.Model
{    
    [Serializable]
    public class RelayPlayer
    {
        public string ID;

        public string Name;

        public uint TransportId;

        public Dictionary<string, string> Properties;
    }
}