using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Data.OleDb;

namespace SniffUART {
    // refer to Tuya Serial Port Protocol
    // McuSerPort: https://developer.tuya.com/en/docs/iot/tuya-cloud-universal-serial-port-access-protocol?id=K9hhi0xxtn9cb#protocols
    // McuLowPower: https://developer.tuya.com/en/docs/iot/tuyacloudlowpoweruniversalserialaccessprotocol?id=K95afs9h4tjjh
    // McuHomeKit: https://developer.tuya.com/en/docs/iot/wifi-module-mcu-development-overview-for-homekit?id=Kaa8fvusmgapc
    internal static class DecodeMsg {
        public enum eDecoder {
            eMcuSerPort = 0,
            eMcuLowPower,
            eMcuHomeKit,
        };

        private static Dictionary<int, string> dataTypes = new Dictionary<int, string>
        {
            { 0, "Raw" },
            { 1, "Bool" },
            { 2, "Value" },
            { 3, "String" },
            { 4, "Enum" },
            { 5, "Bitmap" },
        };

        private static Dictionary<int, string> NetworkStates = new Dictionary<int, string>
        {
            { 0x00, "smartconfig configuration status" },
            { 0x01, "AP configuration status" },
            { 0x02, "Wi-Fi has been configured, but not connected to the router" },
            { 0x03, "Wi-Fi has been configured, and connected to the router" },
            { 0x04, "Wi-Fi has been connected to the router and the cloud" },
        };

        private static Dictionary<int, string> ReportStates = new Dictionary<int, string>
        {
            { 0x00, "Reported successfully" },
            { 0x01, "The current record is reported successfully, and previously saved records need to be reported" },
            { 0x02, "Failed to report" },
        };

        private static Dictionary<int, string> FWUpdateReturns = new Dictionary<int, string>
        {
            { 0x00, "(Start to detect firmware upgrading) Do not power off" },
            { 0x01, "(The latest firmware already) Power off" },
            { 0x02, "(Upgrading the firmware) Do not power off" },
            { 0x03, "(The firmware is upgraded successfully) Power off " },
            { 0x04, "(Failed to upgrade the firmware) Power off" },
        };

        private static Dictionary<int, string> ResultFeatures = new Dictionary<int, string>
        {
            { 0x00, "Success" },
            { 0x01, "Invalid Data" },
            { 0x02, "Failure" },
        };

        // color definitions of appearance
        private static Color colorErr = Color.Red; // error
        private static Color colorCmd = Color.Gray; // command
        private static Color colorDP = Color.Plum; // DP unit number
        private static Color colorType = Color.Goldenrod; // DP type
        private static Color colorData = Color.Blue; // DP data
        private static Color colorInfo= Color.Orange; // information
        private static Color colorACK = Color.Green; // confirmation

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
        public static bool checkHdr(ref RichTextBox rtBox, int num, ref Byte[] data) {
            if (num <= 2 || data[0] != 0x55 || data[1] != 0xAA) {
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Wrong Hdr (num=" + num + " " + data[0].ToString("X2") + " " + data[1].ToString("X2") + ")", colorErr);
                return true;
            }
            return false;
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
                appendTxt(ref rtBox, "Wrong CS=" + (cs & 255).ToString("X2"), colorErr);
                return true;
            }
            return false;
        }

        // returns true, if an error is detected
        public static bool checkFrame(eDecoder decClass, ref RichTextBox rtBox, int num, ref Byte[] data) {
            bool bErr = false; // true, if error detected

            int dataLen = (data[4] << 8) + data[5];

            // check min length; length without data bytes; length with data bytes
            if (num < 7 || (dataLen == 0 && num != 7) || (dataLen > 0 && num != dataLen + 7)) {
                appendTxt(ref rtBox, " Wrong Frame (num=" + num + " dataLen=" + dataLen + ")", colorErr);
                return true;
            }
            bErr |= checkHdr(ref rtBox, num, ref data);
            bErr |= checksum(ref rtBox, num, ref data);

            return bErr;
        }

