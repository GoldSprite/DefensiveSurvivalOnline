namespace Unity.Sync.Relay.Model
{
    public class RelayError
    {
        // 错误码
        public uint Code { get; set; }
        
        // 错误码字符串描述
        public string Description { get; set; }
    }
}