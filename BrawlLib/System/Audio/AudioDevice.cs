﻿namespace System.Audio
{
    public abstract class AudioDevice
    {
        internal string _description, _driver;

        public string Description { get { return _description; } }
        public string Driver { get { return _driver; } }

        public static AudioDevice[] PlaybackDevices
        {
            get
            {
                switch (Environment.OSVersion.Platform) {
#if RSTMLIB
#else
                    case PlatformID.Win32NT: return wAudioDevice.PlaybackDevices;
#endif
                }
                return null;
            }
        }

        public static AudioDevice DefaultPlaybackDevice
        {
            get {
                switch (Environment.OSVersion.Platform) {
#if RSTMLIB
#else
                    case PlatformID.Win32NT: return wAudioDevice.DefaultPlaybackDevice;
#endif
                }
                return null;
            }
        }

        public static AudioDevice DefaultVoicePlaybackDevice
        {
            get {
                switch (Environment.OSVersion.Platform) {
#if RSTMLIB
#else
                    case PlatformID.Win32NT: return wAudioDevice.DefaultVoicePlaybackDevice;
#endif
                }
                return null;
            }
        }
    }
}
