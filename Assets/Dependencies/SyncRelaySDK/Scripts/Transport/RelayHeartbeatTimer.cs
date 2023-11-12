using System;

namespace Unity.Sync.Relay.Transport
{
    public class RelayHeartbeatTimer
    {
        
        private bool _active;
        
        public static double _timeout { get; set; } 
        
        public static DateTime _lastPing { get; set; }
        
        public void Start(double timeout)
        {
            _active = true;
            _timeout = timeout;
            _lastPing = DateTime.Now;
        }

        public void Refresh()
        {
            if (_active)
            {
                _lastPing = DateTime.Now;
            }
        }

        public void SetTimeout(double timeout)
        {
            this.Refresh();
            _timeout = timeout;
        }

        public bool IsTimeout()
        {
            if (!_active) return true;
            
            var pingInterval = DateTime.Now - _lastPing;
            if (pingInterval.TotalSeconds > _timeout)
            {
                return true;
            }

            return false;
        }

    }
    
}