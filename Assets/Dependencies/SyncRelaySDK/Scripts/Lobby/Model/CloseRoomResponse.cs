using System;
using Unity.Sync.Relay.Model;

namespace Unity.Sync.Relay.Lobby
{
    [Serializable]
    public class CloseRoomResponse : LobbyBaseResponse
    {
        public CloseRoomResponse()
        {
            this.Code = (uint)RelayCode.OK;
        }

        public CloseRoomResponse(RelayCode code)
        {
            this.Code = (uint)code;
        }
    }
    
    
    
}