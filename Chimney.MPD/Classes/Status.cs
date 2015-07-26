using System;
using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class Status
    {
        private int _volume;
        public int Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
            }
        }

        public bool IsVolumeEnable
        {
            get
            {
                return (_volume != -1) ? true : false;
            }
        }

        public bool Repeat { get; set; }
        public bool Random { get; set; }
        public bool Single { get; set; }
        public bool Consume { get; set; }
        public int Playlist { get; set; }
        public int PlaylistLength { get; set; }

        private string _state;
        public string State
        {
            get
            {
                return (!string.IsNullOrEmpty(_state)) ? _state : "stop";
            }
            set
            {
                _state = value;
            }
        }

        public bool CanPlay
        {
            get
            {
                if (State.Equals("play")) return false;                
                return true;
            }
        }


        public bool CanPause
        {
            get
            {
                if (State.Equals("pause") || State.Equals("stop")) return false;
                return true;
            }
        }

        public bool CanStop
        {
            get
            {
                //if (State.Equals("stop")) return true;
                return true;
            }
        }

        public bool IsStopped
        {
            get
            {
                if (State.Equals("stop")) return true;
                return false;
            }
        }

        public int Song { get; set; }
        public int SongId { get; set; }
        public int NextSong { get; set; }
        public int NextSongId { get; set; }
        public double Time { get; set; }
        public double Elapsed { get; set; }
        public int Bitrate { get; set; }
        public int XFade { get; set; }

        private string _mixrampdb;
        public string MixRampDb
        {
            get
            {
                if (string.IsNullOrEmpty(_mixrampdb)) return string.Empty;
                return _mixrampdb;
            }
            set
            {
                _mixrampdb = value;
            }
        }

        private string _mixrampdelay;
        public string MixRampDelay
        {
            get
            {
                if (string.IsNullOrEmpty(_mixrampdelay)) return string.Empty;
                return _mixrampdelay;
            }
            set
            {
                _mixrampdelay = value;
            }
        }

        private string _audio;
        public string Audio
        {
            get
            {
                if (string.IsNullOrEmpty(_audio)) return string.Empty;
                else if (_audio.Equals("0:?:0")) return string.Empty;
                return _audio;
            }
            set
            {
                _audio = value;
            }
        }

        private string _updating_db;
        public string UpdatingDb
        {
            get
            {
                if (string.IsNullOrEmpty(_updating_db)) return string.Empty;
                return _updating_db;
            }
            set
            {
                _updating_db = value;
            }
        }

        public bool IsDbUpdating
        {
            get
            {
                return (!string.IsNullOrEmpty(UpdatingDb)) ? true : false;
            }
        }

        private string _error;
        public string Error
        {
            get
            {
                return (!string.IsNullOrEmpty(_error)) ? _error : string.Empty;
            }
            set
            {
                _error = value;
            }
        }

        public bool IsError
        {
            get
            {
                return (!string.IsNullOrEmpty(_error)) ? true : false;
            }
        }

        public Status()
        {

        }

        public override string ToString()
        {
            string statusstring = string.Empty;

            statusstring = statusstring + "volume: " + this.Volume + "\n";
            statusstring = statusstring + "repeat: " + Convert.ToInt32(this.Repeat) + "\n";
            statusstring = statusstring + "random: " + Convert.ToInt32(this.Random) + "\n";
            statusstring = statusstring + "single: " + Convert.ToInt32(this.Single) + "\n";
            statusstring = statusstring + "consume: " + Convert.ToInt32(this.Consume) + "\n";
            statusstring = statusstring + "playlist: " + this.Playlist + "\n";
            statusstring = statusstring + "playlistlength: " + this.PlaylistLength + "\n";
            statusstring = statusstring + "xfade: " + this.XFade + "\n";

            if (!string.IsNullOrEmpty(this.MixRampDb)) statusstring = statusstring + "mixrampdb: " + this.MixRampDb + "\n";
            else statusstring = statusstring + "mixrampdb: 0.00000\n";

            if (!string.IsNullOrEmpty(this.MixRampDelay)) statusstring = statusstring + "mixrampdelay: " + this.MixRampDelay + "\n";
            else statusstring = statusstring + "mixrampdelay: nan\n";

            if (string.IsNullOrEmpty(this.State)) statusstring = statusstring + "state: stop\n";
            else statusstring = statusstring + "state: " + this.State + "\n";

            bool stop = false;
            if (this.State.Equals("stop") || string.IsNullOrEmpty(this.State)) stop = true;

            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "song: " + this.Song + "\n";
            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "songid: " + this.SongId + "\n";

            int a = 0;
            a = (int)this.Time;
            //int b = Convert.ToInt32(this.Time*100);
            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "time: " + a + ":123" + "\n";
            
            a = (int)this.Elapsed;
            
            //b = Convert.ToInt32(this.Elapsed * 100);
            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "elapsed: " + a + ".122" + "\n";
            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "bitrate: " + this.Bitrate + "\n";

            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "audio: " + "44100:16:2" + "\n"; // this.Audio + "\n";

            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "nextsong: " + this.NextSong + "\n";
            if (this.PlaylistLength > 0 && !stop) statusstring = statusstring + "nextsongid: " + this.NextSongId + "\n";

            if(!string.IsNullOrEmpty(this.UpdatingDb)) statusstring = statusstring + "updating_db: " + this.UpdatingDb + "\n";
            if(!string.IsNullOrEmpty(this.Error)) statusstring = statusstring + "error: " + this.Error + "\n";

            return statusstring;
        }

        public Status (List<KeyValuePair<string, string>> keyValuePairList)
        {
            if (keyValuePairList == null) return;

            bool succonvert = false;

            int convi = 0;
            bool convb = false;
            double convd = 0;

            foreach (var kv in keyValuePairList)
            {
                switch (kv.Key)
                {
                    case "volume":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Volume = (succonvert) ? convi : -1;
                        break;
                    case "repeat":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Repeat = (succonvert && convi > 0) ? true : false;
                        break;
                    case "random":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Random = (succonvert && convi > 0) ? true : false;
                        break;
                    case "single":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Single = (succonvert && convi > 0) ? true : false;
                        break;
                    case "consume":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Consume = (succonvert && convi > 0) ? true : false;
                        break;
                    case "playlist":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Playlist = (succonvert) ? convi : 0;
                        break;
                    case "playlistlength":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.PlaylistLength = (succonvert) ? convi : 0;
                        break;
                    case "state":
                        this.State = kv.Value;
                        break;
                    case "song":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Song = (succonvert) ? convi : 0;
                        break;
                    case "songid":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.SongId = (succonvert) ? convi : 0;
                        break;
                    case "nextsong":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.NextSong = (succonvert) ? convi : 0;
                        break;
                    case "nextsongid":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.NextSongId = (succonvert) ? convi : 0;
                        break;
                    case "time":
                        succonvert = double.TryParse(kv.Value.Replace(":", "."), out convd);
                        this.Time = (succonvert) ? convd : 0;
                        break;
                    case "elapsed":
                        succonvert = double.TryParse(kv.Value, out convd);
                        this.Elapsed = (succonvert) ? convd : 0;
                        break;
                    case "bitrate":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.Bitrate = (succonvert) ? convi : 0;
                        break;
                    case "xfade":
                        succonvert = int.TryParse(kv.Value, out convi);
                        this.XFade = (succonvert) ? convi : 0;
                        break;
                    case "mixrampdb":
                        this.MixRampDb = kv.Value;
                        break;
                    case "mixrampdelay":
                        this.MixRampDelay = kv.Value;
                        break;
                    case "audio":
                        this.Audio = kv.Value;
                        break;
                    case "updating_db":
                        this.UpdatingDb = kv.Value;
                        break;
                    case "error":
                        this.Error = kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }


        public Status(Dictionary<string, string> dictionary)
        {
            bool succonvert = false;

            int convi = 0;
            bool convb = false;
            double convd = 0;

            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "volume":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Volume = (succonvert) ? convi : -1;
                        break;
                    case "repeat":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Repeat = (succonvert && convi > 0) ? true : false; 
                        break;
                    case "random":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Random = (succonvert && convi > 0) ? true : false;
                        break;
                    case "single":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Single = (succonvert && convi > 0) ? true : false;
                        break;
                    case "consume":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Consume = (succonvert && convi > 0) ? true : false;
                        break;
                    case "playlist":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Playlist = (succonvert) ? convi : 0;
                        break;
                    case "playlistlength":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.PlaylistLength = (succonvert) ? convi : 0;
                        break;
                    case "state":
                        this.State = dictionary[key];
                        break;
                    case "song":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Song = (succonvert) ? convi : 0;
                        break;
                    case "songid":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.SongId = (succonvert) ? convi : 0;
                        break;
                    case "nextsong":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.NextSong = (succonvert) ? convi : 0;
                        break;
                    case "nextsongid":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.NextSongId = (succonvert) ? convi : 0;
                        break;
                    case "time":
                        succonvert = double.TryParse(dictionary[key].Replace(":", "."), out convd);
                        this.Time = (succonvert) ? convd : 0;
                        break;
                    case "elapsed":
                        succonvert = double.TryParse(dictionary[key], out convd);
                        this.Elapsed = (succonvert) ? convd : 0;
                        break;
                    case "bitrate":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.Bitrate = (succonvert) ? convi : 0;
                        break;
                    case "xfade":
                        succonvert = int.TryParse(dictionary[key], out convi);
                        this.XFade = (succonvert) ? convi : 0;
                        break;
                    case "mixrampdb":
                        this.MixRampDb = dictionary[key];
                        break;
                    case "mixrampdelay":
                        this.MixRampDelay = dictionary[key];
                        break;
                    case "audio":
                        this.Audio = dictionary[key];
                        break;
                    case "updating_db":
                        this.UpdatingDb = dictionary[key];
                        break;
                    case "error":
                        this.Error = dictionary[key];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
