using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

namespace Chimney.MPD.Classes
{
    public class SongTag
    {
        public TagType TagType { get; set; }

        private bool _isEmpty;

        public bool IsEmpty
        {
            get { return this._isEmpty; }
        }

        public int Hash
        {
            get
            {
                return (file + Title + Artist + Album + directory + playlist).GetHashCode();
            }
        }

        private string _file;
        public string file
        {
            get
            {
                if (string.IsNullOrEmpty(_file)) return string.Empty;
                return _file;
            }
            set
            {
                _file = value;
            }
        }

        private string _directory;
        public string directory
        {
            get
            {
                if (string.IsNullOrEmpty(_directory)) return string.Empty;
                return _directory;
            }
            set
            {
                _directory = value;
                file = _directory;
            }
        }

        private string _playlist;
        public string playlist
        {
            get
            {
                if (string.IsNullOrEmpty(_playlist)) return string.Empty;
                return _playlist;
            }
            set
            {
                _playlist = value;
            }
        }

        private string _LastModified;
        public string LastModified
        {
            get
            {
                if (string.IsNullOrEmpty(_LastModified)) return string.Empty;
                return _LastModified;
            }
            set
            {
                _LastModified = value;
            }
        }

        private int _Time = 0;
        public int Time
        {
            get { return _Time; }
            set { _Time = value; }
        }

        public string TimeToString
        {
            get
            {
                if (Time == 0) return string.Empty;
                return (TimeSpan.FromSeconds(Time)).ToString();
            }
        }

        private string _Artist;
        public string Artist
        {
            get
            {
                return (!string.IsNullOrEmpty(_Artist)) ? _Artist : string.Empty;
            }
            set
            {
                _Artist = value;
            }
        }

