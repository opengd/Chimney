using System;
using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class Stats
    {
        private string _albums;
        public string Albums
        {
            get
            {
                return (string.IsNullOrEmpty(this._albums)) ? "0" : this._albums; //string.Empty;
            }
            set
            {
                this._albums = value;
            }
        }

        private string _artists;
        public string Artists
        {
            get
            {
                return (string.IsNullOrEmpty(this._artists)) ? "0" : this._artists;
            }
            set
            {
                this._artists = value;
            }
        }

        private string _songs;
        public string Songs
        {
            get
            {
                return (string.IsNullOrEmpty(this._songs)) ? "0" : this._songs;
            }
            set
            {
                this._songs = value;
            }
        }
        private string _uptime;
        public string Uptime
        {
            get
            {
                if (string.IsNullOrEmpty(this._uptime)) return string.Empty;
                TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToInt32(this._uptime));
                return timeSpan.Days + "d " + timeSpan.Hours + "h " +
                    timeSpan.Minutes + "m " + timeSpan.Seconds + "s";
            }
            set
            {
                this._uptime = value;
            }
        }

        private string _db_playtime;
        public string DbPlaytime
        {
            get
            {
                if (string.IsNullOrEmpty(this._db_playtime)) return string.Empty;
                TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToInt32(this._db_playtime));
                return timeSpan.Days + "d " + timeSpan.Hours + "h " +
                    timeSpan.Minutes + "m " + timeSpan.Seconds + "s";
            }
            set
            {
                this._db_playtime = value;
            }
        }

        private string _db_update;
        public string DbUpdate
        {
            get
            {
                if (string.IsNullOrEmpty(this._db_update)) return string.Empty;
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                dateTime = dateTime.AddSeconds(Convert.ToDouble(this._db_update)).ToLocalTime();
                TimeSpan timeSpan = DateTime.Now - dateTime;
                return timeSpan.Days + "d " + timeSpan.Hours + "h " +
                    timeSpan.Minutes + "m " + timeSpan.Seconds + "s";
            }
            set
            {
                this._db_update = value;
            }
        }

        public TimeSpan DbUpdateTimeSpan
        {
            get
            {
                if (string.IsNullOrEmpty(this._db_update)) return new TimeSpan();
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                dateTime = dateTime.AddSeconds(Convert.ToDouble(this._db_update)).ToLocalTime();
                return DateTime.Now - dateTime;
            }
        }

        private string _playtime;
        public string Playtime
        {
            get
            {
                if (string.IsNullOrEmpty(this._playtime)) return string.Empty;
                TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToInt32(this._playtime));
                return timeSpan.Days + "d " + timeSpan.Hours + "h " +
                    timeSpan.Minutes + "m " + timeSpan.Seconds + "s";
            }
            set
            {
                this._playtime = value;
            }
        }

        public override string ToString()
        {
            string statstext = string.Empty;

            if(!string.IsNullOrEmpty(this.Artists)) statstext = statstext + "artists: " + this.Artists + "\n";
            else statstext = statstext + "artists: 0\n";

            if (!string.IsNullOrEmpty(this.Albums)) statstext = statstext + "albums: " + this.Albums + "\n";
            else statstext = statstext + "albums: 0\n";

            if (!string.IsNullOrEmpty(this.Songs)) statstext = statstext + "songs: " + this.Songs + "\n";
            else statstext = statstext + "songs: 0\n";

            if (!string.IsNullOrEmpty(this.Uptime)) statstext = statstext + "uptime: " + this.Uptime + "\n";
            else statstext = statstext + "uptime: 0\n";

            if (!string.IsNullOrEmpty(this.DbPlaytime)) statstext = statstext + "db_playtime: " + this._db_playtime + "\n";
            else statstext = statstext + "db_playtime: 0\n";

            if (!string.IsNullOrEmpty(this.DbUpdate)) statstext = statstext + "db_update: " + this.DbUpdate + "\n";
            else statstext = statstext + "db_update: 0\n";

            if (!string.IsNullOrEmpty(this.Playtime)) statstext = statstext + "playtime: " + this.Playtime + "\n";
            else statstext = statstext + "playtime: 0\n";

            return statstext;
        }

        public Stats()
        {
        }

        public Stats(List<KeyValuePair<string, string>> keyValuePair)
        {
            if (keyValuePair == null) return;

            foreach (var kv in keyValuePair)
            {
                switch (kv.Key)
                {
                    case "artists":
                        this.Artists = kv.Value;
                        break;
                    case "albums":
                        this.Albums = kv.Value;
                        break;
                    case "songs":
                        this.Songs = kv.Value;
                        break;
                    case "uptime":
                        this.Uptime = kv.Value;
                        break;
                    case "db_playtime":
                        this.DbPlaytime = kv.Value;
                        break;
                    case "db_update":
                        this.DbUpdate = kv.Value;
                        break;
                    case "playtime":
                        this.Playtime = kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public Stats(Dictionary<string, string> dictionary)
        {
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "artists":
                        this.Artists = dictionary[key];
                        break;
                    case "albums":
                        this.Albums = dictionary[key];
                        break;
                    case "songs":
                        this.Songs = dictionary[key];
                        break;
                    case "uptime":
                        this.Uptime = dictionary[key];
                        break;
                    case "db_playtime":
                        this.DbPlaytime = dictionary[key];
                        break;
                    case "db_update":
                        this.DbUpdate = dictionary[key];
                        break;
                    case "playtime":
                        this.Playtime = dictionary[key];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
