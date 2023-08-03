# Introduction
This UART Sniffer is in the style of TuyaMCUAnalyzer analysing bidirectional serial Tuya communication.

Tuya messages are documented here:

    McuSerPort: https://developer.tuya.com/en/docs/iot/tuya-cloud-universal-serial-port-access-protocol?id=K9hhi0xxtn9cb#protocols
                https://developer.tuya.com/en/docs/iot/weather-function-description?id=Ka6dcs2cw4avp

    McuLowPower: https://developer.tuya.com/en/docs/iot/tuyacloudlowpoweruniversalserialaccessprotocol?id=K95afs9h4tjjh

    McuHomeKit: https://developer.tuya.com/en/docs/iot/wifi-module-mcu-development-overview-for-homekit?id=Kaa8fvusmgapc

![image](https://github.com/MkMunich/SniffUART/blob/master/ScreenShots/Mcu%20Protocol%20Menu.PNG)

All specifications are fully done.
SniffUART need to be configured to decode Tuya messages of one decoder 'class' above (menu MCU Protocoll->*). This decoder is taken first. If decoding fails, then SniffUART will try the other classes. If decoding is successful, then it will output a (red) hint, which decoder class had been used.
I am assuming, that only one decoder class is valid at one time; depending on the used Tuya device.
The quality of the Tuya specification is quiete okay. Many examples were give and I'he taken them over (see "Test Mcu*.txt"). Some of the given examples were wrong. In this case I corrected them. Some message definitions did not provide an example. Then I invented my own test message.


# A Quick Guide How-To

In order to record serial communication you'll need 2 USB serial adapters. they should be wired as follows:
As here, the Tuya door contact device is battery powered and the power of the CBS3 module is controlled by MCU, you'll need to solder power wires directly to the CBS3.

CB3S Pin 8 	VCC Power supply pin (3.3V)

CB3S Pin 9 	GND Power supply reference ground


Connect the first USB serial adapter as follows:

3,3V to CB3S VCC		// so the CB3S will be powered by the first adapter

Gnd to CB3S GND

RX to CB3S RXD1 pin 15	// so this adapter will record the messages sent by CB3S to Tucy MCU (Name this port 'CB3S' in port settings)


Connect the second USB serial adapter as follows:
Gnd to CB3S GND	// needed to have a common ground!

RX to CB3S TXD1 pin 16	// so this adapter will record the messages sent by Tucy MCU to CB3S (Name this port 'MCU' in port settings)


Set up USB ports:
![image](https://github.com/MkMunich/SniffUART/blob/master/ScreenShots/Port%20Setting%20dialog.png)

Normally the CBS3 module communicates with Tuya MCU using the default settings

	9600 baud, 8 bits, Non parity, One stop bit, No handshake

SniffUART comes with reasonable default values, but they can be changed in menu File->Port settings->UART0 (Alt 0) or File->Port settings->UART1 (Alt 1)
Please chose a port name and the (correct) COM port of the USB serial adapter so that fitting with your wiring above.
If (any) setting is changed, SniffUART will ask you to save the new settings for the next run.

Start recording with File->open Ports (Alt O)

If everything above is okay, then SniffUART will listen to messages and log them in the GUI.


There are three views available:

	(Alt A): display the message in ASCII

	(Alt H): display the message as hex dump

	(Alt M): display the decoded message (this is the default setting)


![image](https://github.com/MkMunich/SniffUART/blob/master/ScreenShots/SniffUART%20dialog.png)


Dialog 'Decode Messages':

![image](https://github.com/MkMunich/SniffUART/blob/master/ScreenShots/Decode%20Messages.png)

This dialog provides a way to drop messages (as hex dump) into SniffUART. Every line must be one singel Tuya message in hex bytes.
A list of Tuya message examples (directly taken from the Tuya specification) are found in 'Test McuSerPortMsg.txt' and 'Test McuLowPowerMsg.txt'.


Save&Import decoded messages:

The decoded messages of SniffUART can be saved with File->Save as..

The decoded messages of SniffUART can be imported with File->Import..


This will allow you to persist all recording and to display it again lateron. Also, you can provide message flows including its timing to OpenBeken.

Timing:
Every decoded message will get a time stamp, when received. Timing will be persistet as well (Save as.. / Import..).
In order to measure a time delta between two messages, just select both messages and the time diff will be displayed in the bottom of the dialog.


An example of a Tuya door contact (decoder class: McuLowPower)
![image](https://github.com/MkMunich/SniffUART/blob/master/ScreenShots/SniffUART%20McuLowPower.png)


# Compiling

MSVC Community 2022 C# project is included, project is a WinForms app build with NET Framework 4.8 but should also compile with
other MSVC like 2017 and NET Framework 2.0


# Contributing

Feel free to add new features and improve the codebase. The code structure is designed to quickly add further Tuya messages. As each message has its own code section, so e.g. the name can easily be adjusted or the message decoding can easily be corrected without interfering other messages.

Many thanks to the project [OpenBeken](https://github.com/openshwprojects/OpenBK7231T_App).

