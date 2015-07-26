using Chimney.MPD.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chimney.MPD
{
    public class ChimneyMPDClient : ChimneyMPDBase
    {        
        public async Task<bool> Start()
        {
            return await Connect(this.host, this.port, this.password);
        }

        public async Task<List<Output>> Outputs()
        {
            int qId = await Send(MPDKeyWords.Client.Outputs.OUTPUTS);

            var outputs = new List<Output>();

            foreach (var d in await MPDKeyWords.Response.Encode(await Response(qId)))
                outputs.Add(new Output(d));

            return outputs;
        }

        public async Task<bool> EnableOutput(string outputid = "0")
        {
            int qId = await Send(MPDKeyWords.Client.Outputs.ENABLEOUTPUT, 
                new List<string>() { outputid });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK)) 
                ? true 
                : false;
        }

        public async Task<bool> SavePlaylist(string playlist)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.SAVE, 
                new List<string>(), 
                new List<string>() { playlist });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<bool> RemovePlaylist(string playlist)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.RM, 
                new List<string>(), 
                new List<string>() { playlist });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<bool> RenamePlaylist(string oldplaylistname, string newplaylistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.RENAME, 
                new List<string>(), 
                new List<string>() { oldplaylistname, newplaylistname });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<List<StoredPlaylist>> ListPlaylists()
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.LISTPLAYLISTS);

            List<StoredPlaylist> playlists = new List<StoredPlaylist>();

            foreach (var d in await MPDKeyWords.Response.Encode(await Response(qId)))
                playlists.Add(new StoredPlaylist(d));

            return playlists;
        }

        public async Task<bool> LoadPlaylist(string playlist)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.LOAD,
                new List<string>(),
                new List<string>() { playlist });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<bool> LoadPlaylist(string playlist, int start, int end)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.LOAD,
                new List<string>() { playlist },
                new List<string>() { start.ToString() + ":" + end.ToString() },
                true);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<bool> DisableOutput(string outputid = "0")
        {
            int qId = await Send(MPDKeyWords.Client.Outputs.DISABLEOUTPUT, 
                new List<string>() { outputid } );
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Play(int songposition)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.PLAY,
                new List<string>() { songposition.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlayId(int songid)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.PLAYID,
                new List<string>() { songid.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Stop()
        {
            int qId = await Send(MPDKeyWords.Client.Playback.STOP);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Pause()
        {
            int qId = await Send(MPDKeyWords.Client.Playback.PAUSE);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Next()
        {
            int qId = await Send(MPDKeyWords.Client.Playback.NEXT);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Previous()
        {
            int qId = await Send(MPDKeyWords.Client.Playback.PREVIOUS);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Volume(double volume)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.SETVOL,
                new List<string>() { volume.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Shuffle()
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.SHUFFLE);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> Prio(int id, int prio)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.PRIOID,
                new List<string>() { prio.ToString(), id.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> UpdateDb(string URI = "")
        {
            int qId = await Send(MPDKeyWords.Client.Database.UPDATE,
                new List<string>(),
                new List<string>() { URI });
            return ((await Response(qId)).Contains(MPDKeyWords.Response.UPDATING_DB))
               ? true
               : false;
        }

        public async Task<bool> IsDbUpdating()
        {
            Status status = await GetStatus();
            return (string.IsNullOrEmpty(status.UpdatingDb)) 
                ? false 
                : true;
        }

        public async Task<List<SongTag>> Playlist()
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.PLAYLISTINFO);

            var playlist = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                playlist.Add(new SongTag(TagType.File, response[i]));

            return playlist;
        }

        public async Task<List<SongTag>> ListAll()
        {
            int qId = await Send(MPDKeyWords.Client.Database.LISTALL);

            var list = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new SongTag(TagType.File, response[i]));

            return list;
        }

        public async Task<List<SongTag>> ListAllInfo()
        {
            int qId = await Send(MPDKeyWords.Client.Database.LISTALLINFO);

            var list = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new SongTag(TagType.FileOrDirectory, response[i]));

            return list;
        }

        public async Task<SongTag> FindFile(string file)
        {
            int qId = await Send(MPDKeyWords.Client.Database.FIND,
                new List<string>() { "file" },
                new List<string>() { file });

            var reponse = await MPDKeyWords.Response.Encode(await Response(qId));

            return (reponse.Count == 1)
                ? new SongTag(TagType.File, reponse[0])
                : SongTag.Empty;
        }

        public async Task<List<SongTag>> PlaylistChanges(int version)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.PLCHANGES,
                new List<string>(),
                new List<string>() { version.ToString() });

            var list = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new SongTag(TagType.File, response[i]));

            return list;
        }

        public async Task<List<SongTag>> Playlist(string playlistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.LISTPLAYLISTINFO,
                new List<string>(),
                new List<string>() { playlistname.ToString() });

            var list = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new SongTag(TagType.File, response[i], i, playlistname));

            return list;
        }

        public async Task<bool> PlaylistMoveItem(int id, int pos)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.MOVEID,
                new List<string>(),
                new List<string>() { id.ToString(), pos.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaylistMoveItem(int id, int pos, string playlistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.PLAYLISTMOVE,
                new List<string>(),
                new List<string>() { playlistname, id.ToString(), pos.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaylistClear()
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.CLEAR);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaylistClear(string playlistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.PLAYLISTCLEAR,
                new List<string>(),
                new List<string>() { playlistname });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> AddToPlaylist(string file)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.ADD,
                new List<string>(),
                new List<string>() { file });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<string> AddIdToPlaylist(string file)
        {
            if (string.IsNullOrEmpty(file)) return string.Empty;

            int qId = await Send(MPDKeyWords.Client.Playlist.ADDID, 
                new List<string>(),
                new List<string>() { file } );

            string response = await Response(qId);

            if (response.EndsWith(MPDKeyWords.Response.OK_LINEBREAK))
            {
                var list = await MPDKeyWords.Response.Encode(response);

                if (list.Count > 0)
                    return list.FirstOrDefault().FirstOrDefault().Value;
            }
            return string.Empty;
        }

        public async Task<bool> SubscribeToChannel(string channel)
        {
            int qId = await Send(MPDKeyWords.Client.ClientToClient.SUBSCRIBE,
                new List<string>() { channel });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> UnsubscribeToChannel(string channel)
        {
            int qId = await Send(MPDKeyWords.Client.ClientToClient.UNSUBSCRIBE,
                new List<string>() { channel });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> SendMessage(string channel, string message)
        {
            int qId = await Send(MPDKeyWords.Client.ClientToClient.SENDMESSAGE,
                new List<string>() { channel },
                new List<string>() { message });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }


        public async Task<List<Channel>> Channels()
        {
            int qId = await Send(MPDKeyWords.Client.ClientToClient.CHANNELS);

            var list = new List<Channel>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new Channel(response[i]));

            return list;
        }

        public async Task<List<Message>> ReadMessages()
        {
            int qId = await Send(MPDKeyWords.Client.ClientToClient.READMESSAGES);

            var list = new List<Message>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new Message(response[i]));

            return list;
        }

        public async Task<bool> AddToPlaylist(string file, string playlistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.PLAYLISTADD,
                new List<string>(),
                new List<string>() { playlistname, file });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaybackSettingsRandom(bool settings)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.RANDOM,
                new List<string>() { Convert.ToInt32(settings).ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaybackSettingsConsume(bool settings)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.CONSUME,
                new List<string>() { Convert.ToInt32(settings).ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaybackSettingsRepeat(bool settings)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.REPEAT,
                new List<string>() { Convert.ToInt32(settings).ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaybackSettingsSingle(bool settings)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.SINGLE,
                new List<string>() { Convert.ToInt32(settings).ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> PlaybackSettingsCrossfade(int seconds)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.CROSSFADE,
                new List<string>() { seconds.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> RemoveFromPlaylist(int id)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.DELETEID,
                new List<string>() { id.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> RemoveFromPlaylist(int id, string playlistname)
        {
            int qId = await Send(MPDKeyWords.Client.StoredPlaylist.PLAYLISTDELETE,
                new List<string>() { id.ToString() },
                new List<string>() { playlistname },                
                true);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> SeekCurrent(int time)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.SEEKCUR,
                new List<string>() { time.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> SeekId(int time, int id)
        {
            int qId = await Send(MPDKeyWords.Client.Playback.SEEKCUR,
                new List<string>() { id.ToString(), time.ToString() });
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<bool> ClearError()
        {
            int qId = await Send(MPDKeyWords.Client.Status.CLEARERROR);
            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<string> DebugSend(string s)
        {
            int qId = await Send(s);
            string responseString = await Response(qId);
            return responseString;
        }

        public async Task<List<SongTag>> Search(string type, string searchstring)
        {
            int qId = await Send(MPDKeyWords.Client.Database.SEARCH,
                new List<string>(),
                new List<string>() { type, searchstring });

            var list = new List<SongTag>();

            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            for (int i = 0; i < response.Count; i++)
                list.Add(new SongTag(TagType.FileOrDirectory, response[i], i));

            return list;
        }

        public async Task<bool> SearchAddToPlaylist(string playlist, string type, string searchstring)
        {
            var qId = (!string.IsNullOrEmpty(playlist))
                ? await Send(MPDKeyWords.Client.Database.SEARCHADDPL,
                    new List<string>(),
                    new List<string>() { playlist, type, searchstring })
                : await Send(MPDKeyWords.Client.Database.SEARCHADD,
                    new List<string>(),
                    new List<string>() { playlist, type, searchstring });

            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
               ? true
               : false;
        }

        public async Task<List<SongTag>> Songlist(string URI = "", string ordertype ="", List<SongTag> currentPlaylist = null)
        {
            string command = string.Empty;
            var arguments = new List<string>();
            var special = new List<string>();

            switch(ordertype)
            {
                case ("artist"):
                    command = MPDKeyWords.Client.Database.LIST;
                    arguments.Add("artist");
                    break;
                case ("album"):
                    command = MPDKeyWords.Client.Database.LIST;
                    arguments.Add("album");
                    if(!string.IsNullOrEmpty(URI))
                    {
                        arguments.Add("artist");
                        special.Add(URI);
                    }
                    break;
                case ("genre"):
                    arguments.Add("genre");
                    if(string.IsNullOrEmpty(URI))
                        command = MPDKeyWords.Client.Database.LIST;
                    else
                    {
                        command = MPDKeyWords.Client.Database.FIND;
                        special.Add(URI);
                    }
                    break;
                case ("find"):
                    command = MPDKeyWords.Client.Database.FIND;
                    arguments.Add("album");
                    special.Add(URI);
                    break;
                case ("search"):
                    command = MPDKeyWords.Client.Database.SEARCH;
                    arguments.Add("any");
                    special.Add(URI);
                    break;
                default:
                    command = MPDKeyWords.Client.Database.LSINFO;
                    special.Add(URI);
                    break;
            }

            var qId = await Send(command, arguments, special);
            
            var response = await MPDKeyWords.Response.Encode(await Response(qId));

            var songlist = new List<SongTag>();

            for (int i = 0; i < response.Count; i++ )
                songlist.Add(new SongTag(response[i], i));

            return songlist;
        }

        public async Task<Status> GetStatus()
        {
            int qId = await Send(MPDKeyWords.Client.Status.STATUS);
            var response = await Response(qId);
            return new Status( (await MPDKeyWords.Response.Encode(response)).FirstOrDefault() );
        }

        public async Task<Stats> Stats()
        {
            int qId = await Send(MPDKeyWords.Client.Status.STATS);
            return new Stats((await MPDKeyWords.Response.Encode(await Response(qId))).FirstOrDefault());
        }

        public async Task<SongTag> CurrentSong()
        {
            var qId = await Send(MPDKeyWords.Client.Status.CURRENTSONG);

            var response = await Response(qId);
            var list = await MPDKeyWords.Response.Encode(response);

            return new SongTag(TagType.File, list.FirstOrDefault());
        }

        public async Task<SongTag> PlaylistId(int Id)
        {
            int qId = await Send(MPDKeyWords.Client.Playlist.PLAYLISTID,
                new List<string>() { Id.ToString() });
            return new SongTag(TagType.File, (await MPDKeyWords.Response.Encode(await Response(qId))).FirstOrDefault());
        }
    }
}
