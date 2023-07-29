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
using System.Data;
using System.Text.RegularExpressions;

namespace SniffUART {
    internal static class DecoderMcuSerPort {
        // separation delimiters
        public static char[] separators = { '"', ',', '.', ';', '(', ')', '{', '}', '[', ']' };

        // dictionaries defining pairs of <values, string> to output meaningful descriptionof values
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

        public static Dictionary<int, string> FileTypes = new Dictionary<int, string>
        {
            { -1, "FileType" },
            {1, "TXT" },
            { 2, "DOC" },
            { 3, "PDF" },
            { 4, "EXCEL" },
            { 5, "PNG" },
            { 6, "JPG" },
            { 7, "BMP" },
            { 8, "TIF" },
            { 9, "GIF" },
            { 10, "PCX" },
            { 11, "TGA" },
            { 12, "Exif" },
            { 13, "FPX" },
            { 14, "SVG" },
            { 15, "PSD" },
            { 16, "CDR" },
            { 17, "PCD" },
            { 18, "DXF" },
            { 19, "UFO" },
            { 20, "EPS" },
            { 21, "AI" },
            { 22, "Raw" },
            { 23, "WMF" },
            { 24, "WebP" },
            { 25, "AVIF" },
            { 26, "WAV" },
            { 27, "FLAC" },
            { 28, "APE" },
            { 29, "ALAC" },
            { 30, "WavPack(WV)" },
            { 31, "MP3" },
            { 32, "AAC" },
            { 33, "Ogg Vorbis" },
            { 34, "Opus" },
            { 35, "MP4" },
        };


