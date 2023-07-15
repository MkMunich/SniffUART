﻿using System;
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
    // Version 0: https://developer.tuya.com/en/docs/iot/tuyacloudlowpoweruniversalserialaccessprotocol?id=K95afs9h4tjjh
    // Version 3: https://developer.tuya.com/en/docs/iot/tuya-cloud-universal-serial-port-access-protocol?id=K9hhi0xxtn9cb
    internal static class DecodeMsg {
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

        static Dictionary<int, string> ReportStates = new Dictionary<int, string>
        {
            { 0, "Reported successfully" },
            { 1, "The current record is reported successfully, and previously saved records need to be reported" },
            { 2, "Failed to report" },
        };

        static Dictionary<int, string> FWUpdateReturns = new Dictionary<int, string>
        {
            { 0, "(Start to detect firmware upgrading) Do not power off" },
            { 1, "(The latest firmware already) Power off" },
            { 2, "(Upgrading the firmware) Do not power off" },
            { 3, "(The firmware is upgraded successfully) Power off " },
            { 4, "(Failed to upgrade the firmware) Power off" },
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
        public static bool checkVersion(ref RichTextBox rtBox, int num, ref Byte[] data) {
            int ver = data[2];
            if (num >= 3 && !(ver == 0 || ver == 3)) { // version 0 or 3
                appendTxt(ref rtBox, " ");
                appendTxt(ref rtBox, "Unsupported Version=" + ver.ToString(), colorErr);
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
                appendTxt(ref rtBox, " Wrong Frame (num=" + num + " dataLen=" + dataLen + ")", colorErr);
                return true;
            }
            bErr |= checkHdr(ref rtBox, num, ref data);
            bErr |= checkVersion(ref rtBox, num, ref data);
            bErr |= checksum(ref rtBox, num, ref data);

            return bErr;
        }

        // returns true, if an error is detected
        public static bool decodeStatusDataUnits(ref RichTextBox rtBox, int num, ref int offset, ref Byte[] data) {
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

        // Function to decode a single message
        public static RichTextBox decodeMsg(ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
            int ver = (num == 1) ? 0 : (num >= 4) ? data[2] : 0;
            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;
            switch (cmd + (ver * 256)) {
                case 0x000: {
                        if (cmd == 0) {
                            appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        } else {
                            appendTxt(ref rtBox, "Version " + ver.ToString() + " not yet supported", colorErr);
                            bErr = true;
                        }
                    } break;
                case 0x001: { // [Query} product information
                        appendTxt(ref rtBox, "Product", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Query", colorData);
                        } else {
                            string prodTxt = ASCIIEncoding.ASCII.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, " " + prodTxt);
                        }
                    } break;
                case 0x002: { // Report the network status of the device
                        appendTxt(ref rtBox, "McuConf", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else {
                            int netState = data[6];
                            try {
                                string netTxt = NetworkStates[netState];
                                appendTxt(ref rtBox, " " + netTxt, colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong NetworkState=" + netState, colorErr);
                                bErr = true;
                            }
                        }
                    } break;
                case 0x003: { // Reset Wi-Fi
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x004: { // Reset Wi-Fi and select configuration mode
                        appendTxt(ref rtBox, "WifiState", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x005: { // Status Data
                        appendTxt(ref rtBox, "Status Data", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                                bErr |= decodeStatusDataUnits(ref rtBox, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                    } break;
                case 0x006:   // Obtain Local Time
                case 0x01c:   // Obtain Local Time
                case 0x31c: { // Obtain Local Time
                        appendTxt(ref rtBox, "Obtain Local Time", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x007:   // Wi-Fi functional test
                case 0x30e: { // Wi-Fi functional test cmd
                        appendTxt(ref rtBox, "Wi-Fi Func Test", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x008: { // Report Data
                        appendTxt(ref rtBox, "Report Data", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                                bErr |= decodeStatusDataUnits(ref rtBox, num, ref offset, ref data);
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
                    case 0x009: { // Send Module Command
                        appendTxt(ref rtBox, "Send Command", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else {
                            int offset = 6; // start index to read status of DP units
                            // decode all status of DP units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeStatusDataUnits(ref rtBox, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                    } break;
                    case 0x00b: { // Query the signal strength of the currently connected router
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x00a:   // Wi-Fi firmware upgrade
                case 0x00c: { // MCU firmware upgrading status
                        appendTxt(ref rtBox, (cmd == 0x0a)? "Wi-Fi Firmware Upgrade" : "MCU Upgrading Status", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x00d: { // Number Firmware Bytes
                        appendTxt(ref rtBox, "Number Firmware Bytes", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x00e: { // Packet Transfer
                        appendTxt(ref rtBox, "Packet Transfer", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                case 0x010: { // Obtain DP cache command && its answer (which is tricky to distinguish)
                        appendTxt(ref rtBox, "Obtain DP Cache", colorCmd);
                        bErr |= checkFrame(ref rtBox, num, ref data);
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
                                    bErr |= decodeStatusDataUnits(ref rtBox, num, ref offset, ref data);
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
                default: {
                        appendTxt(ref rtBox, "Unknown Version= " + ver.ToString() + " Cmd=" + cmd.ToString(), colorErr);
                        bErr = true;
                } break;
            } // switch

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                appendTxt(ref rtBox, " Msg=", colorData);
                appendTxt(ref rtBox, hex, colorErr);
            }

            return rtBox;
        }
    }

    internal class List<T1, T2> {
    }
}