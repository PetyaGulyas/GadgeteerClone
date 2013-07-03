﻿using System;
using Microsoft.SPOT;

using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;

using System.Threading;
using Microsoft.SPOT.Hardware;
using System.IO;

namespace Gadgeteer.Modules.GHIElectronics
{
    /// <summary>
    /// A Music module for Microsoft .NET Gadgeteer
    /// </summary>
    public class Music : GTM.Module
    {
        // This example implements  a driver in managed code for a simple Gadgeteer module.  The module uses a 
        // single GTI.InterruptInput to interact with a sensor that can be in either of two states: low or high.
        // The example code shows the recommended code pattern for exposing the property (IsHigh). 
        // The example also uses the recommended code pattern for exposing two events: MusicHigh, MusicLow. 
        // The triple-slash "///" comments shown will be used in the build process to create an XML file named
        // GTM.GHIElectronics.Music. This file will provide Intellisense and documention for the
        // interface and make it easier for developers to use the Music module.        


        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private Thread m_recordingThread;
        private Thread m_playBackThread;

        private InputPort m_dreq;

        // Define SPI Configuration for MP3 decoder
        GTI.SPI m_SPICmd;
        GTI.SPI m_SPIData;
        private GTI.SPI.Configuration m_dataConfig2;
        private GTI.SPI.Configuration m_cmdConfig2;

        //SPI m_SPI;
        //private SPI.Configuration m_dataConfig;
        //private SPI.Configuration m_cmdConfig;

        /// <summary>
        /// Playback buffer
        /// </summary>
        private byte[] m_playBackData = null;

        /// <summary>
        /// Command buffer
        /// </summary>
        private byte[] m_cmdBuffer = new byte[4];

        bool _stopPlayingRequested = false;
        bool _isPlaying = false;

        Stream m_recordingStream;

        bool m_isRecording = false;
        bool m_stopRecordingRequested = false;
        ushort[] m_oggPatch;

        bool m_bPlayLocked = false;

        private byte[] m_sampleBuffer = new byte[] { CMD_READ, SCI_HDAT0 };
        static byte[] m_recordingBuffer = new byte[1024 * 8];

        byte m_leftChannelVolume;

        /// <summary>
        /// 
        /// </summary>
        public byte LeftChannelVolume
        {
            get { return m_leftChannelVolume; }
            set { m_leftChannelVolume = value; }
        }

        byte m_rightChannelVolume;

