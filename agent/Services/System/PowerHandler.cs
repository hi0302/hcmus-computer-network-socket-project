using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace RemoteControl.Agent.Services.System
{
    [SupportedOSPlatform("windows")] // Chỉ chạy trên windows
    public class PowerHandler
    {
        // 1. Tắt máy tính
        public bool Shutdown()
        {
            try
            {
                // Lệnh shutdown: /s = shutdown, /t 0 = ngay lập tức
                Process.Start("shutdown", "/s /t 0");
                return true;
            }
            catch { return false; }
        }

        // 2. Khởi động lại máy tính
        public bool Restart()
        {
            try
            {
                // Lệnh restart: /r = restart, /t 0 = ngay lập tức
                Process.Start("shutdown", "/r /t 0");
                return true;
            }
            catch { return false; }
        }
    }
}
