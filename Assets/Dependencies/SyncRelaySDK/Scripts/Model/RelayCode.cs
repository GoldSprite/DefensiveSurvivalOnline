using System.Collections.Generic;

namespace Unity.Sync.Relay.Model
{
    
    // 这边集中定义错误码
    public enum RelayCode
    {
	    OK                  = 0,
	    InvalidParam        = 10000,
	    InternalServerError = 10001,
	    OperationFailed     = 10002,
	    MaxPlayersExceeded  = 20001,
	    MismatchedAppId     = 20002,
	    InvalidJoinCode     = 20003, // JoinCode不合法
	    PlayerJoinedAnotherRoom    = 20004, // 该玩家已加入其他的活跃房间
	    RoomNotFound        = 20005, // 房间不存在
	    InvalidRoomStatus   = 20006, // 房间状态不合法
	    
	    ConnectionFailed    = 20099,
	    UserReLoggedIn      = 20101,
	    RoomClosed          = 20102,
	    UnexpectedRoomError = 20103,
	    ClientTimedOut      = 20104,
	    KickPlayerByMasterClient   = 20105,  // 被房主kick了, 这算原因，不算错误
	    KickPlayerFailForSelf  = 20106,  // 房主不能踢房主本人
	    KickPlayerFailForNotFound  = 20107, // 踢的用户不在房间内
	    KickPlayerFailForNonMasterClient = 20108, // 非房主不能踢人
	    UpdatePlayerFailForOffline		 = 20201, // 要更新的用户不在房间内
	    UpdatePlayerFailForPermissionDenied = 20202, // 更新用户属性的权限不够

	    // 超过限制
	    MaxStickyEventsExceeded  = 20300,
	    MismatchedStickyEventKey = 20301,
	    StickyEventKeyNotFound   = 20302,

	    // 客户端定义的错误码[1, 10000)
	    InvalidClientAppId     = 1000,
	    InvalidClientAppSecret = 1001,
	    InvalidClientPlayerId  = 1002,
	    MissingJoinCode        = 1003,
	    MissingRoomProfileUuid = 1004,

	    
	    // 这部分是lobby相关的错误码
	    LobbyAccessDenied            = 9000,
	    LobbyAuthenticationFailed    = 9001,
	    LobbyRoomClosed              = 9002,
	    LobbyRoomUnknown             = 9003,
	    LobbyQueryRoomFailed         = 9004,
	    LobbyMissingAuthToken        = 9005,
	    LobbyCreateRoomFailed        = 9006,
	    LobbyCreateRoomLimitExceeded = 9007,
	    LobbyJoinRoomFailed          = 9008,
	    LobbyCloseRoomFailed         = 9009,
    }

