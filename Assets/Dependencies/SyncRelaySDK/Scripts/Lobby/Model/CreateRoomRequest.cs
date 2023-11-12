using System;
using System.Collections.Generic;
using Unity.Sync.Relay.Model;
using UnityEngine;

namespace Unity.Sync.Relay.Lobby
{
    // 
    [Serializable]
    public class CreateRoomRequest
    {
        // optional
        // 命名空间
        public string Namespace;

        // optional
        // 房间名称
        public string Name;

        // optional
        // 最大玩家数
        // 默认值为0，表示不设上限
        public int MaxPlayers;

        // optional
        // 指定房间的地域
        // 默认值为Shanghai，目前仅支持Shanghai，建议不填
        public string Region;

        // optional
        // 房间的可见性(公开，私人)
        public LobbyRoomVisibility Visibility;

        // optional
        // 当Visibility为Private时, 需要开发者填充
        public string JoinCode;

        // optional
        // 玩家对应的用户id
        // 已废弃，请用OwnerId
        [Obsolete("PlayerId is obsolete. Use OwnerId instead")]
        public string PlayerId;

        // required
        // 房主对应的用户id
        public string OwnerId;

        // optional
        // 房间的自定义属性
        public Dictionary<string, string> CustomProperties;
        
        // optional
        // 选用的房间Profile UUID
        // 不传会默认使用Inspector面板配置的 Room Profile UUID
        public string RoomProfileUUID;

        public RelayCode Check()
        {
            if (this.Visibility == LobbyRoomVisibility.Private)
            {
                if (string.IsNullOrEmpty(this.JoinCode))
                {
                    Debug.LogError("Private room must have join code");
                    return RelayCode.MissingJoinCode;
                }
            }

            if (string.IsNullOrEmpty(this.RoomProfileUUID) && string.IsNullOrEmpty(RelaySettings.RoomProfileUUID))
            {
                Debug.LogError("Room Profile UUID cannot be null");
                return RelayCode.MissingRoomProfileUuid;
            }
            
            return RelayCode.OK;
        }
    }

    // 这个属于中间层(Dto, 代表数据传输对象)
    [Serializable]
    public class CreateLobbyRoomRequestDto
    {
        // required
        public string UosAppId;

        // required
        public string RoomProfileUuid;
            
        // optional
        // 命名空间
        public string Namespace;
        
        // optional
        // 房间名称
        public string Name;

        // optional
        // 最大玩家数
        // 默认值为0，表示不设上限
        public int MaxPlayers;
        
        // optional
        // 指定房间的地域
        // 默认值为Shanghai，目前仅支持Shanghai，建议不填
        public string Region;

        // optional
        // 房间的可见性(公开，私人)
        public string Visibility;

        // optional
        // 当Visibility为Private时, 需要开发者填充
        public string JoinCode;
        
        // required
        // 玩家对应的用户id，will get from token when auth api is ready
        public string PlayerId;

        // optional
        // 房间的自定义属性
        public Dictionary<string, string> CustomProperties;

        // 创建DTO对象，然后进行转换
        public static CreateLobbyRoomRequestDto Create(CreateRoomRequest request)
        {
            CreateLobbyRoomRequestDto dto = new CreateLobbyRoomRequestDto();
            dto.Name = request.Name;
            dto.Namespace = request.Namespace;
            if (request.Visibility != LobbyRoomVisibility.Unknown)
            {
                dto.Visibility = LobbyRoomVisibilityHelper.ValueOf(request.Visibility);
            }
            dto.JoinCode = request.JoinCode;
            dto.CustomProperties = request.CustomProperties;
            dto.MaxPlayers = request.MaxPlayers;
            dto.Region = request.Region;
            dto.PlayerId = request.OwnerId;
            if (string.IsNullOrEmpty(dto.PlayerId))
            {
                dto.PlayerId = request.OwnerId;
            }

            if (string.IsNullOrEmpty(request.RoomProfileUUID))
            {
                dto.RoomProfileUuid = RelaySettings.RoomProfileUUID;
            }
            else
            {           
                dto.RoomProfileUuid = request.RoomProfileUUID;
            }
            
            dto.UosAppId = RelaySettings.UosAppId;
            return dto;
        }
    }
    
}