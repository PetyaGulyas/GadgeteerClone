﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) GHI Electronics, LLC.
//  Contributions from TinyCLR community members ianlee74 and dobova.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using Gadgeteer.Interfaces;
using Microsoft.SPOT;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;

namespace Gadgeteer.Modules.GHIElectronics
{
    // -- CHANGE FOR MICRO FRAMEWORK 4.2 --
    // If you want to use Serial, SPI, or DaisyLink (which includes GTI.SoftwareI2C), you must do a few more steps
    // since these have been moved to separate assemblies for NETMF 4.2 (to reduce the minimum memory footprint of Gadgeteer)
    // 1) add a reference to the assembly (named Gadgeteer.[interfacename])
    // 2) in GadgeteerHardware.xml, uncomment the lines under <Assemblies> so that end user apps using this module also add a reference.

    /// <summary>
    /// A IO60P16 module for Microsoft .NET Gadgeteer
    /// </summary>
    /// <example>
    /// <para>Since this module has complex features and usage, please see the online documentation and tutorials for this module found on the manufacturer's website.</para>
    /// </example>
    public class IO60P16 : GTM.Module
    {
        private GTI.SoftwareI2C softwareI2C;

        private const byte DEV_ADDR = 0x0020;
        private const int I2C_CLOCKRATE = 400; // KHz

        #region Registers
        const byte INPUT_PORT_0_REGISTER = 0x00;
        const byte OUTPUT_PORT_0_REGISTER = 0x08;
        const byte PORT_SELECT_REGISTER = 0x18;
        const byte PORT_DIRECTION_REGISTER = 0x1c;
        const byte COMMAND_REGISTER = 0x30;
        const byte ENABLE_PWM_REGISTER = 0x1A;
        const byte SELECT_CLOCK_SRC = 0x29;
        const byte PERIOD_REGISTER = 0x2A;
        const byte PULSE_WIDTH_REGISTER = 0x2B;
        const byte DIVIDER_REGISTER = 0x2C;
        #endregion

        private byte[] port_reserve = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        public IO60P16(int socketNumber)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            socket.EnsureTypeIsSupported(new char[] { 'X', 'Y' }, this);

            softwareI2C = new SoftwareI2C(socket, Socket.Pin.Five, Socket.Pin.Four, this);
            //softwareI2C = new SoftwareI2C(socket, Socket.Pin.Eight, Socket.Pin.Nine, this);

            Reset();
        }

