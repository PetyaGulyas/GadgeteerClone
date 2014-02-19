﻿//#define WAITINGONASSEMBLIES

using System;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

using GHI.Premium.IO;
using GHI.Premium.USBHost;
using GHI.Premium.System;
using System.Collections;
using Microsoft.SPOT.IO;

namespace Gadgeteer.Modules.GHIElectronics
{
    // -- CHANGE FOR MICRO FRAMEWORK 4.2 --
    // If you want to use Serial, SPI, or DaisyLink (which includes GTI.SoftwareI2C), you must do a few more steps
    // since these have been moved to separate assemblies for NETMF 4.2 (to reduce the minimum memory footprint of Gadgeteer)
    // 1) add a reference to the assembly (named Gadgeteer.[interfacename])
    // 2) in GadgeteerHardware.xml, uncomment the lines under <Assemblies> so that end user apps using this module also add a reference.

    /// <summary>
    /// Represents a USB port that you can plug a USB storage device into.
    /// </summary>
    /// <remarks>
    /// This module enables you to use a USB Drive as a storage device, or to use a USB mouse as a pointing device. 
    /// Although you can plug other types of USB devices into the USB host module, the Microsoft .NET Gadgeteer software 
    /// only responds to these two device types. Other types of USB devices will not raise the events associated with this module.
    /// </remarks>
    /// <seealso cref="USBDriveConnected"/>
    /// <seealso cref="USBDriveDisconnected"/>
    /// <seealso cref="MouseConnected"/>
    /// <seealso cref="MouseDisconnected"/>
    public partial class USBHost : GTM.Module
    {
        private bool firstHostModule = true;

        private Hashtable mice;
        private Hashtable keyboards;
        private Hashtable storageDevices;

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The mainboard socket that has the module plugged into it.</param>
        public USBHost(int socketNumber)
        {
            if (!this.firstHostModule)
                throw new Exception("Only one USB host module may be connected in the designer at a time. If you have multiple host modules, just connect one of the modules. It will receive all events. See the developers document for more details.");
            else
                this.firstHostModule = false;

            Socket socket = Socket.GetSocket(socketNumber, true, this, null);
            socket.EnsureTypeIsSupported('H', this);

            this.mice = new Hashtable();
            this.keyboards = new Hashtable();
            this.storageDevices = new Hashtable();

            try
            {
                // add Insert/Eject events
                RemovableMedia.Insert += Insert;
                RemovableMedia.Eject += Eject;

                USBHostController.DeviceConnectedEvent += new USBH_DeviceConnectionEventHandler(USBHostController_DeviceConnectedEvent);
                USBHostController.DeviceDisconnectedEvent += new USBH_DeviceConnectionEventHandler(USBHostController_DeviceDisconnectedEvent);

                // Reserve the pins used by the USB Host interface
                socket.ReservePin(Socket.Pin.Three, this);
                socket.ReservePin(Socket.Pin.Four, this);
                socket.ReservePin(Socket.Pin.Five, this);
            }

            catch (Exception e)
            {
                throw new GT.Socket.InvalidSocketException("There is an issue connecting the USB Host module to socket " + socketNumber +
                    ". Please check that all modules are connected to the correct sockets or try connecting the USB Host to a different 'H' socket", e);
            }
        }


        private void USBHostController_DeviceConnectedEvent(USBH_Device device)
        {
            try
            {
                switch (device.TYPE)
                {
                    case USBH_DeviceType.MassStorage:
                        lock (this.storageDevices)
                        {
                            var ps = new PersistentStorage(device);
                            ps.MountFileSystem();
                            this.storageDevices.Add(device.ID, ps);
                        }

                        break;
                    case USBH_DeviceType.Mouse:
                        lock (this.mice)
                        {
                            var mouse = new USBH_Mouse(device);
                            mouse.SetCursorBounds(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue);
                            this.mice.Add(device.ID, mouse);
                            this.OnMouseConnectedEvent(this, mouse);
                        }

                        break;
                    case USBH_DeviceType.Keyboard:
                        lock (this.keyboards)
                        {
                            var keyboard = new USBH_Keyboard(device);
                            this.keyboards.Add(device.ID, keyboard);
                            this.OnKeyboardConnectedEvent(this, keyboard);
                        }

                        break;
                    case USBH_DeviceType.Webcamera:
                        ErrorPrint("Use GTM.GHIElectronics.Camera for USB WebCamera support.");
                        break;
                    default:
                        ErrorPrint("USB device is not supported by the Gadgeteer driver. More devices are supported by the GHI USB Host driver. Remove the USB Host from the designer, and proceed without using Gadgeteer code.");
                        break;
                }
            }
            catch (Exception)
            {
                ErrorPrint("Unable to identify USB Host device.");
            }
        }

