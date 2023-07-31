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
            int subCmd = (num >= 7 && hasSubCmd(cmd)) ? data[6] : 0;
            int dataLen = (num > 5) ? (data[4] << 8) + data[5] : 0;

            int dlSw = (dataLen == 0) ? DLen0 : (dataLen == 1) ? DLen1 : (dataLen == 2) ? DLen2 : (dataLen == 3) ? DLen3 : (dataLen == 4) ? DLen4 : DLenX;

            if (num >= 6) {
                bErr |= checkFrame(dec, ref rtBox, num, ref data);
                if (bErr) { // if an error had been detected, then print all msg bytes in hex
                    string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                    appendTxt(ref rtBox, " No Frame Msg=", colorData);
                    appendTxt(ref rtBox, hex, colorErr);
                    return bErr;
                }
            }

            //-----------------------------------------------------------------------
            // bit16-17=version, bit12-15=DataLen, bit8-11=subCmd, bit0-7=cmd
            //-----------------------------------------------------------------------
            int sw = (ver << 16) + dlSw + (subCmd << 8) + cmd;
            switch (sw) {
                // Heartneat
                case Ver0 | DLen0 | 0x00:
                case Ver0 | DLen1 | 0x00:
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
                            if (dataLen == 1) {
                                int startState = data[6];
                                appendTxt(ref rtBox, (startState == 0) ? "MCU restarting" : "MCU running", colorData);
                            }
                        }
                    }
                    break;

                // [Query] product information
                case Ver0 | DLen0 | 0x01: {
                        appendTxt(ref rtBox, "Product", colorCmd);
                        appendTxt(ref rtBox, " Query", colorSubCmd);
                    }
                    break;

                case Ver0 | DLenX | 0x01: {
                        appendTxt(ref rtBox, "Product", colorCmd);
                        string prodTxt = Encoding.UTF8.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, " " + prodTxt, colorData);
                    }
                    break;

                case Ver0 | DLen0 | 0x02: // ACK Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x02: // Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        int netState = data[6];
                        decodeDictParam(ref rtBox, ref NetworkStates, colorInfo, netState, true);
                    }
                    break;

                case Ver0 | DLen0 | 0x03: // Reset Wi-Fi
                    {
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen0 | 0x04: // ACK Reset Wi-Fi and select configuration mode
                    {
                        appendTxt(ref rtBox, "Wi-Fi State", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x04: // Reset Wi-Fi and select configuration mode
                    {
                        appendTxt(ref rtBox, "Wi-Fi State", colorCmd);
                        int netCfg = data[6];
                        if (netCfg == 0) {
                            appendTxt(ref rtBox, " Enter smartconifg configuration mode", colorInfo);
                        } else if (netCfg == 1) {
                            appendTxt(ref rtBox, " Enter AP configuration mode", colorInfo);
                        } else {
                            appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLen0 | 0x05: // ACK Status Data
                    {
                        appendTxt(ref rtBox, "Status Data", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x05: // Report Status Data
                    {
                        appendTxt(ref rtBox, "Status Data", colorCmd);
                        int netCfg = data[6];
                        if (netCfg == 0) {
                            appendTxt(ref rtBox, " enter smartconifg configuration mode", colorInfo);
                        } else if (netCfg == 1) {
                            appendTxt(ref rtBox, " enter AP configuration mode", colorInfo);
                        } else {
                            appendTxt(ref rtBox, " Wrong NetCfg=" + netCfg, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLenX | 0x05: // Response Status Data
                    {
                        appendTxt(ref rtBox, "Status Data", colorCmd);
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
                    break;

                case Ver0 | DLen0 | 0x06: // Cmd Obtain Local Time 
                    {
                        appendTxt(ref rtBox, "Obtain Local Time", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLenX | 0x06: // Response Obtain Local Time 
                    {
                        appendTxt(ref rtBox, "Obtain Local Time", colorCmd);
                        if (dataLen == 8) {
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

                case Ver0 | DLen0 | 0x07: // Wi-Fi functional test
                    {
                        appendTxt(ref rtBox, "Wi-Fi Func Test", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | 0x07: // Reponse Wi-Fi functional test
                    {
                        appendTxt(ref rtBox, "Wi-Fi Func Test", colorCmd);
                        int obtainFlag = data[6];
                        decodeResponse(ref rtBox, obtainFlag, false, true);
                        if (obtainFlag == 0) {
                            int err = data[7];
                            appendTxt(ref rtBox, " error=", colorErr);
                            appendTxt(ref rtBox, (err == 0) ? "SSID is not found" : (err == 1) ? "No authorization key" : "Wrong err=" + err.ToString(), (err == 0) ? colorData : colorErr);
                        } else if (obtainFlag == 1) {
                            UInt64 signal = data[7];
                            decodeParam(ref rtBox, "Signal", signal);
                        }
                    }
                    break;

                case Ver0 | DLen1 | 0x08: // Report Data
                    {
                        appendTxt(ref rtBox, "Report Data", colorCmd);
                        int repStatus = data[6];
                        decodeDictParam(ref rtBox, ref ReportStates, colorParam, repStatus, true);
                    }
                    break;

                case Ver0 | DLenX | 0x08: // Report Data
                    {
                        appendTxt(ref rtBox, "Report Data", colorCmd);
                        if (dataLen >= 7) {
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
                        }
                    }
                    break;

                case Ver0 | DLenX | 0x09: // Send Module Command
                    {
                        appendTxt(ref rtBox, "Send Command", colorCmd);
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
                    break;

                case Ver3 | DLen0 | 0x09: // ACK Send Module Command
                    {
                        appendTxt(ref rtBox, "Send Command", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen0 | 0x0b: // Query the signal strength of the currently connected router
                        {
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | 0x0b: // Query the signal strength of the currently connected router
                        {
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
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
                    }
                    break;

                case Ver0 | DLen0 | 0x0a: // Cmd Wi-Fi firmware upgrade
                case Ver0 | DLen1 | 0x0a: // Response Wi-Fi firmware upgrade
                case Ver0 | DLen0 | 0x0c: // Cmd MCU firmware upgrading status
                case Ver0 | DLen1 | 0x0c: // Response MCU firmware upgrading status
                    {
                        appendTxt(ref rtBox, (cmd == 0x0a) ? "Wi-Fi Firmware Upgrade" : "MCU Upgrading Status", colorCmd);
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorSubCmd);
                        } else if (dataLen == 1) {
                            int fwUpdRet = data[6];
                            decodeDictParam(ref rtBox, ref FWUpdateReturns, colorParam, fwUpdRet, true);
                        }
                    }
                    break;

                case Ver0 | DLen0 | 0x0d: // ACK Number Firmware Bytes
                    {
                        appendTxt(ref rtBox, "Number Firmware Bytes", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen4 | 0x0d: // Number Firmware Bytes
                    {
                        appendTxt(ref rtBox, "Number Firmware Bytes", colorCmd);
                        int val = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    }
                    break;

                case Ver0 | DLen0 | 0x0e: // ACK Packet Transfer
                    {
                        appendTxt(ref rtBox, "Packet Transfer", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen4 | 0x0e: // Packet Transfer
                case Ver0 | DLenX | 0x0e: // Packet Transfer
                    {
                        appendTxt(ref rtBox, "Packet Transfer", colorCmd);
                        int offset = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                        decodeParam(ref rtBox, "Offset", (UInt64)offset, 8);

                        if (dataLen > 4) {
                            // data bytes
                            string hex = BitConverter.ToString(data, 10, dataLen - 4).Replace('-', ' ');
                            decodeParamStr(ref rtBox, "Data", hex);
                        }
                    }
                    break;

                case Ver3 | DLen2 | 0x0e: // Wi-Fi functional test cmd
                    {
                        appendTxt(ref rtBox, "Wi-Fi Func Test", colorCmd);
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
                    }
                    break;

                case Ver0 | DLen4 | 0x010: // Obtain DP cache command && its answer (which is tricky to distinguish)
                case Ver0 | DLenX | 0x010: // Obtain DP cache command && its answer (which is tricky to distinguish)
                    {
                        appendTxt(ref rtBox, "Obtain DP Cache", colorCmd);
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
                        appendTxt(ref rtBox, "Unknown DLen+Version+Cmd=0x" + sw.ToString("X6"), colorErr);
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
