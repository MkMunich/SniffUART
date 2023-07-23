using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using RichTextBox = System.Windows.Forms.RichTextBox;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Data.OleDb;

using static SniffUART.DecodeMsg;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Controls;
using System.Xml.Linq;
using System.IO.Ports;

namespace SniffUART {
    internal static class DecoderMcuSerPort {
        public static Dictionary<int, string> NetworkStates = new Dictionary<int, string>
        {
            { -1, "NetworkState" },
            { 0x00, "Pairing in EZ mode" },
            { 0x01, "Pairing in AP mode" },
            { 0x02, "The Wi-Fi network is set up, but the device is not connected to the router" },
            { 0x03, "The Wi-Fi network is set up, and the device is connected to the router" },
            { 0x04, "The device is connected to the cloud" },
            { 0x05, "Tuya’s network module is in low power mode" },
            { 0x06, "EZ mode and AP mode coexist" },
        };

        public static Dictionary<int, string> ResultTimes = new Dictionary<int, string>
        {
            { -1, "ResultTime" },
            { 0x00, "Use Module time" },
            { 0x01, "Use local Time" },
            { 0x02, "Use GMT" },
        };

        public static Dictionary<int, string> PacketSizes = new Dictionary<int, string>
        {
            { -1, "PacketSize" },
            { 0x00, "256 bytes" },
            { 0x01, "512 bytes" },
            { 0x02, "1024 bytes" },
            { 0x03, "2048 bytes" },
            { 0x04, "3072 bytes" },
            { 0x05, "4096 bytes" },
            { 0x06, "5120 bytes" },
            { 0x07, "10240 bytes" },
        };

        public static Dictionary<int, string> FileTransferStatus = new Dictionary<int, string>
        {
            { -1, "FileTransferStatus" },
            { 0x00, "No file transfer task" },
            { 0x01, "File transfer is starting" },
            { 0x02, "File transfer is in progress" },
            { 0x03, "File transfer/download is completed" },
            { 0x04, "File upload to the server succeeded" },
            { 0x05, "File transfer with the MCU times out" },
            { 0x06, "Failed to get the URL for file upload" },
            { 0x07, "Failed to upload the file to the server" },
            { 0x08, "Failed to get the file from the server" },
            { 0x09, "The MCU fails to respond to file transfer" },
        };

        public static Dictionary<int, string> ResultsPairing = new Dictionary<int, string>
        {
            { -1, "PairingResult" },
            { 0x00, "Data is received" },
            { 0x01, "The module is not waiting for pairing" },
            { 0x02, "JSON data is invalid" },
            { 0x03, "Other errors occur" },
        };

        public static Dictionary<int, string> MapDataResponses = new Dictionary<int, string>
        {
            { -1, "MapDataResponse" },
            { 0x00, "Success" },
            { 0x01, "Streaming service is not enabled" },
            { 0x02, "Failed to connect to the streaming server" },
            { 0x03, "Data transmission times out" },
            { 0x04, "Data length error" },
        };

        public static Dictionary<int, string> IRStatus = new Dictionary<int, string>
        {
            { -1, "IRStatus" },
            { 0x00, "IR code is being sent" },
            { 0x01, "IR code is sent" },
            { 0x02, "IR learning is in progress" },
            { 0x03, "IR learning is completed" },
        };

        public static Dictionary<int, string> MapMethods = new Dictionary<int, string>
        {
            { -1, "MapMethod" },
            { 0x00, "The map data is accumulated" },
            { 0x01, "The map data is cleared" },
        };

        public static Dictionary<int, string> RFTypes = new Dictionary<int, string>
        {
            { -1, "RFType" },
            { 0x00, "Send code library" },
            { 0x01, "Send learning code" },
        };


        public static Dictionary<int, string> LEDActivities = new Dictionary<int, string>
        {
            { -1, "LEDActivity" },
            { 0x00, "Blinking every 250 ms" },
            { 0x01, "Blinking every 1500 ms" },
            { 0x02, "steady off" },
            { 0x03, "steady on" },
            { 0x06, "steady off" },
        };