        // returns true, if an error is detected
        public static bool decodeStatusDataUnits(ref RichTextBox rtBox, eDecoder dec, int ver, int num, ref int offset, ref Byte[] data) {
            bool bErr = false; // true, if error detected

            if (offset + 5 > (num - 1)) { // testing the minimum # bytes of a DP unit part failed
                appendTxt(ref rtBox, " Wrong DataUnit (num=" + num + " offset=" + offset + ")", colorErr);
                return true;
            }

            int dpid = data[offset];
            appendTxt(ref rtBox, " DP=", colorDP);
            appendTxt(ref rtBox, dpid.ToString(), colorData);

            int type = data[offset + 1];
            try {
                string typeTxt = dataTypes[type];
                appendTxt(ref rtBox, " " + typeTxt, colorType);
            } catch {
                appendTxt(ref rtBox, " Wrong DPType=" + type, colorErr);
                return true;
            }

            int len = (data[offset + 2] << 8) + data[offset + 3];
            if (len == 0 || offset + 5 + len > num) { // testing the effective # bytes of a DP unit part failed
                appendTxt(ref rtBox, " Wrong DataUnitLength (num=" + num + " len=" + len + ")", colorErr);
                return true;
            }

            switch (type) {
                case 0: { // Raw
                        string hex = BitConverter.ToString(data, offset + 4, len).Replace('-', ' ');
                        appendTxt(ref rtBox, "=" + hex, colorData);
                    } break;
                case 1: { // Bool
                        int val = data[offset + 4];
                        if (val == 0) {
                            appendTxt(ref rtBox, "=False", colorData);
                        } else if (val == 1) {
                            appendTxt(ref rtBox, "=True", colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong val=" + val, colorErr);
                            bErr = true;
                        }
                    } break;
                case 2: { // Value
                        int val = (data[offset + 4] << 24) + (data[offset + 5] << 16) + (data[offset + 6] << 8) + data[offset + 7];
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    } break;
                case 3: { // String
                        string ascii = Encoding.ASCII.GetString(data, offset + 4, len);
                        //:-( ascii = Regex.Replace(ascii, @"[^\u0000-\u007F]+", "."); // replace all non printable char by '.'
                        appendTxt(ref rtBox, "=" + ascii, colorData);
                    } break;
                case 4: { // Enum
                        int val = data[offset + 4];
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    } break;
                case 5: { // Bitmap
                        if (len == 1) {
                            int val = data[offset + 4];
                            appendTxt(ref rtBox, "=" + val.ToString("X2"), colorData);
                        } else if (len == 2) {
                            int val = (data[offset + 4] << 6) + data[offset + 5];
                            appendTxt(ref rtBox, "=" + val.ToString("X4"), colorData);
                        } else if (len == 4) {
                            int val = (data[offset + 4] << 24) + (data[offset + 5] << 16) + (data[offset + 6] << 8) + data[offset + 7];
                            appendTxt(ref rtBox, "=" + val.ToString("X8"), colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong BitmapLen=" + len, colorErr);
                            bErr = true;
                        }
                    } break;
            } // switch

            // adjust offset: len bytes had been processed (min length of DP unit + # data bytes)
            offset += 4 + len;
            return bErr;
        }

        // Public function to decode a single Tuya message
        // Hints: Assuming, that Tuya devices are only using messages within one of the following decoders:
        // McuSerPort, McuLowPower or McuHomeKit (refer to urls above).
        // For this, a single Tuya device should not (??really??) use messages from different decoder class.
        // One decoder class is preselected in FormMain._mcuProtocol and will be used first. If decoding of
        // that decoder class fails, then this function will give the other decoder classes a chance.
        public static RichTextBox decodeMsg(ref RichTextBox rtBox, int num, ref byte[] data) {
            eDecoder decoderClass = (eDecoder) FormMain._mcuProtocol;
            bool fstTry = decodingMsg(decoderClass, ref rtBox, num, ref data);
            if (! fstTry) { return rtBox; };

            // save first output
            RichTextBox fstResult = new RichTextBox();
            fstResult.Rtf = rtBox.Rtf;

            // try the other decoder 'classes'
            if (decoderClass != eDecoder.eMcuSerPort) {
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuSerPort.ToString(), colorErr);
                bool nxtTry = decodingMsg(eDecoder.eMcuSerPort, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }
            if (decoderClass != eDecoder.eMcuLowPower) { // try McuLowPower
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuLowPower.ToString(), colorErr);
                bool nxtTry = decodingMsg(eDecoder.eMcuLowPower, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }
            if (decoderClass != eDecoder.eMcuHomeKit) { // try McuHomeKit
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuHomeKit.ToString(), colorErr);
                bool nxtTry = decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }

            rtBox.Rtf = fstResult.Rtf; // return first output
            return rtBox;
        }

        // Internal function to decode a single message
        private static bool decodingMsg(eDecoder dec, ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
            int ver = (num == 1) ? 0 : (num >= 4) ? data[2] : 0;
            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;

            // bit12-15 MCU protocol, bit8-11 version, bit0-7 cmd
            const int McuSerPort    = 0x0000;
            const int McuLowPower   = 0x1000;
            const int McuHomeKit    = 0x2000;
            const int Ver0 = 0x0000;
            const int Ver1 = 0x0100;
            const int Ver2 = 0x0200;
            const int Ver3 = 0x0300;

            int sw = ((int)dec << 12) + (ver << 8) + cmd;
            switch (sw) {
                // Heartneat
                case McuSerPort | Ver0 | 0x00:
                case McuSerPort | Ver3 | 0x00:
                case McuLowPower | Ver0 | 0x00:
                case McuLowPower | Ver3 | 0x00:
                case McuHomeKit | Ver0 | 0x00:
                case McuHomeKit | Ver3 | 0x00:
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
                            bErr |= checkFrame(dec, ref rtBox, num, ref data);
                            if (bErr)
                                break;
                            int dataLen = (data[4] << 8) + data[5];
                            if (dataLen == 0) { // okay
                            } else if (dataLen == 1) {
                                int startState = data[6];
                                appendTxt(ref rtBox, (startState == 0) ? "MCU restarting" : "MCU running", colorData);
                            } else {
                                appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            }
                        }
                    } break;

                // [Query] product information
                case McuSerPort | Ver3 | 0x01:
                case McuLowPower | Ver0 | 0x01:
                    {
                        appendTxt(ref rtBox, "Product", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Query", colorData);
                        } else {
                            string prodTxt = ASCIIEncoding.ASCII.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, " " + prodTxt, colorData);
                        }
                    } break;