        /// <summary>
        /// 
        /// </summary>
        public byte RightChannelVolume
        {
            get { return m_rightChannelVolume; }
            set { m_rightChannelVolume = value; }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Constant Values
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        const byte CMD_WRITE = 0x02;

        const byte CMD_READ = 0x03;

        #region SCI_MODE bits

        const ushort SM_RESET = 0x04;
        const ushort SM_CANCEL = 0x10;
        const ushort SM_TESTS = 0x20;
        const ushort SM_SDINEW = 0x800;
        const ushort SM_ADPCM = 0x1000;
        const ushort SM_LINE1 = 0x4000;

        #endregion

        #region Registers

        /// <summary>
        /// Mode control
        /// R/W
        /// </summary>
        const int SCI_MODE = 0x00;

        /// <summary>
        /// Status of VS1053b
        /// R/W
        /// </summary>
        const int SCI_STATUS = 0x01;

        /// <summary>
        /// Built-in bass/treble control
        /// R/W
        /// </summary>
        const int SCI_BASS = 0x02;

        /// <summary>
        /// Clock freq + multiplier
        /// R/W
        /// </summary>
        const int SCI_CLOCKF = 0x03;

        /// <summary>
        /// Volume control
        /// R/W
        /// </summary>
        const int SCI_WRAM = 0x06;

        /// <summary>
        /// Volume control
        /// R/W
        /// </summary>
        const int SCI_WRAMADDR = 0x07;

        /// <summary>
        /// Stream header data 0
        /// R
        /// </summary>
        const int SCI_HDAT0 = 0x08;

        /// <summary>
        /// Stream header data 1
        /// R
        /// </summary>
        const int SCI_HDAT1 = 0x09;

        /// <summary>
        /// Volume control
        /// R/W
        /// </summary>
        const int SCI_AIADDR = 0x0A;

        /// <summary>
        /// Volume control
        /// R/W
        /// </summary>
        const int SCI_VOL = 0x0B;

        /// <summary>
        /// Application control register 0
        /// R/W
        /// </summary>
        const int SCI_AICTRL0 = 0x0C;

        /// <summary>
        /// Application control register 1
        /// R/W
        /// </summary>
        const int SCI_AICTRL1 = 0x0D;

        /// <summary>
        /// Application control register 2
        /// R/W
        /// </summary>
        const int SCI_AICTRL2 = 0x0E;

        /// <summary>
        /// Application control register 3
        /// R/W
        /// </summary>
        const int SCI_AICTRL3 = 0x0F;

        #endregion

        #region Recording constants

        const int PCM_MODE_JOINTSTEREO = 0x00;
        const int PCM_MODE_DUALCHANNEL = 0x01;
        const int PCM_MODE_LEFTCHANNEL = 0x02;
        const int PCM_MODE_RIGHTCHANNEL = 0x03;

        const int PCM_ENC_ADPCM = 0x00;
        const int PCM_ENC_PCM = 0x04;

        #endregion

        /////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Command Register operations

        /// <summary>
        /// Reads 16bit value from a register
        /// </summary>
        /// <param name="register">Source register</param>
        /// <returns>16bit value from the source register</returns>
        private ushort CommandRead(byte register)
        {
            ushort temp;

            if (m_bPlayLocked)
            {
                m_bPlayLocked = false;

                // be safe
                Thread.Sleep(50);
            }

            while (m_dreq.Read() == false)
                Thread.Sleep(1);

            //m_SPI.Config = m_cmdConfig;
            //m_SPI2.ChangeSpiConfig(m_cmdConfig2);

            m_cmdBuffer[0] = CMD_READ;

            m_cmdBuffer[1] = register;
            m_cmdBuffer[2] = 0;
            m_cmdBuffer[3] = 0;

            //m_SPI.WriteRead(m_cmdBuffer, m_cmdBuffer, 2);
            m_SPICmd.WriteRead(m_cmdBuffer, m_cmdBuffer, 2);

            temp = m_cmdBuffer[0];
            temp <<= 8;

            temp += m_cmdBuffer[1];

            m_bPlayLocked = true;

            return temp;
        }

        /// <summary>
        /// Writes 16bit value to a register
        /// </summary>
        /// <param name="register">target register</param>
        /// <param name="data">data to write</param>
        private void CommandWrite(byte register, ushort data)
        {
            if (m_bPlayLocked)
            {
                m_bPlayLocked = false;

                // be safe
                Thread.Sleep(50);
            }

            while (m_dreq.Read() == false)
                Thread.Sleep(1);

            //m_SPI.Config = m_cmdConfig;
            //m_SPI2.ChangeSpiConfig(m_cmdConfig2);

            m_cmdBuffer[0] = CMD_WRITE;

            m_cmdBuffer[1] = register;
            m_cmdBuffer[2] = (byte)(data >> 8);
            m_cmdBuffer[3] = (byte)data;

            //m_SPI.Write(m_cmdBuffer);
            m_SPICmd.Write(m_cmdBuffer);

            m_bPlayLocked = true;

        }

        #endregion

        #region VS1053b Tests

        /// <summary>
        /// Run sine test
        /// </summary>
        public void SineTest()
        {
            byte[] buf = { 0 };

            CommandWrite(SCI_MODE, SM_SDINEW | SM_TESTS | SM_RESET);

            byte[] start = { 0x53, 0xEF, 0x6E, 0x7E, 0x00, 0x00, 0x00, 0x00 };
            byte[] end = { 0x45, 0x78, 0x69, 0x74, 0x00, 0x00, 0x00, 0x00 };

            //m_SPI.Config = m_dataConfig;
            //m_SPI2.ChangeSpiConfig(m_dataConfig2);


            //Write start sequence
            for (int i = 0; i < start.Length; i++)
            {
                buf[0] = start[i];

                //m_SPI.Write(buf);
                m_SPIData.Write(buf);
                while (m_dreq.Read() == false)
                    ;
            }

            //Play for 2 seconds
            Thread.Sleep(2000);

            //Write stop sequence
            for (int i = 0; i < end.Length; i++)
            {
                buf[0] = end[i];

                //m_SPI.Write(buf);
                m_SPIData.Write(buf);

                while (m_dreq.Read() == false)
                    ;
            }
        }

        #endregion

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        public Music(int socketNumber)
        {
            // This finds the Socket instance from the user-specified socket number.  
            // This will generate user-friendly error messages if the socket is invalid.
            // If there is more than one socket on this module, then instead of "null" for the last parameter, 
            // put text that identifies the socket to the user (e.g. "S" if there is a socket type S)
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            socket.EnsureTypeIsSupported(new char[] { 'S' }, this);

            // Set up our SPI 
            //m_dataConfig = new SPI.Configuration(socket.CpuPins[5], false, 0, 0, false, true, 2000, socket.SPIModule, socket.CpuPins[3], false);
            m_dataConfig2 = new GTI.SPI.Configuration(false, 0, 0, false, true, 2000);

            //m_cmdConfig = new SPI.Configuration(socket.CpuPins[6], false, 0, 0, false, true, 2000, socket.SPIModule, socket.CpuPins[3], false);
            m_cmdConfig2 = new GTI.SPI.Configuration(false, 0, 0, false, true, 2000);

            m_dreq = new InputPort(socket.CpuPins[3], false, Port.ResistorMode.Disabled);

            //m_SPI = new SPI(m_dataConfig);
            m_SPIData = new GTI.SPI(socket, m_dataConfig2, GTI.SPI.Sharing.Shared, socket, Socket.Pin.Five, this);
            m_SPICmd = new GTI.SPI(socket, m_dataConfig2, GTI.SPI.Sharing.Shared, socket, Socket.Pin.Six, this);

            Reset();

            CommandWrite(SCI_MODE, SM_SDINEW);
            CommandWrite(SCI_CLOCKF, 0xa000);
            CommandWrite(SCI_VOL, 0x0101);  // highest volume -1

            if (CommandRead(SCI_VOL) != (0x0101))
            {
                throw new Exception("Failed to initialize MP3 Decoder.");
            }

            SetVolume(200, 200);

            // This creates an GTI.InterruptInput interface. The interfaces under the GTI namespace provide easy ways to build common modules.
            // This also generates user-friendly error messages automatically, e.g. if the user chooses a socket incompatible with an interrupt input.
            //this.input = new GTI.InterruptInput(socket, GT.Socket.Pin.Three, GTI.GlitchFilterMode.On, GTI.ResistorMode.PullUp, GTI.InterruptMode.RisingAndFallingEdge, this);

            // This registers a handler for the interrupt event of the interrupt input (which is below)
            //this.input.Interrupt += new GTI.InterruptInput.InterruptEventHandler(this._input_Interrupt);
        }

        /// <summary>
        /// Performs soft reset
        /// </summary>
        private void Reset()
        {
            while (m_dreq.Read() == false)
                Thread.Sleep(1);

            CommandWrite(SCI_MODE, SM_SDINEW | SM_RESET);

            while (m_dreq.Read() == false)
                Thread.Sleep(1);

            Thread.Sleep(1);

            CommandWrite(SCI_CLOCKF, 0xa000);
        }

        /// <summary>
        /// Set volume for both channels. Valid values 0-255
        /// </summary>
        /// <param name="leftChannelVolume">0 - silence, 255 - loudest</param>
        /// <param name="rightChannelVolume">0 - silence, 255 - loudest</param>
        public void SetVolume(byte leftChannelVolume, byte rightChannelVolume)
        {
            //ErrorPrint("asdf");

            // Cap the volume
            if (leftChannelVolume > 255)
                m_leftChannelVolume = 255;
            else if (leftChannelVolume < 0)
                m_leftChannelVolume = 0;
            else
                m_leftChannelVolume = leftChannelVolume;

            if (rightChannelVolume > 255)
                m_rightChannelVolume = 255;
            else if (rightChannelVolume < 0)
                m_rightChannelVolume = 0;
            else
                m_rightChannelVolume = rightChannelVolume;

            CommandWrite(SCI_VOL, (ushort)((255 - leftChannelVolume) << 8 | (255 - rightChannelVolume)));
        }

        /// <summary>
        /// Set volume for both channels. Valid values 0-255
        /// </summary>
        /// <param name="_dualChannelVolume">0 - silence, 255 - loudest</param>
        public void SetVolume(byte _dualChannelVolume)
        {
            // Cap the volume
            if (_dualChannelVolume > 255)
                m_leftChannelVolume = m_rightChannelVolume = 255;
            else if (_dualChannelVolume < 0)
                m_leftChannelVolume = m_rightChannelVolume = 0;
            else
                m_leftChannelVolume = m_rightChannelVolume = _dualChannelVolume;

            CommandWrite(SCI_VOL, (ushort)((255 - m_leftChannelVolume) << 8 | (255 - m_rightChannelVolume)));
        }

        /// <summary>
        /// Returns true if shield is playing or recording audio
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return _isPlaying || m_isRecording;
            }
        }

