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
    internal static class DecoderMcuHomeKit {
        // refer to Tuya Serial Port Protocol
        // McuHomeKit: https://developer.tuya.com/en/docs/iot/wifi-module-mcu-development-overview-for-homekit?id=Kaa8fvusmgapc
        //
        // Assuming, that a device type is either McuSerPort, McuLowPower or McuHomeKit. This means, that a device just
        // communicates using its decoder class.

        public static Dictionary<int, string> NetworkTuyaConnectionStates = new Dictionary<int, string>
        {
            { -1, "Tuya ConnectionState" },
            { 0x00, "Waiting for connection" },
            { 0x01, "Wi-Fi network has been set up but not connected to the router" },
            { 0x02, "Wi-Fi module has been connected to the router but not to the cloud" },
            { 0x03, "Wi-Fi module has been connected to the cloud" },
        };

        public static Dictionary<int, string> NetworkHomeKitConnectionStates = new Dictionary<int, string>
        {
            { -1, "Homekit ConnectionState" },
            { 0x00, "To be bound or being bound" },
            { 0x01, "Not connected" },
            { 0x02, "Connected" },
            { 0x03, "A prompt for HomeKit connection, which is fire-and-forget" },
        };


        //*********************************************************************************************************************************
        // Internal function to decode a single message
        public static bool decodingMsg(eDecoder dec, ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            // cmd is either the first byte or cmd from Frame message or -1 (means unknown)
            int ver = (num == 1) ? 0 : (num >= 4) ? data[2] : -1;
            int cmd = (num == 1) ? data[0] : (num >= 4) ? data[3] : -1;
            int dataLen = (num > 5) ? (data[4] << 8) + data[5] : 0;

            //-----------------------------------------------------------------------
            // bit12-15=DataLen, bit8-11=version, bit0-7=cmd
            //-----------------------------------------------------------------------
            const int Ver0 = 0x0000;
            //const int Ver1 = 0x0100;
            //const int Ver2 = 0x0200;
            const int Ver3 = 0x0300;

            const int DLen0 = 0x00000; // dataLen == 0
            const int DLen1 = 0x01000; // dataLen == 1
            const int DLen2 = 0x02000; // dataLen == 2
            const int DLen3 = 0x03000; // dataLen == 3
            const int DLen4 = 0x04000; // dataLen == 4
            const int DLenX = 0x05000; // dataLen > 4
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

            int sw = (ver << 8) + dlSw + cmd;
            switch (sw) {
                case Ver0 | DLen0 | 0x00: // Heartbeat
                case Ver0 | DLen1 | 0x00: // Heartbeat
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
                            int startState = data[6];
                            appendTxt(ref rtBox, (startState == 0) ? "MCU restarting" : "MCU running", colorData);
                        }
                    }
                    break;

                case Ver3 | DLen0 | 0x00: // Heartbeat
                case Ver3 | DLen1 | 0x00: // MCU restarting/running
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
                            int startState = data[6];
                            appendTxt(ref rtBox, (startState == 0) ? "MCU restarting" : "MCU running", colorInfo);
                        }
                    }
                    break;

                case Ver0 | DLen0 | 0x01: // Query product information
                    {
                        appendTxt(ref rtBox, "Query Product Information", colorCmd);
                    }
                    break;

                case Ver0 | DLenX | 0x01: // Response product information
                    {
                        appendTxt(ref rtBox, "Product Information", colorCmd);
                        string prodTxt = Encoding.UTF8.GetString(data, 6, dataLen);
                        appendTxt(ref rtBox, " " + prodTxt, colorData);
                    }
                    break;

                case Ver0 | DLen0 | 0x02: // Query the MCU and set the working mode of the module
                    {
                        appendTxt(ref rtBox, "Query MCU", colorCmd);
                    }
                    break;

                case Ver3 | DLen0 | 0x02: // Response Query MCU
                case Ver3 | DLen3 | 0x02: // Response Query MCU
                    {
                        appendTxt(ref rtBox, "Query MCU", colorCmd);
                        if (dataLen == 0) {
                            appendTxt(ref rtBox, "The module works with the MCU to process network events", colorInfo);
                        } else {
                            UInt64 netTuyaStatus = data[6];
                            decodeParam(ref rtBox, "GPIO pin Tuya NetworkStatus", netTuyaStatus);
                            UInt64 netKitStatus = data[6];
                            decodeParam(ref rtBox, "GPIO pin HomeKit NetworkStatus", netKitStatus);
                            UInt64 resetNet = data[6];
                            decodeParam(ref rtBox, "GPIO pin Reset Network", resetNet);
                        }
                    }
                    break;

                case Ver0 | DLen2 | 0x03: // Report network connection status
                    {
                        appendTxt(ref rtBox, "Report Network Connection Status", colorCmd);
                        int netTuyaConState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref NetworkTuyaConnectionStates, colorParam, netTuyaConState, true);
                        int netKitConState = data[6];
                        bErr |= decodeDictParam(ref rtBox, ref NetworkHomeKitConnectionStates, colorParam, netKitConState, true);
                    }
                    break;

                case Ver0 | DLen0 | 0x04: // Reset Wi-Fi connection
                case Ver3 | DLen0 | 0x04: // Reset Wi-Fi connection
                    {
                        appendTxt(ref rtBox, "Reset Wi-Fi connection", colorCmd);
                        if (ver == 0)
                            appendTxt(ref rtBox, " ACK", colorACK);
                        else
                            appendTxt(ref rtBox, " Cmd", colorSubCmd);
                    }
                    break;

                case Ver0 | DLenX | 0x06: // Send commands
                    {
                        appendTxt(ref rtBox, "Send Command", colorCmd);
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
                    break;

                case Ver3 | DLenX | 0x07: // Report Status
                    {
                        appendTxt(ref rtBox, "Report Status", colorCmd);
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
                    break;

                case Ver0 | DLen0 | 0x08: // Query DP Status
                    {
                        appendTxt(ref rtBox, "Query DP Status", colorCmd);
                    }
                    break;

                case Ver0 | DLen4 | 0x0a: // Start OTA update
                    {
                        appendTxt(ref rtBox, "Start OTA update", colorCmd);
                        UInt64 packSize = (UInt64)((data[6] << 24) + (data[7] << 16) + (data[8] << 8) + data[9]);
                        decodeParam(ref rtBox, "Size", packSize, 8);
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
