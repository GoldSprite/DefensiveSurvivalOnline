using System;
using System.Collections.Generic;
using Unity.Sync.Relay.Model;

namespace Unity.Sync.Relay.Lobby
{
    [Serializable]
    public class ListRoomResponse : LobbyBaseResponse
    {
        
        public int TotalCount;

        public List<LobbyRoom> Items;

        public ListRoomResponse()
        {
            this.Code = (uint)RelayCode.OK;
        }

        public ListRoomResponse(RelayCode code)
        {
            this.Code = (uint)code;
        }

    }
    
    [Serializable]
    public class ListRoomResponseDto
    {
        
        public int TotalCount;

        public List<LobbyRoomDto> Items;
        
        // 创建DTO对象，然后进行转换
        public static ListRoomResponse Create(ListRoomResponseDto dto)
        {
            ListRoomResponse result = new ListRoomResponse();
            result.Code = (uint)RelayCode.OK;
            result.TotalCount = dto.TotalCount;
            result.Items = new List<LobbyRoom>();
            if (dto.Items != null)
            {
                foreach (var item in dto.Items)
                {
                    result.Items.Add(LobbyRoomDto.Create(item));
                }
            }
            return result;
        }

    }
    
    
}