                case McuSerPort | Ver0 | 0x02: // Query the network status of the device
                case McuSerPort | Ver3 | 0x02: // Report the network status of the device
                    {
                        if (ver == 0) {
                            appendTxt(ref rtBox, "Query Network Status", colorCmd);
                        } else if (ver == 3) {
                            appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        }
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            string str = " Cmd";
                            Color col = colorCmd;
                            if (ver == 0) {
                                str = " ACK";
                                col = colorACK;
                            } else if (ver == 3) {
                                if (dec == eDecoder.eMcuSerPort) {
                                    str = " ACK";
                                    col = colorACK;
                                }
                            } else {
                                str = " ???";
                                col = colorErr;
                            }
                            appendTxt(ref rtBox, str, col);
                        } else if (ver == 0 && dec == eDecoder.eMcuLowPower && dataLen == 1) {
                            int netState = data[6];
                            try {
                                string netTxt = NetworkStates[netState];
                                appendTxt(ref rtBox, " " + netTxt, colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong NetworkState=" + netState, colorErr);
                                bErr = true;
                            }
                        } else if (ver == 3 && dec == eDecoder.eMcuSerPort && (dataLen == 2 || dataLen == 3)) {
                            int pinWiFiStatus = data[6];
                            appendTxt(ref rtBox, " GPIO pins WiFiStatusLED=", colorType);
                            appendTxt(ref rtBox, pinWiFiStatus.ToString(), colorData);
                            int pinWiFiNetwork = data[7];
                            appendTxt(ref rtBox, " WiFiNetworkReset=", colorType);
                            appendTxt(ref rtBox, pinWiFiNetwork.ToString(), colorData);
                            if (dataLen == 3) {
                                int pinBluetoothStatus = data[8];
                                appendTxt(ref rtBox, " BluetoothStatusLED=", colorType);
                                appendTxt(ref rtBox, pinBluetoothStatus.ToString(), colorData);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                        }
                    }
                    break;

                case McuLowPower | Ver0 | 0x02: // Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 1) {
                            int netState = data[6];
                            try {
                                string netTxt = NetworkStates[netState];
                                appendTxt(ref rtBox, " " + netTxt, colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong NetworkState=" + netState, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                        }
                    } break;

                case McuLowPower | Ver0 | 0x03: // Reset Wi-Fi
                    {
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuLowPower | Ver0 | 0x04: // Reset Wi-Fi and select configuration mode
                    {
                        appendTxt(ref rtBox, "WifiState", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " enter smartconifg configuration mode", colorInfo);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " enter AP configuration mode", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuLowPower | Ver0 | 0x05: // Status Data
                    {
                        appendTxt(ref rtBox, "Status Data", colorCmd);
                        bErr |= checkFrame( dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " enter smartconifg configuration mode", colorInfo);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " enter AP configuration mode", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                                bErr = true;
                            }
                        } else {
                            int offset = 6; // start index to read DP units
                            // decode all DP units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                    } break;

                case McuLowPower | Ver0 | 0x06: // Obtain Local Time 
                case McuHomeKit | Ver0 | 0x1c:  // Obtain Local Time Response
                case McuHomeKit | Ver3 | 0x1c:  // Obtain Local Time Cmd
                    {
                        appendTxt(ref rtBox, "Obtain Local Time", colorCmd);
                        bErr |= checkFrame( dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 8) {
                            int obtainFlag = data[6];
                            if (obtainFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorInfo);
                            } else if (obtainFlag == 1) {
                                appendTxt(ref rtBox, " successful", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong ObtainFlag=" + obtainFlag, colorErr);
                                bErr = true;
                            }
                            int year = data[7] + 2000;
                            int month = data[8];
                            int day = data[9];
                            int hour = data[10];
                            int minute = data[11];
                            int second = data[12];
                            int week = data[13];
                            DateTime date = new DateTime(year, month, day, hour, minute, second);
                            appendTxt(ref rtBox, " Date=", colorType);
                            appendTxt(ref rtBox, date.ToString("yy-MM-dd HH:mm") + " Week="+ week, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuSerPort | Ver3 | 0x0e: // Wi-Fi functional test cmd
                case McuLowPower | Ver0 | 0x07: // Wi-Fi functional test
                    {
                        appendTxt(ref rtBox, "Wi-Fi Func Test", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 2) {
                            int obtainFlag = data[6];
                            if (obtainFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorInfo);

                                int err = data[7];
                                appendTxt(ref rtBox, " error=", colorErr);
                                appendTxt(ref rtBox, (err == 0) ? "SSID is not found" : "no authorization key", colorData);
                            } else if (obtainFlag == 1) {
                                appendTxt(ref rtBox, " Success", colorInfo);

                                int strengh = data[7];
                                appendTxt(ref rtBox, " Signal=", colorType);
                                appendTxt(ref rtBox, strengh.ToString(), colorData);
                            } else {
                                appendTxt(ref rtBox, " Wrong ObtainFlag=" + obtainFlag, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuLowPower | Ver0 | 0x08: // Report Data
                    {
                        appendTxt(ref rtBox, "Report Data", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int repStatus = data[6];
                            try {
                                string statTxt = ReportStates[repStatus];
                                appendTxt(ref rtBox, " " + statTxt, colorACK);
                            } catch {
                                appendTxt(ref rtBox, " Wrong ReportingStatus=" + repStatus, colorErr);
                                bErr = true;
                            }
                        } else if (dataLen >= 7) {
                            int timeFlag = data[6];
                            if (timeFlag == 0) {
                                appendTxt(ref rtBox, " Local time is invalid", colorInfo);
                            } else if (timeFlag == 1) {
                                appendTxt(ref rtBox, " Local time is valid", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong TimeFlag=" + timeFlag, colorErr);
                                bErr = true;
                            }
                            int year = data[7] + 2000;
                            int month = data[8];
                            int day = data[9];
                            int hour = data[10];
                            int minute = data[11];
                            int second = data[12];
                            DateTime date = new DateTime(year, month, day, hour, minute, second);
                            appendTxt(ref rtBox, " Date=", colorType);
                            appendTxt(ref rtBox, date.ToString("yy-MM-dd HH:mm"), colorData);

                            int offset = 13; // start index to read status of DP units
                            // decode all status of DP units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;
                    
                case McuLowPower | Ver0 | 0x09: // Send Module Command
                    {
                        appendTxt(ref rtBox, "Send Command", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else {
                            int offset = 6; // start index to read status of DP units
                            // decode all status of DP units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                    } break;

                case McuLowPower | Ver0 | 0x0b: // Query the signal strength of the currently connected router
                        {
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 2) {
                            int obtainFlag = data[6];
                            if (obtainFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorInfo);

                                int err = data[7];
                                appendTxt(ref rtBox, " error=", colorErr);
                                appendTxt(ref rtBox, (err == 0) ? "no router connection" : "Wrong err=" + err.ToString(), (err == 0) ? colorData : colorErr);
                            } else if (obtainFlag == 1) {
                                appendTxt(ref rtBox, " Success", colorInfo);

                                int strengh = data[7];
                                appendTxt(ref rtBox, " Signal=", colorType);
                                appendTxt(ref rtBox, strengh.ToString(), colorData);
                            } else {
                                appendTxt(ref rtBox, " Wrong ObtainFlag=" + obtainFlag, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;
                
                case McuLowPower | Ver0 | 0x0a: // Wi-Fi firmware upgrade
                case McuLowPower | Ver0 | 0x0c: // MCU firmware upgrading status
                    {
                        appendTxt(ref rtBox, (cmd == 0x0a)? "Wi-Fi Firmware Upgrade" : "MCU Upgrading Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 1) {
                            int fwUpdRet = data[6];
                            try {
                                string statTxt = FWUpdateReturns[fwUpdRet];
                                appendTxt(ref rtBox, " " + statTxt, colorACK);
                            } catch {
                                appendTxt(ref rtBox, " Wrong FWUpdateRet=" + fwUpdRet, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuLowPower | Ver0 | 0x0d: // Number Firmware Bytes
                    {
                        appendTxt(ref rtBox, "Number Firmware Bytes", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 4) {
                            int val = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                            appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuLowPower | Ver0 | 0x0e: // Packet Transfer
                    {
                        appendTxt(ref rtBox, "Packet Transfer", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen >= 4) {
                            int offset = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                            appendTxt(ref rtBox, " Offset=", colorType);
                            appendTxt(ref rtBox, offset.ToString("X8"), colorData);

                            // data bytes
                            string hex = BitConverter.ToString(data, 10, dataLen - 4).Replace('-', ' ');
                            appendTxt(ref rtBox, " " + hex, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;
                
                case McuLowPower | Ver0 | 0x010: // Obtain DP cache command && its answer (which is tricky to distinguish)
                    {
                        appendTxt(ref rtBox, "Obtain DP Cache", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen > 0) {
                            int dpLen = data[6];
                            if (dpLen + 1 == dataLen && 7 + dpLen == num - 1) { // request
                                if (dpLen == 0) {
                                    appendTxt(ref rtBox, " DPs=", colorDP);
                                    appendTxt(ref rtBox, "all", colorData);
                                } else {
                                    appendTxt(ref rtBox, " DPs=", colorDP);
                                    string dps = String.Concat(data.Select((c, i) => i > 6 && i <= (dpLen + 6) ? c.ToString("D") + "," : "")).Trim(',');
                                    appendTxt(ref rtBox, " " + dps, colorData);
                                }
                            } else { // answer
                                int result = data[6];
                                if (result == 0) {
                                    appendTxt(ref rtBox, " Reading DPs failed", colorErr);
                                } else if (result == 1) {
                                    // no output
                                } else {
                                    appendTxt(ref rtBox, " Wrong result=" + result.ToString(), colorErr);
                                    bErr = true;
                                }
                                dpLen = data[7];
                                int offset = 8; // start index to read status of DP units

                                // decode all status of DP units
                                while (bErr == false && offset < (num - 1)) {
                                    bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                                } // while
                                if (offset != (num - 1)) { // all eaten? => no
                                    appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                    bErr = true;
                                }
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    } break;

                case McuSerPort | Ver0 | 0x37: // Notification of new feature setting ACK
                case McuSerPort | Ver3 | 0x37: // Notification of new feature setting
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dec == eDecoder.eMcuSerPort && dataLen == 2) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " SubCmd=", colorInfo);
                            appendTxt(ref rtBox, subCmd.ToString("X2"), colorData);
                            int result = data[7];
                            try {
                                string resultStr = ResultFeatures[result];
                                appendTxt(ref rtBox, " " + resultStr, colorACK);
                            } catch {
                                appendTxt(ref rtBox, " Wrong Result=" + result.ToString("X2"), colorErr);
                                bErr = true;
                            }
                        } else if (dec == eDecoder.eMcuSerPort && ver == 3 && dataLen > 2) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " subCmd=", colorInfo);
                            appendTxt(ref rtBox, subCmd.ToString("X2"), colorData);
                            string prodTxt = ASCIIEncoding.ASCII.GetString(data, 7, dataLen - 1);
                            appendTxt(ref rtBox, " " + prodTxt, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                default: {
                        appendTxt(ref rtBox, "Unknown Msg= " + ver.ToString() + " Switch=0x" + sw.ToString("X4"), colorErr);
                        bErr = true;
                } break;
            } // switch

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                appendTxt(ref rtBox, " Msg=", colorData);
                appendTxt(ref rtBox, hex, colorErr);
            }

            return bErr;
        }
    }

    internal class List<T1, T2> {
    }
}
