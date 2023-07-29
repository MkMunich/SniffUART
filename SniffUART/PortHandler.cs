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
        private static FormMain _frm;
        public int _uartReal;  // real port no
        public int _uartPar;   // port no, where port parameter are stored
        public Thread _readThread;
        public DateTime _dateStart;
        private bool _continue = false;
        private SerialPort _serialPort = new SerialPort();
        private byte[] buf = new byte[256]; // receive buffer

        // c'tor
        public PortHandler(FormMain frm, int uart) {
            _frm = frm;
            _uartReal = uart;
            _uartPar = uart;
            bool bOnePort = (_uartReal == 1 && _frm.OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            if (bOnePort) {
                _uartPar = 0; // take port parameter from UART0
            }

            _readThread = new Thread(ReadLoop);
            _readThread.Priority = ThreadPriority.Highest;

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = _frm._uarts[_uartReal].PortName;
            _serialPort.BaudRate = _frm._uarts[_uartPar].BaudRate;
            _serialPort.Parity = _frm._uarts[_uartPar].Parity;
            _serialPort.DataBits = _frm._uarts[_uartPar].DataBits;
            _serialPort.StopBits = _frm._uarts[_uartPar].StopBits;
            _serialPort.Handshake = _frm._uarts[_uartPar].Handshake;

            // Set the read/write timeouts
            _serialPort.ReadTimeout = _frm._uarts[_uartReal].ReadTimeout;
            _serialPort.ReceivedBytesThreshold = 100;
        }

        public void LogOpen() {
            if (!_continue || !_serialPort.IsOpen)
                return;

            // log Start
            _dateStart = DateTime.Now;
            string dateStr = _dateStart.ToString("yy-MM-dd HH:mm:ss.ff");
            object[] data = new object[] { eLogType.eStart, _uartReal, dateStr };
            _frm.AddData(data);
        }

        public void Open() {
            if (_serialPort.IsOpen)
                return;

            bool bOnePort = (_uartReal == 1 && _frm.OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            _uartPar = (bOnePort) ? 0 : _uartReal; // take port parameter from UART0, if OnePort is true

            try {
                _serialPort.ReceivedBytesThreshold = 2;
                _serialPort.ReadBufferSize = buf.Length;
                _serialPort.Open();
                _continue = true;
                _readThread.Start();

                LogOpen();
            } catch { }
        }

        public void Close() {
            if (_continue) {
                _continue = false;
                _readThread.Abort();
            }
        }

        private Int32 ReadBytes(Int32 idx) {
            Int32 num = 0;
            try {
                num = _serialPort.Read(buf, idx, buf.Length - idx);
            } catch (TaskCanceledException) {
                _continue = false;
            } catch (TimeoutException e) {
                string exeptStr = e.Message;
                Console.WriteLine(exeptStr);
            }

            Console.WriteLine("# Bytes=" + num);
            
            return num;
        }

        private void ReadLoop() {
            Int32 rcvNum = 0;
            Int32 rcv = 0;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            while (_continue && _serialPort.IsOpen) {
                if (_serialPort.BytesToRead > 0) {
                    rcv = ReadBytes(rcvNum);
                }
                if (rcv > 0 && (rcvNum + rcv) < buf.Length) {
                    rcvNum += rcv;
                } else if (rcvNum > 0) {
                    // log Data
                    DateTime date = DateTime.Now;
                    string dateStr = date.ToString("yy-MM-dd HH:mm:ss.ff");
                    TimeSpan diff = date - _dateStart;
                    string diffStr = diff.ToString(@"hh\:mm\:ss\.ff");
                    object[] data= new object[] { eLogType.eData, _uartReal, dateStr, diffStr, rcvNum, buf };
                    _frm.AddData(data);
                    rcvNum = 0;
                }
                rcv = 0;
            } // while
        }
    }
}
