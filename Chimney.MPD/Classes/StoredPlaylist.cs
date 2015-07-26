using System;
using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class StoredPlaylist
    {
        public string Playlist { get; set; }
        public string LastModified { get; set; }

        public override string ToString()
        {
            string storedplayliststring = string.Empty;

            storedplayliststring = storedplayliststring + "playlist: " + this.Playlist + "\n";
            if (string.IsNullOrEmpty(this.LastModified)) storedplayliststring = storedplayliststring + "Last-Modified: " + this.LastModified + "\n";
            else storedplayliststring = storedplayliststring + "Last-Modified: " + TimeSpan.FromSeconds(0) + "\n";

            return storedplayliststring;
        }

        public StoredPlaylist()
        {

        }

        public StoredPlaylist(List<KeyValuePair<string, string>> keyValuePairList)
        {
            foreach (var kv in keyValuePairList)
            {
                switch (kv.Key)
                {
                    case "playlist":
                        this.Playlist = kv.Value;
                        break;
                    case "Last-Modified":
                        this.LastModified = kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public StoredPlaylist(Dictionary<string, string> dictionary)
        {
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "playlist":
                        this.Playlist = dictionary[key];
                        break;
                    case "Last-Modified":
                        this.LastModified = dictionary[key];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
