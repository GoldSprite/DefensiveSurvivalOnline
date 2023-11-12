namespace Unity.Sync.Relay.Model
{
    /*
    public enum RaiseEventTarget
    {
        TO_ALL,
        TO_GROUPS,
        TO_ALL_BUT_ME,
        TO_PLAYERS,
        TO_MASTER,
    }

    public class RaiseEventTargetConvertor
    {
        // 
        public static RaiseEventTarget convert(Message.RaiseEventTarget target)
        {
            switch (target)
            {
                case Message.RaiseEventTarget.ToAll:
                    return RaiseEventTarget.TO_ALL;
                case Message.RaiseEventTarget.ToGroups:
                    return RaiseEventTarget.TO_GROUPS;
                case Message.RaiseEventTarget.ToPlayers:
                    return RaiseEventTarget.TO_PLAYERS;
                case Message.RaiseEventTarget.ToAllButMe:
                    return RaiseEventTarget.TO_ALL_BUT_ME;
                case Message.RaiseEventTarget.ToMaster:
                    return RaiseEventTarget.TO_MASTER;
                default:
                    return RaiseEventTarget.TO_ALL;
            }
        }
        
        public static Unity.Sync.Relay.Message.RaiseEventTarget convert(RaiseEventTarget target)
        {
            switch (target)
            {
                case RaiseEventTarget.TO_ALL:
                    return Message.RaiseEventTarget.ToAll;
                case RaiseEventTarget.TO_GROUPS:
                    return Message.RaiseEventTarget.ToGroups;
                case RaiseEventTarget.TO_PLAYERS:
                    return Message.RaiseEventTarget.ToPlayers;
                case RaiseEventTarget.TO_ALL_BUT_ME:
                    return Message.RaiseEventTarget.ToAllButMe;
                case RaiseEventTarget.TO_MASTER:
                    return Message.RaiseEventTarget.ToMaster;
                default:
                    return Message.RaiseEventTarget.ToAll;
            }
        }
    }

    public class RaiseEventOptions
    {
        public uint[] TargetGroups { get; set; } // for TO_GROUPS only
        public uint[] ReceiverIds { get; set; } // for TO_PLAYERS only
        public RaiseEventTarget Target { get; set; } // by default TO_ALL_BUT_ME
    }
    
        */
    
}