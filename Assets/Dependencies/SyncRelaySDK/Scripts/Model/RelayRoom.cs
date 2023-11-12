using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Message;
using UnityEngine;
using TransportPlayer = Unity.Sync.Relay.Message.RelayPlayer;


namespace Unity.Sync.Relay.Model
{
    [Serializable]
    public class RelayRoom
    {
        public string Name;

        public string NameSpace;

        public string ID;
        
        // Master Client 的 Transport ID
        public ulong MasterClientID;

        public Dictionary<uint, RelayPlayer> Players;

        public LobbyRoomStatus Status;

        public string IP;

        public ushort Port;
        
        public string JoinCode;

        public Dictionary<string, string> CustomProperties;

        public void UpdateWithJoinRoomResponse(JoinRoomResponse resp)
        {
            MasterClientID = resp.MasterClientId;
            Players = new Dictionary<uint, RelayPlayer>();
            foreach (TransportPlayer player in resp.Players)
            {
                Players.Add(player.Id, RelayUtils.ConvertTransportPlayerToRelayPlayer(player));
            }

            CustomProperties = new Dictionary<string, string>();
            foreach (var item in resp.Room.Properties)
            {
                CustomProperties.Add(item.Key, item.Value);
            }
        }

        public RelayPlayer GetPlayer(uint transportId)
        {
            if (!Players.ContainsKey(transportId))
            {
                Debug.LogErrorFormat("Player {0} Not In Room But Getting", transportId);
                return null;
            }

            return Players[transportId];
        }
        
        public void AddPlayer(TransportPlayer player)
        {
            if (Players.ContainsKey(player.Id))
            {
                Debug.LogErrorFormat("Player {0} Already In Room But Adding", player.Id);
                return;
            }
            
            Players.Add(player.Id, RelayUtils.ConvertTransportPlayerToRelayPlayer(player));
        }
        
        public void RemovePlayer(TransportPlayer player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Debug.LogErrorFormat("Player {0} Not In Room But Removing", player.Id);
                return;
            }
            Players.Remove(player.Id);
        }
        
        public void UpdatePlayer(TransportPlayer player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Debug.LogErrorFormat("Player {0} Not In Room But Updating", player.Id);
                return;
            }

            Players[player.Id] = RelayUtils.ConvertTransportPlayerToRelayPlayer(player);
        }
        
        public void UpdatePlayer(RelayPlayer player)
        {
            if (!Players.ContainsKey(player.TransportId))
            {
                Debug.LogErrorFormat("Player {0} Not In Room But Updating", player.TransportId);
                return;
            }

            Players[player.TransportId] = player;
        }

        public void UpdateCustomProperties(Dictionary<string, string> properties)
        {
            CustomProperties = properties;
        }
        
        public void UpdateCustomProperties(MapField<string, string> properties)
        {
            CustomProperties = new Dictionary<string, string>();
            foreach (var item in properties)
            {
                CustomProperties.Add(item.Key, item.Value);
            }
        }
    }
}