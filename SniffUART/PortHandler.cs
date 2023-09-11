using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SniffUART.FormMain;

namespace SniffUART {
    internal class PortHandler {
        // refer to https://stackoverflow.com/questions/1243070/how-to-read-and-write-from-the-serial-port
        public class Uart : SerialPort {
            private int _receiveTimeout;

            public int ReceiveTimeout { get => _receiveTimeout; set => _receiveTimeout = value; }

            static private string ComPortName = "";

            /// <summary>
            /// It builds PortName using ComPortNum parameter and opens SerialPort.
            /// </summary>
            /// <param name="ComPortNum"></param>
            public Uart(string portName) : base() {
                ComPortName = portName;
            }

            public new void Open() {
                try {
                    base.Open();
                } catch (UnauthorizedAccessException) {
                    Console.WriteLine("Error: Port {0} is in use", ComPortName);
                } catch (Exception ex) {
                    Console.WriteLine("Uart exception: " + ex);
                }
            } //Uart()

            /// <summary>
            /// Private property returning positive only Environment.TickCount
            /// </summary>
            private int _tickCount { get => Environment.TickCount & Int32.MaxValue; }

            /// <summary>
            /// It uses SerialPort.BaseStream rather SerialPort functionality .
            /// It Receives up to maxLen number bytes of data, 
            /// Or throws TimeoutException if no any data arrived during ReceiveTimeout. 
            /// It works likes socket-recv routine (explanation in body).
            /// Returns:
            ///    totalReceived - bytes, 
            ///    TimeoutException,
            ///    -1 in non-ComPortNum Exception  
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="maxLen"></param>
            /// <returns></returns>
            public int Recv(byte[] buffer, int maxLen) {
                /// The routine works in "pseudo-blocking" mode. It cycles up to first 
                /// data received using BaseStream.ReadTimeout = TimeOutSpan (2 ms).
                /// If no any message received during ReceiveTimeout property, 
                /// the routine throws TimeoutException 
                /// In other hand, if any data has received, first no-data cycle
                /// causes to exit from routine.

                int TimeOutSpan = 2;
                // counts delay in TimeOutSpan-s after end of data to break receive
                int EndOfDataCnt;
                // pseudo-blocking timeout counter
                int TimeOutCnt = _tickCount + _receiveTimeout;
                //number of currently received data bytes
                int justReceived = 0;
                //number of total received data bytes
                int totalReceived = 0;

                BaseStream.ReadTimeout = TimeOutSpan;
                //causes (2+1)*TimeOutSpan delay after end of data in UART stream
                EndOfDataCnt = 2;
                while (_tickCount < TimeOutCnt && EndOfDataCnt > 0) {
                    try {
                        justReceived = 0;
                        justReceived = base.BaseStream.Read(buffer, totalReceived, maxLen - totalReceived);
                        totalReceived += justReceived;

                        if (totalReceived >= maxLen)
                            break;
                    } catch (TimeoutException) {
                        if (totalReceived > 0)
                            EndOfDataCnt--;
                    } catch (Exception ex) {
                        totalReceived = -1;
                        base.Close();
                        Console.WriteLine("Recv exception: " + ex);
                        break;
                    }

                } //while
                return totalReceived;
            } // Recv()            
        } // Uart

        private static FormMain _frm;
        public int _uartReal;  // real port no
        public int _uartPar;   // port no, where port parameter are stored
        public Thread _readThread;
        public DateTime _dateStart;
        private bool _continue = false;
        private Uart _uart;
        private byte[] buf = new byte[256]; // receive buffer

        // c'tor
        public PortHandler(FormMain frm, int uartNo) {
            _frm = frm;
            _uartReal = uartNo;
            _uartPar = uartNo;

            // Allow the user to set the appropriate properties.
            string portName = _frm._uarts[_uartReal].PortName;
            _uart = new Uart(portName) {
                PortName = _frm._uarts[_uartReal].PortName,
                BaudRate = _frm._uarts[_uartPar].BaudRate,
                Parity = _frm._uarts[_uartPar].Parity,
                DataBits = _frm._uarts[_uartPar].DataBits,
                StopBits = _frm._uarts[_uartPar].StopBits,
                Handshake = _frm._uarts[_uartPar].Handshake,

                // Set the read timeout
                ReceiveTimeout = _frm._uarts[_uartReal].ReadTimeout
            };

            bool bOnePort = (_uartReal == 1 && _frm.OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            if (bOnePort) {
                _uartPar = 0; // take port parameter from UART0
            }
        }

        public void LogOpen() {
            if (!_continue || !_uart.IsOpen)
                return;

            // log Start
            _dateStart = DateTime.Now;
            string dateStr = _dateStart.ToString("yy-MM-dd HH:mm:ss.ff");
            object[] data = new object[] { eLogType.eStart, _uartReal, dateStr };
            _frm.AddData(data);
        }

        public bool Open() {
            if (_uart.IsOpen)
                return true;

            _readThread = new Thread(ReadLoop);
            _readThread.Priority = ThreadPriority.AboveNormal;

            bool bOnePort = (_uartReal == 1 && _frm.OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            _uartPar = (bOnePort) ? 0 : _uartReal; // take port parameter from UART0, if OnePort is true

            try {
                _uart.ReceivedBytesThreshold = buf.Length;
                _uart.ReadBufferSize = buf.Length;
                _uart.Open();
                _continue = true;
                _readThread.Start();

                LogOpen();
            } catch { }

            return _uart.IsOpen;
        }

        public void Close() {
            if (_continue) {
                _continue = false;
                if (_uart.IsOpen) {
                    _uart.Close();
                }
                _readThread.Abort();
            }
        }

        private Int32 ReadBytes(Int32 idx) {
            Int32 num = 0;
            try {
                num = _uart.Recv(buf, buf.Length);
            } catch (TaskCanceledException) {
                _continue = false;
            } catch (TimeoutException) { // continue
            }

            Console.WriteLine("# Bytes=" + num);
            
            return num;
        }

        private void ReadLoop() {
            _uart.DiscardInBuffer();
            _uart.DiscardOutBuffer();

            while (_continue && _uart.IsOpen) {
                Int32 rcvNum = ReadBytes(0);
                if (rcvNum > 0) {
                    // log Data
                    DateTime date = DateTime.Now;
                    string dateStr = date.ToString("yy-MM-dd HH:mm:ss.ff");
                    TimeSpan diff = date - _dateStart;
                    string diffStr = diff.ToString(@"hh\:mm\:ss\.ff");
                    object[] data= new object[] { eLogType.eData, _uartReal, dateStr, diffStr, rcvNum, buf };
                    _frm.AddData(data);
                }
            } // while
        }
    }
}
