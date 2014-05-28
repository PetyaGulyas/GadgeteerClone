//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RelayX1_Tester {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The Display T43 module using sockets 15, 16, 17 and 14 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.DisplayT43 displayT43;
        
        /// <summary>The Relay X1 module using socket 18 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX11;
        
        /// <summary>The Relay X1 module using socket 13 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX12;
        
        /// <summary>The Relay X1 module using socket 12 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX13;
        
        /// <summary>The Relay X1 module using socket 11 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX14;
        
        /// <summary>The Relay X1 module using socket 10 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX15;
        
        /// <summary>The Relay X1 module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX16;
        
        /// <summary>The Relay X1 module using socket 4 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX17;
        
        /// <summary>The Relay X1 module using socket 3 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX18;
        
        /// <summary>The Relay X1 module using socket 2 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX19;
        
        /// <summary>The Relay X1 module using socket 1 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.RelayX1 relayX110;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZRaptor Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZRaptor)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZRaptor();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.displayT43 = new GTM.GHIElectronics.DisplayT43(15, 16, 17, 14);
            this.relayX11 = new GTM.GHIElectronics.RelayX1(18);
            this.relayX12 = new GTM.GHIElectronics.RelayX1(13);
            this.relayX13 = new GTM.GHIElectronics.RelayX1(12);
            this.relayX14 = new GTM.GHIElectronics.RelayX1(11);
            this.relayX15 = new GTM.GHIElectronics.RelayX1(10);
            this.relayX16 = new GTM.GHIElectronics.RelayX1(9);
            this.relayX17 = new GTM.GHIElectronics.RelayX1(4);
            this.relayX18 = new GTM.GHIElectronics.RelayX1(3);
            this.relayX19 = new GTM.GHIElectronics.RelayX1(2);
            this.relayX110 = new GTM.GHIElectronics.RelayX1(1);
        }
    }
}
