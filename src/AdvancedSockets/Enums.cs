using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedSockets
{
    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Options
    }

    public enum WebSocketStatus
    {
        Opening,
        Open,
        Closing,
        Closed
    }

    public enum WebSocketOpcode
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Closing = 8,
        Ping = 9,
        Pong = 10
    }

    public enum WebSocketCloseStatus
    {
        NormalClosure = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        UnacceptableDataFormat = 1003,
        Reserved1 = 1004,
        Reserved2 = 1005,
        Reserved3 = 1006,
        UnconsistentData = 1007,
        PolicyViolation = 1008,
        BufferOverflow = 1009,
        UnnegotiatedExtension = 1010
    }
}
