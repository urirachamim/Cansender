using System;

namespace Canbus_sender
{
    internal class TPCANHandle
    {
        public static TPCANHandle PCAN_USBBUS1 { get; internal set; }

        public static explicit operator TPCANHandle(int v)
        {
            throw new NotImplementedException();
        }
    }
}