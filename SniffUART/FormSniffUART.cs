using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SniffUART
{
    public partial class FormMain : Form
    {
        private FormMain _frm;
        public delegate void AddDataRow(object[] row);
        private AddDataRow _addDataDelegate;
        public SerialPort[] _uarts = new SerialPort[2];
        public string[] _portName = new string[2];
        private PortHandler[] _ports = new PortHandler[2];

        public FormMain()
        {
            InitializeComponent();
            DGVData.RowHeadersWidth = 25;
            DGVData.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            _frm = this;
            _addDataDelegate = new AddDataRow(AddData);
            _uarts[0] = new SerialPort();
            _uarts[1] = new SerialPort();
        }

        private void FormMain_Load(object sender, EventArgs e) {
            // restore settings
            OnePortToolStripMenuItem.Checked = Properties.Settings.Default.OnePort;
            ToggleTimeToolStripMenuItem.Checked = Properties.Settings.Default.ToggleTime;
            string view = Properties.Settings.Default.ToggleView;
            asciiToolStripMenuItem.Checked = (view == "ASCII");
            hexToolStripMenuItem.Checked = (view == "Hex");
            msgToolStripMenuItem.Checked = (view == "Msg");
            if (view == "ASCII")
                asciiToolStripMenuItem_Click(sender, e);
            else if (view == "Hex")
                hexToolStripMenuItem_Click(sender, e);
            else if (view == "Hex")
                hexToolStripMenuItem_Click(sender, e);
            else if (view == "Msg")
                msgToolStripMenuItem_Click(sender, e);

            _portName[0] = (Properties.Settings.Default.UART0_Name == "") ? "CB3S" : Properties.Settings.Default.UART0_Name;
            _uarts[0].PortName = (Properties.Settings.Default.UART0_PortName == "") ? _uarts[0].PortName : Properties.Settings.Default.UART0_PortName;
            _uarts[0].BaudRate = (Properties.Settings.Default.UART0_BaudRate == "") ? _uarts[0].BaudRate : int.Parse(Properties.Settings.Default.UART0_BaudRate);
            _uarts[0].Parity = (Properties.Settings.Default.UART0_Parity == "") ? _uarts[0].Parity : (Parity)Enum.Parse(typeof(Parity), Properties.Settings.Default.UART0_Parity, true);
            _uarts[0].DataBits = (Properties.Settings.Default.UART0_DataBits == "") ? _uarts[0].DataBits : int.Parse(Properties.Settings.Default.UART0_DataBits);
            _uarts[0].StopBits = (Properties.Settings.Default.UART0_StopBits == "") ? _uarts[0].StopBits : (StopBits)Enum.Parse(typeof(StopBits), Properties.Settings.Default.UART0_StopBits, true);
            _uarts[0].Handshake = (Properties.Settings.Default.UART0_Handshake == "") ? _uarts[0].Handshake : (Handshake)Enum.Parse(typeof(Handshake), Properties.Settings.Default.UART0_Handshake, true);
            _uarts[0].ReadTimeout = (Properties.Settings.Default.UART0_ReadTimeout == "") ? _uarts[0].ReadTimeout : int.Parse(Properties.Settings.Default.UART0_ReadTimeout);
            _portName[1] = (Properties.Settings.Default.UART1_Name == "") ? "MCU" : Properties.Settings.Default.UART1_Name;
            _uarts[1].PortName = (Properties.Settings.Default.UART1_PortName == "") ? _uarts[1].PortName : Properties.Settings.Default.UART1_PortName;
            _uarts[1].BaudRate = (Properties.Settings.Default.UART1_BaudRate == "") ? _uarts[1].BaudRate : int.Parse(Properties.Settings.Default.UART1_BaudRate);
            _uarts[1].Parity = (Properties.Settings.Default.UART1_Parity == "") ? _uarts[1].Parity : (Parity)Enum.Parse(typeof(Parity), Properties.Settings.Default.UART1_Parity, true);
            _uarts[1].DataBits = (Properties.Settings.Default.UART1_DataBits == "") ? _uarts[1].DataBits : int.Parse(Properties.Settings.Default.UART1_DataBits);
            _uarts[1].StopBits = (Properties.Settings.Default.UART1_StopBits == "") ? _uarts[1].StopBits : (StopBits)Enum.Parse(typeof(StopBits), Properties.Settings.Default.UART1_StopBits, true);
            _uarts[1].Handshake = (Properties.Settings.Default.UART1_Handshake == "") ? _uarts[1].Handshake : (Handshake)Enum.Parse(typeof(Handshake), Properties.Settings.Default.UART1_Handshake, true);
            _uarts[1].ReadTimeout = (Properties.Settings.Default.UART0_ReadTimeout == "") ? _uarts[1].ReadTimeout : int.Parse(Properties.Settings.Default.UART1_ReadTimeout);

            _ports[0] = new PortHandler(this, 0);
            _ports[1] = new PortHandler(this, 1);

            // adjust controls
            toolStripStatusUART0.Text = _portName[0] + "(" + _uarts[0].PortName + ")";
            toolStripStatusUART1.Text = _portName[1] + "(" + _uarts[1].PortName + ")";
            ToggleTimeToolStripMenuItem_CheckStateChanged(sender, e);
            OnePortToolStripMenuItem_CheckStateChanged(sender, e);
        }

        private void UART0ToolStripMenuItem_Click(object sender, EventArgs e) {
            UARTProperties viewUart = new UARTProperties(this, 0);
            viewUart.Show();
        }

        private void UART1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UARTProperties viewUart = new UARTProperties(this, 1);
            viewUart.Show();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfdlg = new SaveFileDialog {
                Filter = "Text Files (*.txt) | *.txt" //Here you can filter which all files you wanted allow to open
            };
            if (sfdlg.ShowDialog() == DialogResult.OK) {
                // Code to write the stream goes here.
            }
        }

        private void SaveSettingsToolStripMenuItem_Click(object sender, EventArgs e) {
            // need to save?
            string view = (asciiToolStripMenuItem.CheckState == CheckState.Checked) ? "ASCII" : (hexToolStripMenuItem.CheckState == CheckState.Checked) ? "Hex" : "Msg";
            if (Properties.Settings.Default.OnePort == (OnePortToolStripMenuItem.CheckState == CheckState.Checked) &&
                Properties.Settings.Default.ToggleTime == (ToggleTimeToolStripMenuItem.CheckState == CheckState.Checked) &&
                Properties.Settings.Default.ToggleView == view &&
                Properties.Settings.Default.UART0_Name == _frm._portName[0] &&
                Properties.Settings.Default.UART0_PortName == _uarts[0].PortName &&
                Properties.Settings.Default.UART0_BaudRate == _uarts[0].BaudRate.ToString() &&
                Properties.Settings.Default.UART0_Parity == _uarts[0].Parity.ToString() &&
                Properties.Settings.Default.UART0_DataBits == _uarts[0].DataBits.ToString() &&
                Properties.Settings.Default.UART0_StopBits == _uarts[0].StopBits.ToString() &&
                Properties.Settings.Default.UART0_Handshake == _uarts[0].Handshake.ToString() &&
                Properties.Settings.Default.UART0_ReadTimeout == _uarts[0].ReadTimeout.ToString() &&
                Properties.Settings.Default.UART1_Name == _frm._portName[1] &&
                Properties.Settings.Default.UART1_PortName == _uarts[1].PortName &&
                Properties.Settings.Default.UART1_BaudRate == _uarts[1].BaudRate.ToString() &&
                Properties.Settings.Default.UART1_Parity == _uarts[1].Parity.ToString() &&
                Properties.Settings.Default.UART1_DataBits == _uarts[1].DataBits.ToString() &&
                Properties.Settings.Default.UART1_StopBits == _uarts[1].StopBits.ToString() &&
                Properties.Settings.Default.UART1_Handshake == _uarts[1].Handshake.ToString() &&
                Properties.Settings.Default.UART1_ReadTimeout == _uarts[1].ReadTimeout.ToString()
                ) { // no
                return;
            }

            string evtType = e.GetType().Name;
            if (evtType != "EventArgs") {
                DialogResult res = MessageBox.Show("Save?", "Save Settings", MessageBoxButtons.YesNo);
                if (res != DialogResult.Yes)
                    return;
            }

            // save settings
            Properties.Settings.Default.OnePort = (OnePortToolStripMenuItem.CheckState == CheckState.Checked);
            Properties.Settings.Default.ToggleTime = (ToggleTimeToolStripMenuItem.CheckState == CheckState.Checked);
            Properties.Settings.Default.ToggleView = view;
            Properties.Settings.Default.UART0_Name = _frm._portName[0];
            Properties.Settings.Default.UART0_PortName = _uarts[0].PortName;
            Properties.Settings.Default.UART0_BaudRate = _uarts[0].BaudRate.ToString();
            Properties.Settings.Default.UART0_Parity = _uarts[0].Parity.ToString();
            Properties.Settings.Default.UART0_DataBits = _uarts[0].DataBits.ToString();
            Properties.Settings.Default.UART0_StopBits = _uarts[0].StopBits.ToString();
            Properties.Settings.Default.UART0_Handshake = _uarts[0].Handshake.ToString();
            Properties.Settings.Default.UART0_ReadTimeout = _uarts[0].ReadTimeout.ToString();
            Properties.Settings.Default.UART1_Name = _frm._portName[1];
            Properties.Settings.Default.UART1_PortName = _uarts[1].PortName;
            Properties.Settings.Default.UART1_BaudRate = _uarts[1].BaudRate.ToString();
            Properties.Settings.Default.UART1_Parity = _uarts[1].Parity.ToString();
            Properties.Settings.Default.UART1_DataBits = _uarts[1].DataBits.ToString();
            Properties.Settings.Default.UART1_StopBits = _uarts[1].StopBits.ToString();
            Properties.Settings.Default.UART1_Handshake = _uarts[1].Handshake.ToString();
            Properties.Settings.Default.UART1_ReadTimeout = _uarts[0].ReadTimeout.ToString();
            Properties.Settings.Default.Save();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e) {
            _ports[0].Close();
            _ports[1].Close();

            this.Close();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e) {
            _ports[0].Close();
            _ports[1].Close();
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e) {
            SaveSettingsToolStripMenuItem_Click(sender, e);
        }

        private void StartToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void StopToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void ContinueToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void ToggleTimeToolStripMenuItem_CheckStateChanged(object sender, EventArgs e) {
            // toggle time & delta time
            ColTime.Visible = ToggleTimeToolStripMenuItem.CheckState == CheckState.Checked;
            ColDeltaTime.Visible = ! (ToggleTimeToolStripMenuItem.CheckState == CheckState.Checked);
        }

        private void OnePortToolStripMenuItem_CheckStateChanged(object sender, EventArgs e) {
            if (OnePortToolStripMenuItem.CheckState == CheckState.Checked) {
                
            }
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e) {
            DGVData.Rows.Clear();
            _ports[0].LogOpen();
            _ports[1].LogOpen();
        }

        private void DGVData_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e) {
            string colName = e.Column.Name;
            switch (colName) {
                case "ColPort":
                break;
                case "ColDeltaTime":
                    ColTime.Width = ColDeltaTime.Width;
                break;
                case "ColTime":
                    ColDeltaTime.Width = ColTime.Width;
                break;
                case "ColHex":
                break;
                case "ColASCII":
                break;
                case "ColMsg":
                break;
            } // switch
        }

        private void OpenPortsToolStripMenuItem_Click(object sender, EventArgs e) {
            string uart0_err = "";
            string uart1_err = "";
            try {
                _ports[0].Open();
                toolStripStatusUART0.Text = _portName[0] + "(" + _uarts[0].PortName + ")";
                toolStripStatusUART0.BackColor = Color.LightGreen;

            } catch (Exception ex) {
                uart0_err = ex.Message;
            }
            try {
                _ports[1].Open();
                toolStripStatusUART1.Text = _portName[1] + "(" + _uarts[1].PortName + ")";
                toolStripStatusUART1.BackColor = Color.LightGreen;
            } catch (Exception ex) {
                uart1_err = ex.Message;
            }

            string errTxt = "";
            errTxt += (uart0_err != "") ? "\nUART0: " + uart0_err : "";
            errTxt += (uart1_err != "") ? "\nUART1: " + uart1_err : "";
            if (uart0_err != "" || uart1_err != "") {
                MessageBox.Show("Error", "Error in OpenPort():" + errTxt, MessageBoxButtons.OK);
            }
        }

        public void AddData(object[] data) {
            if (_frm.DGVData.IsDisposed)
                return;
            if (_frm.DGVData.InvokeRequired) {
                this.Invoke(_addDataDelegate, new object[] { data });
                return;
            }

            // below is executed in the thread of Form
            object[] row = null; // the row columns of DGV
            int portNo = (int)data[0];
            DateTime dt = (DateTime)data[1];
            string dtStr = dt.ToString("yy-MM-dd HH:mm:ss.ff");
            if (portNo >= 256) { // log start
                portNo -= 256;
                string startTxt = "Open " + _uarts[portNo].PortName;
                row = new object[] { _portName[portNo], dtStr, dtStr, startTxt, startTxt, startTxt };
            } else { // log data
                TimeSpan diff = (TimeSpan)data[2];
                string tsStr = diff.ToString(@"hh\:mm\:ss\.ff");
                int rcvNum = (int)data[3];
                Byte[] buf = (Byte[])data[4];
                string hex = BitConverter.ToString(buf, 0, rcvNum).Replace('-', ';');
                string ascii = Encoding.ASCII.GetString(buf, 0, rcvNum);

                // decode Tuya message
                RichTextBox rt = DisplayMsg.decodeMsg(rcvNum, ref buf);

                row = new object[] { _portName[portNo], tsStr, dtStr, hex, ascii, rt.Rtf };
            }

            _frm.DGVData.Rows.Add(row);
            if (_frm.DGVData.SelectedCells.Count > 0 && _frm.DGVData.RowCount == (_frm.DGVData.SelectedCells[0].RowIndex + 1)) {
                _frm.DGVData.FirstDisplayedScrollingRowIndex = _frm.DGVData.RowCount - 1;
            }
        }

        private void DGVData_CellContentClick(object sender, DataGridViewCellEventArgs e) {
        }

        private void DGVData_RowEnter(object sender, DataGridViewCellEventArgs e) {
            Int32 selectedRowCount = DGVData.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount > 0) {
                string portTxt = (string)DGVData.Rows[e.RowIndex].Cells["colPort"].Value;
                if (portTxt != null) {
                    int idxPort = (_uarts[0].PortName == portTxt) ? 0 : 1;
                    if (selectedRowCount == 1) {
                        DateTime dtStart = _ports[idxPort]._dateStart;
                        string timeTxt = (string)DGVData.Rows[e.RowIndex].Cells["colTime"].Value;
                        DateTime dt = DateTime.ParseExact(timeTxt, "yy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture);
                        TimeSpan diff = dt - dtStart;
                        string tsTxt = diff.ToString(@"hh\:mm\:ss\.ff");
                        toolStripStatusDeltaTime.Text = tsTxt;
                    } else {
                        DataGridViewRow rowStart = DGVData.SelectedRows[0];
                        DataGridViewRow rowEnd = DGVData.SelectedRows[DGVData.SelectedRows.Count - 1];
                        int idxStart = (rowStart.Index < rowEnd.Index) ? rowStart.Index : rowEnd.Index;
                        int idxEnd = (rowStart.Index > rowEnd.Index) ? rowStart.Index : rowEnd.Index;
                        string startTxt = (string)DGVData.Rows[idxStart].Cells["colTime"].Value;
                        string endTxt = (string)DGVData.Rows[idxEnd].Cells["colTime"].Value;
                        DateTime dtStart = DateTime.ParseExact(startTxt, "yy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture);
                        DateTime dtEnd = DateTime.ParseExact(endTxt, "yy-MM-dd HH:mm:ss.ff", CultureInfo.InvariantCulture);
                        TimeSpan diff = dtEnd - dtStart;
                        string tsTxt = diff.ToString(@"hh\:mm\:ss\.ff");
                        toolStripStatusDeltaTime.Text = tsTxt;
                    }
                }
            }
        }

        private void asciiToolStripMenuItem_Click(object sender, EventArgs e) {
            asciiToolStripMenuItem.CheckState = CheckState.Checked;
            hexToolStripMenuItem.CheckState = CheckState.Unchecked;
            msgToolStripMenuItem.CheckState = CheckState.Unchecked;
            ColASCII.Visible = true;
            ColHex.Visible = false;
            ColMsg.Visible = false;
        }

        private void hexToolStripMenuItem_Click(object sender, EventArgs e) {
            asciiToolStripMenuItem.CheckState = CheckState.Unchecked;
            hexToolStripMenuItem.CheckState = CheckState.Checked;
            msgToolStripMenuItem.CheckState = CheckState.Unchecked;
            ColASCII.Visible = false;
            ColHex.Visible = true;
            ColMsg.Visible = false;
        }

        private void msgToolStripMenuItem_Click(object sender, EventArgs e) {
            asciiToolStripMenuItem.CheckState = CheckState.Unchecked;
            hexToolStripMenuItem.CheckState = CheckState.Unchecked;
            msgToolStripMenuItem.CheckState = CheckState.Checked;
            ColASCII.Visible = false;
            ColHex.Visible = false;
            ColMsg.Visible = true;
        }
    }
}
