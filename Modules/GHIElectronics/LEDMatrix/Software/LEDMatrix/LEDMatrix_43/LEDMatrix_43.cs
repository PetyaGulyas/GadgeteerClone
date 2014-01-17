﻿using GTM = Gadgeteer.Modules;

namespace Gadgeteer.Modules.GHIElectronics
{        // -- CHANGE FOR MICRO FRAMEWORK 4.2 --
        // If you want to use Serial, SPI, or DaisyLink (which includes GTI.SoftwareI2CBus), you must do a few more steps
        // since these have been moved to separate assemblies for NETMF 4.2 (to reduce the minimum memory footprint of Gadgeteer)
        // 1) add a reference to the assembly (named Gadgeteer.[interfacename])
        // 2) in GadgeteerHardware.xml, uncomment the lines under <Assemblies> so that end user apps using this module also add a reference.

    /// <summary>
    /// A Daisylink capable 8x8 LED Matrix module for Microsoft .NET Gadgeteer.
    /// </summary>
    /// <example>
    /// <para>The following example uses a <see cref="LEDMatrix"/> object to display an "X" character on the matrix. 
    /// </para>
    /// <code>
    /// using System;
    /// using System.Collections;
    /// using System.Threading;
    /// using Microsoft.SPOT;
    /// using Microsoft.SPOT.Presentation;
    /// using Microsoft.SPOT.Presentation.Controls;
    /// using Microsoft.SPOT.Presentation.Media;
    /// using Microsoft.SPOT.Touch;
    ///
    /// using Gadgeteer.Networking;
    /// using GT = Gadgeteer;
    /// using GTM = Gadgeteer.Modules;
    /// using Gadgeteer.Modules.GHIElectronics;
    ///
    /// namespace TestApp
    /// {
    ///     public partial class Program
    ///     {
    ///         void ProgramStarted()
    ///         {
    ///             byte[] Letter_x = new byte[] { 0x0, 0x0, 0x42, 0x24, 0x18, 0x18, 0x24, 0x42 };
    ///             lEDMatrix.DrawBitmap(Letter_x);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class LEDMatrix : GTM.DaisyLinkModule
    {
        private const byte GHI_DAISYLINK_MANUFACTURER = 0x10;
        private const byte GHI_DAISYLINK_TYPE_LEDMATRIX = 0x02;
        private const byte GHI_DAISYLINK_VERSION_LEDMATRIX = 0x01;

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        public LEDMatrix(int socketNumber)
            : base(socketNumber, GHI_DAISYLINK_MANUFACTURER, GHI_DAISYLINK_TYPE_LEDMATRIX, GHI_DAISYLINK_VERSION_LEDMATRIX, GHI_DAISYLINK_VERSION_LEDMATRIX, 50, "LEDMatrix")
        {

        }

        /// <summary>
        /// Draws a 8x8 "bitmap" to the screen.
        /// </summary>
        /// <param name="bitmap">The array of 8 bytes to display on the LED Matrix.</param>
        public void DrawBitmap(byte[] bitmap)
        {
            for (int i = 0; i < 8; i++)
            {
                WriteRegister((byte)i, bitmap[i]);
            }
        }

        /// <summary>
        /// Writes to the daisylink register specified by the address. Does not allow writing to the reserved registers.
        /// </summary>
        /// <param name="address">Address of the register.</param>
        /// <param name="writebuffer">Byte to write.</param>
        private void WriteRegister(byte address, byte writebuffer)
        {
            Write((byte)(DaisyLinkOffset + address), (byte)writebuffer);
        }

        /// <summary>
        /// Reads a byte from the specified register. Allows reading of reserved registers.
        /// </summary>
        /// <param name="memoryaddress">Address of the register.</param>
        /// <returns></returns>
        private byte ReadRegister(byte memoryaddress)
        {
            return Read(memoryaddress);
        }

    }
}
