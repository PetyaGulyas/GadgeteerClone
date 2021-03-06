﻿using GTM = Gadgeteer.Modules;

namespace Gadgeteer.Modules.GHIElectronics {
	/// <summary>A USBClientSP module for Microsoft .NET Gadgeteer</summary>
	public class USBClientSP : GTM.Module {
		/// <summary>Constructs a new instance.</summary>
		/// <param name="socketNumber">The mainboard socket that has the module plugged into it.</param>
		public USBClientSP(int socketNumber) {
			Socket socket = Socket.GetSocket(socketNumber, true, this, null);
			socket.EnsureTypeIsSupported('D', this);

			socket.ReservePin(Socket.Pin.Four, this);
			socket.ReservePin(Socket.Pin.Five, this);
		}
	}
}