﻿using System;
using System.Threading;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;

using GHINet = GHI.Premium.Net;

namespace Gadgeteer.Modules.GHIElectronics
{
    // -- CHANGE FOR MICRO FRAMEWORK 4.2 --
    // If you want to use Serial, SPI, or DaisyLink (which includes GTI.SoftwareI2C), you must do a few more steps
    // since these have been moved to separate assemblies for NETMF 4.2 (to reduce the minimum memory footprint of Gadgeteer)
    // 1) add a reference to the assembly (named Gadgeteer.[interfacename])
    // 2) in GadgeteerHardware.xml, uncomment the lines under <Assemblies> so that end user apps using this module also add a reference.

    /// <summary>
    /// A WiFi RS21 Gadgeteer module
    /// </summary>
    public class WiFiRS21 : GTM.Module.NetworkModule
    {
        /// <summary>
        /// The class that will be used to interface with the wifi module. This member will handle everything from initialization to joining networks.
        /// </summary>
        public GHINet.WiFiRS9110 Interface;
        private GTI.SPI _spi;

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The mainboard socket that has the module plugged into it.</param>
        public WiFiRS21(int socketNumber)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            socket.EnsureTypeIsSupported('S', this);

            // Since the Configuration parameter is null, this just reserves the pins and sets _spi.SPIModule but doesnt set up SPI (which the WiFi driver does itself)
            _spi = new GTI.SPI(socket, null, GTI.SPI.Sharing.Exclusive, this);

            // Make sure that the INT pin gets reserved. This is used internally by WiFi driver
            socket.ReservePin(Socket.Pin.Three, this);
            socket.ReservePin(Socket.Pin.Four, this);
            socket.ReservePin(Socket.Pin.Six, this);

            Interface = new GHINet.WiFiRS9110(socket.SPIModule, socket.CpuPins[6], socket.CpuPins[3], socket.CpuPins[4], 4000);
            //Interface = new GHINet.WiFiRS9110(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI2, (Microsoft.SPOT.Hardware.Cpu.Pin)16, (Microsoft.SPOT.Hardware.Cpu.Pin)18, (Microsoft.SPOT.Hardware.Cpu.Pin)6, 4000);

            if (!Interface.IsOpen)
            {
                Interface.Open();
            }

            GHINet.NetworkInterfaceExtension.AssignNetworkingStackTo(Interface);

            Thread.Sleep(500);

            NetworkSettings = Interface.NetworkInterface;
        }

        /// <summary>
        /// Instructs the Mainboard to use this module for all network communication, and assigns the networking stack to this module.
        /// </summary>
        /// <remarks>
        /// This function is only needed if more than one network module is being used simultaneously. If not, this function should not be used.
        /// </remarks>
        public void UseThisNetworkInterface()
        {
            GHINet.NetworkInterfaceExtension.AssignNetworkingStackTo(Interface);
        }

        /// <summary>
        /// Gets a value that indicates whether this WiFi module is wirelessly connected to a network device.
        /// </summary>
        /// <remarks>
        /// <para>
        ///  This property enables you to determine if the <see cref="WiFiRS21"/> module is
        ///  physically connected to a network device, such as a router. 
        ///  When this property is <b>true</b>, it does not necessarily mean that the network connection is usable. 
        ///  You must also check the <see cref="P:Microsoft.Gadgeteer.Modules.NetworkModule.IsNetworkUp"/> property. 
        ///  <see cref="P:Microsoft.Gadgeteer.Modules.NetworkModule.IsNetworkUp"/> returns <b>true</b> 
        ///  if the network connection is both connected and configured for Internet Proctocol (IP) communication tasks. 
        /// </para>
        /// <note>
        ///  When <see cref="P:Microsoft.Gadgeteer.Modules.NetworkModule.IsNetworkUp"/> is <b>true</b>, it does not necessarily mean 
        ///  that the network connection is functional. The IP configuration
        ///  for the network connection may be invalid for the network that it is connected to.
        /// </note>
        /// </remarks>
        public override bool IsNetworkConnected
        {
            get
            {
                return Interface.IsLinkConnected;
            }
        }

    }
}
