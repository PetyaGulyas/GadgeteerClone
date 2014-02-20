﻿using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
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
    /// A UsbSerialSP module for Microsoft .NET Gadgeteer providing both serial communications and USB power.
    /// </summary>
    public class USBSerialSP : GTM.Module
    {
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        /// <remarks>
        /// The function <see cref="Configure"/> can be called to configure the <see cref="SerialLine"/> before it is used.  
        /// If it is not called before first use, then the following defaults will be used and cannot be changed afterwards:
        /// <list type="bullet">
        ///  <item>Baud Rate - 115200</item>
        ///  <item>Parity - <see cref="T:Microsoft.Gadgeteer.Interfaces.Serial.SerialParity">SerialParity.None</see></item>
        ///  <item>Stop Bits - <see cref="T:Microsoft.Gadgeteer.Interfaces.Serial.SerialStopBits">SerialStopBits.One</see></item>
        ///  <item>Data Bits - 8</item>
        /// </list>
        /// </remarks>
        public USBSerialSP(int socketNumber)
        {
            socket = Socket.GetSocket(socketNumber, true, this, null);
            if (socket.SupportsType('U') == false)
            {
                throw new GT.Socket.InvalidSocketException("Socket " + socketNumber +
                    " does not support support UsbSerial modules. Please plug the USB Serial module into a socket labeled 'U'");
            }
        }

        private GTI.Serial _SerialLine = null;

        private Socket socket;

        /// <summary>
        /// Gets the <see cref="T:Microsoft.Gadgeteer.Interfaces.Serial"/> device associated with this instance.
        /// </summary>
        public GTI.Serial SerialLine
        {
            get
            {
                if (_SerialLine == null)
                {
                    Configure(115200, GTI.Serial.SerialParity.None, GTI.Serial.SerialStopBits.One, 8);
                }
                return _SerialLine;
            }
            private set
            {
                _SerialLine = value;
            }
        }

        /// <summary>
        /// Configures this serial line.  This should be called at most once.
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="parity">A value from the <see cref="T:Microsoft.Gadgeteer.Interfaces.Serial.SerialParity"/> enumeration that specifies the parity.</param>
        /// <param name="stopBits">A value from the <see cref="T:Microsoft.Gadgeteer.Interfaces.Serial.SerialStopBits"/> enumeration that specifies the number of stop bits.</param>
        /// <param name="dataBits">The number of data bits.</param>
        public void Configure(int baudRate, GTI.Serial.SerialParity parity, GTI.Serial.SerialStopBits stopBits, int dataBits)
        {
            if (_SerialLine != null)
            {
                throw new Exception("UsbSerial.Configure can only be called once");
            }
            // TODO: check if HW flow control should be used
            _SerialLine = new GTI.Serial(socket, baudRate, parity, stopBits, dataBits, GTI.Serial.HardwareFlowControl.NotRequired, this);
        }

        /// <summary>
        /// Whether the device attached to this USB Serial module is currently in a sleep state.
        /// </summary>
        public bool isAsleep
        {
            get { return false; }// !_SleepBar.Read(); }
        }

        private bool _isAsleepLastSent = false;
        private Object _lock = new Object();
        private TimeSpan _glitchTime = new TimeSpan(0, 0, 3); // catch glitches until a full second has passed without any event
        private Gadgeteer.Timer _glitchdt;

        private void _SleepBar_Interrupt(Interfaces.InterruptInput input, bool value)
        {
            lock (_lock)
            {
                if (_glitchdt == null)
                {
                    _glitchdt = new Gadgeteer.Timer(_glitchTime);
                    _glitchdt.Tick += new Gadgeteer.Timer.TickEventHandler(_glitchdt_Tick);
                }

                bool sleeping = isAsleep;
                if (!_glitchdt.IsRunning || _isAsleepLastSent == true && !sleeping)
                {
                    _isAsleepLastSent = sleeping;
                    OnPowerStateChangedEvent(this, _isAsleepLastSent);
                }
                else
                {
                    // filter out all but first "awake" event while glitch filter is running
                }
                if (_glitchdt.IsRunning)
                {
                    // reset the timer interval
                    _glitchdt.Start();
                }
            }
        }

        private void _glitchdt_Tick(Gadgeteer.Timer timer)
        {
            lock (_lock)
            {
                _glitchdt.Stop();

                if (isAsleep != _isAsleepLastSent)
                {
                    _isAsleepLastSent = isAsleep;
                    OnPowerStateChangedEvent(this, _isAsleepLastSent);
                }
            }
        }

        private Gadgeteer.Timer _wakedt = null;
        private TimeSpan _holdRI = new TimeSpan(0, 0, 0, 0, 100);

        /// <summary>
        /// Resumes the USB host controller from a suspended state.
        /// </summary>
        public void Wakeup()
        {
            lock (_lock)
            {
                if (!isAsleep) return;

                if (_wakedt == null)
                {
                    _wakedt = new Gadgeteer.Timer(_holdRI);
                    _wakedt.Tick += new Gadgeteer.Timer.TickEventHandler(wakedt_Tick);
                }

                if (!_wakedt.IsRunning && !_glitchdt.IsRunning)
                {
                    if (_glitchdt == null)
                    {
                        _glitchdt = new Gadgeteer.Timer(_glitchTime);
                        _glitchdt.Tick += new Gadgeteer.Timer.TickEventHandler(_glitchdt_Tick);
                    }

                    //_RIBar.Write(false);
                    _wakedt.Start();
                    _glitchdt.Start();
                }
            }
        }

        private void wakedt_Tick(Gadgeteer.Timer timer)
        {
            lock (_lock)
            {
                //_RIBar.Write(true);
                _wakedt.Stop();
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="PowerStateChanged"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbSerialSP"/> object that raised the event.</param>
        /// <param name="isAsleep">A value that indicates whether the USB host controller is suspended.</param>
        public delegate void PowerStateChangedEventHandler(USBSerialSP sender, bool isAsleep);

        /// <summary>
        /// Raised when the USB host controller is suspended or resumed.
        /// </summary>
        public event PowerStateChangedEventHandler PowerStateChanged;

        private PowerStateChangedEventHandler _OnPowerStateChanged;

        /// <summary>
        /// Raises the <see cref="PowerStateChanged"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbSerialSP"/> object that raised the event.</param>
        /// <param name="isAsleep">A value that indicates whether the USB host controller is suspended.</param>
        protected virtual void OnPowerStateChangedEvent(USBSerialSP sender, bool isAsleep)
        {
            if (_OnPowerStateChanged == null) _OnPowerStateChanged = new PowerStateChangedEventHandler(OnPowerStateChangedEvent);
            if (Program.CheckAndInvoke(PowerStateChanged, _OnPowerStateChanged, sender, isAsleep))
            {
                PowerStateChanged(sender, isAsleep);
            }
        }
    }
}