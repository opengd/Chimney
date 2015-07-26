using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimney.MPD
{
    
    public class ChimneyMPDDirectClient : ChimneyMPDBase
    {

        public bool idle = false;

        DispatcherTimer holdConnectionTimer;


        private bool silent;
        public bool Silent
        {
            get { return silent; }
            set { silent = value; }
        }

        public ChimneyMPDDirectClient(string host, string port, string password = null, bool silent = false, int timeout = 5)
        {
            if (!string.IsNullOrEmpty(host)) this.host = host;
            if (!string.IsNullOrEmpty(port)) this.port = port;
            if(!string.IsNullOrEmpty(password)) this.password = password;
            this.silent = silent;

            holdConnectionTimer = new DispatcherTimer();
            holdConnectionTimer.Interval = new TimeSpan(0, 0, timeout);
            holdConnectionTimer.Tick += holdConnectionTimer_Tick; 
        }

        async void holdConnectionTimer_Tick(object sender, object e)
        {
            await Close(false);
            holdConnectionTimer.Stop();
        }


        public async Task<bool> Ping(bool silent = true)
        {
            await Connect(silent);
            bool suc = await Send(CLIENT_PING, streamSocket, "", silent);
            if (suc)
            {
                string responseString = await Response();
                await Disconnect();
                if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            }
            return false;
        }

        public async Task<bool> Connect(bool silent = false)
        {
            if (holdConnectionTimer.IsEnabled)
            {
                holdConnectionTimer.Stop();
                holdConnectionTimer.Start();
                return true;
            }
            
            bool suc = await Connect(this.host, this.port, this.password, silent);
            if(suc) holdConnectionTimer.Start();
            return suc;
        }

        public async Task<bool> Disconnect(bool silent = false)
        {
            //return await Close(false);
            return true;
        }

        public async Task<bool> CheckConnection(bool silent = true, bool retry = false)
        {
            await Connect(silent);
            bool suc = await Send(CLIENT_PING, streamSocket, "", silent, retry);
            if (suc)
            {
                string responseString = await Response();
                await Disconnect();
                if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            }
            return false;
        }

        public async Task<List<Output>> Outputs()
        {
            await Connect(this.silent);
            await Send(CLIENT_OUTPUTS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> respons = GetResponseOrder(responseString);

            List<Output> outputs = new List<Output>();

            foreach (Dictionary<string, string> d in respons)
            {
                outputs.Add(new Output(d));
            }

            return outputs;
        }

        public async Task<bool> EnableOutput(string outputid = "0")
        {
            await Connect(this.silent);
            await Send(CLIENT_ENABLEOUTPUT, streamSocket, outputid);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> SavePlaylist(string playlist)
        {
            await Connect(this.silent);
            await Send(CLIENT_SAVE, streamSocket, "\"" + playlist + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> RemovePlaylist(string playlist)
        {
            await Connect(this.silent);
            await Send(CLIENT_RM, streamSocket, "\"" + playlist + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<List<StoredPlaylist>> ListPlaylists()
        {
            await Connect(this.silent);
            await Send(CLIENT_LISTPLAYLISTS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseOrder(responseString);

            List<StoredPlaylist> playlists = new List<StoredPlaylist>();

            foreach (Dictionary<string, string> d in responseList)
            {
                playlists.Add(new StoredPlaylist(d));
            }

            return playlists;
        }

        public async Task<bool> LoadPlaylist(string playlist)
        {
            await Connect(this.silent);
            await Send(CLIENT_LOAD, streamSocket, "\"" + playlist + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> LoadPlaylist(string playlist, int start, int end)
        {
            await Connect(this.silent);
            await Send(CLIENT_LOAD, streamSocket, "\"" + playlist + "\" " + start.ToString() + ":" + end.ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> DisableOutput(string outputid = "0")
        {
            await Connect(this.silent);
            await Send(CLIENT_DISABLEOUTPUT, streamSocket, outputid);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Play(int songid)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAY, streamSocket, songid.ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlayId(int songid)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYID, streamSocket, songid.ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Stop()
        {
            await Connect(this.silent);
            await Send(CLIENT_STOP, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Pause()
        {
            await Connect(this.silent);
            await Send(CLIENT_PAUSE, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Next()
        {
            await Connect(this.silent);
            await Send(CLIENT_NEXT, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Previous()
        {
            await Connect(this.silent);
            await Send(CLIENT_PREVIOUS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Volume(double volume)
        {
            await Connect(this.silent);
            await Send(CLIENT_SETVOL, streamSocket, volume.ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Shuffle()
        {
            await Connect(this.silent);
            await Send(CLIENT_SHUFFLE, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> Prio(int id, int prio)
        {
            await Connect(this.silent);
            await Send(CLIENT_PRIOID, streamSocket, prio + " " + id);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> UpdateDb(string URI = "")
        {
            await Connect(this.silent);
            await Send(CLIENT_UPDATE, streamSocket, "\"" + URI + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Contains("updating_db")) return true;
            else return false;
        }

        public async Task<bool> IsDbUpdating()
        {
            Status status = await GetStatus();
            if (string.IsNullOrEmpty(status.UpdatingDb)) return false;
            else return true;
        }

        public async Task<List<SongTag>> Playlist()
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTINFO, streamSocket);
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseLists(responseString);

            List<SongTag> playlist = new List<SongTag>();

            foreach (Dictionary<string, string> d in responseList)
            {
                playlist.Add(new SongTag(TagTypes.File, d));
            }

            return playlist;
        }

        public async Task<List<SongTag>> PlaylistChanges(int version)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLCHANGES, streamSocket, "\"" + version + "\"");
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseLists(responseString);

            List<SongTag> playlist = new List<SongTag>();

            foreach (Dictionary<string, string> d in responseList)
            {
                playlist.Add(new SongTag(TagTypes.File, d));
            }

            return playlist;
        }

        public async Task<List<SongTag>> Playlist(string playlistname)
        {
            await Connect(this.silent);
            await Send(CLIENT_LISTPLAYLISTINFO, streamSocket, "\"" + playlistname + "\"");
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseLists(responseString);

            List<SongTag> playlist = new List<SongTag>();

            int i = 0;
            foreach (Dictionary<string, string> d in responseList)
            {
                playlist.Add(new SongTag(TagTypes.File, d, i, playlistname));
                i++;
            }

            return playlist;
        }

        public async Task<bool> PlaylistMoveItem(int id, int pos)
        {
            await Connect(this.silent);
            await Send(CLIENT_MOVEID, streamSocket, "\"" + id + "\" \"" + pos + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaylistMoveItem(int id, int pos, string playlistname)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTMOVE, streamSocket, "\"" + playlistname + "\" " + "\"" + id + "\" \"" + pos + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaylistClear()
        {
            await Connect(this.silent);
            await Send(CLIENT_CLEAR, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaylistClear(string playlistname)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTCLEAR, streamSocket, "\"" + playlistname + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> AddToPlaylist(string file)
        {
            await Connect(this.silent);
            await Send(CLIENT_ADD, streamSocket, "\"" + file + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<string> AddIdToPlaylist(string file)
        {
            await Connect(this.silent);
            await Send(CLIENT_ADDID, streamSocket, "\"" + file + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.EndsWith(LINEBREAK + RESPONSE_OK + LINEBREAK))
            {
                responseString = responseString.Replace(LINEBREAK + RESPONSE_OK + LINEBREAK, "");
                responseString = responseString.Replace(" ", "");
                string[] responseList = responseString.Split(":".ToCharArray());
                if (responseList.Length > 1) return responseList[1];
            }
            return string.Empty;
        }

        public async Task<bool> SubscribeToChannel(string channel)
        {
            await Connect(this.silent);
            await Send(CLIENT_SUBSCRIBE, streamSocket, channel);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> UnsubscribeToChannel(string channel)
        {
            await Connect(this.silent);
            await Send(CLIENT_UNSUBSCRIBE, streamSocket,  channel);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> SendMessage(string channel, string message)
        {
            await Connect(this.silent);
            await Send(CLIENT_SENDMESSAGE, streamSocket, channel  + " \"" + message + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }


        public async Task<List<Channel>> Channels()
        {
            await Connect(this.silent);
            await Send(CLIENT_CHANNELS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseOrder(responseString);

            List<Channel> channels = new List<Channel>();

            foreach (Dictionary<string, string> d in responseList)
            {
                channels.Add(new Channel(d));
            }
            return channels;
        }

        public async Task<List<Message>> ReadMessages()
        {
            await Connect(this.silent);
            await Send(CLIENT_CHANNELS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            List<Dictionary<string, string>> responseList = GetResponseOrder(responseString);

            List<Message> messages = new List<Message>();

            foreach (Dictionary<string, string> d in responseList)
            {
                messages.Add(new Message(d));
            }
            return messages;
        }

        public async Task<bool> AddToPlaylist(string file, string playlistname)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTADD, streamSocket, "\"" + playlistname + "\" " + "\"" + file + "\"");
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaybackSettingsRandom(bool settings)
        {
            await Connect(this.silent);
            await Send(CLIENT_RANDOM, streamSocket, Convert.ToInt32(settings).ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaybackSettingsConsume(bool settings)
        {
            await Connect(this.silent);
            await Send(CLIENT_CONSUME, streamSocket, Convert.ToInt32(settings).ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaybackSettingsRepeat(bool settings)
        {
            await Connect(this.silent);
            await Send(CLIENT_REPEAT, streamSocket, Convert.ToInt32(settings).ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaybackSettingsSingle(bool settings)
        {
            await Connect(this.silent);
            await Send(CLIENT_SINGLE, streamSocket, Convert.ToInt32(settings).ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> PlaybackSettingsCrossfade(int seconds)
        {
            await Connect(this.silent);
            await Send(CLIENT_CROSSFADE, streamSocket, seconds.ToString());
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> RemoveFromPlaylist(int id)
        {
            await Connect(this.silent);
            await Send(CLIENT_DELETEID, streamSocket, "" + id);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> RemoveFromPlaylist(int id, string playlistname)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTDELETE, streamSocket, "\"" + playlistname + "\" " + id);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> SeekCurrent(int time)
        {
            await Connect(this.silent);
            await Send(CLIENT_SEEKCUR, streamSocket, "" + time);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> SeekId(int time, int id)
        {
            await Connect(this.silent);
            await Send(CLIENT_SEEKID, streamSocket, id + " " + time);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<bool> ClearError()
        {
            await Connect(this.silent);
            await Send(CLIENT_CLEARERROR, streamSocket);
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<string> DebugSend(string s)
        {
            await Connect(this.silent);
            await Send(s, streamSocket);
            string responseString = await Response();
            await Disconnect();

            return responseString;
        }

        public async Task<List<SongTag>> Search(string type, string searchstring)
        {
            await Connect(this.silent);
            List<SongTag> songlist = new List<SongTag>();
            List<Dictionary<string, string>> responsList = new List<Dictionary<string, string>>();
            TagType tagType;

            await Send(CLIENT_SEARCH, streamSocket, "\"" + type + "\"  \"" + searchstring + "\"");
            string responseString = await Response();
            await Disconnect();

            responsList = GetResponseLists(responseString);
            tagType = TagTypes.FileOrDirectory;

            int i = 0;
            foreach (Dictionary<string, string> d in responsList)
            {
                //songlist.Add(new SongTag(tagType, d, currentPlaylist));
                songlist.Add(new SongTag(tagType, d, i));
                i++;
            }

            return songlist;
        }

        public async Task<bool> SearchAddToPlaylist(string playlist, string type, string searchstring)
        {
            await Connect(this.silent);
            if (!string.IsNullOrEmpty(playlist))
            {
                await Send(CLIENT_SEARCHADDPL, streamSocket, "\"" + playlist + "\" \"" + type + "\"  \"" + searchstring + "\"");
            }
            else
            {
                await Send(CLIENT_SEARCHADD, streamSocket,  "\"" + type + "\"  \"" + searchstring + "\"");
            }
            string responseString = await Response();
            await Disconnect();

            if (responseString.Equals(RESPONSE_OK + LINEBREAK)) return true;
            return false;
        }

        public async Task<List<SongTag>> Songlist(string URI = "", string ordertype ="", List<SongTag> currentPlaylist = null)
        {
            await Connect(this.silent);
            List<SongTag> songlist = new List<SongTag>();
            List<Dictionary<string, string>> responsList = new List<Dictionary<string, string>>();

            TagType tagType;

            if (ordertype.Equals("artist"))
            {
                await Send(CLIENT_LIST, streamSocket, "artist");
                string responseString = await Response();
                await Disconnect();

                responsList = GetResponseOrder(responseString);

                responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["Artist"].CompareTo(d2["Artist"]); });

                tagType = TagTypes.Artist;
            }
            else if(ordertype.Equals("album"))
            {
                if(URI.Equals("")) await Send(CLIENT_LIST, streamSocket, "album");
                else await Send(CLIENT_LIST, streamSocket, "album " + "\"" + URI + "\"");               
                string responseString = await Response();
                await Disconnect();

                responsList = GetResponseOrder(responseString);

                responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["Album"].CompareTo(d2["Album"]); });

                tagType = TagTypes.Album;
            }
            else if (ordertype.Equals("genre"))
            {
                if (URI.Equals(""))
                {
                    await Send(CLIENT_LIST, streamSocket, "genre");
                    string responseString = await Response();
                    await Disconnect();

                    responsList = GetResponseOrder(responseString);

                    responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["Genre"].CompareTo(d2["Genre"]); });

                    tagType = TagTypes.Genre;
                }
                else
                {
                    await Send(CLIENT_FIND, streamSocket, "genre " + "\"" + URI + "\"");
                    string responseString = await Response();
                    await Disconnect();

                    responsList = GetResponseLists(responseString);

                    //responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["file"].CompareTo(d2["file"]); });

                    tagType = TagTypes.FileOrDirectory;
                }                              
            }
            else if(ordertype.Equals("find"))
            {
                await Send(CLIENT_FIND, streamSocket, "album " + "\"" + URI + "\"");
                string responseString = await Response();
                await Disconnect();

                responsList = GetResponseLists(responseString);

                //responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["file"].CompareTo(d2["file"]); });

                tagType = TagTypes.FileOrDirectory;
            }
            else if (ordertype.Equals("search"))
            {
                await Send(CLIENT_SEARCH, streamSocket, "any " + "\"" + URI + "\"");
                string responseString = await Response();
                await Disconnect();

                responsList = GetResponseLists(responseString);

                //responsList.Sort(delegate(Dictionary<string, string> d1, Dictionary<string, string> d2) { return d1["file"].CompareTo(d2["file"]); });

                tagType = TagTypes.FileOrDirectory;

            }
            else
            {
                await Send(CLIENT_LSINFO, streamSocket, "\"" + URI + "\"");
                string responseString = await Response();
                await Disconnect();

                responsList = GetResponseLists(responseString);

                tagType = TagTypes.FileOrDirectory;

            }

            int i = 0;
            foreach(Dictionary<string, string> d in responsList)
            {
                songlist.Add(new SongTag(tagType, d, i));
                i++;
            }

            return songlist;
        }

        public async Task<Status> GetStatus()
        {
            await Connect(this.silent);
            await Send(CLIENT_STATUS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            Dictionary<string, string> responseList = GetResponseDictionary(responseString);
            return new Status(responseList);
        }

        public async Task<Stats> Stats()
        {
            await Connect(this.silent);
            await Send(CLIENT_STATS, streamSocket);
            string responseString = await Response();
            await Disconnect();

            Dictionary<string, string> stats = GetResponseDictionary(responseString);
            return new Stats(stats);
        }

        public async Task<SongTag> CurrentSong()
        {
            await Connect(this.silent);
            await Send(CLIENT_CURRENTSONG, streamSocket);
            string responseString = await Response();
            await Disconnect();

            Dictionary<string, string> responseList = GetResponseDictionary(responseString);
            return new SongTag(TagTypes.File, responseList);
        }

        public async Task<SongTag> PlaylistId(int Id)
        {
            await Connect(this.silent);
            await Send(CLIENT_PLAYLISTID, streamSocket, Id.ToString());
            string responseString = await Response();
            await Disconnect();

            Dictionary<string, string> responseList = GetResponseDictionary(responseString);
            return new SongTag(TagTypes.File, responseList);
        }
         

 
       
    }
}