        private void WritePatchFromArray(ushort[] patch)
        {
            //m_SPI.Config = m_cmdConfig;
            //m_SPI2.ChangeSpiConfig(m_cmdConfig2);

            m_cmdBuffer[0] = 0x02;

            int count = 0;
            int i = 0;
            ushort value = 0;

            while (i < patch.Length)
            {

                m_cmdBuffer[1] = (byte)patch[i++];
                count = patch[i++];
                if ((count & 0x8000) != 0)
                {
                    count &= 0x7FFF;
                    value = patch[i++];
                    m_cmdBuffer[2] = (byte)(value >> 8);
                    m_cmdBuffer[3] = (byte)value;

                    while (count-- > 0)
                    {
                        //m_SPI.Write(m_cmdBuffer);
                        m_SPICmd.Write(m_cmdBuffer);

                        while (m_dreq.Read() == false)
                            ;
                    }
                }
                else
                {
                    while (count-- > 0)
                    {
                        value = patch[i++];
                        m_cmdBuffer[2] = (byte)(value >> 8);
                        m_cmdBuffer[3] = (byte)value;

                        //m_SPI.Write(m_cmdBuffer);
                        m_SPICmd.Write(m_cmdBuffer);

                        while (m_dreq.Read() == false)
                            ;
                    }

                }
            }
        }

