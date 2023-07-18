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
    internal static class DecoderMcuSerPort {
        public static Dictionary<int, string> NetworkStates = new Dictionary<int, string>
        {
            { 0x00, "Pairing in EZ mode" },
            { 0x01, "Pairing in AP mode" },
            { 0x02, "The Wi-Fi network is set up, but the device is not connected to the router" },
            { 0x03, "The Wi-Fi network is set up, and the device is connected to the router" },
            { 0x04, "The device is connected to the cloud" },
            { 0x05, "Tuya’s network module is in low power mode" },
            { 0x06, "EZ mode and AP mode coexist" },
        };

        public static Dictionary<int, string> ResultTime = new Dictionary<int, string>
        {
            { 0x00, "Use Module time" },
            { 0x01, "Use local Time" },
            { 0x02, "Use GMT" },
        };

        public static Dictionary<int, string> PacketSizes = new Dictionary<int, string>
        {
            { 0x00, "256 bytes" },
            { 0x01, "512 bytes" },
            { 0x02, "1024 bytes" },
        };

        // refer to Tuya Serial Port Protocol
        // McuSerPort: https://developer.tuya.com/en/docs/iot/tuya-cloud-universal-serial-port-access-protocol?id=K9hhi0xxtn9cb#protocols
        //
        // Assuming, that a device type is either McuSerPort, McuLowPower or McuHomeKit. This means, that a device just
        // communicates using its decoder class.

        // Internal function to decode a single message
        public static bool decodingMsg(eDecoder dec, ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
            int ver = (num == 1) ? 0 : (num >= 4) ? data[2] : -1;
            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;

            // bit12-15 MCU protocol, bit8-11 version, bit0-7 cmd
            const int Ver0 = 0x0000;
            //const int Ver1 = 0x0100;
            //const int Ver2 = 0x0200;
            const int Ver3 = 0x0300;

            int sw = (ver << 8) + cmd;
            switch (sw) {
                // Heartneat
                case Ver0 | 0x00:
                case Ver3 | 0x00:
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
                case Ver0 | 0x01: // Query product information
                case Ver3 | 0x01: // Response product information
                    {
                        appendTxt(ref rtBox, "Product Information", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else {
                            string prodTxt = ASCIIEncoding.ASCII.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, " " + prodTxt, colorData);
                        }
                    }
                    break;

                case Ver0 | 0x02: // Query network status of the device
                case Ver3 | 0x02: // Response network status of the device
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
                            string str = " MCU uses network module";
                            Color col = colorData;
                            appendTxt(ref rtBox, str, col);
                        } else if (ver == 3 && (dataLen == 2 || dataLen == 3)) {
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
            
                case Ver0 | 0x03: // ACK Report the network status of the device
                case Ver3 | 0x03: // Report the network status of the device
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

                case Ver0 | 0x04: // Reset Wi-Fi and select configuration mode
                case Ver3 | 0x04: // ACK Reset Wi-Fi
                    {
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            if (ver == 0)
                                appendTxt(ref rtBox, " ACK", colorACK);
                            else
                                appendTxt(ref rtBox, " Cmd", colorCmd);
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
                    }
                    break;

                case Ver0 | 0x05: // ACK Reset Wi-Fi Pairing Mode
                case Ver3 | 0x05: // Set Wi-Fi Pairing Mode
                    {
                        appendTxt(ref rtBox, "Set Wi-Fi Pairing", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 1) {
                            int netCfg = data[6];
                            if (netCfg == 0) {
                                appendTxt(ref rtBox, " EZ mode", colorInfo);
                            } else if (netCfg == 1) {
                                appendTxt(ref rtBox, " AP mode", colorInfo);
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

                case Ver0 | 0x06: // Send Module Command
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

                case Ver3 | 0x07: // Report Data
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
                        } else if (dataLen >= 6) {
                            int offset = 6; // start index to read status of DP units
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

                case Ver0 | 0x08: // Query DP status
                    {
                        appendTxt(ref rtBox, "Query DP status", colorCmd);
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

                case Ver0 | 0x0a: // Start OTA update
                case Ver3 | 0x0a: // ACK Start OTA update
                    {
                        appendTxt(ref rtBox, "Start OTA update", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int size = data[6];
                            try {
                                string sizeTxt = PacketSizes[size];
                                appendTxt(ref rtBox, " " + sizeTxt, colorInfo);
                            } catch {
                                appendTxt(ref rtBox, " Wrong PacketSizes=" + size, colorErr);
                                bErr = true;
                            }
                        } else if (dataLen == 4) {
                            int val = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                            appendTxt(ref rtBox, " DataLen=", colorType);
                            appendTxt(ref rtBox, val.ToString(), colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x0b: // Transmit update package
                case Ver3 | 0x0b: // ACK Transmit update package
                    {
                        appendTxt(ref rtBox, "Transmit Package", colorCmd);
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

                case Ver3 | 0x0c: // Get system time in GMT
                    {
                        appendTxt(ref rtBox, "Get GMT Time", colorCmd);
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

                case Ver0 | 0x0e: // Response Test Wi-Fi functionality
                case Ver3 | 0x0e: // Test Wi-Fi functionality
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
                                appendTxt(ref rtBox, (err == 0) ? "SSID is not found" : (err == 0) ? "No authorization key" : "Wrong err=" + err.ToString(), (err == 0) ? colorData : colorErr);
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

                case Ver0 | 0x0f: // Response Get module’s memory
                case Ver3 | 0x0f: // Get module’s memory
                    {
                        appendTxt(ref rtBox, "Get Memory", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 4) {
                            int val = (data[9] << 24) + (data[8] << 16) + (data[7] << 8) + data[6];
                            appendTxt(ref rtBox, " DataLen=", colorType);
                            appendTxt(ref rtBox, val.ToString(), colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x1c: // System time
                    {
                        appendTxt(ref rtBox, "System Time", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
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

                case Ver3 | 0x21: // ACK Enable weather services
                case Ver0 | 0x21: // Enable weather services
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen >= 4) {
                            int sucFlag = data[6];
                            if (sucFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorInfo);
                            } else if (sucFlag == 1) {
                                appendTxt(ref rtBox, " successful", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Wrong SuccessFlag=" + sucFlag, colorErr);
                                bErr = true;
                            }
                            int offset = 7;
                            while (!bErr && offset < (num -1)) {
                                int l = data[offset];
                                int idx = offset + l + 3;
                                int tl = 0;
                                if (offset + l < (num - 1)) {
                                    string str = System.Text.Encoding.UTF8.GetString(data, offset + 1, l);
                                    appendTxt(ref rtBox, " Parameter=", colorType);
                                    appendTxt(ref rtBox, str, colorData);
                                    byte t = data[offset + l + 1];
                                    tl = data[offset + l + 2];
                                    if (t == 0) { // int
                                        appendTxt(ref rtBox, " Value=", colorType);
                                        switch (tl) {
                                            case 1: {
                                                    int val = data[idx++];
                                                    appendTxt(ref rtBox, val.ToString("X2"), colorData);
                                                }
                                                break;
                                            case 2: {
                                                    int val = data[idx++] << 8;
                                                    val += data[idx++];
                                                    appendTxt(ref rtBox, val.ToString("X4"), colorData);
                                                }
                                                break;
                                            case 4: {
                                                    int val = data[idx++] << 24;
                                                    val += data[idx++] << 16;
                                                    val += data[idx++] << 8;
                                                    val += data[idx++];
                                                    appendTxt(ref rtBox, val.ToString("X8"), colorData);
                                                }
                                                break;
                                            default: {
                                                    appendTxt(ref rtBox, " Wrong typeLen=" + tl.ToString(), colorErr);
                                                    bErr = true;
                                                }
                                                break;
                                        } // switch
                                    } else if (t == 1) { // string
                                        string parStr = System.Text.Encoding.UTF8.GetString(data, idx, tl);
                                        appendTxt(ref rtBox, " String=", colorType);
                                        appendTxt(ref rtBox, parStr, colorData);
                                        idx += l;
                                    } else {
                                        appendTxt(ref rtBox, " Wrong type=" + t, colorErr);
                                        bErr = true;
                                    }
                                }
                                offset = idx;
                            } // while
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x22: // Report Status
                case Ver3 | 0x22: // Report Status
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int repStatus = data[6];
                            if (repStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (repStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorData);
                            } else {
                                appendTxt(ref rtBox, " Wrong RepStatus=" + repStatus, colorErr);
                            }
                        } else if (dataLen >= 5) {
                            int offset = 6; // start index to read status of DP units
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

                case Ver0 | 0x23: // Response Report Status
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int repStatus = data[6];
                            if (repStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (repStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorData);
                            } else {
                                appendTxt(ref rtBox, " Wrong RepStatus=" + repStatus, colorErr);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x34: // Respone Send Status from MCU to module
                case Ver3 | 0x34: // Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 2) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " SubCmd=", colorInfo);
                            appendTxt(ref rtBox, subCmd.ToString("X2"), colorData);

                            int repStatus = data[7];
                            if (repStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (repStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorData);
                            } else if (repStatus == 2) {
                                appendTxt(ref rtBox, " Invalid Data", colorErr);
                            } else {
                                appendTxt(ref rtBox, " Wrong RepStatus=" + repStatus, colorErr);
                            }
                        } else if (dataLen >= 5) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " SubCmd=", colorInfo);
                            appendTxt(ref rtBox, subCmd.ToString("X2"), colorType);

                            int resTime = data[7];
                            try {
                                string resultStr = ResultTime[resTime];
                                appendTxt(ref rtBox, " " + resultStr, colorACK);
                            } catch {
                                appendTxt(ref rtBox, " Wrong ResultTime=" + resTime.ToString("X2"), colorErr);
                                bErr = true;
                            }

                            int year = data[8] + 2000;
                            int month = data[9];
                            int day = data[10];
                            int hour = data[11];
                            int minute = data[12];
                            int second = data[13];
                            try {
                                DateTime date = new DateTime(year, month, day, hour, minute, second);
                                appendTxt(ref rtBox, " Date=", colorType);
                                appendTxt(ref rtBox, date.ToString("yy-MM-dd HH:mm"), colorData);
                            } catch {
                                appendTxt(ref rtBox, " Wrong DateTime Parameter", colorErr);
                                bErr = true;
                            }

                            // decode all status of DP units
                            int offset = 14; // start index to read status of DP units
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

                case Ver0 | 0x37: // Notification of new feature setting ACK
                case Ver3 | 0x37: // Notification of new feature setting
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 2) {
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
