using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SniffUART
{
    public partial class UARTProperties : Form
    {
        private FormMain _frm;
        private int _uartReal;      // real port
        private int _uartPar;       // parameter port
        private string portName;    // logical port name

        private void updateBaudRateCollection(int possibleBaudRates) {
            const int BAUD_075 = 0x00000001;
            const int BAUD_110 = 0x00000002;
            const int BAUD_150 = 0x00000008;
            const int BAUD_300 = 0x00000010;
            const int BAUD_600 = 0x00000020;
            const int BAUD_1200 = 0x00000040;
            const int BAUD_1800 = 0x00000080;
            const int BAUD_2400 = 0x00000100;
            const int BAUD_4800 = 0x00000200;
            const int BAUD_7200 = 0x00000400;
            const int BAUD_9600 = 0x00000800;
            const int BAUD_14400 = 0x00001000;
            const int BAUD_19200 = 0x00002000;
            const int BAUD_38400 = 0x00004000;
            const int BAUD_56K = 0x00008000;
            const int BAUD_57600 = 0x00040000;
            const int BAUD_115200 = 0x00020000;
            const int BAUD_128K = 0x00010000;
            const int BAUD_USER = 0x10000000;

            cBoxBaudRate.Items.Clear();

            if ((possibleBaudRates & BAUD_075) > 0)
                cBoxBaudRate.Items.Add("75");
            if ((possibleBaudRates & BAUD_110) > 0)
                cBoxBaudRate.Items.Add("110");
            if ((possibleBaudRates & BAUD_150) > 0)
                cBoxBaudRate.Items.Add("150");
            if ((possibleBaudRates & BAUD_300) > 0)
                cBoxBaudRate.Items.Add("300");
            if ((possibleBaudRates & BAUD_600) > 0)
                cBoxBaudRate.Items.Add("600");
            if ((possibleBaudRates & BAUD_1200) > 0)
                cBoxBaudRate.Items.Add("1200");
            if ((possibleBaudRates & BAUD_1800) > 0)
                cBoxBaudRate.Items.Add("1800");
            if ((possibleBaudRates & BAUD_2400) > 0)
                cBoxBaudRate.Items.Add("2400");
            if ((possibleBaudRates & BAUD_4800) > 0)
                cBoxBaudRate.Items.Add("4800");
            if ((possibleBaudRates & BAUD_7200) > 0)
                cBoxBaudRate.Items.Add("7200");
            if ((possibleBaudRates & BAUD_9600) > 0)
                cBoxBaudRate.Items.Add("9600");
            if ((possibleBaudRates & BAUD_14400) > 0)
                cBoxBaudRate.Items.Add("14400");
            if ((possibleBaudRates & BAUD_19200) > 0)
                cBoxBaudRate.Items.Add("19200");
            if ((possibleBaudRates & BAUD_38400) > 0)
                cBoxBaudRate.Items.Add("38400");
            if ((possibleBaudRates & BAUD_56K) > 0)
                cBoxBaudRate.Items.Add("56000");
            if ((possibleBaudRates & BAUD_57600) > 0)
                cBoxBaudRate.Items.Add("57600");
            if ((possibleBaudRates & BAUD_115200) > 0)
                cBoxBaudRate.Items.Add("115200");
            if ((possibleBaudRates & BAUD_128K) > 0)
                cBoxBaudRate.Items.Add("128000");
            if ((possibleBaudRates & BAUD_USER) > 0) { // actual newer rates
                cBoxBaudRate.Items.Add("230400");
                cBoxBaudRate.Items.Add("460800");
                cBoxBaudRate.Items.Add("921600");
            }
        }

        private void getBaudRateCollection(string portName) {
            try {
                SerialPort _serialPort = new SerialPort(portName);
                _serialPort.Open();

                // Getting COMMPROP structure, and its property dwSettableBaud.
                object p = _serialPort.BaseStream.GetType().GetField("commProp",
                   BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_serialPort.BaseStream);
                Int32 dwSettableBaud = (Int32)p.GetType().GetField("dwSettableBaud",
                   BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p);

                _serialPort.Close();
                updateBaudRateCollection(dwSettableBaud);

                // set default baudrate
                cBoxBaudRate.Text = _frm._uarts[_uartReal].BaudRate.ToString();
            } catch {
                cBoxBaudRate.Items.Clear();
            }
        }

        // c'tor
        public UARTProperties(FormMain frm, int uart)
        {
            _frm = frm;
            _uartReal = uart;
            _uartPar = uart;

            InitializeComponent();

            tBoxNo.Text = _uartReal.ToString();

            bool bOnePort = (_uartReal == 1 && _frm.OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            if (bOnePort) {
                _uartPar = 0; // take port parameter from UART0
                cBoxBaudRate.Enabled = false;
                cBoxParity.Enabled = false;
                tBoxDataBits.Enabled = false;
                cBoxStopBits.Enabled = false;
                cBoxHandshake.Enabled = false;
            }

            // logical port name
            tBoxName.Text = _frm._portName[_uartReal];

            // UART
            cBoxUART.Items.Clear();
            foreach (string s in SerialPort.GetPortNames()) {
                cBoxUART.Items.Add(s);
            }
            cBoxUART.Text = _frm._uarts[_uartReal].PortName;
            if (cBoxUART.Text == "")
                cBoxUART.Text = (_uartReal == 0) ? Properties.Settings.Default.UART0_PortName : Properties.Settings.Default.UART1_PortName;

            // BaudRate
            cBoxBaudRate.Text = _frm._uarts[_uartPar].BaudRate.ToString();
            if (cBoxBaudRate.Text == "")
                cBoxBaudRate.Text = (_uartPar == 0) ? Properties.Settings.Default.UART0_BaudRate : Properties.Settings.Default.UART1_BaudRate;

            // Parity
            cBoxParity.Items.Clear();
            foreach (string s in Enum.GetNames(typeof(Parity))) {
                cBoxParity.Items.Add(s);
            }
            cBoxParity.Text = _frm._uarts[_uartPar].Parity.ToString();
            if (cBoxParity.Text == "")
                cBoxParity.Text = (_uartPar == 0) ? Properties.Settings.Default.UART0_Parity : Properties.Settings.Default.UART1_Parity;

            // DataBits
            tBoxDataBits.Text = _frm._uarts[_uartPar].DataBits.ToString();
            if (tBoxDataBits.Text == "")
                tBoxDataBits.Text = (_uartPar == 0) ? Properties.Settings.Default.UART0_DataBits : Properties.Settings.Default.UART1_DataBits;

            // StopBits
            cBoxStopBits.Items.Clear();
            foreach (string s in Enum.GetNames(typeof(StopBits))) {
                cBoxStopBits.Items.Add(s);
            }
            cBoxStopBits.Text = _frm._uarts[_uartPar].StopBits.ToString();
            if (cBoxStopBits.Text == "")
                cBoxStopBits.Text = (_uartPar == 0) ? Properties.Settings.Default.UART0_StopBits : Properties.Settings.Default.UART1_StopBits;

            // Handshake
            cBoxHandshake.Items.Clear();
            foreach (string s in Enum.GetNames(typeof(Handshake))) {
                cBoxHandshake.Items.Add(s);
            }
            cBoxHandshake.Text = _frm._uarts[_uartPar].Handshake.ToString();
            if (cBoxHandshake.Text == "")
                cBoxHandshake.Text = (_uartPar == 0) ? Properties.Settings.Default.UART0_Handshake : Properties.Settings.Default.UART1_Handshake;

            // ReadTimeout
            tBoxReadTimeout.Text = _frm._uarts[_uartReal].ReadTimeout.ToString();
            if (tBoxReadTimeout.Text == "")
                tBoxReadTimeout.Text = (_uartReal == 0) ? Properties.Settings.Default.UART0_ReadTimeout : Properties.Settings.Default.UART1_ReadTimeout;
        }

        private void butOK_Click(object sender, EventArgs e)
        {
            try {
                portName = tBoxName.Text;
                _frm._portName[_uartReal] = portName;
                _frm._uarts[_uartReal].PortName = cBoxUART.GetItemText(cBoxUART.Text);
                _frm._uarts[_uartReal].BaudRate = (cBoxBaudRate.Text != "") ? int.Parse(cBoxBaudRate.Text) : 1;
                _frm._uarts[_uartReal].Parity = (cBoxParity.Text == "") ? _frm._uarts[_uartReal].Parity : (Parity)Enum.Parse(typeof(Parity), cBoxParity.Text, true);
                _frm._uarts[_uartReal].DataBits = (tBoxDataBits.Text == "") ? _frm._uarts[_uartReal].DataBits : int.Parse(tBoxDataBits.Text);
                _frm._uarts[_uartReal].StopBits = (cBoxStopBits.Text == "") ? _frm._uarts[_uartReal].StopBits : (StopBits)Enum.Parse(typeof(StopBits), cBoxStopBits.Text, true);
                _frm._uarts[_uartReal].Handshake = (cBoxHandshake.Text == "") ? _frm._uarts[_uartReal].Handshake : (Handshake)Enum.Parse(typeof(Handshake), cBoxHandshake.Text, true);
                _frm._uarts[_uartReal].ReadTimeout = (tBoxReadTimeout.Text == "") ? _frm._uarts[_uartReal].ReadTimeout : int.Parse(tBoxReadTimeout.Text);

                if (_uartReal == 0)
                    _frm.toolStripStatusUART0.Text = _frm._portName[_uartReal] + "(" + _frm._uarts[_uartReal].PortName + ")";
                else
                    _frm.toolStripStatusUART1.Text = _frm._portName[_uartReal] + "(" + _frm._uarts[_uartReal].PortName + ")";

                this.Close();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cBoxUART_TextChanged(object sender, EventArgs e) {
            if (cBoxUART.Text != "") {
                getBaudRateCollection(cBoxUART.Text);
            }
        }
    }
}
