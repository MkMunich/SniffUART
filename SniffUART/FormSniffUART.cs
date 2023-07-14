using DataGridViewRichTextBox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SniffUART
{
    public partial class FormMain : Form
    {
        private FormMain _frm;
        public FrmDecodeMessages _frmDecode;
        public delegate void AddDataRow(object[] row);
        private AddDataRow _addDataDelegate;
        public SerialPort[] _uarts = new SerialPort[2];
        public string[] _portName = new string[2];
        private PortHandler[] _ports = new PortHandler[2];

        // types of logging to DGV
        public enum eLogType { eStart, eData, eImport };

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

            _frmDecode = new FrmDecodeMessages(_frm);
            _frmDecode.Hide();

            // write header (for Excel csv import)
            object[] row = new object[] { "PortName", "Time", "DeltaTime", "Hex", "ASCII", "Message" };
            _frm.DGVData.Rows.Add(row);
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

        private string getFilename() {
            DateTime dt = DateTime.Now;
            string dtStr = dt.ToString("yyMMdd HHmm");
            return dtStr + " Sniff";
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfdlg = new SaveFileDialog {
                FileName = getFilename(),
                Filter = "Text Files (*.txt) | *.txt"
            };
            if (sfdlg.ShowDialog() == DialogResult.OK) {
                List<string> lines = new List<string>();
                foreach (DataGridViewRow row in DGVData.Rows) {
                    string line = "";
                    DataGridViewCellCollection cells = row.Cells;
                    foreach (DataGridViewCell cell in cells) {
                        string str = (string)cell.Value;
                        if (cell.EditType.Name == "DataGridViewRichTextBoxEditingControl") { // convert rtf to plain text
                            if (str != null && str.IndexOf(@"{\rtf1\") >= 0) { // cell contains rtf
                                DataGridViewRichTextBoxEditingControl richBox = new DataGridViewRichTextBoxEditingControl();
                                RichTextBox ctl = new RichTextBox();
                                ctl.Rtf = str;
                                str = ctl.Text;
                            }
                        }
                        str = (str != null) ? str : "";
                        line += "\"" + str + "\";";
                    } // foreach
                    line = line.Trim(';'); // cut off last ';'
                    lines.Add(line);
                } // foreach
                System.IO.File.WriteAllLines(sfdlg.FileName, lines);
                
            }
        }

        private void ImportToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog rfdlg = new OpenFileDialog {
                Filter = "Text Files (*Sniff.txt) | *Sniff.txt"
            };
            if (rfdlg.ShowDialog() == DialogResult.OK) {
                string[] lines = System.IO.File.ReadAllLines(rfdlg.FileName);
                foreach (string line in lines) {
                    char[] sep = { '"', ';'};
                    string[] cols = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length == 0)
                        continue;
                    string portName = cols[0];
                    string dateStr = cols[1];
                    string diffStr = cols[2];
                    string col = cols[3];
                    object[] data;
                    if (portName == "PortName") {
                        continue; // header has already been written
                    } else if (col.IndexOf("Open ") == 0) {
                        // log Start
                        data = new object[] { eLogType.eStart, portName, dateStr, diffStr, col };
                    } else {
                        // log Import
                        string[] hex = col.Split(' ');
                        byte[] buf = hex.Select(value => Convert.ToByte(value, 16)).ToArray();
                        data = new object[] { eLogType.eImport, portName, dateStr, diffStr, buf.Length, buf };
                    }
                    _frm.AddData(data);

                } // foreach
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

        // insert data into DGV
        public void AddData(object[] data) {
            if (_frm.DGVData.IsDisposed)
                return;
            if (_frm.DGVData.InvokeRequired) {
                this.Invoke(_addDataDelegate, new object[] { data });
                return;
            }

            // below is executed in the thread of Form
            string portName = "";
            string uartName = "";
            Type type = data[1].GetType();
            if (type.Name == "Int32") {
                int portNo = (int)data[1];
                portName = _portName[portNo];
                uartName = _uarts[portNo].PortName;
            } else {
                portName = uartName = (string)data[1];
            }

            object[] row = null; // the row columns of DGV
            eLogType logType = (eLogType)data[0];
            switch (logType) {
                case eLogType.eStart: {
                        string startTxt = "Open " + uartName;
                        string dtStr = (string)data[2];
                        row = new object[] { data[1], dtStr, dtStr, startTxt, startTxt, startTxt };
                    } break;
                case eLogType.eData:
                case eLogType.eImport: {
                        string dtStr = (string)data[2];
                        string tsStr = (string)data[3];
                        int rcvNum = (int)data[4];
                        Byte[] buf = (Byte[])data[5];
                        string hex = BitConverter.ToString(buf, 0, rcvNum).Replace('-', ' ');
                        string ascii = Encoding.ASCII.GetString(buf, 0, rcvNum);
                        //:-( ascii = Regex.Replace(ascii, @"[^\u0000-\u007F]+", "."); // replace all non printable char by '.'

                        // decode Tuya message
                        RichTextBox rt = new RichTextBox(); // must be c'ted outside decodeMsg(), otherwise it will get disposed!
                        DisplayMsg.decodeMsg(ref rt, rcvNum, ref buf);

                        row = new object[] { portName, dtStr, tsStr, hex, ascii, rt.Rtf };
                    } break;
            } // switch

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

        private void DecodeMessagesToolStripMenuItem_Click(object sender, EventArgs e) {
            if (_frmDecode == null)
                _frmDecode = new FrmDecodeMessages(_frm);
            _frmDecode.Show();
            _frmDecode.Activate();
        }
    }
}