        private void USBHostController_DeviceDisconnectedEvent(USBH_Device device)
        {
            switch (device.TYPE)
            {
                case USBH_DeviceType.MassStorage:
                    lock (this.storageDevices)
                    {
                        if (!this.storageDevices.Contains(device.ID))
                            return;

                        var ps = (PersistentStorage)this.storageDevices[device.ID];
                        ps.UnmountFileSystem();
                        ps.Dispose();
                        this.storageDevices.Remove(device.ID);
                    }

                    break;
                case USBH_DeviceType.Mouse:
                    lock (this.mice)
                    {
                        if (!this.mice.Contains(device.ID))
                            return;

                        var mouse = (USBH_Mouse)this.mice[device.ID];
                        this.OnMouseDisconnectedEvent(this, mouse);
                        this.mice.Remove(device.ID);
                    }

                    break;
                case USBH_DeviceType.Keyboard:
                    lock (this.keyboards)
                    {
                        if (!this.keyboards.Contains(device.ID))
                            return;

                        var keyboard = (USBH_Keyboard)this.keyboards[device.ID];
                        this.OnKeyboardDisconnectedEvent(this, keyboard);
                        this.keyboards.Remove(device.ID);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called when a device is inserted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Insert(object sender, MediaEventArgs e)
        {
            if (e.Volume.Name.Length >= 3 && e.Volume.Name.Substring(0, 3) == "USB")
            {
                if (e.Volume.FileSystem != null)
                {
                    OnUSBDriveConnectedEvent(this, new StorageDevice(e.Volume));
                }
                else
                {
                    ErrorPrint("Unable to mount USB drive. Is drive formatted as FAT32?");
                }
            }
        }

        /// <summary>
        /// Called when a device is ejected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Eject(object sender, MediaEventArgs e)
        {
            if (e.Volume.Name.Length >= 3 && e.Volume.Name.Substring(0, 3) == "USB")
            {
                OnUSBDriveDisconnectedEvent(this);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="USBDriveConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="storageDevice">The <see cref="T:Microsoft.Gadgeteer.StorageDevice"/> object associated with the connected USB drive.</param>
        public delegate void USBDriveConnectedEventHandler(USBHost sender, StorageDevice storageDevice);

        /// <summary>
        /// Raised when a USB drive is connected to the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the inserted device is a USB drive.
        /// </remarks>
        public event USBDriveConnectedEventHandler USBDriveConnected;

        private USBDriveConnectedEventHandler _OnUSBDriveConnected;

        /// <summary>
        /// Raises the <see cref="USBDriveConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="storageDevice">The <see cref="T:Microsoft.Gadgeteer.StorageDevice"/> object associated with the connected USB drive.</param>
        protected virtual void OnUSBDriveConnectedEvent(USBHost sender, StorageDevice storageDevice)
        {
            if (_OnUSBDriveConnected == null) _OnUSBDriveConnected = new USBDriveConnectedEventHandler(OnUSBDriveConnectedEvent);
            if (Program.CheckAndInvoke(USBDriveConnected, _OnUSBDriveConnected, sender, storageDevice))
            {
                USBDriveConnected(sender, storageDevice);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="USBDriveDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        public delegate void USBDriveDisconnectedEventHandler(USBHost sender);

        /// <summary>
        /// Raised when a USB drive is disconnected from the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the device that is being removed is a USB drive.
        /// </remarks>
        public event USBDriveDisconnectedEventHandler USBDriveDisconnected;

        private USBDriveDisconnectedEventHandler _OnUSBDriveDisconnected;

        /// <summary>
        /// Raises the <see cref="USBDriveDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        protected virtual void OnUSBDriveDisconnectedEvent(USBHost sender)
        {
            if (_OnUSBDriveDisconnected == null) _OnUSBDriveDisconnected = new USBDriveDisconnectedEventHandler(OnUSBDriveDisconnectedEvent);
            if (Program.CheckAndInvoke(USBDriveDisconnected, _OnUSBDriveDisconnected, sender))
            {
                USBDriveDisconnected(sender);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="MouseConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="mouse">The <see cref="USBH_Mouse"/> object associated with the event.</param>
        public delegate void MouseConnectedEventHandler(USBHost sender, USBH_Mouse mouse);

        /// <summary>
        /// Raised when a USB mouse device is connected to the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the device that is being connected is a USB mouse.
        /// </remarks>
        public event MouseConnectedEventHandler MouseConnected;

        private MouseConnectedEventHandler _OnMouseConnected;

        /// <summary>
        /// Raises the <see cref="MouseConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="mouse">The <see cref="USBH_Mouse"/> object associated with the event.</param>
        protected virtual void OnMouseConnectedEvent(USBHost sender, USBH_Mouse mouse)
        {
            if (_OnMouseConnected == null) _OnMouseConnected = new MouseConnectedEventHandler(OnMouseConnectedEvent);
            if (Program.CheckAndInvoke(MouseConnected, _OnMouseConnected, sender, mouse))
            {
                MouseConnected(sender, mouse);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="MouseDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="mouse">The <see cref="USBH_Mouse"/> object associated with the event.</param>
        public delegate void MouseDisconnectedEventHandler(USBHost sender, USBH_Mouse mouse);

        /// <summary>
        /// Raised when a USB mouse device is disconnected to the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the device that is being removed is a USB mouse.
        /// </remarks>
        public event MouseDisconnectedEventHandler MouseDisconnected;

        private MouseDisconnectedEventHandler _OnMouseDisconnected;

        /// <summary>
        /// Raises the <see cref="MouseDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="mouse">The <see cref="USBH_Mouse"/> object associated with the event.</param>
        protected virtual void OnMouseDisconnectedEvent(USBHost sender, USBH_Mouse mouse)
        {
            if (_OnMouseDisconnected == null) _OnMouseDisconnected = new MouseDisconnectedEventHandler(OnMouseDisconnectedEvent);
            if (Program.CheckAndInvoke(MouseDisconnected, _OnMouseDisconnected, sender, mouse))
            {
                MouseDisconnected(sender, mouse);
            }
        }

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="KeyboardConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="Keyboard">The <see cref="USBH_Keyboard"/> object associated with the event.</param>
        public delegate void KeyboardConnectedEventHandler(USBHost sender, USBH_Keyboard Keyboard);

        /// <summary>
        /// Raised when a USB Keyboard device is connected to the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the device that is being connected is a USB Keyboard.
        /// </remarks>
        public event KeyboardConnectedEventHandler KeyboardConnected;

        private KeyboardConnectedEventHandler _OnKeyboardConnected;

        /// <summary>
        /// Raises the <see cref="KeyboardConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="keyboard">The <see cref="USBH_Keyboard"/> object associated with the event.</param>
        protected virtual void OnKeyboardConnectedEvent(USBHost sender, USBH_Keyboard keyboard)
        {
            if (_OnKeyboardConnected == null) _OnKeyboardConnected = new KeyboardConnectedEventHandler(OnKeyboardConnectedEvent);
            if (Program.CheckAndInvoke(KeyboardConnected, _OnKeyboardConnected, sender, keyboard))
            {
                KeyboardConnected(sender, keyboard);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="KeyboardDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="Keyboard">The <see cref="USBH_Keyboard"/> object associated with the event.</param>
        public delegate void KeyboardDisconnectedEventHandler(USBHost sender, USBH_Keyboard Keyboard);

        /// <summary>
        /// Raised when a USB Keyboard device is disconnected to the host.
        /// </summary>
        /// <remarks>
        /// Although you can plug various types of USB devices into the USB host module, this event is only raised
        /// when the device that is being removed is a USB Keyboard.
        /// </remarks>
        public event KeyboardDisconnectedEventHandler KeyboardDisconnected;

        private KeyboardDisconnectedEventHandler _OnKeyboardDisconnected;

        /// <summary>
        /// Raises the <see cref="KeyboardDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="UsbHost"/> object that raised the event.</param>
        /// <param name="Keyboard">The <see cref="USBH_Keyboard"/> object associated with the event.</param>
        protected virtual void OnKeyboardDisconnectedEvent(USBHost sender, USBH_Keyboard Keyboard)
        {
            if (_OnKeyboardDisconnected == null) _OnKeyboardDisconnected = new KeyboardDisconnectedEventHandler(OnKeyboardDisconnectedEvent);
            if (Program.CheckAndInvoke(KeyboardDisconnected, _OnKeyboardDisconnected, sender, Keyboard))
            {
                KeyboardDisconnected(sender, Keyboard);
            }
        }

    }
}
