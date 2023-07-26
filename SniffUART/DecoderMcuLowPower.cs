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

using static SniffUART.DecodeMsg;

namespace SniffUART {
    internal static class DecoderMcuLowPower {
        public static Dictionary<int, string> NetworkStates = new Dictionary<int, string>
        {
            { 0x00, "smartconfig configuration status" },
            { 0x01, "AP configuration status" },
            { 0x02, "Wi-Fi has been configured, but not connected to the router" },
            { 0x03, "Wi-Fi has been configured, and connected to the router" },
            { 0x04, "Wi-Fi has been connected to the router and the cloud" },
        };

        // refer to Tuya Serial Port Protocol
        // McuLowPower: https://developer.tuya.com/en/docs/iot/tuyacloudlowpoweruniversalserialaccessprotocol?id=K95afs9h4tjjh
        //
        // Assuming, that a device type is either McuSerPort, McuLowPower or McuHomeKit. This means, that a device just
        // communicates using its decoder class.

        // Internal function to decode a single message
        public static bool decodingMsg(eDecoder dec, ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
            int ver = (num == 1) ? 0 : (num >= 4) ? data[2] : -1;
            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;

            // bit8-11 version, bit0-7 cmd
            const int Ver0 = 0x0000;
            //const int Ver1 = 0x0100;
            //const int Ver2 = 0x0200;
            const int Ver3 = 0x0300;

            int sw = (ver << 8) + cmd;
            switch (sw) {
                // Heartneat
                case Ver0 | 0x00:
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
                    }
                    break;

                // [Query] product information
                case Ver0 | 0x01: {
                        appendTxt(ref rtBox, "Product", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Query", colorData);
                        } else {
                            string prodTxt = Encoding.UTF8.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, " " + prodTxt, colorData);
                        }
                    }
                    break;

                case Ver0 | 0x02: // Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
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
                    }
                    break;

                case Ver0 | 0x03: // Reset Wi-Fi
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
                    }
                    break;

                case Ver0 | 0x04: // Reset Wi-Fi and select configuration mode
                    {
                        appendTxt(ref rtBox, "Wi-Fi State", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " Enter smartconifg configuration mode", colorInfo);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " Enter AP configuration mode", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x05: // Status Data
                    {
                        appendTxt(ref rtBox, "Status Data", colorCmd);
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
                    }
                    break;

                case Ver0 | 0x06: // Obtain Local Time 
                    {
                        appendTxt(ref rtBox, "Obtain Local Time", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
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
                            try {
                                DateTime date = new DateTime(year, month, day, hour, minute, second);
                                appendTxt(ref rtBox, " Date=", colorType);
                                appendTxt(ref rtBox, date.ToString("yy-MM-dd HH:mm"), colorData);
                                appendTxt(ref rtBox, " Week=", colorType);
                                appendTxt(ref rtBox, week.ToString(), colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong DateTime Parameter", colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x07: // Wi-Fi functional test
                case Ver3 | 0x0e: // Wi-Fi functional test cmd
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
                                appendTxt(ref rtBox, " Success", colorACK);

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
                    }
                    break;

                case Ver0 | 0x08: // Report Data
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
                            try {
                                DateTime date = new DateTime(year, month, day, hour, minute, second);
                                appendTxt(ref rtBox, " Date=", colorType);
                                appendTxt(ref rtBox, date.ToString("yy-MM-dd HH:mm"), colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong DateTime Parameter", colorErr);
                                bErr = true;
                            }

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
                    }
                    break;

                case Ver0 | 0x09: // Send Module Command
                case Ver3 | 0x09: // Send Module Command
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
                    }
                    break;

                case Ver0 | 0x0b: // Query the signal strength of the currently connected router
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
                                appendTxt(ref rtBox, " Success", colorACK);

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
                    }
                    break;

                case Ver0 | 0x0a: // Wi-Fi firmware upgrade
                case Ver0 | 0x0c: // MCU firmware upgrading status
                    {
                        appendTxt(ref rtBox, (cmd == 0x0a) ? "Wi-Fi Firmware Upgrade" : "MCU Upgrading Status", colorCmd);
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
                    }
                    break;

                case Ver0 | 0x0d: // Number Firmware Bytes
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
                    }
                    break;

                case Ver0 | 0x0e: // Packet Transfer
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
                            appendTxt(ref rtBox, " Data=", colorType);
                            string hex = BitConverter.ToString(data, 10, dataLen - 4).Replace('-', ' ');
                            appendTxt(ref rtBox, " " + hex, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x010: // Obtain DP cache command && its answer (which is tricky to distinguish)
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
                    }
                    break;

                default: {
                        appendTxt(ref rtBox, "Unknown Version+Cmd=0x" + sw.ToString("X4"), colorErr);
                        bErr = true;
                    }
                    break;
            } // switch

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                appendTxt(ref rtBox, " Msg=", colorData);
                appendTxt(ref rtBox, hex, colorErr);
            }

            return bErr;
        }
    }
}
