using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SniffUART.FormMain;
using static System.Net.Mime.MediaTypeNames;

namespace SniffUART {
    public partial class FrmDecodeMessages : Form {
        FormMain _frmMain;
        public DateTime _dateStart;


        public FrmDecodeMessages(FormMain frmMaim) {
            _frmMain = frmMaim;
            _dateStart = DateTime.Now;

            InitializeComponent();
        }

        private void FrmDecodeMessages_FormClosing(object sender, FormClosingEventArgs e) {
            _frmMain._frmDecode = null;
        }

        private string devideMessages() {
            StringBuilder sb = new StringBuilder(1000);
            foreach (string line in tBoxMessages.Lines) {
                string sLine = line.Trim();
                if (sLine == "" || sLine.StartsWith("//") || sLine.StartsWith(";"))
                    continue;
                string sline = line.Trim().Replace(',', ' ');
                sline = sline.Replace(';', ' ');
                sline = sline.Replace("  ", " ");
                string lowLine = sLine.ToLower().Trim();
                int idx = 0;
                while (true) {
                    int nxtIdx = lowLine.IndexOf("55 aa ", idx + 1);
                    if (nxtIdx >= 0) {
                        sb.Append(lowLine.Substring(idx, nxtIdx - idx) + "\r\n");
                        idx = nxtIdx;
                    } else {
                        sb.Append(lowLine.Substring(idx));
                        break;
                    }
                } // while
            } // foreach

            return sb.ToString();
        }

        private void butDecode_Click(object sender, EventArgs e) {
            DateTime date = DateTime.Now;
            string dateStr = date.ToString("yy-MM-dd HH:mm:ss.ff");
            TimeSpan diff = date - _dateStart;
            string diffStr = diff.ToString(@"hh\:mm\:ss\.ff");

            //string msgs = devideMessages();

            foreach (string line in tBoxMessages.Lines) {
                string sLine = line.Trim();
                if (sLine == "" || sLine.StartsWith("//"))
                    continue;
                string sline = line.Trim().Replace(',', ' ');
                sline = sline.Replace(';', ' ');
                sline = sline.Replace("  ", " ");

                object[] data;
                if (sLine.IndexOf(' ') == -1) {
                    string res = String.Concat(sLine.Select((c, i) => i > 0 && (i % 2) == 1 ? c.ToString() + " " : c.ToString()));
                    sLine = res;
                }
                try {
                    // log Import
                    string[] hex = sLine.Trim().Split(' ');
                    byte[] buf = hex.Select(value => Convert.ToByte(value, 16)).ToArray();
                    data = new object[] { eLogType.eImport, "Decoder", dateStr, diffStr, buf.Length, buf };

                    _frmMain.AddData(data);
                } catch {
                    // no code
                }

                // update GUI
                _frmMain.Invalidate();
            } // foreach

            // pop result in front
            _frmMain.Focus();
        }

        private void butClear_Click(object sender, EventArgs e) {
            tBoxMessages.Text = "";
        }
    }
}
