using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace SniffUART {
    internal static class DisplayMsg {
        static Dictionary<int, string> dataTypes = new Dictionary<int, string>
        {
            { 1, "Raw" },
            { 2, "Bool" },
            { 3, "Val" },
            { 4, "Str" },
            { 5, "Enum" },
            { 6, "Bitmap" },
        };

        static Dictionary<int, string> NetworkStates = new Dictionary<int, string>
        {
            { 0, "smartconfig configuration status" },
            { 1, "AP configuration status" },
            { 2, "Wi-Fi has been configured, but not connected to the router" },
            { 3, "Wi-Fi has been configured, and connected to the router" },
            { 4, "Wi-Fi has been connected to the router and the cloud" },
        };

        // definition of appearance
        private static Color colorErr = Color.Red; // error
        private static Color colorCmd = Color.Gray; // command

        public static void appendTxt(ref RichTextBox rtBox, string txt) {
            rtBox.AppendText(txt);
        }

        public static void appendTxt(ref RichTextBox rtBox, string txt, Color color) {
            rtBox.SelectionStart = rtBox.TextLength;
            rtBox.SelectionLength = 0;

            rtBox.SelectionColor = color;
            rtBox.AppendText(txt);
            rtBox.SelectionColor = rtBox.ForeColor;
        }

        // returns true, if an error is detected
        public static bool checksum(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num <= 3) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "No CS (len=" + num + ")", colorErr);
                return true;
            }
            int cs = 0;
            for (int i=0; i<num - 1; i++) {
                cs += data[i];
            } // for
            if ((cs & 255) != data[num - 1]) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong CS (cs=" + cs + ")", colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkVer(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num < 3 || data[2] != 0) { // version 0
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Version (ver=" + data[2].ToString() + ")", colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkHdr(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num <= 2 || data[0] != 0x55 || data[1] != 0xAA) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Hdr (len=" + num + " " + data[0].ToString("X2") + " " + data[1].ToString("X2") + ")", colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkFrame(ref RichTextBox rtBox, int num, ref Byte[] data) {
            bool bErr = false; // true, if error detected

            int dataLen = (data[4] << 8) + data[5];

            // check min length; length without data bytes; length with data bytes
            if (num < 7 || (dataLen == 0 && num != 7) || (dataLen > 0 && num != dataLen + 7)) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Frame (len=" + num + " dataLen=" + dataLen + ")", colorErr);
                return true;
            }
            bErr |= checkHdr(ref rtBox, num, ref data);
            bErr |= checkVer(ref rtBox, num, ref data);
            bErr |= checksum(ref rtBox, num, ref data);

            return bErr;
        }

        public static RichTextBox decodeMsg(int num, ref byte[] data) {
            bool bErr = false; // true, if error detected
            RichTextBox rtBox = new RichTextBox();

            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;
            switch (cmd) {
                case 0: {
                    appendTxt(ref rtBox, "Heartbeat", colorCmd);
                } break;
                case 1: { // [Query} product information
                    appendTxt(ref rtBox, "Product", colorCmd);
                    bErr |= checkFrame(ref rtBox, num, ref data);
                    if (bErr)
                        break;
                    int dataLen = (data[4] << 8) + data[5];
                    if (dataLen == 0) {
                        appendTxt(ref rtBox, " Query", colorCmd);
                    } else {
                        string prodTxt = ASCIIEncoding.ASCII.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, " " + prodTxt);
                    }
                } break;
                case 2: { // Report the network status of the device
                    appendTxt(ref rtBox, "McuConf", colorCmd);
                    bErr |= checkFrame(ref rtBox, num, ref data);
                    if (bErr)
                        break;
                    int dataLen = (data[4] << 8) + data[5];
                    if (dataLen == 0) {
                        appendTxt(ref rtBox, " Confirm", colorCmd);
                    } else {
                        int netState = data[7];
                        string netTxt = NetworkStates[netState];
                        appendTxt(ref rtBox, " " + netTxt);
                    }
                } break;
                case 3: {
                    appendTxt(ref rtBox, "WifiState", colorCmd);
                    bErr |= checkHdr(ref rtBox, num, ref data);
                    bErr |= checksum(ref rtBox, num, ref data);
                } break;
                case 7: {
                    appendTxt(ref rtBox, "State", colorCmd);
                    bErr |= checkHdr(ref rtBox, num, ref data);
                    bErr |= checksum(ref rtBox, num, ref data);
                } break;
                case 8: {
                    appendTxt(ref rtBox, "QueryInitStatus", colorCmd);
                    bErr |= checkHdr(ref rtBox, num, ref data);
                    bErr |= checksum(ref rtBox, num, ref data);
                } break;
                case 0x1c: {
                    appendTxt(ref rtBox, "Date", colorCmd);
                } break;
                default: {
                    appendTxt(ref rtBox, "Unknown (cmd=" + cmd.ToString() + ")", colorErr);
                    bErr = true;
                } break;
            } // switch

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = BitConverter.ToString(data, 0, num).Replace('-', ';');
                appendTxt(ref rtBox, " " + hex);
            }

            return rtBox;
        }
    }

    internal class List<T1, T2> {
    }
}