        private string _Title;
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_Title) && !string.IsNullOrEmpty(_file))
                {
                    string[] s = file.Split("/".ToCharArray());
                    return string.IsNullOrEmpty(s.Last<string>()) ? _file : s.Last<string>();
                }
                else if (string.IsNullOrEmpty(_Title) && !string.IsNullOrEmpty(playlist)) return playlist;
                else if (string.IsNullOrEmpty(_Title)) return string.Empty;
                    
                return _Title;
            }
            set
            {
                _Title = value;
            }
        }

        private string _Name;
        public string Name
        {
            get
            { return _Name; }
            set
            {
                _Name = value;
            }
        }

        private string _Album;
        public string Album
        {
            get
            {
                if (string.IsNullOrEmpty(_Album)) return string.Empty;
                return _Album;
            }
            set
            {
                _Album = value;
            }
        }

        private string _Date;
        public string Date
        {
            get
            {
                if (string.IsNullOrEmpty(_Date)) return string.Empty;
                return _Date;
            }
            set
            {
                _Date = value;
            }
        }

        private string _Genre;
        public string Genre
        {
            get
            {
                if (string.IsNullOrEmpty(_Genre)) return string.Empty;
                return _Genre;
            }
            set
            {
                _Genre = value;
            }
        }

        private string _Track;
        public string Track
        {
            get
            {
                if (string.IsNullOrEmpty(_Track)) return string.Empty;
                return _Track;
            }
            set
            {
                _Track = value;
            }
        }

        private string _AlbumArtist;
        public string AlbumArtist
        {
            get
            {
                if (string.IsNullOrEmpty(_AlbumArtist)) return string.Empty;
                return _AlbumArtist;
            }
            set
            {
                _AlbumArtist = value;
            }
        }

        private string _Disc;
        public string Disc
        {
            get
            {
                if (string.IsNullOrEmpty(_Disc)) return string.Empty;
                return _Disc;
            }
            set
            {
                _Disc = value;
            }
        }

        private int _pos = -1;
        public int Pos
        {
            get
            {
                return _pos;
            }
            set {
                _pos = value;
            }
        }
        public int Id = -1;
        public int Prio = -1;

        public List<KeyValuePair<string, string>> SourceList { get; set; }

        public static SongTag Empty
        {
            get
            {
                return new SongTag();
            }
        }

        public SongTag()
        {
            this._isEmpty = true;
            TagType = TagType.Empty;
        }

        public SongTag(TagType tagType, List<KeyValuePair<string, string>> keyValuePairList, int pos)
        {
            Init(tagType, keyValuePairList, null);

            this.Pos = pos;
        }
        public SongTag(TagType tagType, List<KeyValuePair<string, string>> keyValuePairList, int pos, string playlist)
        {
            Init(tagType, keyValuePairList, null);

            this.Pos = pos;
            this.playlist = playlist;
        }

        public SongTag(TagType tagType, List<KeyValuePair<string, string>> keyValuePairList)
        {
            Init(tagType, keyValuePairList, null);
        }

        public SongTag(List<KeyValuePair<string, string>> keyValuePairList, int Pos)
        {
            if (keyValuePairList == null) return;

            TagType tagType;

            var first = keyValuePairList.FirstOrDefault();

            switch (first.Key)
            {
                case ("Artist"):
                    tagType = TagType.Artist;
                    break;
                case ("Album"):
                    tagType = TagType.Album;
                    break;
                case ("Genre"):
                    tagType = TagType.Genre;
                    break;
                default:
                    tagType = TagType.FileOrDirectory;
                    break;
            }

            Init(tagType, keyValuePairList, null);
            this.Pos = Pos;
        }


        /*
        public SongTag(Dictionary<string, string> dictionary, int Pos)
        {
            TagType tagType;

            if (dictionary.ContainsKey("file")) tagType = TagType.FileOrDirectory;
            else if (dictionary.ContainsKey("directory")) tagType = TagType.FileOrDirectory;
            else if (dictionary.ContainsKey("playlist")) tagType = TagType.FileOrDirectory;
            else if (dictionary.ContainsKey("Artist")) tagType = TagType.Artist;
            else if (dictionary.ContainsKey("Album")) tagType = TagType.Album;
            else if (dictionary.ContainsKey("Genre")) tagType = TagType.Genre;
            else tagType = TagType.FileOrDirectory;

            Init(tagType, dictionary, null);
            this.Pos = Pos;
        }
        */

        public SongTag(Windows.Storage.ApplicationDataContainer dictionary, int Pos)
        {
            TagType tagType;

            if (dictionary.Values.ContainsKey("file")) tagType = TagType.FileOrDirectory;
            else if (dictionary.Values.ContainsKey("directory")) tagType = TagType.FileOrDirectory;
            else if (dictionary.Values.ContainsKey("playlist")) tagType = TagType.FileOrDirectory;
            else if (dictionary.Values.ContainsKey("Artist")) tagType = TagType.Artist;
            else if (dictionary.Values.ContainsKey("Album")) tagType = TagType.Album;
            else if (dictionary.Values.ContainsKey("Genre")) tagType = TagType.Genre;
            else tagType = TagType.FileOrDirectory;

            Init(tagType, dictionary, null);
            this.Pos = Pos;
        }

        /*
        public SongTag(TagType tagType, Dictionary<string, string> dictionary)
        {
            Init(tagType, dictionary, null);
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, List<SongTag> playlist)
        {
            Init(tagType, dictionary, playlist);
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, int Pos)
        {
            Init(tagType, dictionary, null);
            this.Pos = Pos;
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, int Pos, List<SongTag> playlist)
        {
            Init(tagType, dictionary, playlist);
            this.Pos = Pos;
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, int Pos, string playlistname)
        {
            Init(tagType, dictionary, null);

            this.Pos = Pos;
            this.playlist = playlistname;
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, string playlistname)
        {
            Init(tagType, dictionary, null);

            this.playlist = playlistname;
        }

        public SongTag(TagType tagType, Dictionary<string, string> dictionary, int Pos, List<SongTag> playlist, string playlistname)
        {
            Init(tagType, dictionary, playlist);

            this.Pos = Pos;
            this.playlist = playlistname;

        }
        */

        private void Init(TagType tagType, List<KeyValuePair<string, string>> list, List<SongTag> playlist = null)
        {
            if (list == null) list = new List<KeyValuePair<string, string>>();

            this.TagType = tagType;

            this.SourceList = list;

            this._isEmpty = (list.Count == 0) ? true : false;

            bool convsuc = false;
            int convi = 0;

            foreach (var keyValuePair in list)
            {
                switch (keyValuePair.Key)
                {
                    case "file":
                        this.file = keyValuePair.Value;
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.File;
                        break;
                    case "directory":
                        this.directory = keyValuePair.Value;
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.Directory;
                        break;
                    case "playlist":
                        this.playlist = keyValuePair.Value;
                        this.TagType = TagType.Playlist;
                        break;
                    case "Last-Modified":
                        this.LastModified = keyValuePair.Value;
                        break;
                    case "Time":
                        convsuc = int.TryParse(keyValuePair.Value, out convi);
                        this.Time = (convsuc) ? convi : 0;
                        break;
                    case "Artist":
                        this.Artist = keyValuePair.Value;
                        break;
                    case "Title":
                        this.Title = keyValuePair.Value;
                        break;
                    case "Album":
                        this.Album = keyValuePair.Value;
                        break;
                    case "Date":
                        this.Date = keyValuePair.Value;
                        break;
                    case "Genre":
                        this.Genre = keyValuePair.Value;
                        break;
                    case "Track":
                        this.Track = keyValuePair.Value;
                        break;
                    case "AlbumArtist":
                        this.AlbumArtist = keyValuePair.Value;
                        break;
                    case "Disc":
                        this.Disc = keyValuePair.Value;
                        break;
                    case "Name":
                        this.Name = keyValuePair.Value;
                        break;
                    case "Pos":
                        convsuc = int.TryParse(keyValuePair.Value, out convi);
                        this.Pos = (convsuc) ? convi : 0;
                        break;
                    case "Id":
                        convsuc = int.TryParse(keyValuePair.Value, out convi);
                        this.Id = (convsuc) ? convi : 0;
                        break;
                    case "Prio":
                        convsuc = int.TryParse(keyValuePair.Value, out convi);
                        this.Prio = (convsuc) ? convi : 0;
                        break;
                    default:
                        break;
                }
            }

            if (playlist != null)
            {
                foreach (SongTag st in playlist)
                {
                    if (st.file.Equals(this.file))
                    {
                        this.Pos = st.Pos;
                        this.Id = st.Id;
                    }
                }
            }

            if (this.TagType == TagType.File && string.IsNullOrEmpty(this.Title))
            {
                string[] s = this.file.Split("/".ToCharArray());
                if (s.Length > 0) this.Title = s.Last<string>();
            }
        }

        /*
        private void Init(TagType tagType, Dictionary<string, string> dictionary, List<SongTag> playlist = null)
        {
            if (dictionary == null) return;

            this.TagType = tagType;

            //this.SourceDictionary = dictionary;

            if (dictionary.Count == 0)
            {
                this._isEmpty = true;
            }
            else
            {
                this._isEmpty = false;
            }

            bool convsuc = false;
            int convi = 0;

            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "file":
                        this.file = dictionary[key];
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.File;
                        break;
                    case "directory":
                        this.directory = dictionary[key];
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.Directory;
                        break;
                    case "playlist":
                        this.playlist = dictionary[key];
                        this.TagType = TagType.Playlist;
                        break;
                    case "Last-Modified":
                        this.LastModified = dictionary[key];
                        break;
                    case "Time":
                        convsuc = int.TryParse(dictionary[key], out convi);
                        this.Time = (convsuc) ? convi : 0;
                        break;
                    case "Artist":
                        this.Artist = dictionary[key];
                        break;
                    case "Title":
                        this.Title = dictionary[key];
                        break;
                    case "Album":
                        this.Album = dictionary[key];
                        break;
                    case "Date":
                        this.Date = dictionary[key];
                        break;
                    case "Genre":
                        this.Genre = dictionary[key];
                        break;
                    case "Track":
                        this.Track = dictionary[key];
                        break;
                    case "AlbumArtist":
                        this.AlbumArtist = dictionary[key];
                        break;
                    case "Disc":
                        this.Disc = dictionary[key];
                        break;
                    case "Pos":
                        convsuc = int.TryParse(dictionary[key], out convi);
                        this.Pos = (convsuc) ? convi : 0;
                        break;
                    case "Id":
                        convsuc = int.TryParse(dictionary[key], out convi);
                        this.Id = (convsuc) ? convi : 0;
                        break;
                    case "Prio":
                        convsuc = int.TryParse(dictionary[key], out convi);
                        this.Prio = (convsuc) ? convi : 0;
                        break;
                    default:
                        break;
                }
            }

            if (playlist != null)
            {
                foreach (SongTag st in playlist)
                {
                    if (st.file.Equals(this.file))
                    {
                        this.Pos = st.Pos;
                        this.Id = st.Id;
                    }
                }
            }

            if (this.TagType == TagType.File && string.IsNullOrEmpty(this.Title))
            {
                string[] s = this.file.Split("/".ToCharArray());
                if (s.Length > 0) this.Title = s.Last<string>();
            }
        }
        */

        private void Init(TagType tagType, Windows.Storage.ApplicationDataContainer dictionary, List<SongTag> playlist = null)
        {
            this.TagType = tagType;

            this.SourceList = new List<KeyValuePair<string, string>>();

            if (dictionary.Values.Count == 0)
            {
                this._isEmpty = true;
            }
            else
            {
                this._isEmpty = false;
            }

            foreach (string key in dictionary.Values.Keys)
            {
                SourceList.Add(new KeyValuePair<string, string>(key, (string)dictionary.Values[key]));
                switch (key)
                {
                    case "file":
                        this.file = (string)dictionary.Values[key];
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.File;
                        break;
                    case "directory":
                        this.directory = (string)dictionary.Values[key];
                        if (tagType == TagType.FileOrDirectory) this.TagType = TagType.Directory;
                        break;
                    case "playlist":
                        this.playlist = (string)dictionary.Values[key];
                        this.TagType = TagType.Playlist;
                        break;
                    case "Last-Modified":
                        this.LastModified = (string)dictionary.Values[key];
                        break;
                    case "Time":
                        this.Time = Convert.ToInt32((string)dictionary.Values[key]);
                        break;
                    case "Artist":
                        this.Artist = (string)dictionary.Values[key];
                        break;
                    case "Title":
                        this.Title = (string)dictionary.Values[key];
                        break;
                    case "Album":
                        this.Album = (string)dictionary.Values[key];
                        break;
                    case "Date":
                        this.Date = (string)dictionary.Values[key];
                        break;
                    case "Genre":
                        this.Genre = (string)dictionary.Values[key];
                        break;
                    case "Track":
                        this.Track = (string)dictionary.Values[key];
                        break;
                    case "AlbumArtist":
                        this.AlbumArtist = (string)dictionary.Values[key];
                        break;
                    case "Name":
                        this.Name = (string)dictionary.Values[key];
                        break;
                    case "Disc":
                        this.Disc = (string)dictionary.Values[key];
                        break;
                    case "Pos":
                        this.Pos = Convert.ToInt32((string)dictionary.Values[key]);
                        break;
                    case "Id":
                        this.Id = Convert.ToInt32((string)dictionary.Values[key]);
                        break;
                    case "Prio":
                        this.Prio = Convert.ToInt32((string)dictionary.Values[key]);
                        break;
                    default:
                        break;
                }
            }

            if (playlist != null)
            {
                foreach (SongTag st in playlist)
                {
                    if (st.file.Equals(this.file))
                    {
                        this.Pos = st.Pos;
                        this.Id = st.Id;
                    }
                }
            }

            if (this.TagType == TagType.File && string.IsNullOrEmpty(this.Title))
            {
                string[] s = this.file.Split("/".ToCharArray());
                if (s.Length > 0) this.Title = s.Last<string>();
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>();
        }

        public override string ToString()
        {
            string songtagstring = string.Empty;

            if(!string.IsNullOrEmpty(this.file)) songtagstring = songtagstring + "file: " + this.file + "\n";
            if(!string.IsNullOrEmpty(this.directory)) songtagstring = songtagstring + "directory: " + this.directory + "\n";
            if(!string.IsNullOrEmpty(this.playlist)) songtagstring = songtagstring + "playlist: " + this.playlist + "\n";
            if(!string.IsNullOrEmpty(this.LastModified)) songtagstring = songtagstring + "Last-Modified: " + this.LastModified + "\n";
            if(this.Time > 0) songtagstring = songtagstring + "Time: " + this.Time + "\n";
            if(!string.IsNullOrEmpty(this.Artist)) songtagstring = songtagstring + "Artist: " + this.Artist + "\n";
            if(!string.IsNullOrEmpty(this.Title)) songtagstring = songtagstring + "Title: " + this.Title + "\n";
            if(!string.IsNullOrEmpty(this.Album)) songtagstring = songtagstring + "Album: " + this.Album + "\n";
            if(!string.IsNullOrEmpty(this.Date)) songtagstring = songtagstring + "Date: " + this.Date + "\n";
            if(!string.IsNullOrEmpty(this.Genre)) songtagstring = songtagstring + "Genre: " + this.Genre + "\n";
            if(!string.IsNullOrEmpty(this.Track)) songtagstring = songtagstring + "Track: " + this.Track + "\n";
            if(!string.IsNullOrEmpty(this.AlbumArtist)) songtagstring = songtagstring + "AlbumArtist: " + this.AlbumArtist + "\n";
            if(!string.IsNullOrEmpty(this.Disc)) songtagstring = songtagstring + "Disc: " + this.Disc + "\n";
            if(this.Pos > -1) songtagstring = songtagstring + "Pos: " + this.Pos + "\n";
            if(this.Id > -1) songtagstring = songtagstring + "Id: " + this.Id + "\n";
            if(this.Prio > -1) songtagstring = songtagstring + "Prio: " + this.Prio + "\n";

            return songtagstring;
        }

        public static async Task<SongTag> GetSongTagFromFile(StorageFile storageFile)
        {
            SongTag songTag = new SongTag();
            Windows.Storage.FileProperties.MusicProperties mp = await storageFile.Properties.GetMusicPropertiesAsync();
            //songTag.file = seekpath.Item2 + "/" + file.Name;
            songTag.Title = mp.Title;
            songTag.Artist = mp.Artist;
            songTag.Album = mp.Album;
            songTag.AlbumArtist = mp.AlbumArtist;
            songTag.Date = mp.Year.ToString();
            songTag.Time = Convert.ToInt32(mp.Duration.TotalSeconds);
            if (mp.Genre.Count > 0) songTag.Genre = mp.Genre.First<string>();

            return songTag;
        }
    }
}
