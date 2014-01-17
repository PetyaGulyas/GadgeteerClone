﻿using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.SocketInterfaces;

namespace Gadgeteer.Modules.GHIElectronics
{
    /// <summary>
    /// A module that provides access to 16 optically-isolated relays for Microsoft .NET Gadgeteer
    /// </summary>
    /// <example>
    /// <para>The following example uses a <see cref="RelayISOx16"/> object to write to a few of the available 16 relays.</para>
    /// <code>
    /// using Microsoft.SPOT;
    ///
    /// using GTM = Gadgeteer.Modules;
    ///
    /// namespace TestApp
    /// {
    ///     public partial class Program
    ///     {
    ///         void ProgramStarted()
    ///         {
    ///             // Multiple relays can be enabled at once
    ///             relay.EnableRelay(GTM.GHIElectronics.RelayISOx16.Relay.Relay_14 + GTM.GHIElectronics.RelayISOx16.Relay.Relay_4);
    ///
    ///             // Enabling another relay will not affect previously altered relays
    ///             relay.EnableRelay(GTM.GHIElectronics.RelayISOx16.Relay.Relay_7);
    ///
    ///             // Multiple relays can be disabled at once
    ///             relay.DisableRelay(GTM.GHIElectronics.RelayISOx16.Relay.Relay_12 + GTM.GHIElectronics.RelayISOx16.Relay.Relay_2);
    ///
    ///             // Disabling another relay will not affect previously altered relays
    ///             relay.DisableRelay(GTM.GHIElectronics.RelayISOx16.Relay.Relay_6);
    ///
    ///             // Use this to turn off all relays
    ///             relay.DisableAllRelays();
    ///             
    ///             Debug.Print("Program Started");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class RelayISOx16 : GTM.Module
    {
        // -- CHANGE FOR MICRO FRAMEWORK 4.2 --
        // If you want to use Serial, SPI, or DaisyLink (which includes GTI.SoftwareI2CBus), you must do a few more steps
        // since these have been moved to separate assemblies for NETMF 4.2 (to reduce the minimum memory footprint of Gadgeteer)
        // 1) add a reference to the assembly (named Gadgeteer.[interfacename])
        // 2) in GadgeteerHardware.xml, uncomment the lines under <Assemblies> so that end user apps using this module also add a reference.

        private GTI.DigitalOutput data;
        private GTI.DigitalOutput clock;
        private GTI.DigitalOutput latch;
        private GTI.DigitalOutput enable;
        private GTI.DigitalOutput clear;

        /// <summary>
        /// Mask used to toggle relays.
        /// </summary>
        public partial class Relay
        {
            /// <summary>
            /// Mask for relay #1
            /// </summary>
            public const ushort Relay_1 = 1;

            /// <summary>
            /// Mask for relay #2
            /// </summary>
            public const ushort Relay_2 = 2;

            /// <summary>
            /// Mask for relay #3
            /// </summary>
            public const ushort Relay_3 = 4;

            /// <summary>
            /// Mask for relay #4
            /// </summary>
            public const ushort Relay_4 = 8;

            /// <summary>
            /// Mask for relay #5
            /// </summary>
            public const ushort Relay_5 = 16;

            /// <summary>
            /// Mask for relay #6
            /// </summary>
            public const ushort Relay_6 = 32;

            /// <summary>
            /// Mask for relay #7
            /// </summary>
            public const ushort Relay_7 = 64;

            /// <summary>
            /// Mask for relay #8
            /// </summary>
            public const ushort Relay_8 = 128;

            /// <summary>
            /// Mask for relay #9
            /// </summary>
            public const ushort Relay_9 = 256;

            /// <summary>
            /// Mask for relay #10
            /// </summary>
            public const ushort Relay_10 = 512;

            /// <summary>
            /// Mask for relay #11
            /// </summary>
            public const ushort Relay_11 = 1024;

            /// <summary>
            /// Mask for relay #12
            /// </summary>
            public const ushort Relay_12 = 2048;

            /// <summary>
            /// Mask for relay #13
            /// </summary>
            public const ushort Relay_13 = 4096;

            /// <summary>
            /// Mask for relay #14
            /// </summary>
            public const ushort Relay_14 = 8192;

            /// <summary>
            /// Mask for relay #15
            /// </summary>
            public const ushort Relay_15 = 16384;

            /// <summary>
            /// Mask for relay #16
            /// </summary>
            public const ushort Relay_16 = 32768;
        }

        private ushort regData = 0x0000;

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary>Constructor</summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        public RelayISOx16(int socketNumber)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            socket.EnsureTypeIsSupported('Y', this);

            //Default on-state fix submitted by community member 'Lubos'
            data = GTI.DigitalOutputFactory.Create(socket, Socket.Pin.Seven, false, this);
            clock = GTI.DigitalOutputFactory.Create(socket, Socket.Pin.Nine, false, this);
            enable = GTI.DigitalOutputFactory.Create(socket, Socket.Pin.Three, true, this); //Switching lines for enable and latch prevents default on state
            latch = GTI.DigitalOutputFactory.Create(socket, Socket.Pin.Five, false, this);
            clear = GTI.DigitalOutputFactory.Create(socket, Socket.Pin.Four, true, this);

            DisableAllRelays();

            EnableRelay(0);
            
            EnableOutputs();
        }

        /// <summary>
        /// Clears all relays.
        /// </summary>
        public void DisableAllRelays()
        {
            regData = 0;
            ushort reg = (ushort)regData;

            for (int i = 0; i < 16; i++)
            {
                if ((reg & 0x1) == 1)
                {
                    data.Write(false);
                }
                else
                {
                    data.Write(true);
                }


                clock.Write(true);
                clock.Write(false);

                reg >>= 1;
            }

            latch.Write(true);
            latch.Write(false);
        }

        /// <summary>
        /// Enables the outputs to the relays.
        /// </summary>
        private void EnableOutputs()
        {
            enable.Write(false);
        }

        /// <summary>
        /// Disables the outputs to the relays.
        /// </summary>
        private void DisableOutputs()
        {
            enable.Write(true);
        }

        /// <summary>
        /// Enables the relay that is passed in. Multiple relays can be enabled simultaneously.
        /// </summary>
        /// <param name="relay">The relay to turn on.</param>
        public void EnableRelay(ushort relay)
        {
            regData |= relay;

            ushort reg = (ushort)regData;

            for (int i = 0; i < 16; i++)
            {
                if ((reg & 0x1) == 1)
                {
                    data.Write(false);
                }
                else
                {
                    data.Write(true);
                }


                clock.Write(true);
                clock.Write(false);

                reg >>= 1;
            }

            latch.Write(true);
            latch.Write(false);
        }

        /// <summary>
        /// Disables the relay that is passed in. Multiple relays can be disabled simultaneously.
        /// </summary>
        /// <param name="relay">The relay to turn off.</param>
        public void DisableRelay(ushort relay)
        {
            regData &= (ushort)~relay;

            ushort reg = (ushort)regData;

            for (int i = 0; i < 16; i++)
            {
                if ((reg & 0x1) == 1)
                {
                    data.Write(false);
                }
                else
                {
                    data.Write(true);
                }


                clock.Write(true);
                clock.Write(false);

                reg >>= 1;
            }

            latch.Write(true);
            latch.Write(false);
        }
    }
}