        public static Dictionary<int, string> Frequencies = new Dictionary<int, string>
        {
            { -1, "Frequency" },
            { 0x00, "315 MHz" },
            { 0x01, "433.92 MHz" },
        };

        public static Dictionary<int, string> InterruptStates = new Dictionary<int, string>
        {
            { -1, "FileDownloadException" },
            { 0x00, "All transfer tasks are terminated" },
            { 0x01, "Current transfer task is terminated" },
            { 0x02, "Get transfer status" },
        };

        public static Dictionary<int, string> FileDownloadExceptions = new Dictionary<int, string>
        {
            { -1, "InterruptState" },
            { 0x00, "Device is shut down" },
            { 0x01, "File transfer times out" },
            { 0x02, "Battery level is low" },
            { 0x03, "Device is overheating" },
            { 0x04, "File is large" },
            { 0x05, "Memory is not enough" },
            { 0x06, "Operation anomaly" },
        };

        private static bool decodeDictParam(ref RichTextBox rtBox, ref Dictionary<int, string> dict, Color col, int param, bool bHex) {
            try {
                string intTxt = dict[param];
                appendTxt(ref rtBox, " " + intTxt, col);
            } catch {
                string name = dict[-1];
                if (bHex) {
                    appendTxt(ref rtBox, " Wrong " + name + "=0x" + param.ToString("X2"), colorErr);
                } else {
                    appendTxt(ref rtBox, " Wrong " + name + "=" + param.ToString(), colorErr);
                }
                return true;
            }
            return false;
        }

        private static void decodeParamStr(ref RichTextBox rtBox, string name, string param) {
            appendTxt(ref rtBox, " " + name + "=", colorParam);
            appendTxt(ref rtBox, param, colorData);
        }

        private static void decodeParam(ref RichTextBox rtBox, string name, int param, int iHex = 0) {
            appendTxt(ref rtBox, " " + name + "=" + ((iHex > 0) ? "0x" : ""), colorParam);
            switch (iHex) {
                case 2:
                    appendTxt(ref rtBox, param.ToString("X2"), colorData);
                break;
                case 4:
                    appendTxt(ref rtBox, param.ToString("X4"), colorData);
                break;
                case 8:
                    appendTxt(ref rtBox, param.ToString("X8"), colorData);
                break;
                default:
                    appendTxt(ref rtBox, param.ToString(), colorData);
                break;
            } // switch
        }

        private static void decodeAbv(ref RichTextBox rtBox, int abv) {
            appendTxt(ref rtBox, " avb=(", colorParam);
            appendTxt(ref rtBox, "Combo module:", colorDP);
            appendTxt(ref rtBox, (0x01 == (abv & 0x01)) ? "enabled" : "disabled", colorData);
            appendTxt(ref rtBox, ", ", colorParam);
            appendTxt(ref rtBox, "RF remote control:", colorDP);
            appendTxt(ref rtBox, (0x02 == (abv & 0x02)) ? "enabled" : "disabled", colorData);
            appendTxt(ref rtBox, ", ", colorParam);
            appendTxt(ref rtBox, "Bluetooth remote control:", colorDP);
            appendTxt(ref rtBox, (0x04 == (abv & 0x04)) ? "enabled" : "disabled", colorData);
            appendTxt(ref rtBox, ", ", colorParam);
            appendTxt(ref rtBox, "Status query:", colorDP);
            appendTxt(ref rtBox, (0x08 == (abv & 0x08)) ? "enabled" : "disabled", colorData);
            appendTxt(ref rtBox, ")", colorParam);
        }

