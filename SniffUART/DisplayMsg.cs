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
            { 0, "Raw" },
            { 1, "Bool" },
            { 2, "Value" },
            { 3, "String" },
            { 4, "Enum" },
            { 5, "Bitmap" },
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
        private static Color colorData = Color.Blue; // DP data

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
                appendTxt(ref rtBox, "No CS (num=" + num + ")", colorErr);
                return true;
            }
            int cs = 0;
            for (int i=0; i<num - 1; i++) {
                cs += data[i];
            } // for
            if ((cs & 255) != data[num - 1]) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong CS=" + cs, colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkVer(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num < 3 || data[2] != 0) { // version 0
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Version=" + data[2].ToString(), colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkHdr(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num <= 2 || data[0] != 0x55 || data[1] != 0xAA) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Hdr (num=" + num + " " + data[0].ToString("X2") + " " + data[1].ToString("X2") + ")", colorErr);
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
                appendTxt(ref rtBox, "Wrong Frame (num=" + num + " dataLen=" + dataLen + ")", colorErr);
                return true;
            }
            bErr |= checkHdr(ref rtBox, num, ref data);
            bErr |= checkVer(ref rtBox, num, ref data);
            bErr |= checksum(ref rtBox, num, ref data);

            return bErr;
        }

        // returns true, if an error is detected
        public static bool getStatusDataUnit(ref RichTextBox rtBox, int num, int offset, ref Byte[] data) {
            bool bErr = false; // true, if error detected

            if (offset + 6 > num) {
                appendTxt(ref rtBox, "Wrong DataUnit (num=" + num + " offset=" + offset + ")", colorErr);
                return true;
            }

            int dpid = data[offset];
            appendTxt(ref rtBox, " DP=" + dpid);

            int type = data[offset + 1];
            try {
                string typeTxt = dataTypes[type];
                appendTxt(ref rtBox, " " + typeTxt);
            } catch {
                appendTxt(ref rtBox, " Wrong DPType=" + type, colorErr);
                return true;
            }

            int len = (data[offset + 2] << 8) + data[offset + 3];
            if (len == 0 || offset + 5 + len > num) {
                appendTxt(ref rtBox, " Wrong DataUnitLength (num=" + num + " len=" + len + ")", colorErr);
                return true;
            }

            switch (type) {
                case 0: { // Raw
                        string hex = BitConverter.ToString(data, offset + 4, len).Replace('-', ' ');
                        appendTxt(ref rtBox, " Raw=" + hex, colorData);
                    } break;
                case 1: { // Bool
                        int val = data[offset + 4];
                        if (val == 0) {
                            appendTxt(ref rtBox, " False", colorData);
                        } else if (val == 1) {
                            appendTxt(ref rtBox, " True", colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong Bool=" + val, colorErr);
                            bErr = true;
                        }
                    } break;
                case 2: { // Value
                        int val = (data[offset + 4] << 24) + (data[offset + 5] << 16) + (data[offset + 6] << 8) + data[offset + 7];
                        appendTxt(ref rtBox, " Value=" + val, colorData);
                    } break;
                case 3: { // String
                        string ascii = Encoding.ASCII.GetString(data, offset + 4, len);
                        //:-( ascii = Regex.Replace(ascii, @"[^\u0000-\u007F]+", "."); // replace all non printable char by '.'
                        appendTxt(ref rtBox, " String=" + ascii, colorData);
                    } break;
                case 4: { // Enum
                        int val = data[offset + 4];
                        appendTxt(ref rtBox, " Enum=" + val, colorData);
                    } break;
                case 5: { // Bitmap
                        if (len == 1) {
                            int val = data[offset + 4];
                            appendTxt(ref rtBox, " Bitmap=" + val.ToString("2X"), colorData);
                        } else if (len == 2) {
                            int val = (data[offset + 4] << 6) + data[offset + 5];
                            appendTxt(ref rtBox, " Bitmap=" + val.ToString("4X"), colorData);
                        } else if (len == 4) {
                            int val = (data[offset + 4] << 24) + (data[offset + 5] << 16) + (data[offset + 6] << 8) + data[offset + 7];
                            appendTxt(ref rtBox, " Bitmap=" + val.ToString("8X"), colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong BitmapLen=" + len, colorErr);
                            bErr = true;
                        }
                    } break;
            } // switch

            return bErr;
        }

        // Function to decode a single message
        public static RichTextBox decodeMsg(ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
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
                            int netState = data[6];
                            try {
                                string netTxt = NetworkStates[netState];
                                appendTxt(ref rtBox, " " + netTxt);
                            } catch {
                                appendTxt(ref rtBox, " Wrong NetworkState=" + netState, colorErr);
                                bErr = true;
                            }
                        }
                    } break;
                case 3: { // Reset Wi-Fi
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " cmd", colorCmd);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;
                case 4: { // Reset Wi-Fi and select configuration mode
                        appendTxt(ref rtBox, "WifiState", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Confirm", colorCmd);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " enter smartconifg configuration mode", colorCmd);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " enter AP configuration mode", colorCmd);
                            } else {
                                appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;
                case 5: { // Status Data
                        appendTxt(ref rtBox, "Status Data", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Confirm", colorCmd);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " enter smartconifg configuration mode", colorCmd);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " enter AP configuration mode", colorCmd);
                            } else {
                                appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                                bErr = true;
                            }
                        } else {
                            bErr |= getStatusDataUnit(ref rtBox, num, 6, ref data);
                        }
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
                        appendTxt(ref rtBox, "Unknown Cmd=" + cmd.ToString(), colorErr);
                        bErr = true;
                } break;
            } // switch

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = BitConverter.ToString(data, 0, num).Replace('-', ' ');
                appendTxt(ref rtBox, " " + hex);
            }

            return rtBox;
        }
    }

    internal class List<T1, T2> {
    }
}
