// file: Services/Input/Keylogger.cs
using System.Runtime.Versioning;

namespace RemoteControl.Agent.Services.Input
{
    [SupportedOSPlatform("windows")]
    public class Keylogger
    {
        private bool _isLogging = false;

        public bool StartLogging()
        {
            _isLogging = true;
            Console.WriteLine("Keylogger started.");
            return true;
        }

        public bool StopLogging()
        {
            _isLogging = false;
            Console.WriteLine("Keylogger stopped.");
            return true;
        }
    }
}