        // refer to Tuya Serial Port Protocol
        // McuSerPort: https://developer.tuya.com/en/docs/iot/tuya-cloud-universal-serial-port-access-protocol?id=K9hhi0xxtn9cb#protocols
        //             https://developer.tuya.com/en/docs/iot/weather-function-description?id=Ka6dcs2cw4avp
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
                            decodeParam(ref rtBox, "GPIO pins WiFiStatusLED", pinWiFiStatus);
                            int pinWiFiNetwork = data[7];
                            decodeParam(ref rtBox, "WiFiNetworkReset", pinWiFiNetwork);
                            if (dataLen == 3) {
                                int pinBluetoothStatus = data[8];
                                decodeParam(ref rtBox, "BluetoothStatusLED", pinBluetoothStatus);
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
                            bErr |= decodeDictParam(ref rtBox, ref NetworkStates, colorData, netState, false);
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
                            bErr |= decodeDictParam(ref rtBox, ref ReportStates, colorData, repStatus, false);
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
                            int packSize = data[6];
                            bErr |= decodeDictParam(ref rtBox, ref PacketSizes, colorData, packSize, false);
                        } else if (dataLen == 4) {
                            int val = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                            decodeParam(ref rtBox, "DataLen", val);
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
                            appendTxt(ref rtBox, " Offset=0x", colorType);
                            appendTxt(ref rtBox, offset.ToString("X8"), colorData);

                            // data bytes
                            string hex = BitConverter.ToString(data, 10, dataLen - 4).Replace('-', ' ');
                            decodeParamStr(ref rtBox, "Data", hex);
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

                                int signal = data[7];
                                decodeParam(ref rtBox, "Signal", signal);
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
                            decodeParam(ref rtBox, "DataLen", val);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x1c: // System time
                case Ver3 | 0x1c: // System time
                    {
                        appendTxt(ref rtBox, "System Time", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 8) {
                            int obtainFlag = data[6];
                            if (obtainFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorErr);
                            } else if (obtainFlag == 1) {
                                appendTxt(ref rtBox, " successful", colorACK);
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
                                decodeParamStr(ref rtBox, "Date", date.ToString("yy-MM-dd HH:mm"));
                                decodeParam(ref rtBox, "Week", week);
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

                case Ver0 | 0x20: // ACK Weather Service specification
                case Ver3 | 0x20: // Weather Service specification
                case Ver3 | 0x21: // ACK Enable weather services
                case Ver0 | 0x21: // Enable weather services
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            if (cmd == 0x20) {
                                appendTxt(ref rtBox, " Cmd", colorCmd);
                            } else if (cmd == 0x21) {
                                appendTxt(ref rtBox, " ACK", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Wrong msg", colorErr);
                            }
                        } else if (dataLen == 2 && cmd == 0x20) { // ACK
                            int obtainFlag = data[6];
                            if (obtainFlag == 0) {
                                appendTxt(ref rtBox, " failed", colorErr);
                                int sucFlag = data[7];
                                if (sucFlag == 1) {
                                    appendTxt(ref rtBox, " Invalid data format" + sucFlag, colorErr);
                                } else if (sucFlag == 2) {
                                    appendTxt(ref rtBox, " Exception error" + sucFlag, colorErr);
                                } else {
                                    appendTxt(ref rtBox, " Wrong SuccessFlag=" + sucFlag, colorErr);
                                }
                            } else if (obtainFlag == 1) {
                                appendTxt(ref rtBox, " successful", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Wrong ObtainFlag=" + obtainFlag, colorErr);
                                bErr = true;
                            }
                        } else if (dataLen >= 4) {
                            int offset = 7;
                            if (cmd == 0x21) {
                                int sucFlag = data[6];
                                if (sucFlag == 0) {
                                    appendTxt(ref rtBox, " failed", colorErr);
                                } else if (sucFlag == 1) {
                                    appendTxt(ref rtBox, " successful", colorACK);
                                } else {
                                    appendTxt(ref rtBox, " Wrong SuccessFlag=" + sucFlag, colorErr);
                                    bErr = true;
                                }
                            } else {
                                offset = 6;
                            }
                            while (!bErr && offset < (num -1)) {
                                int l = data[offset];
                                int idx = offset + l + 1;
                                int tl = 0;
                                if (offset + l < (num - 1)) {
                                    string str = System.Text.Encoding.UTF8.GetString(data, offset + 1, l);
                                    decodeParamStr(ref rtBox, "Parameter", str);
                                    if (cmd == 0x21) {
                                        idx += 2;
                                        byte t = data[offset + l + 1];
                                        tl = data[offset + l + 2];
                                        if (t == 0) { // int
                                            switch (tl) {
                                                case 1: {
                                                        int val = data[idx++];
                                                        decodeParam(ref rtBox, "Value", val, 2);
                                                    }
                                                    break;
                                                case 2: {
                                                        int val = (data[idx++] << 8) + data[idx++];
                                                        decodeParam(ref rtBox, "Value", val, 4);
                                                    }
                                                    break;
                                                case 4: {
                                                        int val = (data[idx++] << 24) + (data[idx++] << 16) + (data[idx++] << 8) + data[idx++];
                                                        decodeParam(ref rtBox, "Value", val, 8);
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
                                            decodeParamStr(ref rtBox, "String", parStr);
                                            idx += l;
                                        } else {
                                            appendTxt(ref rtBox, " Wrong type=" + t, colorErr);
                                            bErr = true;
                                        }
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
                            int respStatus = data[6];
                            if (respStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (respStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
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
                            int respStatus = data[6];
                            if (respStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (respStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x24: // Get Wi-Fi signal strength
                case Ver3 | 0x24: // Get Wi-Fi signal strength
                    {
                        appendTxt(ref rtBox, "Get Wi-Fi signal strength", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 1) {
                            int signal = data[6];
                            decodeParam(ref rtBox, "Signal", signal);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x25: // ACK Disable heartbeats
                case Ver3 | 0x25: // Cmd Disable heartbeats
                    {
                        appendTxt(ref rtBox, "Disable heartbeats", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            if (ver == 0) {
                                appendTxt(ref rtBox, " ACK", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Cmd", colorCmd);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x28: // Response Map streaming for robot vacuum
                case Ver3 | 0x28: // Map streaming for robot vacuum
                    {
                        appendTxt(ref rtBox, "Map data streaming for robot vacuum", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int netDataResp = data[6];
                            bErr |= decodeDictParam(ref rtBox, ref MapDataResponses, colorData, netDataResp, true);
                        } else if (dataLen >= 6) {
                            int mapId = (data[6] << 8) + data[7];
                            appendTxt(ref rtBox, " Id=0x", colorDP);
                            appendTxt(ref rtBox, mapId.ToString("X4"), colorData);
                            int offset = 8;
                            while (!bErr && offset < (num - 4)) {
                                int dataOffset = (data[offset] << 24) + (data[offset + 1] << 16) + (data[offset + 2] << 8) + data[offset + 3];
                                appendTxt(ref rtBox, " 0x" + dataOffset.ToString("X8"), colorData);
                                offset += 4;
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong Map data offset=" + offset, colorErr);
                                bErr = true;
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2a: // Response Pairing via serial port
                case Ver3 | 0x2a: // Pairing via serial port
                    {
                        appendTxt(ref rtBox, "Pairing via serial port", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int resPairing = data[6];
                            bErr |= decodeDictParam(ref rtBox, ref ResultsPairing, colorData, resPairing, true);
                        } else if (dataLen > 0) {
                            string str = System.Text.Encoding.UTF8.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, str, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2b: // Response Get the current network status
                case Ver3 | 0x2b: // Get the current network status
                    {
                        appendTxt(ref rtBox, "Network Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 1) {
                            int netState = data[6];
                            bErr |= decodeDictParam(ref rtBox, ref NetworkStates, colorData, netState, true);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2c: // Response Test Wi-Fi functionality (connection)
                case Ver3 | 0x2c: // Test Wi-Fi functionality (connection)
                    {
                        appendTxt(ref rtBox, "Test Wi-Fi", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int respStatus = data[6];
                            if (respStatus == 0) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else if (respStatus == 1) {
                                appendTxt(ref rtBox, " Success", colorACK);
                            } else {
                                appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                            }
                        } else if (dataLen > 1) {
                            string str = System.Text.Encoding.UTF8.GetString(data, 6, dataLen);
                            appendTxt(ref rtBox, str, colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2d: // Get module’s MAC address
                case Ver3 | 0x2d: // Get module’s MAC address
                    {
                        appendTxt(ref rtBox, "Get MAC", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 7) {
                            appendTxt(ref rtBox, " Addr=", colorParam);
                            string hex = BitConverter.ToString(data, 6, dataLen - 1).Replace('-', ':');
                            decodeParamStr(ref rtBox, "EntityData", hex);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2e: // IR status notification
                case Ver3 | 0x2e: // IR status notification
                    {
                        appendTxt(ref rtBox, "IR Status", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " ACK", colorACK);
                        } else if (dataLen == 1) {
                            int irStatus = data[6];
                            bErr |= decodeDictParam(ref rtBox, ref IRStatus, colorData, irStatus, true);
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x2f: // IR functionality test
                case Ver3 | 0x2f: // IR functionality test
                    {
                        appendTxt(ref rtBox, "IR Test", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, " Cmd", colorCmd);
                        } else if (dataLen == 1) {
                            int respStatus = data[6];
                            if (respStatus == 0) {
                                appendTxt(ref rtBox, " Success", colorACK);
                            } else if (respStatus == 1) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else {
                                appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x30: // Response Multiple map data streaming
                case Ver3 | 0x30: // Multiple map data streaming
                    {
                        appendTxt(ref rtBox, "Map data streaming", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen == 1) {
                            int respStatus = data[6];
                            if (respStatus == 0) {
                                appendTxt(ref rtBox, " Success", colorACK);
                            } else if (respStatus == 1) {
                                appendTxt(ref rtBox, " Failure", colorErr);
                            } else {
                                appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                            }
                        } else if (dataLen >= 9) {
                            int mapSrvProt = data[6];
                            if (mapSrvProt == 0) {
                                decodeParam(ref rtBox, "Protocol", mapSrvProt, 2);
                            } else {
                                appendTxt(ref rtBox, " Wrong MapSrvProt=" + mapSrvProt, colorErr);
                            }
                            int mapId = (data[6] << 8) + data[7];
                            decodeParam(ref rtBox, "Id", mapId, 2);
                            int subMapId = data[8];
                            decodeParam(ref rtBox, "Id", subMapId, 2);
                            int method = data[9];
                            bErr |= decodeDictParam(ref rtBox, ref MapMethods, colorData, method, true);
                            int mapOffset = (data[10] << 24) + (data[11] << 16) + (data[12] << 8) + data[13];
                            decodeParam(ref rtBox, "MapOffset", subMapId, 8);
                            if (dataLen >= 9) {
                                string hex = BitConverter.ToString(data, 14, dataLen - 9).Replace('-', ' ');
                                decodeParamStr(ref rtBox, "EntityData", hex);
                            }
                        } else {
                            appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | 0x33: // Cmd RF learning
                case Ver3 | 0x33: // Report RF learning
                    {
                        appendTxt(ref rtBox, (ver == 0) ? "RF Learning" : "Report RF learning", colorCmd);
                        bErr |= checkFrame(dec, ref rtBox, num, ref data);
                        if (bErr)
                            break;
                        int dataLen = (data[4] << 8) + data[5];
                        if (dataLen > 0) {
                            int subCmd = data[6];
                            if (subCmd == 1) {
                                appendTxt(ref rtBox, " SubCmd=" + subCmd.ToString(), colorSubCmd);

                                if (dataLen == 2) {
                                    int learnStatus = data[7];
                                    if (learnStatus == 1) {
                                        appendTxt(ref rtBox, " enter RF learning", colorData);
                                    } else if (learnStatus == 2) {
                                        appendTxt(ref rtBox, " exit RF learning", colorData);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong LearnStatus=" + learnStatus, colorErr);
                                    }
                                } else if (dataLen == 3) {
                                    int learnStatus = data[7];
                                    if (learnStatus == 1) {
                                        appendTxt(ref rtBox, " enter RF learning", colorData);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong LearnStatus=" + learnStatus, colorErr);
                                    }
                                    int respStatus = data[8];
                                    if (respStatus == 0) {
                                        appendTxt(ref rtBox, " Success", colorACK);
                                    } else if (respStatus == 1) {
                                        appendTxt(ref rtBox, " Failure", colorErr);
                                    } else if (respStatus == 2) {
                                        appendTxt(ref rtBox, " Exit the RF learning status", colorErr);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                    }
                                } else {
                                    appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                                    bErr = true;
                                }
                            } else if (subCmd == 2) {
                                appendTxt(ref rtBox, " SubCmd=" + subCmd.ToString(), colorSubCmd);
                                int rfType = data[7];
                                bErr |= decodeDictParam(ref rtBox, ref RFTypes, colorData, rfType, true);
                                int numKeyVal = data[8];
                                decodeParam(ref rtBox, "NumKeyVal", numKeyVal);
                                int serNum = data[9];
                                decodeParam(ref rtBox, "SerNum", serNum);
                                int freq = data[10];
                                bErr |= decodeDictParam(ref rtBox, ref Frequencies, colorData, freq, true);
                                int transRate = (data[11] << 8) + data[12];
                                decodeParam(ref rtBox, "TransmissionRate", transRate);

                                if (dataLen >= 8) {
                                    int offset = 13;
                                    while (!bErr && offset < (num - 7)) {
                                        int times = data[offset];
                                        decodeParam(ref rtBox, "T", times);
                                        int delay = (data[offset + 1] << 8) + data[offset + 2];
                                        decodeParam(ref rtBox, "D", delay);
                                        int intervals = (data[offset + 3] << 8) + data[offset + 4];
                                        decodeParam(ref rtBox, "I", intervals);
                                        int length = (data[offset + 5] << 8) + data[offset + 6];
                                        decodeParam(ref rtBox, "L", length);
                                        int code = data[offset + 7];
                                        decodeParam(ref rtBox, "C", code);
                                        offset += 8;
                                    } // while
                                    if (offset != (num - 1)) { // all eaten? => no
                                        appendTxt(ref rtBox, " Wrong RF data offset=" + offset, colorErr);
                                        bErr = true;
                                    }
                                } else {
                                    appendTxt(ref rtBox, " Wrong DataLen=" + dataLen, colorErr);
                                    bErr = true;
                                }
                            } else if (subCmd == 3) {
                                appendTxt(ref rtBox, " SubCmd=" + subCmd.ToString(), colorSubCmd);

                                int respStatus = data[7];
                                if (respStatus == 0) {
                                    appendTxt(ref rtBox, " Success", colorACK);
                                } else if (respStatus == 1) {
                                    appendTxt(ref rtBox, " Failure", colorErr);
                                } else {
                                    appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                }
                                if (dataLen > 2) {
                                    string hex = BitConverter.ToString(data, 8, dataLen - 2).Replace('-', ' ');
                                    decodeParamStr(ref rtBox, "Learned", hex);
                                }
                            } else {
                                appendTxt(ref rtBox, " Wrong SubCmd=" + subCmd.ToString(), colorErr);
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

                        if (dataLen >= 1) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " SubCmd=" + subCmd.ToString(), colorSubCmd);

                            if (dataLen == 1 && subCmd == 6) {
                                appendTxt(ref rtBox, " Get map session ID", colorInfo);
                            } else if (dataLen >= 2) {
                                int respStatus = data[7];
                                if (subCmd == 0x03) {
                                    if (respStatus == 0) {
                                        appendTxt(ref rtBox, " Success", colorACK);
                                    } else if (respStatus == 1) {
                                        appendTxt(ref rtBox, " Failure", colorErr);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                    }
                                } else if (subCmd == 0x06 && dataLen == 4) {
                                    int sucFlag = data[7];
                                    if (sucFlag == 0) {
                                        appendTxt(ref rtBox, " Success", colorACK);
                                    } else if (sucFlag == 1) {
                                        appendTxt(ref rtBox, " The map streaming service is not enabled", colorErr);
                                    } else if (sucFlag == 2) {
                                        appendTxt(ref rtBox, " Failed to get the session ID", colorErr);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong SuccessFlag=" + sucFlag, colorErr);
                                        bErr = true;
                                    }
                                    int mapId = (data[8] << 8) + data[9];
                                    appendTxt(ref rtBox, " Id=0x", colorDP);
                                    appendTxt(ref rtBox, mapId.ToString("X4"), colorData);
                                } else if (subCmd == 0x0b) {
                                    if (respStatus == 0) {
                                        appendTxt(ref rtBox, " Failure", colorErr);
                                    } else if (respStatus == 1) {
                                        appendTxt(ref rtBox, " Success", colorACK);
                                    } else if (respStatus == 2) {
                                        appendTxt(ref rtBox, " Invalid Data", colorErr);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                    }
                                    if (dataLen >= 9) {
                                        int resTime = data[8];
                                        bErr |= decodeDictParam(ref rtBox, ref ResultTimes, colorData, resTime, true);

                                        int year = data[9] + 2000;
                                        int month = data[10];
                                        int day = data[11];
                                        int hour = data[12];
                                        int minute = data[13];
                                        int second = data[14];
                                        try {
                                            DateTime date = new DateTime(year, month, day, hour, minute, second);
                                            decodeParamStr(ref rtBox, "Date", date.ToString("yy-MM-dd HH:mm"));
                                        } catch {
                                            string hex = BitConverter.ToString(data, 10, 5).Replace('-', ' ');
                                            appendTxt(ref rtBox, " Wrong DateTime Parameter=" + hex, colorErr);
                                            bErr = true;
                                        }

                                        // decode all status of DP units
                                        int offset = 15; // start index to read status of DP units
                                        while (bErr == false && offset < (num - 1)) {
                                            bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                                        } // while
                                        if (offset != (num - 1)) { // all eaten? => no
                                            appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                                            bErr = true;
                                        }
                                    }
                                } else {
                                    appendTxt(ref rtBox, " Wrong SubCmd=" + subCmd.ToString(), colorErr);
                                }
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
                        if (dataLen >= 1) {
                            int subCmd = data[6];
                            appendTxt(ref rtBox, " SubCmd=" + subCmd.ToString(), colorSubCmd);
                            if (ver == 0 && subCmd == 0 && dataLen >= 2) {
                                int mcuOTA = data[7];
                                if (mcuOTA < 2) {
                                    appendTxt(ref rtBox, " McuOTA=" + ((mcuOTA == 0) ? "Scratchpad" : "No scratchpad"), colorInfo);
                                } else {
                                    appendTxt(ref rtBox, " Wrong McuOTA=" + mcuOTA, colorErr);
                                    bErr = true;
                                }
                                if (dataLen == 3) {
                                    int abv = data[8];
                                    decodeAbv(ref rtBox, abv);
                                }
                            } else if (ver == 3 && subCmd == 0 && dataLen == 1) {
                                appendTxt(ref rtBox, " Request", colorSubCmd);
                            } else if (ver == 3 && subCmd == 0 && dataLen == 2) {
                                int respStatus = data[7];
                                if (respStatus == 0) {
                                    appendTxt(ref rtBox, " Success", colorACK);
                                } else if (respStatus == 1) {
                                    appendTxt(ref rtBox, " Failure", colorErr);
                                } else if (respStatus == 2) {
                                    appendTxt(ref rtBox, " Invalid Data", colorErr);
                                } else {
                                    appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                }
                            } else if (ver == 3 && subCmd == 0 && dataLen >= 2) {
                                string prodTxt = ASCIIEncoding.ASCII.GetString(data, 7, dataLen - 1);
                                appendTxt(ref rtBox, " " + prodTxt, colorData);
                                int idx = prodTxt.IndexOf("\"abv\"");
                                if (idx >= 0) {
                                    idx = prodTxt.IndexOf(':', idx) + 1;
                                    int abv;
                                    Int32.TryParse(prodTxt.Substring(idx,1), out abv);
                                    decodeAbv(ref rtBox, abv);
                                }
                            } else if (ver == 0 && subCmd == 1 && dataLen == 3) {
                                int accept = data[7];
                                if (accept == 0) {
                                    int packSize = data[8];
                                    bErr |= decodeDictParam(ref rtBox, ref PacketSizes, colorData, packSize, false);
                                } else if (accept == 1) {
                                    int transfStatus = data[8];
                                    bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                                } else {
                                    appendTxt(ref rtBox, " Wrong Accept=" + accept, colorErr);
                                    bErr = true;
                                }
                            } else if (ver == 0 && subCmd == 1 && dataLen == 2) {
                                appendTxt(ref rtBox, " Notify file download task", colorInfo);
                            } else if (ver == 0 && subCmd == 6 && dataLen == 2) {
                                appendTxt(ref rtBox, " Request uploading files", colorInfo);
                                int respStatus = data[7];
                                if (respStatus == 1) {
                                    appendTxt(ref rtBox, " Success", colorACK);
                                } else if (respStatus == 2) {
                                    appendTxt(ref rtBox, " Failure", colorErr);
                                } else {
                                    appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus, colorErr);
                                }
                            } else if (ver == 0 && subCmd == 4 && dataLen == 3) {
                                appendTxt(ref rtBox, " Response of interrupt file transfer", colorInfo);
                                int cmdTerm = data[7];
                                int respStatus = data[8];

                                if (cmdTerm == 1) {
                                    if (respStatus == 0) {
                                        appendTxt(ref rtBox, " Success", colorACK);
                                    } else if (respStatus == 1) {
                                        appendTxt(ref rtBox, " Failure", colorErr);
                                    } else if (respStatus == 2) {
                                        appendTxt(ref rtBox, " Canceled", colorErr);
                                    } else {
                                        appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus.ToString(), colorErr);
                                    }
                                } else if (cmdTerm == 2) {
                                    int transfStatus = data[8];
                                    bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                                } else {
                                    appendTxt(ref rtBox, " Wrong CmdTerm=" + cmdTerm.ToString(), colorErr);
                                    bErr = true;
                                }
                            } else if (ver == 3 && subCmd == 4 && dataLen == 3) {
                                appendTxt(ref rtBox, " Interrupt file transfer", colorInfo);
                                int intState = data[7];
                                bErr |= decodeDictParam(ref rtBox, ref InterruptStates, colorData, intState, false);
                                int downloadExc = data[8];
                                bErr |= decodeDictParam(ref rtBox, ref FileDownloadExceptions, colorErr, downloadExc, false);
                            } else if (ver == 3 && subCmd == 6 && dataLen > 2) {
                                appendTxt(ref rtBox, " Request uploading files", colorInfo);
                                string prodTxt = ASCIIEncoding.ASCII.GetString(data, 7, dataLen - 1);
                                appendTxt(ref rtBox, " " + prodTxt, colorData);
                            } else if (ver == 3 && subCmd == 7 && dataLen == 3) {
                                int respStatus = data[7];
                                if (respStatus == 0) {
                                    appendTxt(ref rtBox, " Success", colorACK);
                                } else if (respStatus == 1) {
                                    appendTxt(ref rtBox, " Failure", colorErr);
                                } else if (respStatus == 2) {
                                    appendTxt(ref rtBox, " Canceled", colorErr);
                                } else {
                                    appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus.ToString(), colorErr);
                                }
                                int transfStatus = data[8];
                                bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                            } else if (ver == 3 && subCmd == 7 && dataLen >= 6) {
                                appendTxt(ref rtBox, " File data", colorInfo);
                                int transId = (data[7] << 8) + data[8];
                                decodeParam(ref rtBox, "TransmissionId", transId);
                                int offset = (data[9] << 24) + (data[10] << 16) + (data[11] << 8) + data[12];
                                decodeParam(ref rtBox, "Offset", offset, 8);
                                if (dataLen > 6) { // data bytes
                                    string hex = BitConverter.ToString(data, 13, dataLen - 7).Replace('-', ' ');
                                    decodeParamStr(ref rtBox, "Data", hex);
                                }
                            } else {
                                appendTxt(ref rtBox, " Wrong Decoding ver=" + ver.ToString() + " subCmd=" + subCmd.ToString() + " DataLen=" + dataLen.ToString(), colorErr);
                                bErr = true;
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
