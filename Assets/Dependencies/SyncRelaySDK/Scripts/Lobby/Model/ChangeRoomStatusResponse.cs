using System;
using Unity.Sync.Relay.Model;

namespace Unity.Sync.Relay.Lobby
{
    [Serializable]
    public class ChangeRoomStatusResponse : LobbyBaseResponse
    {
        public ChangeRoomStatusResponse()
        {
            this.Code = (uint)RelayCode.OK;
        }

        public ChangeRoomStatusResponse(RelayCode code)
        {
            this.Code = (uint)code;
        }
    }
    
    
    
}