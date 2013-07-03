﻿using GTM = Gadgeteer.Modules;

namespace Gadgeteer.Modules.GHIElectronics
{
    /// <summary>
    /// A Daisylink LEDMatrix module for Microsoft .NET Gadgeteer
    /// </summary>
    public class LEDMatrix : GTM.Module.DaisyLinkModule
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
        public void WriteRegister(byte address, byte writebuffer)
        {
            WriteParams((byte)(DaisyLinkOffset + address), (byte)writebuffer);
        }

        /// <summary>
        /// Reads a byte from the specified register. Allows reading of reserved registers.
        /// </summary>
        /// <param name="memoryaddress">Address of the register.</param>
        /// <returns></returns>
        public byte ReadRegister(byte memoryaddress)
        {
            return Read(memoryaddress);
        }

    }
}
