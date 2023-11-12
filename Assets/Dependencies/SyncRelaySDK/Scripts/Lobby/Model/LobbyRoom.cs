using System;
using System.Collections.Generic;

namespace Unity.Sync.Relay.Lobby
{

    [Serializable]
    public class LobbyRoom
    {
        public string UosAppId;
        
        public string RoomProfileUuid;
        
        public string RoomUuid;

        public string OwnerId;

        public LobbyRoomStatus Status;

        public string Namespace;

        public string Name;

        public string JoinCode;

        public uint MaxPlayers;

        public LobbyRoomVisibility Visibility;

        public Dictionary<string, string> CustomProperties;
        
        public uint PlayerCount;
        
    }
    
    [Serializable]
    public class RoomPropertyDto
    {

        public string maxPlayers;

        public string visibility;
        
    }

    [Serializable]
    public class LobbyRoomDto
    {
        public string UosAppId;
        
        public string RoomProfileUuid;
        
        public string RoomUuid;
        
        public string OwnerId;
        
        public string Status;
        
        public string Namespace;
        
        public string Name;
        
        // public ServerInfo ServerInfo;
        
        public string JoinCode;

        public RoomPropertyDto Properties;

        public Dictionary<string, string> CustomProperties;

        public uint PlayerCount;

        // 创建DTO对象，然后进行转换
        public static LobbyRoom Create(LobbyRoomDto dto)
        {
            LobbyRoom result = new LobbyRoom();
            result.UosAppId = dto.UosAppId;
            result.RoomProfileUuid = dto.RoomProfileUuid;
            result.RoomUuid = dto.RoomUuid;
            result.Name = dto.Name;
            result.Namespace = dto.Namespace;
            if (dto.Properties != null)
            {
                result.Visibility = LobbyRoomVisibilityHelper.Resolve(dto.Properties.visibility);
                uint maxPlayers = 0;
                uint.TryParse(dto.Properties.maxPlayers, out maxPlayers);
                result.MaxPlayers = maxPlayers;
            }
            else
            {
                result.Visibility = LobbyRoomVisibility.Unknown;
                result.MaxPlayers = 0;
            }
            result.CustomProperties = dto.CustomProperties;
            result.OwnerId = dto.OwnerId;
            result.JoinCode = dto.JoinCode;
            // result.ServerInfo = dto.ServerInfo;
            result.Status = LobbyRoomStatusHelper.Resolve(dto.Status);
            result.PlayerCount = dto.PlayerCount;

            return result;
        }

    }
}
