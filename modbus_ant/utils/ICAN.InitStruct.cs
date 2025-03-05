using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MCU_CAN_AV.utils;
using Newtonsoft.Json;
using Splat;

namespace modbus_ant.utils
{
    public partial interface ICAN
    {
        public enum TransportTypes
        {
            SerialPort,
            Ethernet,
            CAN,
        }

        public struct TransportInitStruct
        {

            public UInt32 DevId = 1;
            public UInt32 CanId = 0;
            public UInt32 Baudrate = 460800;
            public UInt32 RcvCode = 0;
            public UInt32 Mask = 0xffffffff;
            public UInt32 PollIntervalMs = 100;

            /// Poll interval, ms
            public string ServerName = "localhost";

            public uint ServerPort = 8888;
            public string ComName = "/dev/ttyUSB0";
            public TransportTypes TransportType = TransportTypes.SerialPort;
            
            public TransportInitStruct()
            {
            }

        }
    }
}
