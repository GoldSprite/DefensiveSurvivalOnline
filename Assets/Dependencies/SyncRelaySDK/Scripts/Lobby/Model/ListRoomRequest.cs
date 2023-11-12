using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Sync.Relay.Lobby
{
    
    [Serializable]
    public class ListRoomRequest
    {
        // 可选
        // 命名空间
        public string Namespace;
        
        // 可选
        // 房间状态
        public List<LobbyRoomStatus> Statuses;
        
        // 可选
        // 房间可见性
        public LobbyRoomVisibility Visibility;

        // 可选
        public string OwnerId;
        
        // 可选
        // 返回列表的起始点
        // 默认为0
        public Int32 Start;

        // 可选
        // 返回列表的个数
        // 默认为20
        public Int32 Count;
        
        // 可选
        // 用于房间名的模糊搜索
        public string Name;
        
        public Dictionary<string, string> ToDictionay()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(this.Namespace))
            {
                dict.Add("namespace", this.Namespace);
            }
            if (this.Statuses != null && this.Statuses.Count > 0)
            {
                var statusStr = string.Join("&", this.Statuses.Select(status => "status=" + LobbyRoomStatusHelper.ValueOf(status)));
                statusStr = statusStr.Substring(7);
                dict.Add("status", statusStr);
            }
            if (this.Visibility != LobbyRoomVisibility.Unknown)
            {
                dict.Add("visibility", LobbyRoomVisibilityHelper.ValueOf(this.Visibility));
            }
            if (!string.IsNullOrWhiteSpace(this.OwnerId))
            {
                dict.Add("ownerId", this.OwnerId);
            }
            // 这属于分页相关的内容
            if (this.Start != 0)
            {
                dict.Add("start", this.Start.ToString());
            }
            if (this.Count != 0)
            {
                dict.Add("count", this.Count.ToString());
            }
            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                dict.Add("name", this.Name);
            }
            return dict;
        }
        
        public String ToQueryString()
        {
            var res = ""; 
            if (!string.IsNullOrWhiteSpace(this.Namespace))
            {
                res = String.Format("{0}&namespace={1}", res, Uri.EscapeDataString(this.Namespace));
            }

            if (this.Statuses != null)
            {
                foreach (var status in this.Statuses)
                {
                    res = String.Format("{0}&status={1}", res, LobbyRoomStatusHelper.ValueOf(status));
                }
            }
            
            if (this.Visibility != LobbyRoomVisibility.Unknown)
            {
                res = String.Format("{0}&visibility={1}", res, LobbyRoomVisibilityHelper.ValueOf(this.Visibility));
            }
            
            if (!string.IsNullOrWhiteSpace(this.OwnerId))
            {
                res = String.Format("{0}&ownerId={1}", res, this.OwnerId);
            }
            // 这属于分页相关的内容
            if (this.Start != 0)
            {
                res = String.Format("{0}&start={1}", res, this.Start);
            }
            if (this.Count != 0)
            {
                res = String.Format("{0}&count={1}", res, this.Count);
            }
            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                res = String.Format("{0}&name={1}", res, Uri.EscapeDataString(this.Name));
            }

            if (res.Length > 0)
            {
                res = res.Substring(1);
            }
            return res;
        }

    }
    
    
    [Serializable]
    public class ListRoomRequestDto
    {

        public string Namespace;
        
        public List<LobbyRoomStatus> Statuses;
        
        public LobbyRoomVisibility Visibility;

        public string OwnerId;
        
        public Int32 Start;

        public Int32 Count;

        public string Name;

        public Dictionary<string, string> ToDictionay()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(this.Namespace))
            {
                dict.Add("namespace", this.Namespace);
            }
            if (this.Statuses != null && this.Statuses.Count > 0)
            {
                var statusStr = string.Join("&", this.Statuses.Select(status => "status=" + LobbyRoomStatusHelper.ValueOf(status)));
                statusStr = statusStr.Substring(7);
                dict.Add("status", statusStr);
            }
            if (this.Visibility != LobbyRoomVisibility.Unknown)
            {
                dict.Add("visibility", LobbyRoomVisibilityHelper.ValueOf(this.Visibility));
            }
            if (!string.IsNullOrWhiteSpace(this.OwnerId))
            {
                dict.Add("ownerId", this.OwnerId);
            }
            // 这属于分页相关的内容
            if (this.Start != 0)
            {
                dict.Add("start", this.Start.ToString());
            }
            if (this.Count != 0)
            {
                dict.Add("count", this.Count.ToString());
            }
            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                dict.Add("name", this.Name);
            }
            return dict;
        }

    }
    
    
    
}