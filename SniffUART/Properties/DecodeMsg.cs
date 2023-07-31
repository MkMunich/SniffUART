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
    //             https://developer.tuya.com/en/docs/iot/weather-function-description?id=Ka6dcs2cw4avp
    // McuLowPower: https://developer.tuya.com/en/docs/iot/tuyacloudlowpoweruniversalserialaccessprotocol?id=K95afs9h4tjjh
    // McuHomeKit: https://developer.tuya.com/en/docs/iot/wifi-module-mcu-development-overview-for-homekit?id=Kaa8fvusmgapc
    internal static class DecodeMsg {
        public enum eDecoder {
            eMcuSerPort = 0,
            eMcuLowPower,
            eMcuHomeKit,
        };


        //*********************************************************************************************************************************
        //-----------------------------------------------------------------------
        // bit16-17=version, bit12-15=DataLen, bit8-11=subCmd, bit0-7=cmd
        //-----------------------------------------------------------------------
        public const int Ver0 = 0x000000;
        //public const int Ver1 = 0x010000;
        //public const int Ver2 = 0x020000;
        public const int Ver3 = 0x030000;

        public const int DLen0 = 0x00000; // dataLen == 0
        public const int DLen1 = 0x01000; // dataLen == 1
        public const int DLen2 = 0x02000; // dataLen == 2
        public const int DLen3 = 0x03000; // dataLen == 3
        public const int DLen4 = 0x04000; // dataLen == 4
        public const int DLenX = 0x05000; // dataLen > 4

        // sub-command is used to devide a command into several commands
        public const int SCmd0 = 0x0000; // subCmd == 0x00
        public const int SCmd1 = 0x0100; // subCmd == 0x01
        public const int SCmd2 = 0x0200; // subCmd == 0x02
        public const int SCmd3 = 0x0300; // subCmd == 0x03
        public const int SCmd4 = 0x0400; // subCmd == 0x04
        public const int SCmd5 = 0x0500; // subCmd == 0x05
        public const int SCmd6 = 0x0600; // subCmd == 0x06
        public const int SCmd7 = 0x0700; // subCmd == 0x07
        public const int SCmd8 = 0x0800; // subCmd == 0x08
        public const int SCmd9 = 0x0900; // subCmd == 0x09
        public const int SCmda = 0x0a00; // subCmd == 0x0a
        public const int SCmdb = 0x0b00; // subCmd == 0x0b
        public const int SCmdc = 0x0c00; // subCmd == 0x0c
        public const int SCmdd = 0x0d00; // subCmd == 0x0d
        public const int SCmde = 0x0e00; // subCmd == 0x0e
        public const int SCmdf = 0x0f00; // subCmd == 0x0f


        //*********************************************************************************************************************************
        public static Dictionary<int, string> DataTypes = new Dictionary<int, string>
        {
            { -1, "DataType" },
            { 0, "Raw" },
            { 1, "Bool" },
            { 2, "Value" },
            { 3, "String" },
            { 4, "Enum" },
            { 5, "Bitmap" },
        };

        public static Dictionary<int, string> ReportStates = new Dictionary<int, string>
        {
            { -1, "Result" },
            { 0x00, "Reported successfully" },
            { 0x01, "The current record is reported successfully, and previously saved records need to be reported" },
            { 0x02, "Failed to report" },
        };

        public static Dictionary<int, string> FWUpdateReturns = new Dictionary<int, string>
        {
            { -1, "Result" },
            { 0x00, "(Start to detect firmware upgrading) Do not power off" },
            { 0x01, "(No firmware to upgrade) Power off" },
            { 0x02, "(Upgrading the firmware) Do not power off" },
            { 0x03, "(The firmware is upgraded successfully) Power off " },
            { 0x04, "(Failed to upgrade the firmware) Power off" },
        };

        public static Dictionary<int, string> ResultFeatures = new Dictionary<int, string>
        {
            { -1, "Result" },
            { 0x00, "Success" },
            { 0x01, "Invalid Data" },
            { 0x02, "Failure" },
        };

        //*********************************************************************************************************************************
        public static bool decodeDictParam(ref RichTextBox rtBox, ref Dictionary<int, string> dict, Color col, int param, bool bHex) {
            try {
                string dictTxt = dict[param];
                if (col == colorParam) { // display as parameter
                    string name = dict[-1];
                    appendTxt(ref rtBox, " " + name + "=", colorParam);
                    appendTxt(ref rtBox, dictTxt, colorData);
                } else {
                    appendTxt(ref rtBox, " " + dictTxt, col);
                }
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

        public static void decodeParamStr(ref RichTextBox rtBox, string name, string param) {
            appendTxt(ref rtBox, " " + name + "=", colorParam);
            appendTxt(ref rtBox, param, colorData);
        }

        public static void decodeParam(ref RichTextBox rtBox, string name, UInt64 param, int iHex = 0) {
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
                case 12:
                appendTxt(ref rtBox, param.ToString("X12"), colorData);
                break;
                default:
                appendTxt(ref rtBox, param.ToString(), colorData);
                break;
            } // switch
        }

        public static void decodeResponse(ref RichTextBox rtBox, int resp, bool bInverse, bool bVerb = false) {
            if ((!bInverse && resp == 0) || (bInverse && resp == 1)) {
                appendTxt(ref rtBox, bVerb ? " failed" : " Failure", colorErr);
            } else if ((!bInverse && resp == 1) || (bInverse && resp == 0)) {
                appendTxt(ref rtBox, bVerb ? " sucessful" : " Success", colorACK);
            } else {
                appendTxt(ref rtBox, " Wrong Resp=" + resp, colorErr);
            }
        }

        // color definitions of appearance
        public static Color colorErr = Color.Red; // error
        public static Color colorCmd = Color.DarkGray; // command
        public static Color colorSubCmd = Color.Gray; // subcommand
        public static Color colorParam = Color.DarkSalmon; // parameter
        public static Color colorDP = Color.Plum; // DP unit number
        public static Color colorType = Color.Goldenrod; // DP type
        public static Color colorData = Color.Blue; // DP data
        public static Color colorInfo= Color.Orange; // information
        public static Color colorACK = Color.Green; // confirmation

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

            int dataLen = (num >= 5) ? (data[4] << 8) + data[5] : 0;

            // check min length; length without data bytes; length with data bytes
            if (num < 7 || num != dataLen + 7) {
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
            bErr |= decodeDictParam(ref rtBox, ref DataTypes, colorInfo, type, true);

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
                            appendTxt(ref rtBox, "=False", colorErr);
                        } else if (val == 1) {
                            appendTxt(ref rtBox, "=True", colorACK);
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
                        string ascii = Encoding.UTF8.GetString(data, offset + 4, len);
                        //:-( ascii = Regex.Replace(ascii, @"[^\u0000-\u007F]+", "."); // replace all non printable char by '.'
                        appendTxt(ref rtBox, "=\"" + ascii + "\"", colorData);
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

        public static bool hasSubCmd(int cmd) {
            if (cmd == 0x33 || cmd == 0x34 || cmd == 0x35 || cmd == 0x36 || cmd == 0x37 || cmd == 0x65 || cmd == 0x72) {
                return true;
            }
            return false;
        }


        // Public function to decode a single Tuya message
        // Hints: Assuming, that Tuya devices are only using messages within one of the following decoders:
        // McuSerPort, McuLowPower or McuHomeKit (refer to urls above).
        // For this, a single Tuya device should not (??really??) use messages from different decoder class.
        // One decoder class is preselected in FormMain._mcuProtocol and will be used first. If decoding of
        // that decoder class fails, then this function will give the other decoder classes a chance.
        public static RichTextBox decodeMsg(ref RichTextBox rtBox, int num, ref byte[] data) {
            eDecoder decoderClass = (eDecoder) FormMain._mcuProtocol;
            bool fstTry = false;
            switch (decoderClass) {
                case eDecoder.eMcuSerPort: {
                        fstTry = DecoderMcuSerPort.decodingMsg(decoderClass, ref rtBox, num, ref data);
                    }
                    break;
                case eDecoder.eMcuLowPower: {
                        fstTry = DecoderMcuLowPower.decodingMsg(decoderClass, ref rtBox, num, ref data);
                    } break;
                case eDecoder.eMcuHomeKit: {
                        fstTry = DecoderMcuHomeKit.decodingMsg(decoderClass, ref rtBox, num, ref data);
                    }
                    break;

                default: {
                    } break;
            } // switch

            // no errors in first decoding
            if (! fstTry) { return rtBox; };

            // save first output
            RichTextBox fstResult = new RichTextBox();
            fstResult.Rtf = rtBox.Rtf;

            // try the other decoder 'classes'
            if (decoderClass != eDecoder.eMcuSerPort) {
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuSerPort.ToString(), colorErr);
                bool nxtTry = DecoderMcuSerPort.decodingMsg(eDecoder.eMcuSerPort, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }
            if (decoderClass != eDecoder.eMcuLowPower) { // try McuLowPower
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuLowPower.ToString(), colorErr);
                bool nxtTry = DecoderMcuLowPower.decodingMsg(eDecoder.eMcuLowPower, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }
            if (decoderClass != eDecoder.eMcuHomeKit) { // try McuHomeKit
                rtBox.Clear(); // delete previous output
                appendTxt(ref rtBox, "Decoder=" + eDecoder.eMcuHomeKit.ToString(), colorErr);
                bool nxtTry = DecoderMcuHomeKit.decodingMsg(eDecoder.eMcuHomeKit, ref rtBox, num, ref data);
                if (!nxtTry) { return rtBox; };
            }

            rtBox.Rtf = fstResult.Rtf; // return first output
            return rtBox;
        }

    }
}