        /// <summary>
        /// Resets the module.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < 8; i++)
            {
                int max_pins = 8;

                if (i == 2)
                    max_pins = 4; // port 2 has only 4 pins

                for (int j = 0; j < max_pins; j++)
                {
                    ReleasePin((byte)i, (byte)j);
                }
            }
        }

        private void WriteRegister(byte reg, byte value)
        {
            byte[] data = new byte[] { reg, value };

            int send = softwareI2C.Write(DEV_ADDR, data);
        }

        private byte ReadRegister(byte reg)
        {
            byte[] data = new byte[1];
            //int numwrite = 0;
            //int numread = 0;

            //softwareI2C.WriteRead(new byte[] { reg }, 0, 1, data, 0, data.Length, out numwrite, out numread);
            softwareI2C.WriteRead(DEV_ADDR, new byte[] { reg }, data);

            return data[0];
        }

        private void WritePort(byte port, byte value)
        {
            // Write data start from register 0x08
            WriteRegister((byte)(OUTPUT_PORT_0_REGISTER + port), value);
        }

        private byte ReadPort(byte port)
        {
            // Read data start from register 0x00
            return ReadRegister((byte)(INPUT_PORT_0_REGISTER + port));
        }

        private void ReleasePin(byte port, byte pin)
        {
            //MakePinOutput((byte)port, (byte)pin);

            //MakePinHigh((byte)port, (byte)pin);

            port_reserve[port] &= (byte)(~(1 << pin));
        }

        private void MakePinOutput(byte port, byte pin)
        {
            byte b;

            WriteRegister(PORT_SELECT_REGISTER, port); // Select port

            b = ReadRegister(ENABLE_PWM_REGISTER); // Check is current mode is pwm 
            b &= (byte)(~(1 << pin)); // need to be clear pwm

            WriteRegister(ENABLE_PWM_REGISTER, b); // select GPIO

            b = ReadRegister(PORT_DIRECTION_REGISTER); // Read direction
            b &= (byte)(~(1 << pin)); // 0 is out put

            WriteRegister(PORT_DIRECTION_REGISTER, b);   // write to register
        }

        private void MakePinInput(byte port, byte pin, ResistorMode resistorMode)
        {
            byte b;

            WriteRegister(PORT_SELECT_REGISTER, port); // Select port

            b = ReadRegister(ENABLE_PWM_REGISTER); // Check is current mode is pwm 
            b &= (byte)(~(1 << pin)); // need to be clear pwm

            WriteRegister(ENABLE_PWM_REGISTER, b); // select GPIO

            //Set mode
            b = ReadRegister((byte)resistorMode);
            b |= (byte)((1 << pin)); // 1 is input

            WriteRegister((byte)resistorMode, b);

            //Set direction
            b = ReadRegister(PORT_DIRECTION_REGISTER); // Return value
            b |= (byte)((1 << pin)); // 1 is input

            WriteRegister(PORT_DIRECTION_REGISTER, b); // write to register
        }

        private void MakePinHigh(byte port, byte pin)
        {
            // Read port
            byte b = ReadPort((byte)(OUTPUT_PORT_0_REGISTER+port));

            // Config pin
            b |= (byte)(1 << (pin));

            // Apply
            WritePort(port, b);
        }

        private void MakePinLow(byte port, byte pin)
        {
            // Read port
            byte b = ReadPort((byte)(OUTPUT_PORT_0_REGISTER + port));

            // Config pin
            b &= (byte)(~(1 << (pin)));

            // Apply
            WritePort(port, b);
        }

        private Boolean IsPinReserved(byte port, byte pin)
        {
            if ((port_reserve[port] & (1 << pin)) != 0)
            {
                return true;
            }
            return false;
        }

        private byte GetPortNumber(IOPin pin)
        {
            return (byte)((byte)(pin) >> 4);
        }

        private byte GetPinNumber(IOPin pin)
        {
            return (byte)((byte)(pin) & 0x0f);
        }

        /// <summary>
        /// Enumeration designating the available PWM pins.
        /// </summary>
        public enum PWMPin : byte
        {
            PWM0 = 0x60,
            PWM1,
            PWM2,
            PWM3,
            PWM4,
            PWM5,
            PWM6,
            PWM7,
            PWM8 = 0x70,
            PWM9,
            PWM10,
            PWM11,
            PWM12,
            PWM13,
            PWM14,
            PWM15
        }

        /// <summary>
        /// Enumeration designating the available IO pins.
        /// </summary>
        public enum IOPin : byte
        {
            Port0_Pin0 = 0x00,
            Port0_Pin1,
            Port0_Pin2,
            Port0_Pin3,
            Port0_Pin4,
            Port0_Pin5,
            Port0_Pin6,
            Port0_Pin7,
            Port1_Pin0 = 0x10,
            Port1_Pin1,
            Port1_Pin2,
            Port1_Pin3,
            Port1_Pin4,
            Port1_Pin5,
            Port1_Pin6,
            Port1_Pin7,
            Port2_Pin0 = 0x20,
            Port2_Pin1,
            Port2_Pin2,
            Port2_Pin3,
            Port3_Pin0 = 0x30,
            Port3_Pin1,
            Port3_Pin2,
            Port3_Pin3,
            Port3_Pin4,
            Port3_Pin5,
            Port3_Pin6,
            Port3_Pin7,
            Port4_Pin0 = 0x40,
            Port4_Pin1,
            Port4_Pin2,
            Port4_Pin3,
            Port4_Pin4,
            Port4_Pin5,
            Port4_Pin6,
            Port4_Pin7,
            Port5_Pin0 = 0x50,
            Port5_Pin1,
            Port5_Pin2,
            Port5_Pin3,
            Port5_Pin4,
            Port5_Pin5,
            Port5_Pin6,
            Port5_Pin7,
            Port6_pin0_PWM0 = 0x60,
            Port6_pin1_PWM1,
            Port6_pin2_PWM2,
            Port6_pin3_PWM3,
            Port6_pin4_PWM4,
            Port6_pin5_PWM5,
            Port6_pin6_PWM6,
            Port6_pin7_PWM7,
            Port7_pin0_PWM8 = 0x70,
            Port7_pin1_PWM9,
            Port7_pin2_PWM10,
            Port7_pin3_PWM11,
            Port7_pin4_PWM12,
            Port7_pin5_PWM13,
            Port7_pin6_PWM14,
            Port7_pin7_PWM15
        }

        /// <summary>
        /// Enumeration designating the available resistor modes for the IO pins.
        /// </summary>
        public enum ResistorMode : byte
        {
            ResistivePullUp = 0x1D,
            ResistivePullDown = 0x1E,
            OpenDrainHigh = 0x1F,
            OpenDrainLow = 0x20,
            StrongDrive = 0x21,
            SlowStrongDrive = 0x22,
            HighImpedence = 0x23
        }

        /// <summary>
        /// Class that represents a digital input on the module.
        /// </summary>
        public class InputPort
        {
            byte _portId;
            byte _pinId;
            IO60P16 _io60p16;
            /// <summary>
            /// Constructor
			/// </summary>
			/// <param name="io60p16">The IO60 the port is on.</param>
			/// <param name="pin">Pin to be created</param>
            /// <param name="resisterMode">Desired resistor mode for the pin to be constructed</param>
            public InputPort(IO60P16 io60p16, IOPin pin, ResistorMode resisterMode)
            {
                _io60p16 = io60p16;
                _portId = _io60p16.GetPortNumber(pin);
                _pinId = _io60p16.GetPinNumber(pin);

                if (_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reserved");
                }

                _io60p16.MakePinInput(_portId, _pinId, resisterMode);

                _io60p16.port_reserve[_portId] |= (byte)(1 << _pinId);
            }

            /// <summary>
            /// Reads the state of a pin
            /// </summary>
            /// <returns>Returns the state of the pin</returns>
            public Boolean Read()
            {
                if (!_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reset or has not been initialized.");// reserve already
                }

                byte b = _io60p16.ReadPort(_portId);

                if ((b & (1 << _pinId)) != 0)
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Disposes of the current object
            /// </summary>
            public void Dispose()
            {
                _io60p16.ReleasePin(_portId, _pinId);
            }
        }

        /// <summary>
        /// Class that represents a digital output object on the module
        /// </summary>
        public class OutputPort
        {
            byte _portId;
            byte _pinId;
            IO60P16 _io60p16;
            /// <summary>
            /// Constructor
			/// </summary>
			/// <param name="io60p16">The IO60 the port is on.</param>
            /// <param name="pin">Pin to be created</param>
            /// <param name="initialState">If the pin should be created and set high (true) or low (false)</param>
            public OutputPort(IO60P16 io60p16, IOPin pin, Boolean initialState)
            {
                _io60p16 = io60p16;
                _portId = _io60p16.GetPortNumber(pin);
                _pinId = _io60p16.GetPinNumber(pin);

                if (_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reserved");
                }

                _io60p16.MakePinOutput(_portId, _pinId);

                _io60p16.port_reserve[_portId] |= (byte)(1 << _pinId);

                if (initialState)
                {
                    _io60p16.MakePinHigh(_portId, _pinId);
                }
                else
                {
                    _io60p16.MakePinLow(_portId, _pinId);
                }
            }

            /// <summary>
            /// Writes the state of the pin
            /// </summary>
            /// <param name="state">The state to set the pin to. High (true) or low (false)</param>
            public void Write(Boolean state)
            {
                if (!_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reset or has not been initialized.");// reserve already
                }

                if (state)
                    _io60p16.MakePinHigh(_portId, _pinId);
                else
                    _io60p16.MakePinLow(_portId, _pinId);
            }

            /// <summary>
            /// Disposes of the current object
            /// </summary>
            public void Dispose()
            {
                _io60p16.ReleasePin(_portId, _pinId);
            }
        }

        /// <summary>
        /// Class the represents a PWM pin on the module
        /// </summary>
        public class PWM
        {
            byte _portId;
            byte _pinId;
            byte _divider = 0x4;
            byte _periodTicks = 0xFF;
            byte _highPulseWidthTicks = 0;
            IO60P16 _io60p16;

            TickWidth _tickWidth;

            //public int SERVO_SOURCE_PERIOD_US = 43;

            /// <summary>
            /// Enumeration designating the available tick widths for controlling servos 
            /// </summary>
            public enum TickWidth : byte
            {
                TickWidth_32KHz_31520ns = 0, // 32KHz
                TickWidth_24MHz_42ns, // 24MHz actually 41.66 round to 42
                TickWidth_1500KHz_667ns,// 1.5MHz actually 666.67 round to 667
                TickWidth_94KHz_10667ns,// 93.75KHz actually 10666.6 round to 10667
                TickWidth_Servo_23438hz_42666ns, // 90Hz
            }

            /// <summary>
            /// Constructor
			/// </summary>
			/// <param name="io60p16">The IO60 the port is on.</param>
            /// <param name="pin">The pin to be constructed</param>
            /// <param name="tickWidth">The desired width between ticks</param>
            public PWM(IO60P16 io60p16, PWMPin pin, TickWidth tickWidth)
            {
                _io60p16 = io60p16;
                _portId = _io60p16.GetPortNumber((IOPin)pin);
                _pinId = _io60p16.GetPinNumber((IOPin)pin);

                if (_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reserved");// reserve already
                }

                _io60p16.port_reserve[_portId] |= (byte)(1 << _pinId);

                _tickWidth = tickWidth;
                _io60p16.MakePinOutput(_portId, _pinId);
                _io60p16.MakePinHigh(_portId, _pinId);
                SetPWM();
            }

            /// <summary>
            /// Accessor for the pin's tick width
            /// </summary>
            private TickWidth tickWidth
            {
                get { return _tickWidth; }
            }

            /// <summary>
            /// 
            /// </summary>
            public byte periodTicks
            {
                get { return _periodTicks; }
            }

            /// <summary>
            /// 
            /// </summary>
            public byte highPulseWidthTicks
            {
                get { return _highPulseWidthTicks; }
            }

            /// <summary>
            /// Sets the PWM frequency
            /// </summary>
            private void SetPWM()
            {
              
                _io60p16.WriteRegister(PORT_SELECT_REGISTER, _portId);
                _io60p16.WriteRegister(0x28, (byte)(_pinId + (_portId - 6) * 8));
                byte pwm_output_register = _io60p16.ReadRegister(ENABLE_PWM_REGISTER);
                pwm_output_register |= (byte)(1 << _pinId);
                _io60p16.WriteRegister(ENABLE_PWM_REGISTER, pwm_output_register);
                _io60p16.WriteRegister(DIVIDER_REGISTER, _divider);
                _io60p16.WriteRegister(SELECT_CLOCK_SRC, (byte)_tickWidth);
                _io60p16.WriteRegister(PERIOD_REGISTER, _periodTicks);      // (Period rising : 255)
                _io60p16.WriteRegister(PULSE_WIDTH_REGISTER, _highPulseWidthTicks);        //(DutyCycle )
            }

            // the frequency is automatically set to source ticks x 255
            // For example, using 94Khz (94000hz) source will result in about 368hz
            // this is fixed by design to result in maximum resolution of 255

            /// <summary>
            /// Sets the PWM duty cycle
            /// </summary>
            /// <param name="dutyCycle"></param>
            public void Set(double dutyCycle)
            {
                if (dutyCycle > 1)
                    throw new Exception("Invalid param. The dutyCycle should not be bigger than 1.");

                byte x = (byte)(dutyCycle * 255);

                SetPulse(x, 255);
            }

            /// <summary>
            /// Sets the PWM pulse width
            /// </summary>
            /// <param name="highPulseWidthTicks">Number of ticks for the PWM to be high</param>
            public void SetPulse(byte highPulseWidthTicks)
            {
                SetPulse(highPulseWidthTicks, 255);
            }

            /// <summary>
            /// Sets the PWM Pulse width and period
            /// </summary>
            /// <param name="highPulseWidthTicks">Number of ticks for the PWM to be high</param>
            /// <param name="periodTicks">Number of ticks for the period</param>
            public void SetPulse(byte highPulseWidthTicks, byte periodTicks)
            {
                if (highPulseWidthTicks > periodTicks)
                {
                    throw new Exception("Invalid param. highPulseWidthTicks should be smaller than " + periodTicks);
                }

                if (!_io60p16.IsPinReserved(_portId, _pinId))
                {
                    throw new Exception("This pin has already been reset or has not been initialized.");// reserve already
                }

                _periodTicks = periodTicks;
                _highPulseWidthTicks = highPulseWidthTicks;
                SetPWM();

            }

            /// <summary>
            /// Disposed of the current object
            /// </summary>
            public void Dispose()
            {
                _io60p16.ReleasePin(_portId, _pinId);
            }
        }
    }
}