	// 对错误码进行映射工作
    public class RelayStatusCodeHelper
    {
	    // 
	    private static Dictionary<uint, string> gErrMap = new Dictionary<uint, string>();
	    static RelayStatusCodeHelper()
	    {
		    // 这是类的静态构造函数
		    // Ok = 0,
		    gErrMap[(uint)RelayCode.OK] = "OK";  
		    // InvalidParam = 10000,
		    gErrMap[(uint)RelayCode.InvalidParam] = "Invalid Param"; 
		    // InternalServerError = 10001,
		    gErrMap[(uint)RelayCode.InternalServerError] = "Internal Server Error";
		    // OperationFailed = 10002,
		    gErrMap[(uint)RelayCode.OperationFailed] = "Operation Failed";
		    // MaxPlayersExceeded = 20001,
		    gErrMap[(uint)RelayCode.MaxPlayersExceeded] = "Max Players Exceeded";
		    // MismatchedAppId = 20002,
		    gErrMap[(uint)RelayCode.MismatchedAppId] = "Mismatched AppId";
		    // InvalidJoinCode = 20003,
		    gErrMap[(uint)RelayCode.InvalidJoinCode] = "Invalid Join Code";
		    // PlayerJoinAnotherRoom = 20004,
		    gErrMap[(uint)RelayCode.PlayerJoinedAnotherRoom] = "Player Joined Another Room";
		    // RoomNotFound = 20005,
		    gErrMap[(uint)RelayCode.RoomNotFound] = "Room Not Found";
		    // InvalidRoomStatus = 20006,
		    gErrMap[(uint)RelayCode.InvalidRoomStatus] = "Invalid Room Status";

		    // ConnectionFailed = 20099,
		    gErrMap[(uint)RelayCode.ConnectionFailed] = "Connection Failed";
		    // UserReLoggedIn = 20101,
		    gErrMap[(uint)RelayCode.UserReLoggedIn] = "User ReLogged In";
		    // RoomClosed = 20102,
		    gErrMap[(uint)RelayCode.RoomClosed] = "Room Closed";
		    // UnexpectedRoomError = 20103,
		    gErrMap[(uint)RelayCode.UnexpectedRoomError] = "Unexpected Room Error";
		    // ClientTimedOut = 20104,
		    gErrMap[(uint)RelayCode.ClientTimedOut] = "Client TimedOut";
		    // KickPlayerByMasterClient = 20105
		    gErrMap[(uint)RelayCode.KickPlayerByMasterClient] = "Kick Player By Master Client";
		    // KickPlayerFailForSelf = 20106
		    gErrMap[(uint)RelayCode.KickPlayerFailForSelf] = "Kick Player Fail For Self";
		    // KickPlayerFailForNotFound = 20107
		    gErrMap[(uint)RelayCode.KickPlayerFailForNotFound] = "Kick Player Fail For Not Found";
		    // KickPlayerFailForNonMasterClient = 20108
		    gErrMap[(uint)RelayCode.KickPlayerFailForNonMasterClient] = "Kick Player Fail For Not Master Client";
		    
		    // UpdatePlayerFailForOffline = 20201
		    gErrMap[(uint)RelayCode.UpdatePlayerFailForOffline] = "Update Player Fail For Offline"; // 要更新的用户不在房间内
		    // UpdatePlayerFailForPermissionDenied = 20202
		    gErrMap[(uint)RelayCode.UpdatePlayerFailForPermissionDenied] = "Update Player Fail For Permission Denied"; // 预留, 权限不足
		    
			//
		    // // 超过限制
		    // MaxStickyEventsExceeded = 20300,
		    gErrMap[(uint)RelayCode.MaxStickyEventsExceeded] = "Max Sticky Events Exceeded";
		    // MismatchedStickyEventKey = 20301,
		    gErrMap[(uint)RelayCode.MismatchedStickyEventKey] = "Mismatched Sticky Event Key";
		    // StickyEventKeyNotFound   = 20302,
		    gErrMap[(uint)RelayCode.StickyEventKeyNotFound] = "Sticky Event Key Not Found";
		    
			//
		    // // 客户端定义的错误码[1, 10000)
		    // InvalidClientAppId = 1000,
		    gErrMap[(uint)RelayCode.InvalidClientAppId] = "Invalid Client AppId";
		    // InvalidClientAppSecret = 1001,
		    gErrMap[(uint)RelayCode.InvalidClientAppSecret] = "Invalid Client AppSecret";
		    // InvalidClientPlayerId = 1002,
		    gErrMap[(uint)RelayCode.InvalidClientPlayerId] = "Invalid Client PlayerId";
		    //  
			   
		    // // 这部分是lobby相关的错误码
		    // LobbyAccessDenied = 9000,
		    gErrMap[(uint)RelayCode.LobbyAccessDenied] = "Lobby Access Denied";
		    // LobbyAuthenticationFailed = 9001,
		    gErrMap[(uint)RelayCode.LobbyAuthenticationFailed] = "Lobby Authentication Failed";
		    // LobbyRoomClosed= 9002,
		    gErrMap[(uint)RelayCode.LobbyRoomClosed] = "Lobby Room Closed";
		    // LobbyRoomUnknown	= 9003,
		    gErrMap[(uint)RelayCode.LobbyRoomUnknown] = "Lobby Room Unknown Error";
		    // LobbyQueryRoomFailed = 9004, 
		    gErrMap[(uint)RelayCode.LobbyQueryRoomFailed] = "Lobby Query Room Failed";
		    // LobbyMissingAuthToken = 9005
		    gErrMap[(uint)RelayCode.LobbyMissingAuthToken] = "Lobby Missing AuthToken";
			// LobbyCreateRoomFailed = 9006
		    gErrMap[(uint)RelayCode.LobbyCreateRoomFailed] = "Lobby Create Room Failed";
		    // LobbyCreateRoomLimitExceeded = 9007,
		    gErrMap[(uint)RelayCode.LobbyCreateRoomLimitExceeded] = "Lobby Create Room Limit Exceeded";
		    // LobbyJoinRoomFailed = 9008,
		    gErrMap[(uint)RelayCode.LobbyJoinRoomFailed] = "Lobby Join Room Failed";
		    // LobbyCloseRoomFailed = 9009
		    gErrMap[(uint)RelayCode.LobbyCloseRoomFailed] = "Lobby Close Room Failed";

	    }

	    // 进行快速的错误码映射
	    public static RelayError Convert(uint code)
	    {
		    RelayError error = new RelayError();
		    error.Code = code;
		    if (gErrMap.TryGetValue(code, out string description))
		    {
			    error.Description = description;
		    }
		    else
		    {
			    error.Description = "Unknown Error";
		    }
		    
		    return error;
	    }
	    
	    public static RelayError Convert(RelayCode code)
	    {
		    return Convert((uint)code);
	    }
	    
    }
    
    
}
