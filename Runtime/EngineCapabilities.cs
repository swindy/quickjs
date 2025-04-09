using System;

namespace QuickJS
{
    [Flags]
    public enum EngineCapabilities
    {
        None = 0,
        Fetch = 1,
        XHR = 2,
        WebSocket = 4,
        Console = 8,
        Scheduler = 16,
        Base64 = 32,
        URL = 64,
        Navigator = 128,
        Encoding = 256,
        AbortController = 512,
        QueueMicrotask = 1024,
    }
}