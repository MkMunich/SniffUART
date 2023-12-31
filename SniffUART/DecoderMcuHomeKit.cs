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
            { -1, "ConnectionState" },
            { 0x00, "To be bound or being bound" },
            { 0x01, "Not connected" },
            { 0x02, "Connected" },
            { 0x03, "A prompt for HomeKit connection, which is fire-and-forget" },
        };

        public static Dictionary<int, string> HomeKitDataTypes = new Dictionary<int, string>
         {
            { -1, "DataType" },
            { 0, "Bool" },
            { 1, "UInt" },
            { 2, "Int" },
            { 3, "UInt64" },
            { 4, "Float" },
            { 5, "String" },
            { 6, "Raw" },
        };

        public static Dictionary<int, string> HomeKitValidValuesCharacteristics = new Dictionary<int, string>
        {
            { -1, "Response" },
            { 0x00, "Success" },
            { 0x01, "service_serial not found" },
            { 0x02, "char_str  not found" },
            { 0x03, "val_type  not found" },
            { 0x04, "valid_val  not found" },
            { 0x05, "Incorrect parameter" },
            { 0x06, "Configuration is not available currently" },
            { 0x07, "Failed to process data" },
        };

        public static Dictionary<int, string> HomeKitFeatureOTA = new Dictionary<int, string>
        {
            { -1, "OTA" },
            { 0x00, "MCU has a scratchpad" },
            { 0x01, "MCU does not have a scratchpad" },
        };

        public static Dictionary<int, string> HomeKitResponseNewFeatures = new Dictionary<int, string>
        {
            { -1, "Response" },
            { 0x00, "success" },
            { 0x01, "invalid content" },
            { 0x02, "failure" },
        };

        // Appendix 1: HomeKit accessory list
        public static Dictionary<int, string> HomeKitAccessories = new Dictionary<int, string>
        {
            { -1, "Homekit Accessory" },
            { 2, "Bridges" },
            { 3, "Fans" },
            { 4, "Garage Door Openers" },
            { 5, "Lighting" },
            { 6, "Locks" },
            { 7, "Outlets" },
            { 8, "Switches" },
            { 9, "Thermostats" },
            { 10, "Sensors" },
            { 11, "Security Systems" },
            { 12, "Doors" },
            { 13, "Windows" },
            { 14, "Window Coverings" },
            { 15, "Programmable Switches" },
            { 16, "Reserved" },
            { 17, "IP Cameras" },
            { 18, "Video Doorbells" },
            { 19, "Air Purifiers" },
            { 20, "Heaters" },
            { 21, "Air Conditioners" },
            { 22, "Humidifiers" },
            { 23, "Dehumidifiers" },
            { 28, "Sprinklers" },
            { 29, "Faucets" },
            { 30, "Shower Systems" },
        };

        // Appendix 2: HomeKit service list
        public static Dictionary<int, string> HomeKitServices = new Dictionary<int, string>
        {
            { -1, "Service" },
            { 0x3e, "Accessory Information" },
            { 0xa2, "HAP Protocol Information" },
            { 0x41, "Garage Door Opener" },
            { 0x43, "Light Bulb" },
            { 0x44, "Lock Management" },
            { 0x45, "Lock Mechanism" },
            { 0x49, "Switch" },
            { 0x47, "Outlet" },
            { 0x4a, "Thermostat" },
            { 0x8d, "Air Quality Sensor" },
            { 0x7e, "Security System" },
            { 0x7f, "Carbon Monoxide Sensor" },
            { 0x80, "Contact Sensor" },
            { 0x81, "Door" },
            { 0x82, "Humidity Sensor" },
            { 0x83, "Leak Sensor" },
            { 0x84, "Light Sensor" },
            { 0x85, "Motion Sensor" },
            { 0x86, "Occupancy Sensor" },
            { 0x87, "Smoke Sensor" },
            { 0x89, "Stateless Programmable Switch" },
            { 0x8a, "Temperature Sensor" },
            { 0x8b, "Window" },
            { 0x8c, "Window Covering" },
            { 0x96, "Battery Service" },
            { 0x97, "Carbon Dioxide Sensor" },
            { 0xb7, "Fan" },
            { 0xb9, "Stat" },
            { 0xba, "Filter Maintenance" },
            { 0xbb, "Air Purifier" },
            { 0xbc, "Heater Cooler" },
            { 0xbd, "Humidifier Dehumidifier" },
            { 0xcc, "Service Label" },
            { 0xcf, "Irrigation System" },
            { 0xd0, "Valve" },
            { 0xd7, "Faucet" },
        };

        // Appendix 3: HomeKit characteristics
        public static Dictionary<int, string> HomeKitCharacteristics = new Dictionary<int, string>
        {
            { -1, "Characteristic" },
            { 0x01, "Administrator Only Access" },
            { 0x08, "Brightness" },
            { 0x0d, "Cooling Threshold Temperature" },
            { 0x0e, "Current Door State" },
            { 0x0f, "Current Heating/Cooling State" },
            { 0x10, "Current Relative Humidity" },
            { 0x11, "Current Temperature" },
            { 0x52, "Firmware Revision" },
            { 0x53, "Hardware Revision" },
            { 0x12, "Heating Threshold Temperature" },
            { 0x13, "Hue" },
            { 0x14, "Identify" },
            { 0x19, "Lock Control Point" },
            { 0x1d, "Lock Current State" },
            { 0x1c, "Lock Last Known Action" },
            { 0x1a, "Lock Management Auto Security Timeout" },
            { 0x1e, "Lock Target State" },
            { 0x1f, "Logs" },
            { 0x20, "Manufacturer" },
            { 0x21, "Model" },
            { 0x22, "Motion Detected" },
            { 0x23, "Name" },
            { 0x24, "Obstruction Detected" },
            { 0x25, "On" },
            { 0x26, "Outlet In Use" },
            { 0x28, "Rotation Direction" },
            { 0x29, "Rotation Speed" },
            { 0x2f, "Saturation" },
            { 0x30, "Serial Number" },
            { 0x32, "Target Door State" },
            { 0x33, "Target Heating Cooling State" },
            { 0x34, "Target Relative Humidity" },
            { 0x35, "Target Temperature" },
            { 0x36, "Temperature Display Units" },
            { 0x37, "Version" },
            { 0x64, "Air Particulate Density" },
            { 0x65, "Air Particulate Size" },
            { 0x66, "Security System Current State" },
            { 0x67, "Security System Target State" },
            { 0x68, "Battery Level" },
            { 0x69, "Carbon Dioxide Detected" },
            { 0x6a, "Contact Sensor State" },
            { 0x6b, "Current Ambient Light Level" },
            { 0x6c, "Current Horizontal Tilt Angle" },
            { 0x6d, "Current Position" },
            { 0x6e, "Current Vertical Tilt Angle" },
            { 0x6f, "Hold Position" },
            { 0x70, "Leak Detected" },
            { 0x71, "Occupancy Detected" },
            { 0x72, "Position State \t" },
            { 0x73, "Programmable Switch Event" },
            { 0x75, "Status Active" },
            { 0x76, "Smoke Detected" },
            { 0x77, "Status Fault" },
            { 0x78, "Status Jammed" },
            { 0x79, "Status Low Battery" },
            { 0x7a, "Status Tampered" },
            { 0x7b, "Target Horizontal Tilt Angle" },
            { 0x7c, "Target Position" },
            { 0x7d, "Target Vertical Tilt Angle" },
            { 0x8e, "Security System Alarm Type" },
            { 0x8f, "Charging State" },
            { 0x90, "Carbon Monoxide Level" },
            { 0x91, "Carbon Monoxide Peak Level" },
            { 0x92, "Carbon Dioxide Detected" },
            { 0x93, "Carbon Dioxide Level" },
            { 0x94, "Carbon Dioxide Peak Level" },
            { 0x95, "Air Quality" },
            { 0xa6, "Accessory Flags" },
            { 0xa7, "Lock Physical Controls" },
            { 0xa9, "Current Air Purifier State" },
            { 0xaa, "Current Slat State" },
            { 0xab, "Filter Life Level" },
            { 0xac, "Filter Change Indication" },
            { 0xad, "Reset Filter Indication \t" },
            { 0xa8, "Target Air Purifier State" },
            { 0xaf, "Current Fan State" },
            { 0xb0, "Active" },
            { 0xb1, "Current Heater Cooler State" },
            { 0xb2, "Target Heater Cooler State" },
            { 0xb3, "Current Humidifier Dehumidifier State" },
            { 0xb4, "Target Humidifier Dehumidifier State" },
            { 0xb5, "Water Level" },
            { 0xb6, "Swing Mode" },
            { 0xbf, "Target Fan State" },
            { 0xc0, "Slat Type" },
            { 0xc1, "Current Tilt Angle" },
            { 0xc2, "Target Tilt Angle" },
            { 0xc3, "Ozone Density" },
            { 0xc4, "Nitrogen Dioxide Density" },
            { 0xc5, "Sulphur Dioxide Density" },
            { 0xc6, "PM2.5 Density" },
            { 0xc7, "PM10 Density" },
            { 0xc8, "VOC Density" },
            { 0xc9, "Relative Humidity Dehumidifier Threshold" },
            { 0xca, "Relative Humidity Humidifier Threshold" },
            { 0xcb, "Service Label Index" },
            { 0xcd, "Service Label Namespace" },
            { 0xce, "Color Temperature" },
            { 0xd1, "Program Mode" },
            { 0xd2, "In Use" },
            { 0xd3, "Set Duration" },
            { 0xd4, "Remaining Duration" },
            { 0xd5, "Valve Type" },
            { 0xd6, "Is Configured" },
            { 0x220, "Product Data" },
        };

        // returns true, if an error is detected
        public static bool decodeHomeKitStatusDataUnits(ref RichTextBox rtBox, eDecoder dec, int num, ref int offset, ref Byte[] data) {
            bool bErr = false; // true, if error detected

            if (offset + 5 > (num - 1)) { // testing the minimum # bytes of a DP unit part failed
                appendTxt(ref rtBox, " Wrong DataUnit (num=" + num + " offset=" + offset + ")", colorErr);
                return true;
            }

            UInt64 srvNo = (UInt64)data[offset++];
            decodeParam(ref rtBox, "SrvSerNo", srvNo);

            int caracteristicsLen = data[offset++];
            string caracteristicHex = Encoding.UTF8.GetString(data, offset, caracteristicsLen);
            offset += caracteristicsLen;
            int caracteristicId = Convert.ToInt32(caracteristicHex, 16);
            decodeDictParam(ref rtBox, ref HomeKitCharacteristics, colorParam, caracteristicId, true);

            int type = data[offset + 1];
            bErr |= decodeDictParam(ref rtBox, ref HomeKitDataTypes, colorInfo, type, true);

            int len = (data[offset + 2] << 8) + data[offset + 3];
            if (len == 0 || offset + 5 + len > num) { // testing the effective # bytes of a DP unit part failed
                appendTxt(ref rtBox, " Wrong DataUnitLength (num=" + num + " len=" + len + ")", colorErr);
                return true;
            }

            switch (type) {
                case 0: { // Bool
                        int val = data[offset + 4];
                        if (val == 0) {
                            appendTxt(ref rtBox, "=False", colorErr);
                        } else if (val == 1) {
                            appendTxt(ref rtBox, "=True", colorACK);
                        } else {
                            appendTxt(ref rtBox, " Wrong Bool=" + val.ToString(), colorErr);
                            bErr = true;
                        }
                    }
                    break;

                case 1: { // (uint) Value
                        int val = (data[offset + 4] << 24) + (data[offset + 5] << 16) + (data[offset + 6] << 8) + data[offset + 7];
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    }
                    break;

                case 2: { // (int) Value
                        int val = (data[offset + 4] << 8) + data[offset + 5];
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    }
                    break;

                case 3: { // (uint64) Value
                        UInt64 val = (UInt64)((data[offset + 4] << 56)) + (UInt64)((data[offset + 5] << 48)) + (UInt64)((data[offset + 6] << 40)) + (UInt64)(data[offset + 7] << 32) +
                                     (UInt64)((data[offset + 9] << 24)) + (UInt64)((data[offset + 10] << 16)) + (UInt64)((data[offset + 11] << 8)) + (UInt64)(data[offset + 12]);
                        appendTxt(ref rtBox, "=" + val.ToString(), colorData);
                    }
                    break;

                case 4: { // float
                        int beforeCom = (data[offset + 4] << 8) + data[offset + 5];
                        int afterCom = (data[offset + 6] << 8) + data[offset + 7];
                        appendTxt(ref rtBox, "=" + beforeCom.ToString() + '.' + afterCom.ToString(), colorData);
                    }
                    break;

                case 5: { // String
                        string str = Encoding.UTF8.GetString(data, offset + 4, len);
                        //:-( ascii = Regex.Replace(ascii, @"[^\u0000-\u007F]+", "."); // replace all non printable char by '.'
                        appendTxt(ref rtBox, "=\"" + str + "\"", colorData);
                    }
                    break;

                case 6: { // Raw
                        string hex = BitConverter.ToString(data, offset + 4, len).Replace('-', ' ');
                        appendTxt(ref rtBox, "=" + hex, colorData);
                    }
                    break;

                default: {
                        appendTxt(ref rtBox, " Wrong UnitType=" + type.ToString("X2"), colorErr);
                        bErr = true;
                    }
                    break;
            } // switch

            // adjust offset: len bytes had been processed (min length of DP unit + # data bytes)
            offset += 4 + len;
            return bErr;
        }


        //*********************************************************************************************************************************
        // Internal function to decode a single message
        public static bool decodingMsg(eDecoder dec, ref RichTextBox rtBox, int num, ref byte[] data) {
            bool bErr = false; // true, if error detected

            try {
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
                    case Ver0 | DLen0 | 0x00: // Heartbeat
                    case Ver0 | DLen1 | 0x00: // Heartbeat
                    case Ver3 | DLen0 | 0x00: // Heartbeat
                    case Ver3 | DLen1 | 0x00: // MCU restarting/running
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen0 | 0x01: // Query product information
                    case Ver0 | DLenX | 0x01: // Response product information
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen0 | 0x02: // Query the MCU and set the working mode of the module
                    case Ver3 | DLen0 | 0x02: // Response Query MCU working mode
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver3 | DLen3 | 0x02: // Response Query MCU working mode
                        {
                            appendTxt(ref rtBox, "MCU Working Mode GPIO pins", colorCmd);
                            UInt64 netTuyaStatus = data[6];
                            decodeParam(ref rtBox, " Tuya NetworkStatus", netTuyaStatus);
                            UInt64 netKitStatus = data[6];
                            decodeParam(ref rtBox, " HomeKit NetworkStatus", netKitStatus);
                            UInt64 resetNet = data[6];
                            decodeParam(ref rtBox, " Reset Network", resetNet);
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
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLenX | 0x06: // Send commands
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver3 | DLenX | 0x07: // Report Status
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen0 | 0x08: // Query DP Status
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen4 | 0x0a: // Start OTA update
                    case Ver3 | DLen1 | 0x0a: // Start OTA update
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen4 | 0x0b: // Transmit update package (last packet)
                    case Ver0 | DLenX | 0x0b: // Transmit update package
                    case Ver3 | DLen0 | 0x0b: // ACK Transmit update package
                        {

                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLenX | 0x0c: // Response Get system time in GMT
                    case Ver3 | DLen0 | 0x0c: // Get system time in GMT
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLenX | 0x1c: // Response Local time
                    case Ver3 | DLen0 | 0x1c: // Get Local time
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen2 | 0x0e: // Response Test Wi-Fi functionality
                    case Ver3 | DLen0 | 0x0e: // Cmd Test Wi-Fi functionality
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen4 | 0x0f: // Response Get module’s memory
                    case Ver3 | DLen0 | 0x0f: // Get module’s memory
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen1 | 0x24: // Response Get Wi-Fi signal strength
                    case Ver3 | DLen0 | 0x24: // Get Wi-Fi signal strength
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen0 | 0x25: // ACK Disable heartbeats
                    case Ver3 | DLen0 | 0x25: // Cmd Disable heartbeats
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen1 | 0x2b: // Response Get the current Wi-Fi status
                    case Ver3 | DLen0 | 0x2b: // Get the current Wi-Fi status
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLenX | 0x2d: // Reponse Get module’s MAC address
                    case Ver3 | DLen0 | 0x2d: // Get module’s MAC address
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLenX | SCmd7 | 0x34: // Response Get information about Wi-Fi module
                    case Ver3 | DLen2 | SCmd7 | 0x34: // Get information about Wi-Fi module
                    case Ver3 | DLen3 | SCmd7 | 0x34:
                    case Ver3 | DLen4 | SCmd7 | 0x34:
                    case Ver3 | DLenX | SCmd7 | 0x34:
                        {
                            bErr |= DecoderMcuSerPort.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                        }
                        break;

                    case Ver0 | DLen1 | SCmd1 | 0x36: // Query HomeKit service configuration
                        {
                            appendTxt(ref rtBox, "Query HomeKit service configuration", colorCmd);
                        }
                        break;

                    case Ver3 | DLen1 | SCmd1 | 0x36: // ACK Query HomeKit service configuration
                        {
                            appendTxt(ref rtBox, "Query HomeKit service configuration", colorCmd);
                            appendTxt(ref rtBox, " ACK", colorACK);
                        }
                        break;

                    case Ver0 | DLen3 | SCmd2 | 0x36: // Response Request HomeKit service configuration
                    case Ver0 | DLen4 | SCmd2 | 0x36:
                    case Ver0 | DLenX | SCmd2 | 0x36:
                        {
                            appendTxt(ref rtBox, "Response HomeKit service configuration", colorCmd);
                            int resultDataProcessing = data[7];
                            decodeResponse(ref rtBox, resultDataProcessing, true, true);
                            if (resultDataProcessing == 0) {
                                // decode all service responses
                                int offset = 8; // start index to read DP units
                                while (bErr == false && offset < (num - 1)) {
                                    decodeParam(ref rtBox, "SrvSerNo", (UInt64)(offset - 7));
                                    int respStatus = data[offset++];
                                    decodeResponse(ref rtBox, respStatus, true, true);
                                } // while
                            }
                        }
                        break;

                    case Ver3 | DLenX | SCmd2 | 0x36: // Request HomeKit service configuration
                        {
                            appendTxt(ref rtBox, "Request HomeKit service configuration", colorCmd);
                            UInt64 srvNo = (UInt64)data[7];
                            decodeParam(ref rtBox, "StartSrvSerNo", srvNo);

                            // decode all service configurations
                            int offset = 8; // start index to read HomeKit service configurations
                            while (bErr == false && offset < (num - 1)) {
                                srvNo = (UInt64)data[offset++];
                                decodeParam(ref rtBox, "SrvSerNo", srvNo);
                                int srvLen = data[offset++];
                                string ascii = Encoding.UTF8.GetString(data, offset, srvLen);
                                offset += srvLen;
                                int srvId = Convert.ToInt32(ascii, 16);
                                decodeDictParam(ref rtBox, ref HomeKitServices, colorParam, srvId, true);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong HomeKit Srv configuration offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                        break;

                    case Ver0 | DLen2 | SCmd3 | 0x36: // Send HomeKit commands
                    case Ver0 | DLen3 | SCmd3 | 0x36:
                    case Ver0 | DLen4 | SCmd3 | 0x36:
                    case Ver0 | DLenX | SCmd3 | 0x36:
                        {
                            appendTxt(ref rtBox, "Send HomeKit", colorCmd);
                            appendTxt(ref rtBox, " Cmd", colorSubCmd);

                            // decode all HomeKit Data units
                            int offset = 7; // start index to read HomeKit data units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeHomeKitStatusDataUnits(ref rtBox, dec, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong HomeKit Data Unit decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                        break;

                    case Ver3 | DLen2 | SCmd4 | 0x36: // Report HomeKit status
                    case Ver3 | DLen3 | SCmd4 | 0x36:
                    case Ver3 | DLen4 | SCmd4 | 0x36:
                    case Ver3 | DLenX | SCmd4 | 0x36:
                        {
                            appendTxt(ref rtBox, "Report HomeKit status", colorCmd);

                            // decode all HomeKit Data units
                            int offset = 7; // start index to read HomeKit data units
                            while (bErr == false && offset < (num - 1)) {
                                bErr |= decodeHomeKitStatusDataUnits(ref rtBox, dec, num, ref offset, ref data);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong HomeKit Data Unit decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                        break;

                    case Ver3 | DLen3 | SCmd5 | 0x36: // Optional characteristic configuration
                    case Ver3 | DLen4 | SCmd5 | 0x36:
                    case Ver3 | DLenX | SCmd5 | 0x36:
                        {
                            appendTxt(ref rtBox, "Optional Homekit Characteristic Configuration", colorCmd);
                            UInt64 srvNo = (UInt64)data[7];
                            decodeParam(ref rtBox, "SrvSerNo", srvNo);

                            // decode all HomeKit Data units
                            int offset = 8; // start index to read HomeKit data units
                            while (bErr == false && offset < (num - 1)) {
                                int characteristicLen = data[offset++];
                                string ascii = Encoding.UTF8.GetString(data, offset, characteristicLen);
                                offset += characteristicLen;
                                int characteristicId = Convert.ToInt32(ascii, 16);
                                decodeDictParam(ref rtBox, ref HomeKitCharacteristics, colorParam, characteristicId, true);
                            } // while
                            if (offset != (num - 1)) { // all eaten? => no
                                appendTxt(ref rtBox, " Wrong HomeKit characteristics decoding offset=" + offset, colorErr);
                                bErr = true;
                            }
                        }
                        break;

                    case Ver0 | DLen3 | SCmd5 | 0x36: // Response Optional Homekit Characteristic Configuration
                    case Ver0 | DLen4 | SCmd5 | 0x36:
                    case Ver0 | DLenX | SCmd5 | 0x36:
                        {
                            appendTxt(ref rtBox, "Response Optional HomeKit Characteristic Configuration", colorCmd);
                            int resultDataProcessing = data[7];
                            decodeResponse(ref rtBox, resultDataProcessing, true, true);
                            if (resultDataProcessing == 0) {
                                // decode all service responses
                                int offset = 8; // start index to read DP units
                                while (bErr == false && offset < (num - 1)) {
                                    decodeParam(ref rtBox, "No", (UInt64)(offset - 7));
                                    int respStatus = data[offset++];
                                    decodeResponse(ref rtBox, respStatus, true, true);
                                } // while
                            }
                        }
                        break;

                    case Ver0 | DLen2 | SCmd6 | 0x36: // Response Valid values of characteristic (optional)
                        {
                            appendTxt(ref rtBox, "Valid Values of Characteristic", colorCmd);
                            int resp = data[7];
                            decodeDictParam(ref rtBox, ref HomeKitValidValuesCharacteristics, colorParam, resp, true);
                        }
                        break;

                    case Ver3 | DLenX | SCmd6 | 0x36: // Valid values of characteristic (optional)
                        {
                            appendTxt(ref rtBox, "Valid Values of Characteristic", colorCmd);
                            string characteristicTxt = Encoding.UTF8.GetString(data, 7, dataLen);
                            appendTxt(ref rtBox, " " + characteristicTxt, colorData);
                        }
                        break;

                    case Ver0 | DLen2 | SCmd0 | 0x37: // Response Notify new feature configuration
                        {
                            appendTxt(ref rtBox, "Notify new feature configuration", colorCmd);
                            int resp = data[7];
                            decodeDictParam(ref rtBox, ref HomeKitResponseNewFeatures, colorParam, resp, true);
                        }
                        break;

                    case Ver3 | DLenX | SCmd0 | 0x37: // Notify new feature configuration
                        {
                            appendTxt(ref rtBox, "Notify new feature configuration", colorCmd);
                            string newFeaturesTxt = Encoding.UTF8.GetString(data, 7, dataLen - 1);
                            appendTxt(ref rtBox, " " + newFeaturesTxt, colorData);

                            int idx = newFeaturesTxt.IndexOf("\"mcu_ota\":") + 10;
                            if (idx > 9) {
                                int mcuOta;
                                string subStr = newFeaturesTxt.Substring(idx, newFeaturesTxt.IndexOfAny(separators, idx) - idx);
                                if (Int32.TryParse(subStr, out mcuOta)) {
                                    decodeDictParam(ref rtBox, ref HomeKitFeatureOTA, colorParam, mcuOta, true);
                                }
                            }
                        }
                        break;


                    default: {
                            appendTxt(ref rtBox, "Unknown DLen+Version+Cmd=0x" + sw.ToString("X6"), colorErr);
                            bErr = true;
                        }
                        break;
                } // switch
            } catch { bErr = true; }

            if (bErr) { // if an error had been detected, then print all msg bytes in hex
                string hex = (num > 0) ? BitConverter.ToString(data, 0, num).Replace('-', ' ') : "-";
                appendTxt(ref rtBox, " Msg=", colorData);
                appendTxt(ref rtBox, hex, colorErr);
            }

            return bErr;
        }
    }
}
