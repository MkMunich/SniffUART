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
                // Heartbeat
                case Ver0 | 0x00:
                case Ver3 | 0x00:
                    {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
                        if (num > 1) {
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

                case Ver3 | 0x01: {
                        appendTxt(ref rtBox, "Heartbeat", colorCmd);
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
