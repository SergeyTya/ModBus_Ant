using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modbus_ant.utils
{
    internal interface IModbusTransport
    {
        public ICAN.TransportInitStruct InitStructure { get; }
        
        public Task<byte[]> ExecCustomRequest(byte[] pld, bool logEnabled);

        public bool SlaveMode { get; set; }

        /// <summary>
        ///     Close hardware transport and dispose all objects
        /// </summary>
        public void Close();
        
        /// <summary>
        ///     Is can transport open
        /// </summary>
        /// <returns>
        ///     bool
        /// </returns>
        public bool IsOpen { get; }
        
        private static IModbusTransport? _instance;

        public static IModbusTransport? GetInstance()
        {
            return _instance;   
        }
        
        public static void CreateInstance(ICAN.TransportInitStruct initStructure)
        {
            IModbusTransport? retVal;
            switch (initStructure.TransportType)
            {
                case ICAN.TransportTypes.SerialPort:
                    retVal = new SerialRTU(initStructure);
                    break;
            
                default: throw new NotImplementedException(nameof(initStructure.TransportType));
            }
            
            _instance = retVal;
            
        }
        
        public static void CloseInstance()
        {
            _instance?.Close();
        }
    }
}