        private void WritePatchFromStream(Stream s)
        {
            //m_SPI.Config = m_cmdConfig;
            //m_SPI2.ChangeSpiConfig(m_cmdConfig2);

            m_cmdBuffer[0] = 0x02;

            byte highByte = 0;
            byte lowByte = 0;
            int count = 0;
            while (true)
            {
                while (m_dreq.Read() == false)
                    Thread.Sleep(1);

                //register address
                m_cmdBuffer[1] = (byte)s.ReadByte();
                s.ReadByte();
                lowByte = (byte)s.ReadByte();
                highByte = (byte)s.ReadByte();
                count = (highByte & 0x7F) << 8 | lowByte;
                if ((highByte & 0x80) != 0)
                {
                    m_cmdBuffer[3] = (byte)s.ReadByte();
                    m_cmdBuffer[2] = (byte)s.ReadByte();
                    while (count-- > 0)
                    {
                        //m_SPI.Write(m_cmdBuffer);
                        m_SPICmd.Write(m_cmdBuffer);
                    }
                }
                else
                {
                    while (count-- > 0)
                    {
                        m_cmdBuffer[3] = (byte)s.ReadByte();
                        m_cmdBuffer[2] = (byte)s.ReadByte();
                        //m_SPI.Write(m_cmdBuffer);
                        m_SPICmd.Write(m_cmdBuffer);
                    }
                }
            }
        }

        #region Playback

        /// <summary>
        /// Play samples from byte array async
        /// </summary>
        /// <param name="data"></param>
        public void Play(byte[] data)
        {
            _isPlaying = true;
            _stopPlayingRequested = false;

            m_playBackData = data;
            m_playBackThread = new Thread(new ThreadStart(this.PlayBackThreadFunction));
            m_playBackThread.Start();
        }

        /// <summary>
        /// Playback thread function
        /// </summary>
        private void PlayBackThreadFunction()
        {
            byte[] block = new byte[32];

            int size = m_playBackData.Length - m_playBackData.Length % 32;

            //m_SPI.Config = m_dataConfig;
            //m_SPI2.ChangeSpiConfig(m_dataConfig2);

            for (int i = 0; i < size; i += 32)
            {
                if (_stopPlayingRequested)
                    break;

                Array.Copy(m_playBackData, i, block, 0, 32);

                while (m_dreq.Read() == false)
                    Thread.Sleep(1);  // wait till done

                // fake spinlock is fake
                while (true)
                {
                    if (!m_bPlayLocked)
                    {
                        //someone still has the spi
                        Thread.Sleep(1);
                    }
                    else
                    {
                        // we can have the spi back
                        //m_SPI.Config = m_dataConfig;
                        //m_SPI2.ChangeSpiConfig(m_dataConfig2);
                        break;
                    }
                }

                // pause goes here
                //while(paused)
                //sleep(1)

                //m_SPI.Write(block);
                m_SPIData.Write(block);
            }

            this.OnMusicFinished(this);

            Reset();

            //m_playBackData = null;
            _isPlaying = false;
        }

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="musicFinished"/> event.
        /// </summary>
        /// <param name="sender">The sending module.</param>
        public delegate void MusicFinishedPlayingEventHandler(Music sender);

        /// <summary>
        /// Raised when a sound file has finished playing.
        /// </summary>
        public event MusicFinishedPlayingEventHandler musicFinished;

        private MusicFinishedPlayingEventHandler onMusicFinished;

