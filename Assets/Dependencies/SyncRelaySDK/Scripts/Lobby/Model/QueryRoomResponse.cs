using System;
using System.Collections.Generic;
using Unity.Sync.Relay.Model;

namespace Unity.Sync.Relay.Lobby
{
    [Serializable]
    public class QueryRoomResponse : LobbyBaseResponse
    {

        public string RoomUuid;

        public string OwnerId;

        public string Namespace;

        public string Name;

        public string JoinCode;
        
        public LobbyRoomStatus Status;
        
        // optional
        // 最大玩家数
        public int MaxPlayers;

        // optional
        // 房间的可见性(公开，私人)
        public LobbyRoomVisibility Visibility;
        
        public ServerInfo ServerInfo;
        
        // optional
        // 房间的自定义属性
        public Dictionary<string, string> CustomProperties;

        public QueryRoomResponse()
        {
            Code = (uint)RelayCode.OK;
        }

        public QueryRoomResponse(RelayCode code)
        {
            this.Code = (uint)code;
        }
    }
    
    [Serializable]
    public class QueryRoomResponseDto
    {

        public string RoomUuid;

        public string OwnerId;

        public string Status;

        public string Namespace;

        public string Name;

        public string JoinCode;

        public RoomPropertiesDto Properties;

        public ServerInfo ServerInfo;
        
        // optional
        // 房间的自定义属性
        public Dictionary<string, string> CustomProperties;
        
        
        // 创建DTO对象，然后进行转换
        public static QueryRoomResponse Create(QueryRoomResponseDto dto)
        {
            QueryRoomResponse resp = new QueryRoomResponse();
            resp.RoomUuid = dto.RoomUuid;
            resp.Name = dto.Name;
            resp.Namespace = dto.Namespace;
            if (dto.Properties != null)
            {
                resp.Visibility = LobbyRoomVisibilityHelper.Resolve(dto.Properties.visibility);
                resp.MaxPlayers = dto.Properties.maxPlayers;
            }
            resp.CustomProperties = dto.CustomProperties;
            resp.OwnerId = dto.OwnerId;
            resp.JoinCode = dto.JoinCode;
            resp.ServerInfo = dto.ServerInfo;
            resp.Status = LobbyRoomStatusHelper.Resolve(dto.Status);
            
            return resp;
        }
        
    }
    
    [Serializable]
    public class RoomPropertiesDto
    {
        // optional
        // 最大玩家数
        public int maxPlayers;

        // optional
        // 房间的可见性(公开，私人)
        public string visibility;

    }
    
}

