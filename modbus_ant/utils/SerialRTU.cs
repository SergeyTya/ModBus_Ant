
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using static modbus_ant.utils.ServerModbusTCP;

namespace modbus_ant.utils
{
    internal class SerialRTU : IModbusTransport, IEnableLogger
    {
        private readonly SerialPort _port =new();
        private static readonly Stopwatch TimerWdg = new Stopwatch();
        private readonly IObservable<long> _wdg = Observable.Interval(TimeSpan.FromSeconds(0.5));
        public bool ReceiveTimeout;
        private readonly ICAN.TransportInitStruct _initStruct;
        private readonly List<byte> _rxbuf = [];
        private static readonly SemaphoreSlim SemaphoreSlim = new(1);
        private static readonly Subject<byte[]?> RxSub = new Subject<byte[]?>();
        private readonly bool _isOpen;
        private bool _connected;

        private uint _regCount;
        //bool _init_done = false; 
        
        private bool _logging = false;

        
        public SerialRTU(ICAN.TransportInitStruct initStructure)
        {
            _initStruct = initStructure;

            _port.DataReceived += SerialReceive;
            _port.ReadBufferSize = 10000;
            
            Connect();
            
            _wdg.Subscribe((_) =>
            {
                if (!TimerWdg.IsRunning) return;
                if (TimerWdg.ElapsedMilliseconds <= 500) return;
                this.Log().Error($"Serial timeout");
                TimerWdg.Reset();
                _rxbuf.Clear();
                ReceiveTimeout = true;
                RxSub.OnNext(null);
            });


           
            _isOpen = true;
        }

        private void Connect()
        {
            this.Log().Info($"Connecting {_initStruct.ComName} : {_initStruct.Baudrate} : {_initStruct.DevId} ");

            if (_port.IsOpen) _port.Close();
            _port.PortName = _initStruct.ComName;
            _port.BaudRate = (int) _initStruct.Baudrate;
            ReceiveTimeout = false;
            _port.WriteTimeout = 100;
            try
            {
                _port.Open();
            }
            catch (Exception e)
            {
                this.Log().Fatal(e);
            }
        }

        public void Close()
        {
            if (!_port.IsOpen) return;
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
            _port.Close();
        }

        public ICAN.TransportInitStruct InitStructure => _initStruct;

        public async Task<byte[]> ExecCustomRequest(byte[] pld, bool logEnabled=false) // return pld
        {
            await SemaphoreSlim.WaitAsync(/*TimeSpan.FromSeconds(0.3)*/);
            _logging = logEnabled;
            try
            {
                SerialWrite(pld.ToList());
                
                var cacheTimeout = TimeSpan.FromMilliseconds(500);
                var res = await RxSub.FirstAsync().ToTask().WaitAsync(cacheTimeout);

                // if (res == null) throw new ServerModbusTCPException("ExecCustomRequest: Null response");
                // if (res[0] != pld[0]) throw new ServerModbusTCPException("ExecCustomRequest: Not valid id");

                if (res == null )
                {
                    return [0];
                }
                
                if (res[0] != pld[0])
                {
                    return [0];
                }

                if (res.Length > 3)
                {
                    if (res[1] != pld[1])
                    {
                        //TODO modbus exception code parsing
                        //  throw new ServerModbusTCPException($"ExecCustomRequest: Not valid function code {res[1]} expected {pld[1]}")
                    }
                    
                }
                
                var count = res.Length - 4;
                byte[] retval;
                if (res.Length == 3)
                {
                    retval = [0];
                    // this.Log().Warn("EmptyResponse");
                }
                else
                {
                    // remove Id, cmd and crc
                    retval = res.ToList().GetRange(2, res.Length - 4).ToArray();
                }
                return retval; 
                
            }catch(System.TimeoutException e)
            {
                return [0];
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        
        public bool IsOpen =>  _port.IsOpen;

        private void SerialWrite(List<byte> req)
        {
           
            var crc = ChMbcrc16(req.ToArray(), (ushort)req.Count);

            req.Add((byte)(0x00FF & crc));
            req.Add((byte)((0xFF00 & crc) >> 8));

            LogPld(req.ToArray(), prefix: "->");
            if (TimerWdg.IsRunning)
            {
                this.Log().Error($"Skip write request. Port is on wait");
                return;
            }
           
            TimerWdg.Restart();
            if (!_port.IsOpen) return;
            try
            {
                _port.ReadExisting();
                _port.Write(req.ToArray(), 0, req.Count);
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        private void SerialReceive(object sender, SerialDataReceivedEventArgs e)
        {
            UInt16 crc = 0;
            UInt16 frameCrc = 1;

            var size = _port.BytesToRead;
            var data = new byte[size];
            _port.Read(data, 0, size);
            _rxbuf.AddRange(data);

            LogPld(_rxbuf.ToArray(), prefix: "<-");
            size = _rxbuf.Count;
            if(size > 2) {
                crc = ChMbcrc16(_rxbuf.ToArray(), (ushort)(size - 2));
                frameCrc = (UInt16)(_rxbuf.ToArray()[size - 2] + (_rxbuf.ToArray()[size - 1] << 8));
            }
            else
            {
                this.Log().Error("Serial Recieve wrong frame size");
            }

            if (crc == frameCrc)
            {
                
                TimerWdg.Stop();
                if (_logging)
                {
                    this.Log().Info($"{TimerWdg.ElapsedMilliseconds} ms elapsed, CRC OK ");
                }
                
                RxSub.OnNext(_rxbuf.ToArray());
                _rxbuf.Clear();
            }
        }

        private static readonly byte[] AucCrcLo =
        [
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 
            0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E,
        0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9,
        0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC,
        0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
        0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32,
        0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D,
        0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
        0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF,
        0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
        0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1,
        0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4,
        0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB,
        0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA,
        0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
        0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
        0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97,
        0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E,
        0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89,
        0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
        0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83,
        0x41, 0x81, 0x80, 0x40
        ];

        private static readonly byte[] AucCrcHi =
        [
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40
        ];

        private static UInt16 ChMbcrc16(byte[] pucFrame, UInt16 len)
        {
            byte ucCrcHi = 0xFF;
            byte ucCrcLo = 0xFF;

            for (var i = 0; i < len; i++)
            {
                try
                {
                    var iIndex = ucCrcLo ^ pucFrame[i];
                    ucCrcLo = (byte)(ucCrcHi ^ AucCrcHi[iIndex]);
                    ucCrcHi = AucCrcLo[iIndex];
                }
                catch (Exception) { return 0; }
            }

            return (UInt16)(ucCrcLo + (ucCrcHi << 8));
        }

        private static UInt16[] ConvertFromByte(byte[] data)
        {
            int index = 0;
            var res = data.GroupBy(_ => (index++) / 2).Select(x => BitConverter.ToUInt16(x.Reverse().ToArray(), 0)).ToList();
            return res.ToArray();
        }

        private void LogPld(byte[] pld, string prefix = "")
        {
            if(!_logging) return;
            string res = $"{prefix} ";
            foreach (var b in pld)
            {
                res += $"{b:X2} ";
            }
            this.Log().Info($"{res}");
        }




    }
}