        public static Dictionary<int, string> FileActions = new Dictionary<int, string>
        {
            { -1, "FileAction" },
            { 0x01, "Print" },
            { 0x02, "Text display" },
            { 0x03, "Audio play" },
            { 0x04, "Video play" },
            { 0x05, "Store" },
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

        public static Dictionary<int, string> VoiceStatus = new Dictionary<int, string>
        {
            { -1, "VoiceStatus" },
            { 0x00, "Idle" },
            { 0x01, "Mic is muted" },
            { 0x02, "Woken up" },
            { 0x03, "Recording voices" },
            { 0x04, "Recognizing voices" },
            { 0x05, "Voice is recognized" },
            { 0x06, "Failed to recognize voices" },
        };

        public static Dictionary<int, string> VoiceStatusCmds = new Dictionary<int, string>
        {
            { -1, "VoiceStatus Cmd" },
            { 0x00, "Turn on the mic" },
            { 0x01, "Mute the mic" },
            { 0xa0, "Query the mic status" },
        };

        public static Dictionary<int, string> VoiceTestCmds = new Dictionary<int, string>
        {
            { -1, "VoiceTest Cmd" },
            { 0x00, "Disable audio test" },
            { 0x01, "Perform audio loop test on mic1" },
            { 0x02, "Perform audio loop test on mic2" },
            { 0xa0, "Query test status" },
        };

        public static Dictionary<int, string> TestWakingUpCmds = new Dictionary<int, string>
        {
            { -1, "VoiceTesting cmd" },
            { 0x00, "Failed to be woken up" },
            { 0x01, "Woken up successfully" },
        };

        public static Dictionary<int, string> VoiceASRCmds = new Dictionary<int, string>
        {
            { -1, "ASR Cmd" },
            { 0x00, "turn off ASR" },
            { 0x01, "turn on ASR" },
        };

        public static Dictionary<int, string> VoiceCallCmds = new Dictionary<int, string>
{
            { -1, "Call Cmd" },
            { 0x00, "Online" },
            { 0x01, "Bluetooth connected" },
            { 0x02, "In-call" },
        };

        public static Dictionary<int, string> RingToneOpStates = new Dictionary<int, string>
        {
            { -1, "RingToneOpState" },
            { 0x00, "Poll" },
            { 0x01, "Add" },
            { 0x02, "Delete" },
            { 0x03, "Update" },
        };

        public static Dictionary<int, string> SwitchLocalAlarms = new Dictionary<int, string>
        {
            { -1, "Switch" },
            { 0x00, "off" },
            { 0x01, "on" },
        };

        public static Dictionary<int, string> SetAlarms = new Dictionary<int, string>
        {
            { -1, "Operation" },
            { 0x01, "Set an alarm" },
            { 0x02, "Edit an alarm" },
            { 0x03, "Delete an alarm" },
            { 0x04, "Turn on an alarm" },
            { 0x05, "Turn off an alarm" },
        };

        public static Dictionary<int, string> AlarmOpResponses = new Dictionary<int, string>
        {
            { -1, "OpResp" },
            { 0x00, "Operation succeeded" },
            { 0x01, "JSON format error occurs" },
            { 0x02, "Parameter is missing" },
            { 0x03, "Failed to call service" },
            { 0x04, "Other errors" },
        };

        public static Dictionary<int, string> TimeZones = new Dictionary<int, string>
        {
            { -1, "TimeZone" },
            { 0x00, "GMT" },
            { 0x01, "Local time" },
        };

        public static Dictionary<int, string> ResetStates = new Dictionary<int, string>
        {
            { -1, "Reset Status" },
            { 0x00, "Reset by hardware operation" },
            { 0x01, "Reset performed on the mobile app" },
            { 0x02, "Factory reset performed on the mobile app" },
        };

        public static Dictionary<int, string> WiFiInfos = new Dictionary<int, string>
        {
            { -1, "Wi-Fi Info" },
            { 0xff, "All" },
            { 0x01, "SSID of an AP" },
            { 0x02, "Country code" },
        };

        public static Dictionary<int, string> BluetoothStatus = new Dictionary<int, string>
        {
            { -1, "Status" },
            { 0x00, "Unbound and not connected" },
            { 0x01, "Unbound and connected" },
            { 0x02, "Bound and not connected" },
            { 0x03, "Bound and connected" },
            { 0x04, "Unknown status" },
        };

        public static Dictionary<int, string> BluetoothLEDStatus = new Dictionary<int, string>
        {
            { -1, "LEDStatus" },
            { 0x00, "Blink quickly" },
            { 0x01, "Steady off" },
            { 0x02, "Steady off" },
            { 0x03, "Steady on" },
            { 0x04, "Blink slowly" },
        };

        public static Dictionary<int, string> DataSources = new Dictionary<int, string>
        {
            { -1, "DataSources" },
            { 0x00, "Unkown" },
            { 0x01, "LAN" },
            { 0x02, "WAN" },
            { 0x03, "Scheduled tasks in LAN" },
            { 0x04, "Scene linkage in WAN" },
            { 0x05, "reliable channels" },
            { 0x06, "Bluetooth" },
            { 0x07, "Scene linkage in LAN" },
            { 0xf0, "Offline voice modules" },
        };

        public static Dictionary<int, string> McuReportStatus = new Dictionary<int, string>
        {
            { -1, "McuReportStatus" },
            { 0x00, "MCU proactively reports status" },
            { 0x01, "MCU responds to status queries" },
            { 0x02, "MCU responds to commands of extended DPs" },
        };


        //*********************************************************************************************************************************
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

        private static bool decodeWeatherParameter(ref RichTextBox rtBox, ref byte[] data, int num, bool bContainsData, int offset) {
            bool bErr = false;
            while (!bErr && offset < (num - 1)) {
                int l = data[offset];
                int idx = offset + l + 1;
                int tl = 0;
                if (offset + l < (num - 1)) {
                    string str = Encoding.UTF8.GetString(data, offset + 1, l);
                    decodeParamStr(ref rtBox, "Parameter", str);

                    if (bContainsData) { // data values to decode
                        idx += 2;
                        byte t = data[offset + l + 1];
                        tl = data[offset + l + 2];
                        if (t == 0) { // int
                            switch (tl) {
                                case 1: {
                                        UInt64 val = (UInt64)(data[idx++]);
                                        decodeParam(ref rtBox, "Value", val, 2);
                                    }
                                    break;
                                case 2: {
                                        UInt64 val = (UInt64)((data[idx++] << 8) + data[idx++]);
                                        decodeParam(ref rtBox, "Value", val, 4);
                                    }
                                    break;
                                case 4: {
                                        UInt64 val = (UInt64)((data[idx++] << 24) + (data[idx++] << 16) + (data[idx++] << 8) + data[idx++]);
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
                            string parStr = Encoding.UTF8.GetString(data, idx, tl);
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
            if (offset != num - 1) {
                appendTxt(ref rtBox, " Not all bytes consumed", colorErr);
                bErr = true;
            }
            return bErr;
        }

        private static bool hasSubCmd(int cmd) {
            if (cmd == 0x33 || cmd == 0x34 || cmd == 0x35 || cmd == 0x36 || cmd == 0x37 || cmd == 0x65 || cmd == 0x72) {
                return true;
            }
            return false;
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
            int subCmd = (num >= 7 && hasSubCmd(cmd)) ? data[6] : 0;
            int dataLen = (num > 5) ? (data[4] << 8) + data[5] : 0;

            //-----------------------------------------------------------------------
            // bit16-17=version, bit12-15=DataLen, bit8-11=subCmd, bit0-7=cmd
            //-----------------------------------------------------------------------
            const int Ver0 = 0x000000;
            //const int Ver1 = 0x010000;
            //const int Ver2 = 0x020000;
            const int Ver3 = 0x030000;

            const int DLen0 = 0x00000; // dataLen == 0
            const int DLen1 = 0x01000; // dataLen == 1
            const int DLen2 = 0x02000; // dataLen == 2
            const int DLen3 = 0x03000; // dataLen == 3
            const int DLen4 = 0x04000; // dataLen == 4
            const int DLenX = 0x05000; // dataLen > 4
            int dlSw = (dataLen == 0) ? DLen0 : (dataLen == 1) ? DLen1 : (dataLen == 2) ? DLen2 : (dataLen == 3) ? DLen3 : (dataLen == 4) ? DLen4 : DLenX;

            const int SCmd0 = 0x0000; // subCmd == 0x00
            const int SCmd1 = 0x0100; // subCmd == 0x01
            const int SCmd2 = 0x0200; // subCmd == 0x02
            const int SCmd3 = 0x0300; // subCmd == 0x03
            const int SCmd4 = 0x0400; // subCmd == 0x04
            const int SCmd5 = 0x0500; // subCmd == 0x05
            const int SCmd6 = 0x0600; // subCmd == 0x06
            const int SCmd7 = 0x0700; // subCmd == 0x07
            const int SCmd8 = 0x0800; // subCmd == 0x08
            const int SCmd9 = 0x0900; // subCmd == 0x09
            const int SCmda = 0x0a00; // subCmd == 0x0a
            const int SCmdb = 0x0b00; // subCmd == 0x0b
            const int SCmdc = 0x0c00; // subCmd == 0x0c
            const int SCmdd = 0x0d00; // subCmd == 0x0d
            const int SCmde = 0x0e00; // subCmd == 0x0e
            const int SCmdf = 0x0f00; // subCmd == 0x0f

            if (num >= 6) {
                bErr |= checkFrame(dec, ref rtBox, num, ref data);
                if (bErr) { // if an error had been detected, then print all msg bytes in hex
                    string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                    appendTxt(ref rtBox, " No Frame Msg=", colorData);
                    appendTxt(ref rtBox, hex, colorErr);
                    return bErr;
                }
            }

            int sw = (ver << 16) + dlSw + (subCmd << 8) + cmd;
            switch (sw) {
                case Ver0 | DLen0 | 0x00: // Heartbeat
                case Ver3 | DLen0 | 0x00: // Heartbeat frame
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                    }
                    break;

                case Ver3 | DLen1 | 0x00: // MCU restarting
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
                            int startState = data[6];
                            appendTxt(ref rtBox, (startState == 0) ? "MCU restarting" : "MCU running", colorInfo);
                        }
                    }
                    break;

                // Query product information
                case Ver0 | DLen0 | 0x01: // Query product information
                    {
                        appendTxt(ref rtBox, "Query Product Information", colorCmd);
                    }
                    break;

                // Response product information
                case Ver0 | DLenX | 0x01: // Response product information
                    {
                        appendTxt(ref rtBox, "Product Information", colorCmd);
                        string prodTxt = Encoding.UTF8.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, " " + prodTxt, colorData);
                    }
                    break;

                case Ver0 | DLen0 | 0x02: // Query network status of the device
                    {
                        appendTxt(ref rtBox, "Query Network Status", colorCmd);
                    }
                    break;

                case Ver3 | DLen0 | 0x02: // Respose network status of the device
                    {
                        appendTxt(ref rtBox, "Network Status", colorCmd);
                        appendTxt(ref rtBox, " MCU uses network module", colorInfo);
                    }
                    break;

                case Ver3 | DLen2 | 0x02: // Response network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        UInt64 pinWiFiStatus = data[6];
                        decodeParam(ref rtBox, "GPIO pins WiFiStatusLED", pinWiFiStatus);
                        UInt64 pinWiFiNetwork = data[7];
                        decodeParam(ref rtBox, "WiFiNetworkReset", pinWiFiNetwork);
                    }
                    break;

                case Ver3 | DLen3 | 0x02: // Response network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        UInt64 pinWiFiStatus = data[6];
                        decodeParam(ref rtBox, "GPIO pins WiFiStatusLED", pinWiFiStatus);
                        UInt64 pinWiFiNetwork = data[7];
                        decodeParam(ref rtBox, "WiFiNetworkReset", pinWiFiNetwork);
                        UInt64 pinBluetoothStatus = data[8];
                        decodeParam(ref rtBox, "BluetoothStatusLED", pinBluetoothStatus);
                    }
                    break;

                case Ver0 | DLen1 | 0x03: // Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        int netState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref NetworkStates, colorData, netState, false);
                    }
                    break;

                case Ver3 | DLen0 | 0x03: // ACK Report the network status of the device
                    {
                        appendTxt(ref rtBox, "Report Network Status", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x04: // Reset Wi-Fi and select configuration mode
                case Ver0 | DLen0 | 0x04: // ACK Reset Wi-Fi
                case Ver3 | DLen0 | 0x04: // ACK Reset Wi-Fi
                    {
                        appendTxt(ref rtBox, "Reset Wi-Fi", colorCmd);
                        if (dataLen == 0) {
                            if (ver == 0)
                                appendTxt(ref rtBox, " ACK", colorACK);
                            else
                                appendTxt(ref rtBox, " Cmd", colorSubCmd);
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
                        }
                    }
                    break;

                case Ver0 | DLen0 | 0x05: // ACK Reset Wi-Fi Pairing Mode
                case Ver3 | DLen1 | 0x05: // Set Wi-Fi Pairing Mode
                case Ver3 | DLenX | 0x05: // Set Wi-Fi Pairing Mode
                    {
                        appendTxt(ref rtBox, "Set Wi-Fi Pairing", colorCmd);
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
                            // decode all DP units
                            int offset = 6; // start index to read DP units
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

                case Ver0 | DLen0 | 0x06: // Send Module Command
                    {
                        appendTxt(ref rtBox, "Send Module Command", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLenX | 0x06: // Send Module Command
                    {
                        appendTxt(ref rtBox, "Send Module Command", colorCmd);
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

                case Ver0 | DLen1 | 0x07: // Report Data
                    {
                        appendTxt(ref rtBox, "Report Data", colorCmd);
                        int repStatus = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref ReportStates, colorData, repStatus, false);
                    }
                    break;

                case Ver3 | DLenX | 0x07: // Report Data
                    {
                        appendTxt(ref rtBox, "Report Data", colorCmd);
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

                case Ver0 | DLen0 | 0x08: // Query DP status
                    {
                        appendTxt(ref rtBox, "Query DP status", colorCmd);
                    }
                    break;

                case Ver0 | DLen4 | 0x0a: // Start OTA update
                    {
                        appendTxt(ref rtBox, "Start OTA update", colorCmd);
                        UInt64 val = (UInt64)((data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9]);
                        decodeParam(ref rtBox, "DataLen", val);
                    }
                    break;

                case Ver3 | DLen1 | 0x0a: // Start OTA update
                    {
                        appendTxt(ref rtBox, "Start OTA update", colorCmd);
                        int packSize = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref PacketSizes, colorData, packSize, false);
                    }
                    break;

                case Ver0 | DLenX | 0x0b: // Transmit update package
                    {
                        appendTxt(ref rtBox, "Transmit Package", colorCmd);
                        int offset = (data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9];
                        appendTxt(ref rtBox, " Offset=0x", colorType);
                        appendTxt(ref rtBox, offset.ToString("X8"), colorData);

                        // data bytes
                        string hex = BitConverter.ToString(data, 10, dataLen - 4).Replace('-', ' ');
                        decodeParamStr(ref rtBox, "Data", hex);
                    }
                    break;

                case Ver3 | DLen0 | 0x0b: // ACK Transmit update package
                    {
                        appendTxt(ref rtBox, "Transmit Package", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen2 | 0x0e: // Test Wi-Fi functionality
                    {
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
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

                case Ver3 | DLen0 | 0x0c: // Get system time in GMT
                    {
                        appendTxt(ref rtBox, "Get GMT Time", colorCmd);
                    }
                    break;

                case Ver3 | DLen0 | 0x0e: // Response Test Wi-Fi functionality
                    {
                        appendTxt(ref rtBox, "Query Signal Strength", colorCmd);
                    }
                    break;

                case Ver0 | DLen4 | 0x0f: // Response Get module’s memory
                    {
                        appendTxt(ref rtBox, "Memory", colorCmd);
                        UInt64 val = (UInt64)((data[9] << 24) + (data[8] << 16) + (data[7] << 8) + data[6]);
                        decodeParam(ref rtBox, "Free", val);
                    }
                    break;

                case Ver3 | DLen0 | 0x0f: // Get module’s memory
                    {
                        appendTxt(ref rtBox, "Get Memory", colorCmd);
                    }
                    break;

                case Ver3 | DLen0 | 0x1c: // System time
                    {
                        appendTxt(ref rtBox, "Get System Time", colorCmd);
                    }
                    break;

                case Ver0 | DLenX | 0x1c: // System time
                    {
                        appendTxt(ref rtBox, "System Time", colorCmd);
                        int obtainFlag = data[6];
                        decodeResponse(ref rtBox, obtainFlag, false, true);
                        int year = data[7] + 2000;
                        int month = data[8];
                        int day = data[9];
                        int hour = data[10];
                        int minute = data[11];
                        int second = data[12];
                        UInt64 week = data[13];
                        try {
                            DateTime date = new DateTime(year, month, day, hour, minute, second);
                            decodeParamStr(ref rtBox, "Date", date.ToString("yy-MM-dd HH:mm"));
                            decodeParam(ref rtBox, "Week", week);
                        } catch {
                            appendTxt(ref rtBox, " Wrong DateTime Parameter", colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLen2 | 0x20: // ACK Weather Service specification
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        int obtainFlag = data[6];
                        decodeResponse(ref rtBox, obtainFlag, false, true);
                        if (obtainFlag == 0) {
                            int sucFlag = data[7];
                            if (sucFlag == 1) {
                                appendTxt(ref rtBox, " Invalid data format" + sucFlag, colorErr);
                            } else if (sucFlag == 2) {
                                appendTxt(ref rtBox, " Exception error" + sucFlag, colorErr);
                            } else {
                                appendTxt(ref rtBox, " Wrong SuccessFlag=" + sucFlag, colorErr);
                            }
                        }
                    }
                    break;

                case Ver3 | DLenX | 0x20: // Weather Service specification
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        bErr |= decodeWeatherParameter(ref rtBox, ref data, num, false, 6);
                    }
                    break;

                case Ver0 | DLenX | 0x21: // Enable weather services
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        int sucFlag = data[6];
                        decodeResponse(ref rtBox, sucFlag, false, true);
                        bErr |= decodeWeatherParameter(ref rtBox, ref data, num, true, 7);
                    }
                    break;

                case Ver0 | DLen0 | 0x21: // ACK Enable weather services
                    {
                        appendTxt(ref rtBox, "Enable weather services", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 |DLen1 | 0x22: // Report Status (hint: might be that cmd 0x23 is used)
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);
                        int respStatus = data[6];
                        decodeResponse(ref rtBox, respStatus, false, true);
                    }
                    break;

                case Ver3 | DLenX | 0x22: // Report Status
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);

                        // decode all status of DP units
                        int offset = 6; // start index to read status of DP units
                        while (bErr == false && offset < (num - 1)) {
                            bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                        } // while
                        if (offset != (num - 1)) { // all eaten? => no
                            appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLen1 | 0x23: // Response Report Status
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);
                        int respStatus = data[6];
                        decodeResponse(ref rtBox, respStatus, false);
                    }
                    break;

                case Ver0 | DLen1 | 0x24: // Response Get Wi-Fi signal strength
{
                        appendTxt(ref rtBox, "Wi-Fi signal strength", colorCmd);
                        UInt64 signal = data[6];
                        decodeParam(ref rtBox, "Signal", signal);
                    }
                    break;

                case Ver3 | DLen0 | 0x24: // Get Wi-Fi signal strength
                    {
                        appendTxt(ref rtBox, "Get Wi-Fi signal strength", colorCmd);
                    }
                    break;

                case Ver0 | DLen0 | 0x25: // ACK Disable heartbeats
                    {
                        appendTxt(ref rtBox, "Disable heartbeats", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen0 | 0x25: // Cmd Disable heartbeats
                    {
                        appendTxt(ref rtBox, "Disable heartbeats", colorCmd);
                    }
                    break;

                case Ver0 | DLen1 | 0x28: // Response Map streaming for robot vacuum
                    {
                        appendTxt(ref rtBox, "Map data streaming for robot vacuum", colorCmd);
                        int netDataResp = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref MapDataResponses, colorData, netDataResp, true);
                    }
                    break;

                case Ver3 | DLenX | 0x28: // Map streaming for robot vacuum
                    {
                        appendTxt(ref rtBox, "Map data streaming for robot vacuum", colorCmd);
                        UInt64 mapId = (UInt64)((data[6] << 8) + data[7]);
                        decodeParam(ref rtBox, "Id", mapId, 4);
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
                    }
                    break;

                case Ver0 | DLen1 | 0x2a: // Response Pairing via serial port
                    {
                        appendTxt(ref rtBox, "Pairing via serial port", colorCmd);
                        int resPairing = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref ResultsPairing, colorData, resPairing, true);
                    }
                    break;

                case Ver3 | DLenX | 0x2a: // Pairing via serial port
                    {
                        appendTxt(ref rtBox, "Pairing via serial port", colorCmd);
                        string str = Encoding.UTF8.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, str, colorData);
                    }
                    break;

                case Ver0 | DLen1 | 0x2b: // Response Get the current network status
                    {
                        appendTxt(ref rtBox, "Network Status", colorCmd);
                        int netState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref NetworkStates, colorData, netState, true);
                    }
                    break;

                case Ver3 | DLen0 | 0x2b: // Get the current network status
                    {
                        appendTxt(ref rtBox, "Get Network Status", colorCmd);
                    }
                    break;

                case Ver0 | DLen1 | 0x2c: // Test Wi-Fi functionality (connection)
                    {
                        appendTxt(ref rtBox, "Test Wi-Fi", colorCmd);
                        int respStatus = data[6];
                        decodeResponse(ref rtBox, respStatus, false, true);
                    }
                    break;

                case Ver3 | DLenX | 0x2c: // Response Test Wi-Fi functionality (connection)
                    {
                        appendTxt(ref rtBox, "Test Wi-Fi", colorCmd);
                        string str = Encoding.UTF8.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, str, colorData);
                    }
                    break;

                case Ver0 | DLenX | 0x2d: // Reponse Get module’s MAC address
                    {
                        appendTxt(ref rtBox, "MAC", colorCmd);
                        appendTxt(ref rtBox, " Addr=", colorParam);
                        string hex = BitConverter.ToString(data, 6, dataLen - 1).Replace('-', ':');
                        decodeParamStr(ref rtBox, "EntityData", hex);
                    }
                    break;

                case Ver3 | DLen0 | 0x2d: // Get module’s MAC address
                    {
                        appendTxt(ref rtBox, "Get MAC", colorCmd);
                    }
                    break;

                case Ver0 | DLen1 | 0x2e: // Response IR status notification
                    {
                        appendTxt(ref rtBox, "IR Status", colorCmd);
                        int irStatus = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref IRStatus, colorData, irStatus, true);
                    }
                    break;

                case Ver3 | DLen0 | 0x2e: // Cmd IR status notification
                    {
                        appendTxt(ref rtBox, "IR Status", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x2f: // Response IR functionality test
                    {
                        appendTxt(ref rtBox, "IR Test", colorCmd);
                        int respStatus = data[6];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver3 | DLen0 | 0x2f: // Cmd IR functionality test
                    {
                        appendTxt(ref rtBox, "IR Test", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver3 | DLen1 | 0x30: // Multiple map data streaming
                    {
                        appendTxt(ref rtBox, "Map data streaming", colorCmd);
                        int respStatus = data[6];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver3 | DLenX | 0x30: // Response Multiple map data streaming
                    {
                        appendTxt(ref rtBox, "Map data streaming", colorCmd);
                        UInt64 mapSrvProt = data[6];
                        if (mapSrvProt == 0) {
                            decodeParam(ref rtBox, "Protocol", mapSrvProt, 2);
                        } else {
                            appendTxt(ref rtBox, " Wrong MapSrvProt=" + mapSrvProt, colorErr);
                        }

                        UInt64 mapId = (UInt64)((data[6] << 8) + data[7]);
                        decodeParam(ref rtBox, "Id", mapId, 2);
                        UInt64 subMapId = data[8];
                        decodeParam(ref rtBox, "Id", subMapId, 2);
                        int method = data[9];
                        bErr |= decodeDictParam(ref rtBox, ref MapMethods, colorData, method, true);
                        UInt64 mapOffset = (UInt64)((data[10] << 24) + (data[11] << 16) + (data[12] << 8) + data[13]);
                        decodeParam(ref rtBox, "MapOffset", subMapId, 8);

                        if (dataLen >= 9) {
                            string hex = BitConverter.ToString(data, 14, dataLen - 9).Replace('-', ' ');
                            decodeParamStr(ref rtBox, "EntityData", hex);
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd1 | 0x33: // Cmd RF learning
                    {
                        appendTxt(ref rtBox, "RF Learning", colorCmd);
                        int learnStatus = data[7];
                        if (learnStatus == 1) {
                            appendTxt(ref rtBox, " enter RF learning", colorData);
                        } else if (learnStatus == 2) {
                            appendTxt(ref rtBox, " exit RF learning", colorData);
                        } else {
                            appendTxt(ref rtBox, " Wrong LearnStatus=" + learnStatus, colorErr);
                        }
                    }
                    break;

                case Ver3 | DLen3 | SCmd1 | 0x33: // Cmd RF learning
                    {
                        appendTxt(ref rtBox, "RF Learning", colorCmd);
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
                    }
                    break;

                case Ver0 | DLen2 | SCmd2 | 0x33: // Response RF learning
                case Ver3 | DLen2 | SCmd2 | 0x33: // (hint: the Tuya example is sent with Ver0, but defined with Ver3)
                    {
                        appendTxt(ref rtBox, "RF Learning", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver0 | DLenX | SCmd2 | 0x33: // Report RF learning
                    {
                        appendTxt(ref rtBox, "Report RF learning", colorCmd);
                        int rfType = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref RFTypes, colorData, rfType, true);
                        UInt64 numKeyVal = data[8];
                        decodeParam(ref rtBox, "NumKeyVal", numKeyVal);
                        UInt64 serNum = data[9];
                        decodeParam(ref rtBox, "SerNum", serNum);
                        int freq = data[10];
                        bErr |= decodeDictParam(ref rtBox, ref Frequencies, colorData, freq, true);
                        UInt64 transRate = (UInt64)((data[11] << 8) + data[12]);
                        decodeParam(ref rtBox, "TransmissionRate", transRate);

                        if (dataLen >= 8) {
                            int offset = 13;
                            while (!bErr && offset < (num - 7)) {
                                UInt64 times = data[offset];
                                decodeParam(ref rtBox, "T", times);
                                UInt64 delay = (UInt64)((data[offset + 1] << 8) + data[offset + 2]);
                                decodeParam(ref rtBox, "D", delay);
                                UInt64 intervals = (UInt64)((data[offset + 3] << 8) + data[offset + 4]);
                                decodeParam(ref rtBox, "I", intervals);
                                UInt64 length = (UInt64)((data[offset + 5] << 8) + data[offset + 6]);
                                decodeParam(ref rtBox, "L", length);
                                UInt64 code = (UInt64)data[offset + 7];
                                decodeParam(ref rtBox, "C", code);
                                offset += 8;
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong RF data offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd3 | 0x33: // Result RF learning
                case Ver3 | DLenX | SCmd3 | 0x33: // Report RF learning
                    {
                        appendTxt(ref rtBox, "RF learning", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true, true);
                        if (dataLen > 2) {
                            string hex = BitConverter.ToString(data, 8, dataLen - 2).Replace('-', ' ');
                            decodeParamStr(ref rtBox, "Learned", hex);
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd1 | 0x34: // Response Enable time service notification
                    {
                        appendTxt(ref rtBox, "Enable time service notification", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd1 | 0x34: // Enable time service notification
                    {
                        appendTxt(ref rtBox, "Enable time service notification", colorCmd);
                        int timeZone = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref TimeZones, colorData, timeZone, true);
                    }
                    break;

                case Ver0 | DLenX | SCmd2 | 0x34: // Response time
                    {
                        appendTxt(ref rtBox, "Response time", colorCmd);
                        int timeZone = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref TimeZones, colorData, timeZone, true);
                        int offset = 8;
                        int year = data[offset++] + 2000;
                        int month = data[offset++];
                        int day = data[offset++];
                        int hour = data[offset++];
                        int minute = data[offset++];
                        int second = data[offset++];
                        UInt64 week = data[13];
                        try {
                            DateTime date = new DateTime(year, month, day, hour, minute, second);
                            decodeParamStr(ref rtBox, "Date", date.ToString("yy-MM-dd HH:mm"));
                            decodeParam(ref rtBox, "Week", week);
                        } catch {
                            string hex = BitConverter.ToString(data, 10, 5).Replace('-', ' ');
                            appendTxt(ref rtBox, " Wrong DateTime Parameter=" + hex, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver3 | DLen1 | SCmd2 | 0x34: // ACK Response time
                    {
                        appendTxt(ref rtBox, "Response time", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen1 | SCmd3 | 0x34: // Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmd3 | 0x34: // Response Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver0 | DLen2 | SCmd4 | 0x34: // Response Reset
                    {
                        appendTxt(ref rtBox, "Reset", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen1 | SCmd4 | 0x34: // Reset Cmd
                    {
                        appendTxt(ref rtBox, "Reset", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmd5 | 0x34: // Reset Status
                    {
                        appendTxt(ref rtBox, "Reset Status", colorCmd);
                        int state = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref ResetStates, colorData, state, true);
                    }
                    break;

                case Ver3 | DLen1 | SCmd5 | 0x34: // ACK Reset Status
                    {
                        appendTxt(ref rtBox, "Reset Status", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen1 | SCmd6 | 0x34: // Response Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
                        appendTxt(ref rtBox, " Get map session ID", colorInfo);
                    }
                    break;

                case Ver0 | DLen4 | SCmd6 | 0x34: // Response Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
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
                    }
                    break;

                case Ver0 | DLenX | SCmd7 | 0x34: // Response Get information about Wi-Fi module
                    {
                        appendTxt(ref rtBox, "Wi-Fi Infos", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                        if (dataLen > 8) {
                            string infoTxt = Encoding.UTF8.GetString(data, 8, dataLen - 1);
                            appendTxt(ref rtBox, " " + infoTxt, colorData);
                        }
                    }
                    break;

                case Ver3 | DLen2 | SCmd7 | 0x34: // Get information about Wi-Fi module
                case Ver3 | DLen3 | SCmd7 | 0x34: // Get information about Wi-Fi module
                case Ver3 | DLen4 | SCmd7 | 0x34: // Get information about Wi-Fi module
                case Ver3 | DLenX | SCmd7 | 0x34: // Get information about Wi-Fi module
                    {
                        appendTxt(ref rtBox, "Get Wi-Fi Infos", colorCmd);
                        int offset = 7;
                        while (offset < (num - 1)) {
                            int info = data[offset++];
                            bErr |= decodeDictParam(ref rtBox, ref WiFiInfos, colorData, info, true);
                        } // while
                    }
                    break;

                case Ver0 | DLen2 | SCmdb | 0x34: // Response Send Status from MCU to module
                case Ver3 | DLenX | SCmdb | 0x34: // Response Send Status from MCU to module
                    {
                        appendTxt(ref rtBox, "Send Status", colorCmd);
                        int respStatus = data[7];
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
                    }
                    break;

                case Ver0 | DLen3 | SCmd1 | 0x35: // Response Bluetooth functional test
                    {
                        appendTxt(ref rtBox, "Bluetooth functional test", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, false);
                        if (respStatus == 0) {
                            int err = data[8];
                            appendTxt(ref rtBox, " error=", colorErr);
                            appendTxt(ref rtBox, (err == 0) ? "No beacon" : (err == 1) ? "No license" : "Wrong err=" + err.ToString(), (err == 0) ? colorData : colorErr);
                        } else {
                            UInt64 signal = data[8];
                            decodeParam(ref rtBox, "Signal", signal);
                        }
                    }
                    break;

                case Ver3 | DLen1 | SCmd1 | 0x35: // Cmd Bluetooth functional test
                    {
                        appendTxt(ref rtBox, "Bluetooth functional test", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmd4 | 0x35: // Response Bluetooth Status
                case Ver3 | DLen2 | SCmd5 | 0x35: // Response Bluetooth connection status
                    {
                        appendTxt(ref rtBox, "Bluetooth Status", colorCmd);
                        int state = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref BluetoothStatus, colorInfo, state, true);
                        bErr |= decodeDictParam(ref rtBox, ref BluetoothLEDStatus, colorParam, state, true);
                    }
                    break;

                case Ver0 | DLen1 | SCmd4 | 0x35: // ACK Bluetooth Status
                    {
                        appendTxt(ref rtBox, "Bluetooth Status", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen1 | SCmd5 | 0x35: // Request Bluetooth connection status
                    {
                        appendTxt(ref rtBox, "Request Bluetooth connection status", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLenX | SCmd6 | 0x35: // Data notification for Bluetooth/Beacon remote control
                    {
                        appendTxt(ref rtBox, "Data notification for Bluetooth/Beacon remote control", colorCmd);
                        UInt64 catId = data[7];
                        decodeParam(ref rtBox, "CatId", catId);
                        UInt64 ctrlCmd = data[8];
                        decodeParam(ref rtBox, "CtrlCmd", ctrlCmd);
                        UInt64 cmdData = (UInt64)((data[9] << 24) + (data[10] << 16) + (data[11] << 8) + data[12]);
                        decodeParam(ref rtBox, "CmdData", cmdData, 8);
                    }
                    break;

                case Ver3 | DLen1 | SCmd6 | 0x35: // ACK Data notification for Bluetooth/Beacon remote control
                    {
                        appendTxt(ref rtBox, "Data notification for Bluetooth/Beacon remote control", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen2 | SCmd1 | 0x36: // Response Enable the extended DP service
                    {
                        appendTxt(ref rtBox, "Enable ext. DP service", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd1 | 0x36: // Enable the extended DP service
                    {
                        appendTxt(ref rtBox, "Enable ext. DP service", colorCmd);
                        int flag = data[7];
                        if (flag == 0) {
                            appendTxt(ref rtBox, " disable", colorInfo);
                        } else if (flag == 1) {
                            appendTxt(ref rtBox, " enable", colorInfo);
                        } else {
                            appendTxt(ref rtBox, " Wrong Flag=" + flag, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLenX | SCmd2 | 0x36: // Send commands of extended DPs
                    {
                        appendTxt(ref rtBox, "Send commands of extended DPs", colorCmd);
                        int dataSrc = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref DataSources, colorData, dataSrc, true);

                        // decode all DP units
                        int offset = 8; // start index to read DP units
                        while (bErr == false && offset < (num - 1)) {
                            bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                        } // while
                        if (offset != (num - 1)) { // all eaten? => no
                            appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver3 | DLenX | SCmd3 | 0x36: // Report status of extended DPs
                    {
                        appendTxt(ref rtBox, "Report status of extended DPs", colorCmd);
                        int repStatus = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref McuReportStatus, colorData, repStatus, true);
                        int dataSrc = data[8];
                        bErr |= decodeDictParam(ref rtBox, ref DataSources, colorData, dataSrc, true);

                        // decode all DP units
                        int offset = 9; // start index to read DP units
                        while (bErr == false && offset < (num - 1)) {
                            bErr |= decodeStatusDataUnits(ref rtBox, dec, ver, num, ref offset, ref data);
                        } // while
                        if (offset != (num - 1)) { // all eaten? => no
                            appendTxt(ref rtBox, " Wrong DP decoding offset=" + offset, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd0 | 0x37: // Notification of new features
                case Ver0 | DLen3 | SCmd0 | 0x37: // Notification of new features
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
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
                    }
                    break;

                case Ver3 | DLen1 | SCmd0 | 0x37: // Request of new features
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        appendTxt(ref rtBox, " Request", colorSubCmd);
                    }
                    break;

                case Ver3 | DLen2 | SCmd0 | 0x37: // Response of new feature request
                    {
                        appendTxt(ref rtBox, "Feature Request", colorCmd);
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
                    }
                    break;

                case Ver3 | DLenX | SCmd0 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        string prodTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + prodTxt, colorData);
                        int idx = prodTxt.IndexOf("\"abv\":") + 6;
                        if (idx > 6) {
                            int abv;
                            string subStr = prodTxt.Substring(idx, prodTxt.IndexOfAny(separators, idx) - idx);
                            if (Int32.TryParse(subStr, out abv)) {
                                decodeAbv(ref rtBox, abv);
                            }
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd1 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        appendTxt(ref rtBox, " Notify file download task", colorInfo);
                    }
                    break;

                case Ver0 | DLen3 | SCmd1 | 0x37: // Notification of new feature setting
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        int accept = data[7];
                        if (accept == 0) {
                            int packSize = data[8];
                            bErr |= decodeDictParam(ref rtBox, ref PacketSizes, colorParam, packSize, false);
                        } else if (accept == 1) {
                            int transfStatus = data[8];
                            bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                        } else {
                            appendTxt(ref rtBox, " Wrong Accept=" + accept, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver0 | DLenX | SCmd2 | 0x37: // File Info Sync
                    {
                        appendTxt(ref rtBox, "File Info Sync", colorCmd);
                        // assuming Tuya spec means: file info is presented like in 0x37 subCmd 6
                        string fileInfosTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + fileInfosTxt, colorData);

                        int idx = fileInfosTxt.IndexOf("\"type\":") + 7;
                        if (idx > 7) {
                            int fInfo;
                            string subStr = fileInfosTxt.Substring(idx, fileInfosTxt.IndexOfAny(separators, idx) - idx);
                            if (Int32.TryParse(subStr, out fInfo)) {
                                decodeDictParam(ref rtBox, ref FileTypes, colorParam, fInfo, false);
                            }
                        }

                        idx = fileInfosTxt.IndexOf("\"act\":") + 6;
                        if (idx > 6) {
                            int fAction;
                            string subStr = fileInfosTxt.Substring(idx, fileInfosTxt.IndexOfAny(separators, idx) - idx);
                            if (Int32.TryParse(subStr, out fAction)) {
                                decodeDictParam(ref rtBox, ref FileActions, colorParam, fAction, false);
                            }
                        }
                    }
                    break;

                case Ver3 | DLen2 | SCmd2 | 0x37: // Response File Info Sync
                    {
                        appendTxt(ref rtBox, "File Info Sync", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver0 | DLen3 | SCmd4 | 0x37: // Response Interrupt file transfer
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
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
                    }
                    break;

                case Ver3 | DLen3 | SCmd4 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        appendTxt(ref rtBox, " Interrupt file transfer", colorInfo);
                        int intState = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref InterruptStates, colorData, intState, false);
                        int downloadExc = data[8];
                        bErr |= decodeDictParam(ref rtBox, ref FileDownloadExceptions, colorErr, downloadExc, false);
                    }
                    break;

                case Ver0 | DLen2 | SCmd6 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Set Features", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLenX | SCmd6 | 0x37: // Request File transfer
                    {
                        appendTxt(ref rtBox, "File transfer", colorCmd);
                        appendTxt(ref rtBox, " Request uploading files", colorInfo);
                        string fileInfosTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + fileInfosTxt, colorData);

                        int idx = fileInfosTxt.IndexOf("\"type\":") + 7;
                        if (idx > 7) {
                            int fInfo;
                            string subStr = fileInfosTxt.Substring(idx, fileInfosTxt.IndexOfAny(separators, idx) - idx);
                            if (Int32.TryParse(subStr, out fInfo)) {
                                decodeDictParam(ref rtBox, ref FileTypes, colorParam, fInfo, false);
                            }
                        }
                    }
                    break;

                case Ver3 | DLen3 | SCmd7 | 0x37: // Response File transfer
                    {
                        appendTxt(ref rtBox, "File transfer", colorCmd);
                        int respStatus = data[7];
                        if (respStatus == 0) {
                            appendTxt(ref rtBox, " successful", colorACK);
                        } else if (respStatus == 1) {
                            appendTxt(ref rtBox, " failed", colorErr);
                        } else if (respStatus == 2) {
                            appendTxt(ref rtBox, " canceled", colorErr);
                        } else {
                            appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus.ToString(), colorErr);
                        }
                        int transfStatus = data[8];
                        bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                    }
                    break;

                case Ver3 | DLenX | SCmd7 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        appendTxt(ref rtBox, " File data", colorInfo);
                        UInt64 transId = (UInt64)((data[7] << 8) + data[8]);
                        decodeParam(ref rtBox, "TransmissionId", transId);
                        UInt64 offset = (UInt64)((data[9] << 24) + (data[10] << 16) + (data[11] << 8) + data[12]);
                        decodeParam(ref rtBox, "Offset", offset, 8);
                        if (dataLen > 6) { // data bytes
                            string hex = Regex.Replace(BitConverter.ToString(data, 13, dataLen - 7), @"\-\S", "");
                            decodeParamStr(ref rtBox, "Data", hex);
                        }
                    }
                    break;

                case Ver0 | DLen4 | SCmd8 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        int cmdRet = data[7];
                        if (cmdRet == 1) {
                            appendTxt(ref rtBox, " File download", colorACK);
                        } else if (cmdRet == 2) {
                            appendTxt(ref rtBox, " File upload", colorErr);
                        } else {
                            appendTxt(ref rtBox, " Wrong CmdRet=" + cmdRet.ToString(), colorErr);
                        }
                        int respStatus = data[8];
                        decodeResponse(ref rtBox, respStatus, true);
                        if (respStatus == 1) {
                            int transfStatus = data[9];
                            bErr |= decodeDictParam(ref rtBox, ref FileTransferStatus, colorErr, transfStatus, true);
                        }
                    }
                    break;

                case Ver3 | DLen1 | SCmd8 | 0x37: // Notification of new feature setting ACK
                    {
                        appendTxt(ref rtBox, "Features", colorCmd);
                        appendTxt(ref rtBox, " File download ACK", colorACK);
                    }
                    break;

                case Ver0 | DLen1 | 0x60: // Voice features
                    {
                        appendTxt(ref rtBox, "Voice Features", colorCmd);
                        int voiceState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref VoiceStatus, colorParam, voiceState, false);
                    }
                    break;

                case Ver3 | DLen0 | 0x60: // Voice features
                    {
                        appendTxt(ref rtBox, "Voice Features", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen1 | 0x61: // Voice features Mute Mic
                    {
                        appendTxt(ref rtBox, "Voice Features Mute Mic", colorCmd);
                        int micState = data[6];
                        decodeParamStr(ref rtBox, "VoiceState", (micState == 0) ? "Mic on" : "Mic muted");
                    }
                    break;

                case Ver3 | DLen1 | 0x61: // Voice features Mute Mic
                    {
                        appendTxt(ref rtBox, "Voice Features Mute Mic", colorCmd);
                        int voiceCmd = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref VoiceStatusCmds, colorInfo, voiceCmd, false);
                    }
                    break;

                case Ver0 | DLen1 | 0x62: // Voice features Mute Mic
                    {
                        appendTxt(ref rtBox, "Voice Features", colorCmd);
                        UInt64 volCmd = data[6];
                        decodeParam(ref rtBox, "Volume", volCmd, 0);
                    }
                    break;

                case Ver3 | DLen1 | 0x62: // Voice features Mute Mic
                    {
                        appendTxt(ref rtBox, "Voice Features", colorCmd);
                        UInt64 volCmd = data[6];
                        if (volCmd == 0xa0) {
                            appendTxt(ref rtBox, " Query", colorCmd);
                        } else {
                            appendTxt(ref rtBox, " Set", colorSubCmd);
                            decodeParam(ref rtBox, "Volume", volCmd, 0);
                        }
                    }
                    break;

                case Ver0 | DLen1| 0x63: // Voice features Test
                    {
                        appendTxt(ref rtBox, "Voice Test", colorCmd);
                        int testState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref VoiceTestCmds, colorInfo, testState, false);
                    }
                    break;

                case Ver3 | DLen1 | 0x63: // Voice features Test
                    {
                        appendTxt(ref rtBox, "Voice Test", colorCmd);
                        int voiceCmd = data[6];
                        if (voiceCmd == 0xa0) {
                            appendTxt(ref rtBox, " Query", colorCmd);
                        } else {
                            appendTxt(ref rtBox, " Set", colorSubCmd);
                            bErr |= decodeDictParam(ref rtBox, ref VoiceTestCmds, colorInfo, voiceCmd, false);
                        }
                    }
                    break;

                case Ver0 | DLen1 | 0x64: // Test waking up voice assistant
                    {
                        appendTxt(ref rtBox, "Test Waking Up", colorCmd);
                        int testWakUp = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref TestWakingUpCmds, colorInfo, testWakUp, false);
                    }
                    break;

                case Ver3 | DLen0 | 0x64: // Test waking up voice assistant
                    {
                        appendTxt(ref rtBox, "Test Waking Up", colorCmd);
                        appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmd0 | 0x65: // Voice module play
                    {
                        appendTxt(ref rtBox, "Voice Module Play", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLenX | SCmd0 | 0x65: // Voice module play list
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        string playTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + playTxt, colorData);
                    }
                    break;

                case Ver0 | DLenX | SCmd1 | 0x65: // Voice module play list
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        string playTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + playTxt, colorData);
                    }
                    break;

                case Ver3 | DLen2 | SCmd1 | 0x65: // Voice module Play
                    {
                        appendTxt(ref rtBox, "Voice Module Play", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver0 | DLen2 | SCmd2 | 0x65: // Voice module wake up
                    {
                        appendTxt(ref rtBox, "Voice Module Wake Up", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen1 | SCmd2 | 0x65: // Voice module wake up
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        appendTxt(ref rtBox, " Wake Up", colorCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmd3 | 0x65: // Voice module ASR
                    {
                        appendTxt(ref rtBox, "Voice Module ASR", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd3 | 0x65: // Response Voice module ASR
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        int asrCmd = data[7]; // automatic speech recognition
                        bErr |= decodeDictParam(ref rtBox, ref VoiceASRCmds, colorParam, asrCmd, true);
                    }
                    break;

                case Ver0 | DLen1 | SCmd4 | 0x65: // Voice module Query play media
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        appendTxt(ref rtBox, " Query Play Media", colorSubCmd);
                    }
                    break;

                case Ver3 | DLenX | SCmd4 | 0x65: // Response Voice module play media
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        string playTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                        appendTxt(ref rtBox, " " + playTxt, colorData);
                    }
                    break;

                case Ver3 | DLen1 | SCmd5 | 0x65: // Voice module Query playlist
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        appendTxt(ref rtBox, " Query Playlist", colorSubCmd);
                    }
                    break;

                case Ver0 | DLenX | SCmd5 | 0x65: // Response Voice module query playlist
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        int respStatus = data[7];
                        if (respStatus == 0) {
                            appendTxt(ref rtBox, " Query Playlist Success", colorACK);
                        } else if (respStatus == 1) {
                            appendTxt(ref rtBox, " Query Playlist Failure", colorErr);
                        } else {
                            appendTxt(ref rtBox, " Wrong RespStatus=" + respStatus.ToString(), colorErr);
                        }
                        if (respStatus == 0 && dataLen > 2) {
                            string playTxt = Encoding.UTF8.GetString(data, 8, dataLen - 2);
                            appendTxt(ref rtBox, " " + playTxt, colorData);
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd6 | 0x65: // Voice module Call
                    {
                        appendTxt(ref rtBox, "Voice Module Call", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd6 | 0x65: // Response Voice module Call
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        int vcCmd = data[7]; // voice cmd
                        bErr |= decodeDictParam(ref rtBox, ref VoiceCallCmds, colorParam, vcCmd, true);
                    }
                    break;

                case Ver0 | DLen2 | SCmd7 | 0x65: // Voice module Start Rec
                    {
                        appendTxt(ref rtBox, "Voice Module Start Recording", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd7 | 0x65: // Response Voice module Start Rec
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        int recCmd = data[7]; // start recording
                        if (recCmd == 1) {
                            appendTxt(ref rtBox, " Start Recording", colorSubCmd);
                        } else {
                            appendTxt(ref rtBox, " Wrong recCmd=" + recCmd.ToString(), colorErr);
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd8 | 0x65: // Voice module Stop Rec
                    {
                        appendTxt(ref rtBox, "Voice Module Stop Recording", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd8 | 0x65: // Response Voice module start/stop recording
                    {
                        appendTxt(ref rtBox, "Voice Module", colorCmd);
                        int recStatus = data[7];
                        if (recStatus == 0) {
                            appendTxt(ref rtBox, " Recording stopped", colorInfo);
                        } else if (recStatus == 1) {
                            appendTxt(ref rtBox, " Recording started", colorInfo);
                        } else {
                            appendTxt(ref rtBox, " Wrong RespStatus=" + recStatus.ToString(), colorErr);
                        }
                    }
                    break;

                case Ver0 | DLen2 | SCmd9 | 0x65: // Alarm state change
                    {
                        appendTxt(ref rtBox, "Alarm state Change", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd9 | 0x65: // Alarm en-/disable
                    {
                        appendTxt(ref rtBox, "Alarm", colorCmd);
                        int alarmState = data[7];
                        if (alarmState == 0) {
                            appendTxt(ref rtBox, " disabled", colorSubCmd);
                        } else if (alarmState == 1) {
                            appendTxt(ref rtBox, " enabled", colorSubCmd);
                        } else {
                            appendTxt(ref rtBox, " Wrong RespStatus=" + alarmState.ToString(), colorErr);
                        }
                    }
                    break;

                case Ver0 | DLenX | SCmda | 0x65: // Set alarms
                    {
                        appendTxt(ref rtBox, "Set Alarms", colorCmd);
                        UInt64 numAlarms = data[7];
                        decodeParam(ref rtBox, "NumAlarms", numAlarms);
                        int opType = data[8];
                        bErr |= decodeDictParam(ref rtBox, ref RingToneOpStates, colorParam, opType, false);

                        int offset = 9; // start index to read alarms
                        // decode all alarms
                        while (bErr == false && offset < (num - 1)) {
                            UInt64 alarmId = (UInt64)(data[offset++] << 40) + (UInt64)(data[offset++] << 32) + (UInt64)(data[offset++] << 24) + (UInt64)(data[offset++] << 16) + (UInt64)(data[offset++] << 8) + data[offset++];
                            decodeParam(ref rtBox, "AlarmId", alarmId, 12);

                            int year = data[offset++] + 2000;
                            int month = data[offset++];
                            int day = data[offset++];
                            int hour = data[offset++];
                            int minute = data[offset++];
                            try {
                                DateTime date = new DateTime(year, month, day, hour, minute, 0);
                                decodeParamStr(ref rtBox, "Date", date.ToString("yy-MM-dd HH:mm"));
                            } catch {
                                appendTxt(ref rtBox, " Wrong DateTime Parameter", colorErr);
                                bErr = true;
                            }
                            int rule = data[offset++];
                            if (rule == 0) {
                                appendTxt(ref rtBox, " One-Time-Alarm", colorData);
                            } else {
                                string days = "";
                                if (0x01 == (rule & 0x01)) { days += "enabled "; } else { days += "dismissed "; }
                                if (0x02 == (rule & 0x02)) { days += "Saturday,"; }
                                if (0x04 == (rule & 0x04)) { days += "Friday,"; }
                                if (0x08 == (rule & 0x08)) { days += "Thursday,"; }
                                if (0x10 == (rule & 0x10)) { days += "Wednesday,"; }
                                if (0x20 == (rule & 0x20)) { days += "Tuesday,"; }
                                if (0x40 == (rule & 0x40)) { days += "Monday,"; }
                                if (0x80 == (rule & 0x80)) { days += "Sunday,"; }
                                decodeParamStr(ref rtBox, "Rule", days.Trim(','));
                            }

                            int tone = data[offset++];
                            if (tone == 0) {
                                appendTxt(ref rtBox, " Online ringtone", colorInfo);
                            } else {
                                appendTxt(ref rtBox, " Local ringtone", colorInfo);
                            }

                            string toneTxt = Encoding.UTF8.GetString(data, offset, 21);
                            if (toneTxt[0] == '\0')
                                toneTxt = "";
                            decodeParamStr(ref rtBox, "RingToneName", toneTxt);

                            offset += 21;
                        } // while
                        if (offset != (num - 1)) { // all eaten? => no
                            appendTxt(ref rtBox, " Wrong Alarm decoding offset=" + offset, colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case Ver3 | DLen1 | SCmda | 0x65: // Set alarms ACK
                    {
                        appendTxt(ref rtBox, "Set Alarms", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen1 | SCmdb | 0x65: // Query alarm list
                    {
                        appendTxt(ref rtBox, "Query Alarms", colorCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmdc | 0x65: // Response Voice module Turn on/off local alarm
                    {
                        appendTxt(ref rtBox, "Turn on/off local alarm", colorCmd);
                        appendTxt(ref rtBox, " ACK", colorACK);
                    }
                    break;

                case Ver3 | DLen2 | SCmdc | 0x65: // Voice module Turn on/off local alarm
                    {
                        appendTxt(ref rtBox, "Turn on/off local alarm", colorCmd);
                        int onOff = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref SwitchLocalAlarms, colorParam, onOff, false);
                    }
                    break;

                case Ver0 | DLenX | SCmdd | 0x65: // Response Change Alarm
                    {
                        appendTxt(ref rtBox, "Change alarm", colorCmd);
                        int cmdAlarm = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref SetAlarms, colorParam, cmdAlarm, false);
                        int op = data[8];
                        bErr |= decodeDictParam(ref rtBox, ref AlarmOpResponses, colorParam, op, false);
                    }
                    break;

                case Ver3 | DLenX | SCmdd | 0x65: // Change Alarm
                    {
                        appendTxt(ref rtBox, "Change alarm", colorCmd);
                        int cmdAlarm = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref SetAlarms, colorParam, cmdAlarm, false);
                        if (dataLen > 8) {
                            string alarmTxt = Encoding.UTF8.GetString(data, 8, dataLen - 1);
                            appendTxt(ref rtBox, " " + alarmTxt, colorData);
                        }
                        int offset = 9;
                        UInt64 timerId = (UInt64)(data[offset++] << 40) + (UInt64)(data[offset++] << 32) + (UInt64)(data[offset++] << 24) + (UInt64)(data[offset++] << 16) + (UInt64)(data[offset++] << 8) + data[offset++];
                        decodeParam(ref rtBox, "TimerId", timerId, 12);
                    }
                    break;

                case Ver0 | DLen2 | SCmde | 0x65: // Response Query the number of reminders
                    {
                        appendTxt(ref rtBox, "Query number of reminders", colorCmd);
                        UInt64 numReminder = data[6];
                        decodeParam(ref rtBox, "#", numReminder, 0);
                    }
                    break;

                case Ver3 | DLen1 | SCmde | 0x65: // Query the number of reminders
                    {
                        appendTxt(ref rtBox, "Query number of reminders", colorCmd);
                    }
                    break;

                case Ver0 | DLen2 | SCmdf | 0x65: // Response Send alarm data 
                    {
                        appendTxt(ref rtBox, "Send alarm data", colorCmd);
                        int op = data[7];
                        bErr |= decodeDictParam(ref rtBox, ref AlarmOpResponses, colorInfo, op, false);
                    }
                    break;

                case Ver3 | DLenX | SCmdf | 0x65: // Send alarm data 
                    {
                        appendTxt(ref rtBox, "Send alarm data", colorCmd);
                        string alarmTxt = Encoding.UTF8.GetString(data, 8, dataLen - 1);
                        appendTxt(ref rtBox, " " + alarmTxt, colorData);
                    }
                    break;

                case Ver0 | DLen2 | SCmd1 | 0x72: // Response Fan functional test
                    {
                        appendTxt(ref rtBox, "Fan Functional Test", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true);
                    }
                    break;

                case Ver3 | DLen3 | SCmd1 | 0x72: // Fan functional test
                    {
                        appendTxt(ref rtBox, "Fan Functional Test", colorCmd);
                        UInt64 fanSpeed = data[7];
                        decodeParam(ref rtBox, "Speed", fanSpeed, 0);
                        UInt64 holdTime = data[8];
                        decodeParam(ref rtBox, "Hold-Time", holdTime, 0);
                    }
                    break;

                case Ver0 | DLen2 | SCmd2 | 0x72: // Response Set duty cycle
                    {
                        appendTxt(ref rtBox, "Set Fan", colorCmd);
                        int respStatus = data[7];
                        decodeResponse(ref rtBox, respStatus, true, true);
                    }
                    break;

                case Ver3 | DLen2 | SCmd2 | 0x72: // Set duty cycle
                    {
                        appendTxt(ref rtBox, "Set Fan", colorCmd);
                        UInt64 dutyCycle = data[7];
                        decodeParam(ref rtBox, "DutyCycle", dutyCycle, 0);
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