        /// <summary>
        /// Raises the <see cref="musicFinished"/> event.
        /// </summary>
        /// <param name="sender">The sending module.</param>
        protected virtual void OnMusicFinished(Music sender)
        {
            if (this.onMusicFinished == null)
                this.onMusicFinished = new MusicFinishedPlayingEventHandler(this.OnMusicFinished);

            if (Program.CheckAndInvoke(this.musicFinished, this.onMusicFinished, this))
                this.musicFinished(sender);
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopPlaying()
        {
            _stopPlayingRequested = true;
            this.OnMusicFinished(this);
        }

        #endregion

        /// <summary>
        /// Optimized CommandRead for SCI_HDAT0
        /// </summary>
        /// <param name="nSamples"></param>
        private void ReadData(int nSamples)
        {
            int i = 0;
            //m_SPI.Config = m_cmdConfig;
            //m_SPI2.ChangeSpiConfig(m_cmdConfig2);

            while (i < nSamples)
            {
                //m_SPI.WriteRead(m_sampleBuffer, 0, 2, m_recordingBuffer, i * 2, 2, 2);
                i++;
            }
        }


        /// <summary>
        /// Request recording to stop
        /// </summary>
        public void StopRecording()
        {
            if (!m_stopRecordingRequested)
                m_stopRecordingRequested = true;
        }

        #region Ogg Vorbis Recording

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordingStream"></param>
        /// <param name="oggPatch"></param>
        public void RecordOggVorbis(Stream recordingStream, ushort[] oggPatch)
        {
            m_isRecording = true;
            m_stopRecordingRequested = false;
            m_oggPatch = oggPatch;

            m_recordingStream = recordingStream;
            m_recordingThread = new Thread(new ThreadStart(this.RecordOggVorbisThreadFunction));
            m_recordingThread.Start();
        }

        private void RecordOggVorbisThreadFunction()
        {
            Reset();

            SetVolume(255, 255);

            CommandWrite(SCI_CLOCKF, 0xc000);

            CommandWrite(SCI_BASS, 0x0000);
            CommandWrite(SCI_AIADDR, 0x0000);

            CommandWrite(SCI_WRAMADDR, 0xC01A);
            CommandWrite(SCI_WRAM, 0x0002);

            //Load Ogg Vorbis Encoder
            WritePatchFromArray(m_oggPatch);

            CommandWrite(SCI_MODE, (ushort)(CommandRead(SCI_MODE) | SM_ADPCM | SM_LINE1));
            CommandWrite(SCI_AICTRL1, 0);
            CommandWrite(SCI_AICTRL2, 4096);

            ////0x8000 - MONO
            ////0x8080 - STEREO
            CommandWrite(SCI_AICTRL0, 0x0000);

            CommandWrite(SCI_AICTRL3, 0);
            //CommandWrite(SCI_AICTRL3, 0x40);

            CommandWrite(SCI_AIADDR, 0x0034);

            while (m_dreq.Read() == false)
                ;

            int totalSamples = 0;

            bool stopRecording = false;
            bool stopRecordingRequestInProgress = false;

            int samples = 0;

            while (!stopRecording)
            {
                if (m_stopRecordingRequested && !stopRecordingRequestInProgress)
                {
                    CommandWrite(SCI_AICTRL3, 0x0001);
                    m_stopRecordingRequested = false;
                    stopRecordingRequestInProgress = true;
                }

                if (stopRecordingRequestInProgress)
                {
                    stopRecording = ((CommandRead(SCI_AICTRL3) & 0x0002) != 0);
                }

                samples = CommandRead(SCI_HDAT1);
                if (samples > 0)
                {
                    totalSamples = samples;// > 512 ? 512 : samples;

                    ReadData(totalSamples);
                    if (m_recordingStream != null)
                        m_recordingStream.Write(m_recordingBuffer, 0, totalSamples << 1);

                    //Debug.Print("I have: " + samples.ToString() + " samples");
                }
                //Debug.Print("no data");
            }

            samples = CommandRead(SCI_HDAT1);
            while (samples > 0)
            {
                totalSamples = samples;// > 512 ? 512 : samples;

                ReadData(totalSamples);
                if (m_recordingStream != null)
                    m_recordingStream.Write(m_recordingBuffer, 0, totalSamples << 1);

                Debug.Print("I have: " + samples.ToString() + " samples");

                samples = CommandRead(SCI_HDAT1);
            }

            if (m_recordingStream != null)
            {
                m_recordingStream.Close();
                m_recordingStream = null;
            }

            Reset();

            m_isRecording = false;
            m_oggPatch = null;
        }

        #endregion
    }
}
