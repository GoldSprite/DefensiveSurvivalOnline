using System.Collections.Generic;
using RelayPlayer = Unity.Sync.Relay.Model.RelayPlayer;
using TransportRoom = Unity.Sync.Relay.Message.RelayRoom;
using TransportPlayer = Unity.Sync.Relay.Message.RelayPlayer;

namespace Unity.Sync.Relay
{
    public class RelayUtils
    {
        public static RelayPlayer ConvertTransportPlayerToRelayPlayer(TransportPlayer player)
        {
            RelayPlayer p = new RelayPlayer();

            p.Name = player.Name;
            p.ID = player.UniqueId;
            p.TransportId = player.Id;
            
            p.Properties = new Dictionary<string, string>();
            foreach (var item in player.Properties)
            {
                p.Properties.Add(item.Key, item.Value);
            }

            return p;
        }
    }
}