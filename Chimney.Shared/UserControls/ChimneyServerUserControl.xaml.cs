using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using SQLite;

using System.Threading.Tasks;

using Chimney.MPD;

#if WINDOWS_PHONE_APP
using Windows.Media.Playback;
using Windows.Foundation.Collections;
#endif

//using Windows.Media.Playlists;

using Windows.Storage.Streams;
using Windows.Storage;

using Chimney.Shared.DatabaseModel;

using Chimney.MPD.Classes;
using System.Diagnostics;
using Windows.UI.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Chimney.Shared.UserControls
{
    public class IdleEventArgs : EventArgs
    {

        public List<string> Events;

        public int id = -1;

        public IdleEventArgs(string EventString)
        {
            this.Events = new List<string>() { EventString };
        }

        public IdleEventArgs(List<string> EventStrings)
        {
            this.Events = EventStrings;
        }

        public override string ToString()
        {
            string response = string.Empty;

            foreach(string s in Events)
            {
                response += "changed: " + s + "\n";
            }
            return response;
        }
    }

    public class IdleListner
    {
        public List<string> events;

        public int id;

        public IdleListner(int id)
        {
            this.id = id;
            this.events = new List<string>();
        }

        public IdleListner(int id, List<string> events)
        {
            this.id = id;
            if(events != null) this.events = events;
            else this.events = new List<string>();
        }

    }

    public sealed partial class ChimneyServerUserControl : UserControl
    {
        //ChimneyMPDServer chimneyMpdServer;

        List<Tuple<StorageFolder, string>> RootPaths = new List<Tuple<StorageFolder, string>>();

        SQLiteAsyncConnection Dbconnection;

        DateTime ServerStatedTime = DateTime.Now;

        int ServerPlaytime = 0;

        public delegate void IdleEvent(object sender, IdleEventArgs e);
        public event IdleEvent OnIdleEvent;

        Dictionary<int, IdleListner> IndleEventHolder = new Dictionary<int, IdleListner>();

        string[] allowedFileTypes = new string[] { ".mp3", ".wav", ".wma", ".mp4", ".acc", ".3gp", ".3g2", ".m4a", ".amr", ".mpr" };

        bool option_random = false;
        bool option_single = false;
        bool option_repeat = false;
        bool option_consume = false;

        bool is_db_updating = false;

        int db_updating_id = 0;

        DateTime db_last_update = DateTime.Now;

        string db_name = "chimneympd.db";

        string current_state = "stop";

        public ChimneyServerUserControl()
        {
            this.InitializeComponent();

            //chimneyMpdServer = new ChimneyMPDServer();

            this.OnIdleEvent += ChimneyServerUC_OnIdleEvent;
        }

        private async Task<bool> CheckDbAsync(string dbName)
        {
            bool dbExist = true;
            try
            {
                StorageFile sf = await ApplicationData.Current.LocalFolder.GetFileAsync(dbName);
            }
            catch (Exception)
            {
                dbExist = false;
            }

            return dbExist;
        }

        void ChimneyServerUC_OnIdleEvent(object sender, IdleEventArgs e)
        {
            if (IndleEventHolder.Count > 0)
            {
                foreach (IdleListner idleListner in IndleEventHolder.Values)
                {
                    bool contain = false;

                    foreach(string events in e.Events)
                    {
                        contain = idleListner.events.Contains(events);
                        if (contain) break;
                    }

                    if (idleListner.events.Count == 0 || contain)
                    {
                        chimneyMpdServer.AppendResponse(e.ToString(), idleListner.id);
                    }
                }
            }
        }

        void NowPlayingPlaylist_OnChange(object sender, EventArgs e)
        {
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));
        }

        bool started = false;

        public async Task Start(string port, string allow = "localhost", string password = "", bool update = true)
        {
            bool suc = false;
            if (!started) suc = await chimneyMpdServer.Start(port, allow, password);

            if (suc && !started)
            {
                chimneyMpdServer.OnDefault += chimneyMpdServer_OnDefault;

                //
                // Db Working
                //
                chimneyMpdServer.OnStatus += chimneyMpdServer_OnStatus;

                //
                // Db Working
                //
                chimneyMpdServer.OnCurrentSong += chimneyMpdServer_OnCurrentSong;

                //
                // Db Working
                //
                chimneyMpdServer.OnStats += chimneyMpdServer_OnStats;

                chimneyMpdServer.OnCommandListBegin += chimneyMpdServer_OnCommandListBegin;
                chimneyMpdServer.OnCommandListBeginOk += chimneyMpdServer_OnCommandListBeginOk;
                chimneyMpdServer.OnCommandListEnd += chimneyMpdServer_OnCommandListEnd;

                chimneyMpdServer.OnSetVol += chimneyMpdServer_OnSetVol;

                //
                // Db Working, complete, event: outputs
                //
                chimneyMpdServer.OnOutputs += chimneyMpdServer_OnOutputs;
                chimneyMpdServer.OnToggleOutput += chimneyMpdServer_OnToggleOutput;
                chimneyMpdServer.OnEnableOutput += chimneyMpdServer_OnEnableOutput;
                chimneyMpdServer.OnDisableOutput += chimneyMpdServer_OnDisableOutput;
                
                chimneyMpdServer.OnPlay += chimneyMpdServer_OnPlay;
                chimneyMpdServer.OnStop += chimneyMpdServer_OnStop;
                chimneyMpdServer.OnPause += chimneyMpdServer_OnPause;
                chimneyMpdServer.OnNext += chimneyMpdServer_OnNext;
                chimneyMpdServer.OnPrevious += chimneyMpdServer_OnPrevious;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnClear += chimneyMpdServer_OnClear;

                //
                // Db Working, it looks like that but I'm not that sure
                //
                chimneyMpdServer.OnList += chimneyMpdServer_OnList;

                //
                // Db Working
                //
                chimneyMpdServer.OnListAll += chimneyMpdServer_OnListAll;
                
                //
                // Db Working
                //
                chimneyMpdServer.OnListAllInfo += chimneyMpdServer_OnListAllInfo;

                //
                // Db Working
                //
                chimneyMpdServer.OnLsInfo += chimneyMpdServer_OnLsInfo;

                //
                // Db Working for files and folders, event: playlist 
                //
                chimneyMpdServer.OnAdd += chimneyMpdServer_OnAdd;
                
                //
                // Db Working, event: options
                //
                chimneyMpdServer.OnConsume += chimneyMpdServer_OnConsume;
                chimneyMpdServer.OnRepeat += chimneyMpdServer_OnRepeat;
                chimneyMpdServer.OnRandom += chimneyMpdServer_OnRandom;
                chimneyMpdServer.OnSingle += chimneyMpdServer_OnSingle;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnShuffle += chimneyMpdServer_OnShuffle;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnAddId += chimneyMpdServer_OnAddId;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnMoveId += chimneyMpdServer_OnMoveId;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnDelete += chimneyMpdServer_OnDelete;

                //
                // Db Working, event: playlist 
                //
                chimneyMpdServer.OnDeleteId += chimneyMpdServer_OnDeleteId;

                //
                // Db Working
                //
                chimneyMpdServer.OnPlaylistInfo += chimneyMpdServer_OnPlaylistInfo;

                //
                // Db Working
                //
                chimneyMpdServer.OnPlChanges += chimneyMpdServer_OnPlChanges;

                chimneyMpdServer.OnPlayId += chimneyMpdServer_OnPlayId;

                //
                // Db Working, on the most importan tags
                //
                chimneyMpdServer.OnFind += chimneyMpdServer_OnFind;

                //
                // Db Working, on the most importan tags
                //
                chimneyMpdServer.OnSearch += chimneyMpdServer_OnSearch;

                //
                // Stored Playlists, complete
                //
                // Db Working, event: stored_playlist
                //
                chimneyMpdServer.OnSave += chimneyMpdServer_OnSave;
                chimneyMpdServer.OnRm += chimneyMpdServer_OnRm;
                chimneyMpdServer.OnRename += chimneyMpdServer_OnRename;
                chimneyMpdServer.OnPlaylistMove += chimneyMpdServer_OnPlaylistMove;
                chimneyMpdServer.OnPlaylistDelete += chimneyMpdServer_OnPlaylistDelete;
                chimneyMpdServer.OnPlaylistClear += chimneyMpdServer_OnPlaylistClear;
                chimneyMpdServer.OnPlaylistAdd += chimneyMpdServer_OnPlaylistAdd;

                // Event: playlist
                chimneyMpdServer.OnLoad += chimneyMpdServer_OnLoad;
                chimneyMpdServer.OnListPlaylists += chimneyMpdServer_OnListPlaylists;
                chimneyMpdServer.OnListPlaylistInfo += chimneyMpdServer_OnListPlaylistInfo;
                chimneyMpdServer.OnListPlaylist += chimneyMpdServer_OnListPlaylist;

                chimneyMpdServer.OnUpdate += chimneyMpdServer_OnUpdate;

                chimneyMpdServer.OnIdle += chimneyMpdServer_OnIdle;
                chimneyMpdServer.OnNoIdle += chimneyMpdServer_OnNoIdle;

                chimneyMpdServer.OnSeekCur += chimneyMpdServer_OnSeekCur;

#if WINDOWS_PHONE_APP
                BackgroundMediaPlayer.Current.MediaEnded += BackgroundMediaPlayer_MediaEnded;
                //BackgroundMediaPlayer.Current.MediaOpened += BackgroundMediaPlayer_MediaOpened;
                //BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
                //BackgroundMediaPlayer.Current.VolumeChanged += BackgroundMediaPlayer_VolumeChanged;
#else
                ChimneyMPDMediaElement.MediaEnded += ChimneyMPDMediaElement_MediaEnded;
                //ChimneyMPDMediaElement.VolumeChanged += ChimneyMPDMediaElement_VolumeChanged;
#endif

            }

            if (suc && !started)
            {
                await InitDatabase(db_name);
                if (update)
                {
                    is_db_updating = true;
                    await UpdateDatabase();
                }

                var options = await Dbconnection.QueryAsync<Option>("SELECT * FROM Options");

                foreach (Option option in options)
                {
                    switch (option.Name)
                    {
                        case ("random"):
                            option_random = option.ValueBool;
                            break;
                        case ("single"):
                            option_single = option.ValueBool;
                            break;
                        case ("repeat"):
                            option_repeat = option.ValueBool;
                            break;
                        case ("consume"):
                            option_consume = option.ValueBool;
                            break;
                        //case ("state"):
                        //    current_state = option.ValueString;
                        //    break;
                        default:
                            break;
                    }
                }

                started = true;

                this.ServerStatedTime = DateTime.Now;
            }
        }

        //void ChimneyMPDMediaElement_VolumeChanged(object sender, RoutedEventArgs e)
        //{
            //CurrentStatus.Volume = Convert.ToInt32(BackgroundMediaPlayer.Current.Volume * 100);
        //}

        async void ChimneyMPDMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            var currentPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 1")).FirstOrDefault<CurrentPlaylist>();

            List<string> events = new List<string>();

            if (currentPlaylist != null)
            {
                if (!option_single && !option_repeat)
                {
                    var nextPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE PositionId = " + currentPlaylist.PositionId + 1)).FirstOrDefault<CurrentPlaylist>();

                    if (nextPlaylist != null)
                    {
                        Play(nextPlaylist.Uri, nextPlaylist.IsUri);

                        currentPlaylist.CurrentSong = false;
                        nextPlaylist.CurrentSong = true;

                        await Dbconnection.UpdateAsync(currentPlaylist);
                        await Dbconnection.UpdateAsync(nextPlaylist);
                    }
                    else
                    {
                        Stop();
                    }
                }
                else if (option_repeat)
                {
                    Play(currentPlaylist.Uri, currentPlaylist.IsUri);
                }
                else
                {
                    Stop();
                }

                events.Add("player");

                if (option_consume && !option_repeat)
                {
                    await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId = PositionId - 1 WHERE PositionId > " + currentPlaylist.PositionId);
                    await Dbconnection.DeleteAsync(currentPlaylist);

                    events.Add("playlist");
                }

                if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs(events));
            }
        }

        async void chimneyMpdServer_OnSeekCur(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            bool canseek = false;

#if WINDOWS_PHONE_APP
            canseek = BackgroundMediaPlayer.Current.CanSeek;
#else
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    canseek = ChimneyMPDMediaElement.CanSeek;
}); 
}
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                canseek = false;
            }
#endif
            if (canseek)
            {
                if (e.arguments.Count > 0)
                {
                    int newpos = 0;

                    if (e.arguments[0].StartsWith("+"))
                    {
                        suc = int.TryParse(e.arguments[0].Remove(0, 1), out newpos);
                        if (suc)
                        {
#if WINDOWS_PHONE_APP
                            BackgroundMediaPlayer.Current.Position = BackgroundMediaPlayer.Current.Position + new TimeSpan(0, 0, newpos);
#else
                            try
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Position = ChimneyMPDMediaElement.Position + new TimeSpan(0, 0, newpos);
}); 
}
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
#endif  
                         }
                    }
                    else if (e.arguments[0].StartsWith("-"))
                    {
                        suc = int.TryParse(e.arguments[0].Remove(0,1), out newpos);
                        if (suc)
                        {
#if WINDOWS_PHONE_APP
                            BackgroundMediaPlayer.Current.Position = BackgroundMediaPlayer.Current.Position - new TimeSpan(0, 0, newpos);
#else
                            try
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Position = ChimneyMPDMediaElement.Position - new TimeSpan(0, 0, newpos);
}); 
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }

#endif
                        }
                    }
                    else
                    {
                        suc = int.TryParse(e.arguments[0], out newpos);
                        if(suc)
                        {
#if WINDOWS_PHONE_APP
                            BackgroundMediaPlayer.Current.Position = new TimeSpan(0, 0, newpos);
#else
                            try
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Position = new TimeSpan(0, 0, newpos);
});
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
#endif
                        }
                    }
                }
            }


            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if(suc) if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
        }

        async void chimneyMpdServer_OnUpdate(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            if (!is_db_updating)
            {
                is_db_updating = true;
                db_updating_id++;

                await UpdateDatabase();
            }

            response += "updating_db: " + db_updating_id;

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnDisableOutput(object sender, ResponseEventArgs e)
        {
            bool suc = false;
            int id = 0;

            if (e.arguments.Count > 0)
            {
                suc = int.TryParse(e.arguments[0], out id);
            }

            if (suc)
            {
                var audioOutput = await Dbconnection.FindAsync<AudioOutput>(o => o.AudioOutputId == id);

                if (audioOutput != null)
                {
                    audioOutput.Enabled = false;

                    await Dbconnection.UpdateAsync(audioOutput);
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("outputs"));
        }

        async void chimneyMpdServer_OnEnableOutput(object sender, ResponseEventArgs e)
        {
            bool suc = false;
            int id = 0;

            if (e.arguments.Count > 0)
            {
                suc = int.TryParse(e.arguments[0], out id);
            }

            if (suc)
            {
                var audioOutput = await Dbconnection.FindAsync<AudioOutput>(o => o.AudioOutputId == id);

                if (audioOutput != null)
                {
                    audioOutput.Enabled = true;

                    await Dbconnection.UpdateAsync(audioOutput);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("outputs"));
        }

        async void chimneyMpdServer_OnToggleOutput(object sender, ResponseEventArgs e)
        {
            bool suc = false;
            int id = 0;

            if (e.arguments.Count > 0)
            {
                suc = int.TryParse(e.arguments[0], out id);
            }

            if (suc)
            {
                var audioOutput = await Dbconnection.FindAsync<AudioOutput>(o => o.AudioOutputId == id);

                if(audioOutput != null)
                {
                    audioOutput.Enabled = (audioOutput.Enabled) ? false : true;

                    await Dbconnection.UpdateAsync(audioOutput);
                }
            }


            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("outputs"));
        }

        async void chimneyMpdServer_OnLoad(object sender, ResponseEventArgs e)
        {
            string playlistName = string.Empty;

            int start = 0;
            int end = 0;

            bool suc = false;

            if (e.arguments.Count > 0)
            {
                playlistName = e.arguments[0];

                if (e.arguments.Count > 1)
                {
                    string[] par = e.arguments[1].Split(new char[] { ':' });
                    suc = int.TryParse(par[1], out start);

                    if (suc && par.Length > 1) suc = int.TryParse(par[2], out end);
                    else end = start;
                }
            }

            var playlistToAddFrom = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

            if(!suc && playlistToAddFrom != null)
            {
                end = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PlaylistFiles WHERE PlaylistId = " + playlistToAddFrom.PlaylistId);
            }

            if(playlistToAddFrom != null)
            {
                var plFiles = await Dbconnection.QueryAsync<PlaylistFile>("SELECT * FROM PlaylistFiles WHERE PlaylistId = " + playlistToAddFrom.PlaylistId
                    + " AND Postion >= " + start + " AND Position < " + end);


                foreach(PlaylistFile plFile in plFiles)
                {
                    CurrentPlaylist newCurrentPlaylistItem = new CurrentPlaylist()
                    {
                        FileId = (plFile.IsUri) ? -1 : plFile.FileId,
                        IsUri = plFile.IsUri,
                        Uri = plFile.Uri,
                        PositionId = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CurrentPlaylist")
                    };

                    await Dbconnection.InsertAsync(newCurrentPlaylistItem);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));
        }

        async void chimneyMpdServer_OnListPlaylist(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            string playlistName = string.Empty;

            string response = string.Empty;

            if (e.arguments.Count > 0)
            {
                playlistName = e.arguments[0];
            }

            var playlistToList = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

            if (playlistToList != null)
            {
                var playlistsItems = await Dbconnection.QueryAsync<PlaylistFile>("SELECT * FROM PlaylistFiles WHERE PlaylistId = " + playlistToList.PlaylistId + " ORDER BY Position");

                foreach (PlaylistFile playlistItem in playlistsItems)
                {
                    if (playlistItem.IsUri)
                    {
                        response += "file: " + playlistItem.Uri + "\n";
                        response += "Pos: " + playlistItem.Position + "\n";
                    }
                    else
                    {
                        var file = await Dbconnection.FindAsync<File>(o => o.FileId == playlistItem.FileId);

                        if (file != null)
                        {
                            response += file.ToSmallResponseString();
                        }
                    }

                }
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);

        }

        async void chimneyMpdServer_OnListPlaylistInfo(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            string playlistName = string.Empty;

            string response = string.Empty;

            if (e.arguments.Count > 0)
            {
                playlistName = e.arguments[0];
            }

            var playlistToList = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

            if (playlistToList != null)
            {
                var playlistsItems = await Dbconnection.QueryAsync<PlaylistFile>("SELECT * FROM PlaylistFiles WHERE PlaylistId = " + playlistToList.PlaylistId + " ORDER BY Position");
                
                foreach(PlaylistFile playlistItem in playlistsItems)
                {
                    if(playlistItem.IsUri)
                    {
                        response += "file: " + playlistItem.Uri + "\n";
                        response += "Pos: " + playlistItem.Position + "\n";
                    }
                    else
                    {
                        var file = await Dbconnection.FindAsync<File>(o => o.FileId == playlistItem.FileId);

                        if(file != null)
                        {
                            response += file.ToResponseString();
                        }
                    }
                    
                }
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);

        }

        async void chimneyMpdServer_OnListPlaylists(object sender, ResponseEventArgs e)
        {
            var allPlaylists = await Dbconnection.QueryAsync<Playlist>("SELECT * FROM Playlists");

            string response = string.Empty;

            foreach(Playlist playlist in allPlaylists)
            {
                response += playlist.ToResponseString();
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnPlaylistAdd(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            string playlistName = string.Empty;
            string uri = string.Empty;

            bool addasuri = false;

            if (e.arguments.Count > 1)
            {
                playlistName = e.arguments[0];
                uri = e.arguments[1];
            }

            var playlistToAddTo = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

            if (playlistToAddTo == null)
            {
                Playlist newStoredPlaylist = new Playlist()
                {
                    Name = playlistName,
                    LastModified = DateTime.Now.ToString("s")
                };

                await Dbconnection.InsertAsync(newStoredPlaylist);

                playlistToAddTo = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);
            }

            var fileToAdd = await Dbconnection.FindAsync<File>(o => o.RelativePath == uri);

            if (fileToAdd == null) addasuri = true;

            if(playlistToAddTo != null)
            {
                PlaylistFile newPlaylistFile = new PlaylistFile() { 
                    FileId = (addasuri) ? -1 : fileToAdd.FileId,
                    IsUri = addasuri,
                    Uri = (addasuri) ? uri : fileToAdd.FilePath,
                    Position = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PlaylistFiles WHERE PlaylistId = " + playlistToAddTo.PlaylistId),
                    PlaylistId = playlistToAddTo.PlaylistId
                };

                await Dbconnection.InsertAsync(newPlaylistFile);

                playlistToAddTo.LastModified = DateTime.Now.ToString("s");

                await Dbconnection.UpdateAsync(playlistToAddTo);

            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));
        }

        async void chimneyMpdServer_OnPlaylistClear(object sender, ResponseEventArgs e)
        {
            string playlistName = null;

            if (e.arguments.Count > 0)
            {
                playlistName = e.arguments[0];
            }

            if(playlistName != null)
            {
                var playlistToClear = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

                if (playlistToClear != null)
                {
                    await Dbconnection.QueryAsync<PlaylistFile>("DELETE FROM PlaylistFiles WHERE PlaylistId = " + playlistToClear.PlaylistId);

                    playlistToClear.LastModified = DateTime.Now.ToString("s");

                    await Dbconnection.UpdateAsync(playlistToClear);
                }

            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));
        }

        async void chimneyMpdServer_OnPlaylistDelete(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            string playlistName = string.Empty;
            int position = 0;

            if (e.arguments.Count > 1)
            {
                playlistName = e.arguments[0];
                suc = int.TryParse(e.arguments[1], out position);
            }

            if (suc)
            {
                var playlistToDeleteIn = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

                if (playlistToDeleteIn != null)
                {
                    var currentPlaylistFiles = await Dbconnection.QueryAsync<PlaylistFile>("SELECT * FROM PlaylistFiles WHERE PlaylistId = " + playlistToDeleteIn.PlaylistId + " AND Position = " + position);

                    PlaylistFile playlistFile = (currentPlaylistFiles.Count > 0) ? currentPlaylistFiles[0] : null;

                    if (playlistFile != null)
                    {
                        await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE PlaylistFiles SET Position=Position-1 WHERE PlaylistId " + playlistToDeleteIn.PlaylistId + " Position > " + playlistFile.Position + " AND Position <= " + position);
                        await Dbconnection.DeleteAsync(playlistFile);

                        playlistToDeleteIn.LastModified = DateTime.Now.ToString("s");

                        await Dbconnection.UpdateAsync(playlistToDeleteIn);
                    }
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));
        }

        async void chimneyMpdServer_OnPlaylistMove(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            string playlistName = string.Empty;
            int fileId = 0;
            int position = 0;

            if (e.arguments.Count > 2)
            {
                playlistName = e.arguments[0];
                suc = int.TryParse(e.arguments[1], out fileId);
                suc = (suc) ? int.TryParse(e.arguments[2], out position) : false;
            }

            if (suc)
            {
                var playlistToMoveIn = await Dbconnection.FindAsync<Playlist>(o => o.Name == playlistName);

                if (playlistToMoveIn != null)
                {
                    int playlistid = playlistToMoveIn.PlaylistId;
                    var currentPlaylistFiles = await Dbconnection.QueryAsync<PlaylistFile>("SELECT * FROM PlaylistFiles WHERE PlaylistId = " + playlistid + " AND FileId = " + fileId + " AND IsUri = 0");

                    PlaylistFile playlistFile = (currentPlaylistFiles.Count > 0) ? currentPlaylistFiles[0] : null;

                    if (playlistFile != null)
                    {
                        if (position >= 0)
                        {
                            await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE PlaylistFiles SET Position=Position+1 WHERE PlaylistId " + playlistid + " Position >= " + position);
                            if (position > playlistFile.Position)
                            {
                                await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE PlaylistFiles SET Position=Position-1 WHERE PlaylistId " + playlistid + " Position > " + playlistFile.Position + " AND Position <= " + position);
                            }
                            playlistFile.Position = position;
                            await Dbconnection.UpdateAsync(playlistFile);

                            playlistToMoveIn.LastModified = DateTime.Now.ToString("s");

                            await Dbconnection.UpdateAsync(playlistToMoveIn);
                        }
                        else suc = false;
                        suc = true;
                    }
                }
                else suc = false;
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));
        }

        async void chimneyMpdServer_OnRename(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 1)
            {
                string oldName = e.arguments[0];
                string newName = e.arguments[1];

                var renamePlaylists = await Dbconnection.FindAsync<Playlist>(o => o.Name == oldName);
                if(renamePlaylists != null)
                {
                    renamePlaylists.Name = newName;
                    renamePlaylists.LastModified = DateTime.Now.ToString("s");
                    await Dbconnection.UpdateAsync(renamePlaylists);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));

        }

        async void chimneyMpdServer_OnRm(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                string rmplaylistname = e.arguments[0];

                var rmPlaylists = await Dbconnection.FindAsync<Playlist>(o => o.Name == rmplaylistname);

                if (rmPlaylists != null)
                {
                    await Dbconnection.QueryAsync<PlaylistFile>("DELETE FROM PlaylistFiles WHERE PlaylistId = " + rmPlaylists.PlaylistId);
                    
                    await Dbconnection.DeleteAsync(rmPlaylists);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));

        }

        async void chimneyMpdServer_OnSave(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                string newplaylistname = e.arguments[0];

                var playlistExist = await Dbconnection.FindAsync<Playlist>(o => o.Name == newplaylistname);

                if (playlistExist == null)
                {
                    Playlist newStoredPlaylist = new Playlist()
                    {
                        Name = newplaylistname,
                        LastModified = DateTime.Now.ToString("s")
                    };

                    await Dbconnection.InsertAsync(newStoredPlaylist);
                }
            }
            
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("stored_playlist"));

        }

        public async Task InitDatabase(string name)
        {
            //
            // Check if the database is already exist
            //
            bool dbexist = await CheckDbAsync(name);

            //
            // Open the database, creating the database if it not already exist
            //
            Dbconnection = new SQLiteAsyncConnection(name);

            //
            // If the database is new create tables
            //
            if (!dbexist)
            {
                await Dbconnection.CreateTableAsync<File>();
                await Dbconnection.CreateTableAsync<Directory>();
                await Dbconnection.CreateTableAsync<Album>();
                await Dbconnection.CreateTableAsync<Artist>();
                await Dbconnection.CreateTableAsync<Genre>();
                await Dbconnection.CreateTableAsync<CurrentPlaylist>();
                await Dbconnection.CreateTableAsync<PlaylistFile>();
                await Dbconnection.CreateTableAsync<Playlist>();
                await Dbconnection.CreateTableAsync<AudioOutput>();

                AudioOutput newAudioOutput = new AudioOutput()
                {
                    Name = "Windows Phone Audio Output",
                    Enabled = true
                };

                await Dbconnection.InsertAsync(newAudioOutput);

                await Dbconnection.CreateTableAsync<Option>();

                Option newOption = new Option()
                {
                    Name = "repeat",
                    ValueBool = false
                };
                await Dbconnection.InsertAsync(newOption);

                newOption = new Option()
                {
                    Name = "random",
                    ValueBool = false
                };
                await Dbconnection.InsertAsync(newOption);

                newOption = new Option()
                {
                    Name = "consume",
                    ValueBool = false
                };
                await Dbconnection.InsertAsync(newOption);

                newOption = new Option()
                {
                    Name = "single",
                    ValueBool = false
                };
                await Dbconnection.InsertAsync(newOption);

                /*
                newOption = new Option()
                {
                    Name = "state",
                    ValueString = "stop"
                };
                await Dbconnection.InsertAsync(newOption);
                */
            }

            //
            // Check if empty start Directory exist
            //
            Directory startDirectory = await GetDirectory(string.Empty, 0);

            //
            // If not, create a empty start directory
            //
            // This is so MPD uri's should work in a easy way
            //
            if (startDirectory == null)
            {

                //
                // Create empty start parent Directory
                //
                startDirectory = new Directory()
                {
                    Name = string.Empty,
                    Path = string.Empty,
                    RelativePath = string.Empty,
                    ParentDirectoryId = 0,
                    FolderRelativeId = string.Empty
                };

                //
                // Add start Directory to Directories
                //
                await Dbconnection.InsertAsync(startDirectory);
            }
        }

        public async Task UpdateDatabase()
        {
            is_db_updating = true;

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("update"));

            //StorageFolder externalDevices = Windows.Storage.KnownFolders.RemovableDevices;

            List<StorageFolder> storageFolders = new List<StorageFolder>() { Windows.Storage.KnownFolders.MusicLibrary, Windows.Storage.KnownFolders.RemovableDevices };

            //
            // Check if empty start Directory exist
            //
            Directory startDirectory = await GetDirectory(string.Empty, 0);

            if (startDirectory != null)
            {
                //
                // Get a filelist for all matching files on filesystem in selected StorageFolders and add them to the Files table
                //
                foreach (StorageFolder sf in storageFolders)
                {
                    List<File> filelist =
                        await UpdateDb(
                            sf,
                            await GetDirectory(startDirectory.Name, 0),
                            new List<string>() { startDirectory.Name }
                        );

                    await Dbconnection.InsertAllAsync(filelist);
                }
            }

            bool fileremoved = false;

            try
            {
                var updateposCurrentPlaylist = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist ORDER BY PositionId");

                if (updateposCurrentPlaylist != null)
                {
                    int pos = 0;

                    foreach (CurrentPlaylist c in updateposCurrentPlaylist)
                    {
                        if (!c.IsUri)
                        {
                            var file = await Dbconnection.FindAsync<File>(o => o.FileId == c.FileId);
                            if (file == null)
                            {
                                await Dbconnection.DeleteAsync(c);
                                fileremoved = true;
                            }
                            else
                            {
                                c.PositionId = pos;
                                await Dbconnection.UpdateAsync(c);
                                pos++;
                            }
                        }
                        else
                        {
                            c.PositionId = pos;
                            await Dbconnection.UpdateAsync(c);
                            pos++;
                        }
                    }
                }
            }
            catch { }

            db_last_update = DateTime.Now;
            is_db_updating = false;

            List<string> eventlist = new List<string>() { "update", "database" };
            if (fileremoved) eventlist.Add( "playlist");

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs(eventlist));
        }

        async void chimneyMpdServer_OnList(object sender, ResponseEventArgs e)
        {

            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("files", "Files");
            tags.Add("album", "Album");
            tags.Add("artist", "Artist");
            tags.Add("albumartist", "AlbumArtist");
            tags.Add("genre", "Genre");
            tags.Add("title", "Title");

            string listtype = "file";
            string filtertype = string.Empty;
            string filterwhat = string.Empty;

            bool suc = true;

            string groupby = string.Empty;

            if (e.arguments.Count > 0)
            {
                listtype = e.arguments[0];
                if(e.arguments.Count > 1)
                {
                    filtertype = (!e.arguments[1].Equals("group") && tags.ContainsKey(e.arguments[1].ToLower())) 
                        ? tags[e.arguments[1].ToLower()] 
                        : null;
                    int groupcounter = 2;
                    if (filtertype == null) groupcounter = 1;
                    else if (e.arguments.Count > 2)
                    {
                        filterwhat = e.arguments[2];
                        groupcounter++;
                    }
                    else filterwhat = null;
                    if (e.arguments.Count > groupcounter && e.arguments[groupcounter].Equals("group"))
                    {
                        for (int i = groupcounter; i < e.arguments.Count; i++ )
                        {
                            if(!e.arguments[i].Equals("group"))
                            {
                                if(tags.ContainsKey(e.arguments[i].ToLower())) groupby += " GROUP BY " + tags[e.arguments[i].ToLower()];
                            }
                        }
                    }
                }
            }

            string filterwhattype = (!string.IsNullOrEmpty(filtertype) && !string.IsNullOrEmpty(filterwhat)) 
                ? " WHERE " + filtertype + " = \"" + filterwhat + "\"" 
                : string.Empty; 

            List<File> queryResult = null;

            switch (listtype)
            {
                case ("album"):
                    queryResult = await Dbconnection.QueryAsync<File>("SELECT DISTINCT Album FROM Files" + filterwhattype + groupby);
                    break;
                case ("artist"):
                    queryResult = await Dbconnection.QueryAsync<File>("SELECT DISTINCT Artist FROM Files" + filterwhattype + groupby);
                    break;
                case ("genre"):
                    queryResult = await Dbconnection.QueryAsync<File>("SELECT DISTINCT Genre FROM Files" + filterwhattype + groupby);
                    break;
                case ("albumartist"):
                    queryResult = await Dbconnection.QueryAsync<File>("SELECT DISTINCT AlbumArtist FROM Files" + filterwhattype + groupby);
                    break;
                case ("file"):
                    queryResult = await Dbconnection.QueryAsync<File>("SELECT DISTINCT RelativePath FROM Files" + filterwhattype + groupby);
                    break;
                default:
                    break;
            }

            string response = string.Empty;

            foreach (File file in queryResult)
            {
                switch (listtype)
                {
                    case ("album"):
                        response += "Album: " + file.Album + "\n";
                        break;
                    case ("artist"):
                        response += "Artist: " + file.Artist + "\n";
                        break;
                    case ("genre"):
                        response += "Genre: " + file.Genre + "\n";
                        break;
                    case ("albumartist"):
                        response += "AlbumArtist: " + file.AlbumArtist + "\n";
                        break;
                    case ("file"):
                        response += "file: " + file.RelativePath + "\n";
                        break;
                    default:
                        break;
                }
            }

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnSearch(object sender, ResponseEventArgs e)
        {
            bool suc = true;

            List<string> searchstrings = new List<string>();
            string searchtype = null;

            if (e.arguments.Count > 1)
            {
                searchtype = e.arguments[0].ToLower();

                for (int i = 1; i < e.arguments.Count; i++ )
                {
                    searchstrings.Add(e.arguments[i].ToLower());
                }
            }

            string response = string.Empty;

            foreach(string searchstring in searchstrings)
            {
                AsyncTableQuery<File> queryFiles = null;

                switch(searchtype)
                {
                    case ("album"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Album.ToLower().Contains(searchstring));
                        break;
                    case ("artist"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Artist.ToLower().Contains(searchstring));
                        break;
                    case ("genre"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Genre.ToLower().Contains(searchstring));
                        break;
                    case ("albumartist"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.AlbumArtist.ToLower().Contains(searchstring));
                        break;
                    case ("file"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Name.ToLower().Contains(searchstring));
                        break;
                    case ("any"):
                        queryFiles = Dbconnection.Table<File>().Where(o =>
                            o.Title.ToLower().Contains(searchstring) ||
                            o.Name.ToLower().Contains(searchstring) ||
                            o.AlbumArtist.ToLower().Contains(searchstring) ||
                            o.Genre.ToLower().Contains(searchstring) ||
                            o.Artist.ToLower().Contains(searchstring) ||
                            o.Album.ToLower().Contains(searchstring));
                        break;
                    default:
                        break;
                }

                if (queryFiles != null)
                {
                    var result = await queryFiles.ToListAsync();

                    foreach (File file in result)
                    {
                        response += file.ToResponseString();
                    }
                }
            }

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = (e.arguments.Count > 0) ? errorfile = e.arguments.First<string>():  string.Empty;
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnFind(object sender, ResponseEventArgs e)
        {
            bool suc = true;

            List<string> searchstrings = new List<string>();
            string searchtype = null;

            if (e.arguments.Count > 1)
            {
                searchtype = e.arguments[0].ToLower();

                for (int i = 1; i < e.arguments.Count; i++)
                {
                    searchstrings.Add(e.arguments[i]);
                }
            }

            string response = string.Empty;

            foreach (string searchstring in searchstrings)
            {
                AsyncTableQuery<File> queryFiles = null;

                switch (searchtype)
                {
                    case ("album"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Album.Contains(searchstring));
                        break;
                    case ("artist"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Artist.Contains(searchstring));
                        break;
                    case ("genre"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Genre.Contains(searchstring));
                        break;
                    case ("albumartist"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.AlbumArtist.Contains(searchstring));
                        break;
                    case ("file"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.RelativePath.Contains(searchstring));
                        break;
                    case ("Title"):
                        queryFiles = Dbconnection.Table<File>().Where(o => o.Title.Contains(searchstring));
                        break;
                    case("any"):
                        queryFiles = Dbconnection.Table<File>().Where(o => 
                            o.Title.Contains(searchstring) || 
                            o.Name.Contains(searchstring) ||
                            o.AlbumArtist.Contains(searchstring) ||
                            o.Genre.Contains(searchstring) ||
                            o.Artist.Contains(searchstring) ||
                            o.Album.Contains(searchstring));
                        break;
                    default:
                        break;
                }

                if (queryFiles != null)
                {
                    var result = await queryFiles.ToListAsync();

                    if (result != null)
                    {
                        foreach (File file in result)
                        {
                            response += file.ToResponseString();
                        }
                    }
                }
            }

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = (e.arguments.Count > 0) ? e.arguments.First<string>() : string.Empty;
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }


        async void chimneyMpdServer_OnPlayId(object sender, ResponseEventArgs e)
        {
            int id = -1;
            if (e.arguments.Count == 1)
            {
                try
                {
                    id = Convert.ToInt32(e.arguments[0]);
                }
                catch
                {
                }
            }
            
            if (id >= 0)
            {
                var playfile = await Dbconnection.FindAsync<File>(o => o.FileId == id);
                if(playfile != null)
                {
                    Play(playfile.FilePath, playfile.IsUrl);
                }

                var currentPlaylist = await Dbconnection.FindAsync<CurrentPlaylist>(o => o.FileId == playfile.FileId );

                if (currentPlaylist != null)
                {
                    await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET CurrentSong = 0 WHERE CurrentSong = 1");

                    currentPlaylist.CurrentSong = true;
                    await Dbconnection.UpdateAsync(currentPlaylist);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
        }


        void chimneyMpdServer_OnNoIdle(object sender, ResponseEventArgs e)
        {
            IndleEventHolder.Remove(e.id);
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
        }

        void chimneyMpdServer_OnIdle(object sender, ResponseEventArgs e)
        {
            if (IndleEventHolder.ContainsKey(e.id)) IndleEventHolder[e.id] = new IdleListner(e.id, e.arguments);
            else IndleEventHolder.Add(e.id, new IdleListner(e.id, e.arguments));
        }

        async void chimneyMpdServer_OnDeleteId(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            int id = 0;

            if (e.arguments.Count > 0)
            {
                suc = int.TryParse(e.arguments[0], out id);
            }

            if (suc)
            {
                try
                {
                    var currentPlaylists = await Dbconnection.FindAsync<CurrentPlaylist>(o => o.FileId == id);
                    
                    if(currentPlaylists != null)
                    {
                        await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId=PositionId-1 WHERE PositionId > " + currentPlaylists.PositionId);
                        await Dbconnection.DeleteAsync(currentPlaylists);
                        suc = true;
                    }
                    else suc = false;
                }
                catch(Exception)
                {
                    suc = false;
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));

        }

        async void chimneyMpdServer_OnDelete(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count == 1)
            {
                int start, end = 0;
                
                string[] par = e.arguments[0].Split(new char[] { ':' });
                bool suc = int.TryParse(par[0], out start);

                if (suc && par.Length > 1) suc = int.TryParse(par[1], out end);
                else end = start;

                await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId=PositionId-1 WHERE PositionId > " + end);

                if(suc) await Dbconnection.QueryAsync<CurrentPlaylist>("DELETE FROM CurrentPlaylist WHERE PositionId >= " + start + 
                    " AND PositionId <= " + end);
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));
        }

        async void chimneyMpdServer_OnMoveId(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            int id = 0;
            int position = 0;

            if (e.arguments.Count > 1)
            {
                suc = int.TryParse(e.arguments[0], out id);
                suc = (suc) ? int.TryParse(e.arguments[1], out position) : false;
            }

            if (suc)
            {
                var currentPlaylists = await Dbconnection.FindAsync<CurrentPlaylist>(o => o.FileId == id);

                if (position >= 0 && currentPlaylists != null)
                {
                    await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId=PositionId+1 WHERE PositionId >= " + position);
                    if (position > currentPlaylists.PositionId)
                    {
                        await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId=PositionId-1 WHERE PositionId > " + currentPlaylists.PositionId + " AND PositionId <= " + position);
                    }
                    currentPlaylists.PositionId = position;
                    await Dbconnection.UpdateAsync(currentPlaylists);
                }
                else suc = false;
                suc = true;
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));
        }

        async void chimneyMpdServer_OnPlChanges(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                int b = -1;
                try
                {
                   b  = Convert.ToInt32(e.arguments.First<string>());
                }
                catch
                {
                    b = -1;
                }

                if (b == -1) chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
                else
                {
                    var currentPlaylist = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE Version > " + b + " ORDER BY PositionId");

                    string response = string.Empty;

                    foreach (CurrentPlaylist cp in currentPlaylist)
                    {
                        //var files = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE FileId = " + cp.FileId);
                        if (cp.IsUri)
                        {
                            response += "file: " + cp.Uri + "\n";
                            response += "Pos: " + cp.PositionId + "\n";
                        }
                        else
                        {
                            var file = await Dbconnection.FindAsync<File>(o => o.FileId == cp.FileId);
                            if (file != null)
                            {
                                file.Pos = cp.PositionId;
                                await Dbconnection.UpdateAsync(file);

                                response += file.ToResponseString();
                            }
                        }
                    }

                    chimneyMpdServer.AppendResponse(response, e.id, e.position);
                }
            }

        }

        async void chimneyMpdServer_OnPlaylistInfo(object sender, ResponseEventArgs e)
        {
            var currentPlaylist = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist ORDER BY PositionId");

            string response = string.Empty;

            foreach(CurrentPlaylist cp in currentPlaylist)
            {
                if (cp.IsUri)
                {
                    response += "file: " + cp.Uri + "\n";
                    response += "Pos: " + cp.PositionId + "\n";
                }
                else
                {
                    var file = await Dbconnection.FindAsync<File>(o => o.FileId == cp.FileId);
                    if (file != null)
                    {
                        file.Pos = cp.PositionId;
                        await Dbconnection.UpdateAsync(file);

                        response += file.ToResponseString();
                    }
                }
            }
            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnAddId(object sender, ResponseEventArgs e)
        {
            bool suc = false;

            int id = 0;

            File file = null;
  
            if (e.arguments.Count > 0)
            {
                string uri = e.arguments[0];
                //var files = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE RelativePath = \"" + uri + "\"");
                //file = (files.Count > 0) ? files[0] : null;

                file = await Dbconnection.FindAsync<File>(o => o.RelativePath.Equals(uri));

                //var query = Dbconnection.Table<File>().Where(o => o.RelativePath.Equals(uri));
                //file = await query.FirstAsync();
            }

            int position = -1;

            if (e.arguments.Count > 1) suc = int.TryParse(e.arguments[1], out position);
            else suc = false;

            position = (suc) ? position : -1;

            if (file != null)
            {
                int currentPlaylistCount = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CurrentPlaylist");

                position = (position <= currentPlaylistCount && position >= 0) ? position : currentPlaylistCount;

                if (position != currentPlaylistCount)
                {
                    var affectedFiles = await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId=PositionId+1 WHERE PositionId >= " + position);
                }

                CurrentPlaylist newCurrentPlaylistItem = new CurrentPlaylist()
                {
                    FileId = file.FileId,
                    PositionId = position,
                    Bitrate = file.Bitrate,
                    IsUri = false,
                    Uri = file.FilePath
                };

                await Dbconnection.InsertAsync(newCurrentPlaylistItem);

                suc = true;

                id = file.FileId;
            }
          
            if (suc) 
            {
                chimneyMpdServer.AppendResponse("Id: " + id + "\n", e.id, e.position);
            }
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {add} could not add file:" + " \"" + errorfile + "\"", e.id, e.position);
            }

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));

        }

        async void chimneyMpdServer_OnLsInfo(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;
            bool suc = true;

            //
            // Get the uri from arguments, if empty uri is empty
            //

            string uri = (e.arguments.Count > 0) ? e.arguments.First<string>(): string.Empty;

            //
            // Get the Directories with RelativePath of uri
            //

            var directories = await Dbconnection.QueryAsync<Directory>("SELECT DirectoryId FROM Directories WHERE RelativePath = \"" + uri + "\"");
            foreach (Directory dir in directories)
            {
                //
                // Get the sub directories for the the uri directories
                //

                var subDirectories = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE ParentDirectoryId = " + dir.DirectoryId);
                foreach (Directory subdir in subDirectories)
                {
                    response += subdir.ToReponseString();
                }

                //
                // Get all files in the current uri
                //

                var files = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + dir.DirectoryId + " AND Type = \"File\"");
                foreach (File file in files)
                {
                    response += file.ToResponseString();
                }
            }

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = (e.arguments.Count > 0) ? e.arguments.First<string>() : string.Empty;
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnLsInfoOld(object sender, ResponseEventArgs e)
        {
            bool suc = true;

            var musicLibraryFolder = Windows.Storage.KnownFolders.MusicLibrary;

            string path = musicLibraryFolder.Name;

            string response = string.Empty;

            if (e.arguments.Count > 0) 
            {            
                path = e.arguments.First<string>();

                Tuple<StorageFolder, string> seekpath = new Tuple<StorageFolder,string>(musicLibraryFolder, musicLibraryFolder.Name);

                if (!path.Equals(musicLibraryFolder.Name.ToLower()))
                {
                    seekpath = await GetPath(musicLibraryFolder, path);
                }
                 
                var files = await seekpath.Item1.GetFilesAsync();
                var folders = await seekpath.Item1.GetFoldersAsync();

                response = string.Empty;

                foreach (StorageFolder folder in folders)
                {
                    response += "directory: " + seekpath.Item2 + "/" + folder.Name + "\n";
                }

                foreach (StorageFile file in files)
                {
                    if (file.FileType.ToLower().Equals(".mp3") ||
                        file.FileType.ToLower().Equals(".wav") ||
                        file.FileType.ToLower().Equals(".wma"))
                    {
                        //response += "file: " + seekpath.Item2 + "/" + file.Name + "\n";

                        SongTag songTag = await SongTag.GetSongTagFromFile(file);
                        songTag.file = seekpath.Item2 + "/" + file.Name;

                        response += songTag.ToString();

                    }
                }
            }
            else
            {
                foreach (Tuple<StorageFolder, string> rootpath in RootPaths)
                {
                    response += "directory: " + rootpath.Item2 + "\n";
                }
            }

            //if (suc) response = await FolderCrawler(seekpath.Item1, seekpath.Item2, false, true);
            
            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnShuffle(object sender, ResponseEventArgs e)
        {
            //NowPlayingPlaylist.Shuffle();

            Random rng = new Random();
            var currentPlaylist = await Dbconnection.Table<CurrentPlaylist>().ToListAsync();

            foreach(CurrentPlaylist cp in currentPlaylist)
            {
                int k = rng.Next(currentPlaylist.Count -1);
                int pos = currentPlaylist[k].PositionId;
                currentPlaylist[k].PositionId = cp.PositionId;
                cp.PositionId = pos;
            }

            await Dbconnection.UpdateAllAsync(currentPlaylist);

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));

        }

        async void chimneyMpdServer_OnSingle(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                int newOptionSetting = 0;
                bool suc = int.TryParse(e.arguments.First<string>(), out newOptionSetting);

                var option = await Dbconnection.FindAsync<Option>(o => o.Name == "single");

                if (option != null)
                {
                    option.ValueBool = (newOptionSetting == 0) ? false : true;
                    await Dbconnection.UpdateAsync(option);
                    option_single = (newOptionSetting == 0) ? false : true;
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("options"));

        }

        async void chimneyMpdServer_OnRandom(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                int newOptionSetting = 0;
                bool suc = int.TryParse(e.arguments.First<string>(), out newOptionSetting);

                var option = await Dbconnection.FindAsync<Option>(o => o.Name == "random");

                if (option != null)
                {
                    option.ValueBool = (newOptionSetting == 0) ? false : true;
                    await Dbconnection.UpdateAsync(option);
                    option_random = (newOptionSetting == 0) ? false : true;
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("options"));

        }

        async void chimneyMpdServer_OnRepeat(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                int newOptionSetting = 0;
                bool suc = int.TryParse(e.arguments.First<string>(), out newOptionSetting);

                var option = await Dbconnection.FindAsync<Option>(o => o.Name == "repeat");

                if (option != null)
                {
                    option.ValueBool = (newOptionSetting == 0) ? false : true;
                    await Dbconnection.UpdateAsync(option);
                    option_repeat = (newOptionSetting == 0) ? false : true;
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("options"));

        }

        async void chimneyMpdServer_OnConsume(object sender, ResponseEventArgs e)
        {
            if (e.arguments.Count > 0)
            {
                int newOptionSetting = 0;
                bool suc = int.TryParse(e.arguments.First<string>(), out newOptionSetting);

                var option = await Dbconnection.FindAsync<Option>(o => o.Name == "consume");

                if(option != null)
                {
                    option.ValueBool = (newOptionSetting == 0) ? false : true;
                    await Dbconnection.UpdateAsync(option);
                    option_consume = (newOptionSetting == 0) ? false : true;

                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("options"));

        }

        Tuple<string, string> GetPathAndFilenameFromArgs(List<string> args)
        {
            if (args.Count > 0)
            {
                string path = args.First<string>();

                string filename = path.Split("/".ToArray(), StringSplitOptions.RemoveEmptyEntries).Last<string>();

                path = path.Replace(filename, string.Empty);

                return new Tuple<string, string>(path, filename);
            }

            return null;
        }

        async void chimneyMpdServer_OnAdd(object sender, ResponseEventArgs e)
        {
            bool suc = true;

            string uri = string.Empty;

            File file = null;

            bool addasUri = false;

            if(e.arguments.Count > 0)
            {
                uri = e.arguments[0];

                file = await Dbconnection.FindAsync<File>(o => o.RelativePath.Equals(uri));

                if (file == null) addasUri = true;
            }

            if (file != null || addasUri)
            {
                CurrentPlaylist newCurrentPlaylistItem = new CurrentPlaylist()
                {
                    FileId = (addasUri) ? -1 : file.FileId,
                    IsUri = addasUri,
                    Uri = (addasUri) ? uri : file.FilePath,
                    Bitrate = (addasUri) ? 0 : file.Bitrate,
                    PositionId = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CurrentPlaylist")
                };

                await Dbconnection.InsertAsync(newCurrentPlaylistItem);
            }
            else
            {
                //
                // Get the uri from arguments, if empty uri is empty
                //
                var directory = (e.arguments.Count > 0) ? await Dbconnection.Table<Directory>().Where(o => o.RelativePath.Equals(e.arguments[0])).FirstAsync() : null;

                //
                // Get the sub directories for the the uri directories
                //
                var subDirQuery = Dbconnection.Table<Directory>().Where(o => o.RelativePath.StartsWith(directory.RelativePath) || directory.RelativePath.Equals(string.Empty));
                var subDirectories = await subDirQuery.ToListAsync();
                foreach (Directory subdir in subDirectories)
                {
                    //
                    // Get all files in the current uri
                    //
                    var subfiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + subdir.DirectoryId);
                    foreach (File subfile in subfiles)
                    {
                        CurrentPlaylist newCurrentPlaylistItem = new CurrentPlaylist()
                        {
                            FileId = subfile.FileId,
                            PositionId = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CurrentPlaylist")
                        };

                        await Dbconnection.InsertAsync(newCurrentPlaylistItem);
                    }
                }
            }


            if (suc) chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {add} could not add file:" + " \"" + errorfile + "\"", e.id, e.position);
            }

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("playlist"));

        }

        private async Task<List<File>> UpdateDb(StorageFolder currentfolder, Directory parentDirectory, List<string> relativepath)
        {
            List<File> songlist = new List<File>();

            //
            // Get all files and sub folders in current folder
            //
            var files = await currentfolder.GetFilesAsync();

            //
            // Get all files matching the avaible playback froamts
            //
            var matchingFiles = files.Where<StorageFile>(o => allowedFileTypes.Contains<string>(o.FileType.ToLower()));


            var folders = await currentfolder.GetFoldersAsync();
            
            //
            // Add the current foldername to list of relativepaths
            //

            relativepath.Add(currentfolder.DisplayName);

            //
            // Get the full current relative path
            //
            string currentrelativepath = string.Empty;

            for (int i = 0; i < relativepath.Count; i++)
            {
                currentrelativepath += relativepath[i];
                currentrelativepath += (i < relativepath.Count - 1 && i > 0) ? "/" : string.Empty;
            }

            //
            // Check if the Folder already exist, if not create the folder
            //
            if (await GetDirectory(currentfolder.DisplayName, parentDirectory.DirectoryId) == null)
            {

                //
                // Create new Directory from current folder
                //
                Directory newDirectory = new Directory()
                {
                    Name = currentfolder.DisplayName,
                    Path = currentfolder.Path,
                    RelativePath = currentrelativepath,
                    ParentDirectoryId = parentDirectory.DirectoryId,
                    FolderRelativeId = currentfolder.FolderRelativeId
                };

                //
                // Add the Directory for current folder to Directories
                //
                await Dbconnection.InsertAsync(newDirectory);
            }

            //
            // Get the Directory for the current folder 
            //

            Directory currentDirectory = await GetDirectory(currentfolder.DisplayName, parentDirectory.DirectoryId) ;

            //
            // Check if files don't exist in the filesystem any more and remove the files from the database
            //
            var currentDirectoryFiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE DirectoryId = " + currentDirectory.DirectoryId);

            //
            // Get any files to remove
            //
            IEnumerable<File> filestoremove =
                from cfile in currentDirectoryFiles
                where !((from file in matchingFiles select file.Name).ToList<string>().Contains<string>(cfile.Name))
                select cfile;

            //
            // Remove files that don't exist on filesystem anymore
            //
            foreach (File removefile in filestoremove)
            {
                await Dbconnection.DeleteAsync(removefile);
            }

            //
            // Check if files don't exist in any more in filesystem and remove the files from the database
            //
            var currentSubDirectory = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE ParentDirectoryId = " + currentDirectory.DirectoryId);

            //
            // Get any directories to remove
            //
            IEnumerable<Directory> directroiesstoremove =
                from cDirectory in currentSubDirectory
                where !((from folder in folders
                         select folder.DisplayName).ToList<string>().Contains<string>(cDirectory.Name))
                select cDirectory;

            //
            // Remove directories that don't exist on filesystem anymore
            //
            foreach (Directory removedirectory in directroiesstoremove)
            {
                List<Directory> subDirToRemove = await GetSubDirectories(removedirectory);

                foreach(Directory d in subDirToRemove)
                {
                    var removesubdirectoryFiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE DirectoryId = " + d.DirectoryId);

                    foreach (File f in removesubdirectoryFiles)
                    {
                        await Dbconnection.DeleteAsync(f);
                    }

                    await Dbconnection.DeleteAsync(d);
                }

                var removedirectoryFiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE DirectoryId = " + removedirectory.DirectoryId);

                foreach(File f in removedirectoryFiles)
                {
                    await Dbconnection.DeleteAsync(f);
                }

                await Dbconnection.DeleteAsync(removedirectory);
            }


            foreach (StorageFile storagefile in matchingFiles)
            {
                //
                // Check if the file aready exist in the database, if not the add the file
                //
                if (await GetFile(storagefile.Name, currentDirectory.DirectoryId) == null)
                {
                    Windows.Storage.FileProperties.MusicProperties mp = null;

                    int? bitrate = null;

                    mp = mp ?? await storagefile.Properties.GetMusicPropertiesAsync();               

                    Tuple<int, string> genre = await GetGenreId(mp.Genre);
                    
                    File newFile = new File()
                    {
                        Name = storagefile.Name,
                        FilePath = storagefile.Path,
                        DirectoryId = currentDirectory.DirectoryId,
                        RelativePath = (string.IsNullOrEmpty(currentrelativepath)) ? storagefile.Name : currentrelativepath + "/" + storagefile.Name,
                        Title = mp.Title,
                        Date = (mp.Year > 0) ? mp.Year.ToString() : string.Empty,
                        AlbumArtist = mp.AlbumArtist,
                        Time = Convert.ToInt32(mp.Duration.TotalSeconds),
                        Disc = -1,
                        PositionId = -1,
                        Prio = 0,
                        Pos = 0,
                        LastModified = storagefile.DateCreated.ToString(),
                        Track = Convert.ToInt32(mp.TrackNumber),
                        Album = mp.Album,
                        Artist = mp.Artist,
                        Genre = genre.Item2,
                        GenreId = genre.Item1,
                        AlbumId = await GetAlbumId(mp.Album),
                        ArtistId = await GetArtistId(mp.Artist),
                        FolderRelativeId = storagefile.FolderRelativeId,
                        Bitrate = bitrate ?? Convert.ToInt32(mp.Bitrate),
                        Type = "File"
                    };

                    songlist.Add(newFile);
                }
            }

            //
            // Loop through sub folders and add any files to current songlist
            //
            foreach (StorageFolder folder in folders)
            {
                songlist.AddRange(await UpdateDb(folder, currentDirectory, relativepath));
            }

            //
            // Remove this current folder from relativepaths
            //
            if (relativepath.Count > 0) relativepath.RemoveAt(relativepath.Count - 1);

            return songlist;
        }


        async Task<List<Directory>> GetSubDirectories(Directory directory)
        {
            List<Directory> subdir = new List<Directory>();

            var subDirectories = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE ParentDirectoryId = " + directory.DirectoryId);
            subdir.AddRange(subDirectories);

            foreach (Directory d in subDirectories)
            {
                subdir.AddRange(await GetSubDirectories(d));
            }

            return subdir;
        }

        async Task<Tuple<int, string>> GetGenreId(IList<string> genrenames)
        {
            object[] genrestrings = new object[genrenames.Count];

            if (genrenames.Count > 0)
            {
                for (int i = 0; i < genrenames.Count; i++)
                {
                    genrestrings[i] = genrenames[i];
                }
            }
            else genrestrings = new object[] { string.Empty };

            int GenreId = -1;
            string genrename = string.Empty;
            while (GenreId < 0)
            {
                var genres = await Dbconnection.QueryAsync<Genre>("SELECT * FROM Genres WHERE Name = ?", genrestrings);

                if (genres.Count > 0)
                {
                    GenreId = genres.First<Genre>().GenreId;
                    genrename = genres.First<Genre>().Name;
                }
                else
                {
                    List<Genre> newGenres = new List<Genre>();
                    foreach (string newgenrename in genrestrings)
                    {
                        Genre newGenre = new Genre()
                        {
                            Name = newgenrename
                        };
                        newGenres.Add(newGenre);
                    }

                    await Dbconnection.InsertAllAsync(newGenres);
                }
            }

            return new Tuple<int,string> (GenreId, genrename);
        }

        async Task<Directory> GetDirectory(string Name, int ParentDirectoryId)
        {
            //
            // Select Directory with FolderRelativeId
            //
            var directory = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE Name = \"" +  Name +
                "\" AND ParentDirectoryId = " + ParentDirectoryId);

            //
            // If the select find a matching file return the File else return null
            //
            return (directory.Count > 0) ? directory.First<Directory>() : null;
        }

        async Task<File> GetFile(string Name, int DirectoryId)
        {
            //
            // Select File with FolderRelativeId
            //
            var file = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Name = \"" + Name +
                "\" AND DirectoryId = " + DirectoryId);

            //
            // If the select find a matching file return the File else return null
            //
            return (file.Count > 0) ? file.First<File>() : null;
        }

        async Task<Directory> GetParentDirectory(Directory ParentDirectory)
        {
            while (true)
            {
                var directory = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE FolderRelativeId = \"" + ParentDirectory.FolderRelativeId + "\"");
                if (directory.Count > 0)
                {
                    return directory.First<Directory>();
                }                
                else
                {
                    await Dbconnection.InsertAsync(ParentDirectory);
                }
            }
        }

        async Task<int> GetAlbumId(string name)
        {
            int id = -1;
            while (id < 0)
            {
                if (string.IsNullOrEmpty(name)) name = string.Empty;

                var album = await Dbconnection.QueryAsync<Album>("SELECT * FROM Albums WHERE Name = \"" + name + "\"");

                if (album.Count > 0)
                {
                    id = album.First<Album>().AlbumId;
                }
                else
                {
                    Album newAlbum = new Album()
                    {
                        Name = name
                    };

                    await Dbconnection.InsertAsync(newAlbum);
                }
            }

            return id;
        }

        async Task<int> GetArtistId(string name)
        {
            int id = -1;
            while (id < 0)
            {
                if (string.IsNullOrEmpty(name)) name = string.Empty;

                var artist = await Dbconnection.QueryAsync<Artist>("SELECT * FROM Artists WHERE Name = \"" + name + "\"");

                if (artist.Count > 0)
                {
                    id = artist.First<Artist>().ArtistId;
                }
                else
                {
                    Artist newArtist = new Artist()
                    {
                        Name = name
                    };

                    await Dbconnection.InsertAsync(newArtist);
                }
            }

            return id;
        }

        async Task<int> GetPostionId(int FileId)
        {
            int id = -1;

            var currentplaylist = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE FileId = " + FileId);

            if (currentplaylist.Count > 0)
            {
                id = currentplaylist.First<CurrentPlaylist>().PositionId;
            }

            return id;
        }

        async Task<string> FolderCrawler(StorageFolder sf, string folderpath, bool crawl = true, bool addpath = true)
        {
            var files = await sf.GetFilesAsync();
            var folders = await sf.GetFoldersAsync();

            string outs = string.Empty;

            if (string.IsNullOrEmpty(folderpath)) outs += "directory: \n";
            else outs += "directory: " + folderpath + "\n";

            foreach (StorageFile file in files)
            {
                if (file.FileType.ToLower().Equals(".mp3") || 
                    file.FileType.ToLower().Equals(".wav") || 
                    file.FileType.ToLower().Equals(".wma"))
                {
                    SongTag songTag = await SongTag.GetSongTagFromFile(file);

                    if (addpath)
                    {
                        if (string.IsNullOrEmpty(folderpath)) songTag.file = sf.Name + "/" + file.Name; // + "\n";
                        else songTag.file = folderpath + "/" + file.Name; // +"\n";
                    }
                    else {
                        songTag.file = file.Name; // +"\n";
                    }

                    outs += songTag.ToString();
                }
            }

            foreach(StorageFolder folder in folders)
            {
                string fpath = string.Empty;
                if (string.IsNullOrEmpty(folderpath)) fpath = folder.Name;
                else fpath = folderpath + "/" + folder.Name;
                if(crawl) outs += await FolderCrawler(folder, fpath);
            }

            return outs;
        }

        async Task<StorageFile> GetStorageFileFromPathList(StorageFolder sf, List<string> filepath)
        {
            if (sf == null || filepath == null) return null;

            bool getFileCrawler = true;

            while (getFileCrawler || filepath.Count > 1)
            {
                sf = await sf.GetFolderAsync(filepath.FirstOrDefault<string>());

                if (sf != null)
                {
                    filepath.RemoveAt(0);
                }
                else
                {
                    getFileCrawler = false;
                }
            }

            if (sf != null && filepath.Count == 1)
            {
                return await sf.GetFileAsync(filepath.FirstOrDefault<string>());
            }

            return null;
        }

        async Task<Tuple<StorageFolder, string>> GetPath(StorageFolder startfolder, string path)
        {
            bool suc = false;
            var storageFolder = startfolder;

            string folderpath = null;

            try
            {
                string[] folders = path.Split("/".ToArray(), StringSplitOptions.RemoveEmptyEntries);

                var tempstorageFolder = startfolder;
                foreach (string f in folders)
                {
                    if (!tempstorageFolder.FolderRelativeId.Equals(startfolder.FolderRelativeId))
                    {
                        tempstorageFolder = await storageFolder.GetFolderAsync(f);
                        storageFolder = tempstorageFolder;
                    }

                    folderpath += (string.IsNullOrEmpty(folderpath)) ? storageFolder.Name : "/" + storageFolder.Name;
                }

                suc = true;

            }
            catch
            {
                suc = false;
            }

            return (suc) ? new Tuple<StorageFolder, string>(storageFolder, folderpath) : null;
        }

        async void chimneyMpdServer_OnListAllInfo(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            //
            // Get the uri from arguments, if empty uri is empty
            //
            string uri = (e.arguments.Count > 0) ? e.arguments.First<string>() : string.Empty;

            //
            // Get the Directories with RelativePath of uri
            //
            var directories = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE RelativePath = \"" + uri + "\"");

            foreach (Directory dir in directories)
            {
                //
                // Get the sub directories for the the uri directories
                //
                var subDirQuery = Dbconnection.Table<Directory>().Where(o => o.DirectoryId > 1 && (o.RelativePath.StartsWith(dir.RelativePath) || dir.RelativePath.Equals(string.Empty)));
                var subDirectories = await subDirQuery.ToListAsync();
                foreach (Directory subdir in subDirectories)
                {
                    response += subdir.ToReponseString();
                    //
                    // Get all files in the current uri
                    //
                    var subfiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + subdir.DirectoryId + " AND Type = \"File\"");
                    foreach (File file in subfiles)
                    {
                        response += file.ToResponseString();
                    }
                }

                //
                // Get all files in the current uri
                //
                var files = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + dir.DirectoryId + " AND Type = \"File\"");
                foreach (File file in files)
                {
                    response += file.ToResponseString();
                }
            }

            bool suc = true;

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnListAll(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            //
            // Get the uri from arguments, if empty uri is empty
            //
            string uri = (e.arguments.Count > 0) ? e.arguments.First<string>() : string.Empty;

            //
            // Get the Directories with RelativePath of uri
            //
            var directories = await Dbconnection.QueryAsync<Directory>("SELECT * FROM Directories WHERE RelativePath = \"" + uri + "\"");
            
            foreach (Directory dir in directories)
            {
                //
                // Get the sub directories for the the uri directories
                //
                var subDirQuery = Dbconnection.Table<Directory>().Where(o => o.DirectoryId > 1 && (o.RelativePath.StartsWith(dir.RelativePath) || dir.RelativePath.Equals(string.Empty)));
                var subDirectories = await subDirQuery.ToListAsync();
                foreach (Directory subdir in subDirectories)
                {
                    response += subdir.ToReponseString();
                    //
                    // Get all files in the current uri
                    //
                    var subfiles = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + subdir.DirectoryId + " AND Type = \"File\"");
                    foreach (File file in subfiles)
                    {
                        response += file.ToSmallResponseString();
                    }
                }

                //
                // Get all files in the current uri
                //
                var files = await Dbconnection.QueryAsync<File>("SELECT * FROM Files WHERE Files.DirectoryId = " + dir.DirectoryId + " AND Type = \"File\"");
                foreach (File file in files)
                {
                    response += file.ToSmallResponseString();
                }
            }

            bool suc = true;

            if (suc) chimneyMpdServer.AppendResponse(response, e.id, e.position);
            else
            {
                string errorfile = string.Empty;
                if (e.arguments.Count > 0) errorfile = e.arguments.First<string>();
                chimneyMpdServer.ErrorResponse(MPDKeyWords.Response.ACK + " [50@0] {listall} could not find path:" + " \"" + errorfile + "\"", e.id, e.position);
            }
        }

        async void chimneyMpdServer_OnClear(object sender, ResponseEventArgs e)
        {
            await Dbconnection.QueryAsync<CurrentPlaylist>("DELETE FROM CurrentPlaylist");

            List<string> returnevents = new List<string>();

            returnevents.Add("playlist");

            if(!current_state.Equals("stop"))
            {
                Stop();

                returnevents.Add("player");
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs(returnevents));
        }

        async void chimneyMpdServer_OnPrevious(object sender, ResponseEventArgs e)
        {
            var currentPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 1")).FirstOrDefault<CurrentPlaylist>();

            if (currentPlaylist != null)
            {
                int index = (currentPlaylist.PositionId > 0) ? currentPlaylist.PositionId - 1 : 0;
                var previousPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE Position = " + index)).FirstOrDefault<CurrentPlaylist>();

                if (previousPlaylist != null)
                {
                    Play(previousPlaylist.Uri, previousPlaylist.IsUri);

                    currentPlaylist.CurrentSong = false;
                    previousPlaylist.CurrentSong = true;

                    await Dbconnection.UpdateAsync(currentPlaylist);
                    await Dbconnection.UpdateAsync(previousPlaylist);
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));

        }

        async void chimneyMpdServer_OnNext(object sender, ResponseEventArgs e)
        {
            var currentPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 1")).FirstOrDefault<CurrentPlaylist>();

            if (currentPlaylist != null)
            {
                var nextPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE Position = " + currentPlaylist.PositionId + 1)).FirstOrDefault<CurrentPlaylist>();

                if(nextPlaylist != null)
                {
                    Play(nextPlaylist.Uri, nextPlaylist.IsUri);

                    currentPlaylist.CurrentSong = false;
                    nextPlaylist.CurrentSong = true;

                    await Dbconnection.UpdateAsync(currentPlaylist);
                    await Dbconnection.UpdateAsync(nextPlaylist);
                }
            }
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));

        }

        void chimneyMpdServer_OnPause(object sender, ResponseEventArgs e)
        {
            Pause();
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
        }

        void chimneyMpdServer_OnStop(object sender, ResponseEventArgs e)
        {
            Stop();
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
        }

        async void chimneyMpdServer_OnPlay(object sender, ResponseEventArgs e)
        {
            int index = 0;
            if (e.arguments.Count > 0)
            {
                try
                {
                    index = Convert.ToInt32(e.arguments[0]);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

#if WINDOWS_PHONE_APP
            if (current_state.Equals("pause") && BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused)
#else

            bool statetrue = false;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
            statetrue = (ChimneyMPDMediaElement.CurrentState == MediaElementState.Paused) ? true : false;
});

            if (current_state.Equals("pause") && statetrue)
#endif
            {
                PlayOnPause();

                //if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
            }           
            else
            {
                CurrentPlaylist song = null;

                var currentPlaylist = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist");

                if (currentPlaylist.Count > 0)
                {
                    if (option_random)
                    {
                        Random ran = new Random();
                        song = currentPlaylist[ran.Next(0, currentPlaylist.Count - 1)];
                    }
                    else if (currentPlaylist.Count > index)
                    {                      
                        song = currentPlaylist[index];
                    }

                    if (song != null)
                    {
                       Play(song.Uri, song.IsUri);
                    }

                    await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET CurrentSong = 0 WHERE CurrentSong = 1");

                    song.CurrentSong = true;
                    await Dbconnection.UpdateAsync(song);
                }
            }

            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);

            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));

        }

        async void chimneyMpdServer_OnOutputs(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;
            var audioOutputs = await Dbconnection.QueryAsync<AudioOutput>("SELECT * FROM AudioOutputs");

            foreach (AudioOutput audioOutput in audioOutputs)
            {
                response += audioOutput.ToResponseString();
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnSetVol(object sender, ResponseEventArgs e)
        {
            if(e.arguments.Count > 0)
            {
                try
                {
                    double newvol = Convert.ToDouble(e.arguments[0]);

#if WINDOWS_PHONE_APP
                    BackgroundMediaPlayer.Current.Volume = newvol * 0.01;
#else
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Volume = newvol * 0.01;
});
#endif
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
                if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("mixer"));

            }

        }

        void chimneyMpdServer_OnCommandListEnd(object sender, CommandListResponseEventArgs e)
        {
        }

        void chimneyMpdServer_OnCommandListBegin(object sender, CommandListResponseEventArgs e)
        {
        }

        void chimneyMpdServer_OnCommandListBeginOk(object sender, CommandListResponseEventArgs e)
        {
        }


        async void chimneyMpdServer_OnStats(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            response += "artists: " + await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Artists") + "\n";

            response += "albums: " + await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Albums") + "\n";

            int files = await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Files");

            response += "songs: " + files + "\n";

            response += "uptime: " + DateTime.Now.Subtract(ServerStatedTime).Seconds + "\n";

            response += (files > 0) ? "db_playtime: " + await Dbconnection.ExecuteScalarAsync<int>("SELECT SUM(Time) FROM Files") + "\n" : "db_playtime: 0\n";

            TimeSpan span = db_last_update.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            response += "db_update: " + span.TotalSeconds + "\n";

            try
            {

#if WINDOWS_PHONE_APP
            response += "playtime: " + (ServerPlaytime + BackgroundMediaPlayer.Current.Position.TotalSeconds) + "\n";
#else
                             await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                response += "playtime: " + (ServerPlaytime + ChimneyMPDMediaElement.Position.TotalSeconds) + "\n";
                });
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                response += "playtime: 0\n";
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnCurrentSong(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            if (!current_state.Equals("stop"))
            {
                //var currentSonga = await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 0");

                var currentSong = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 1")).FirstOrDefault<CurrentPlaylist>();

                if (currentSong != null)
                {
                    if (currentSong.IsUri)
                    {
                        response += "file: " + currentSong.Uri;
                        response += "Pos: " + currentSong.PositionId;
                    }
                    else
                    {
                        var currentFile = await Dbconnection.FindAsync<File>(o => o.FileId == currentSong.FileId);
                        if (currentFile != null)
                        {
                            currentFile.Pos = currentSong.PositionId;
                            response += currentFile.ToResponseString();
                        }
                    }
                }
            }

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        async void chimneyMpdServer_OnStatus(object sender, ResponseEventArgs e)
        {
            string response = string.Empty;

            try
            {

#if WINDOWS_PHONE_APP
            response += "volume: " + Convert.ToInt32(BackgroundMediaPlayer.Current.Volume * 100) + "\n";
#else
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    response += "volume: " + Convert.ToInt32(ChimneyMPDMediaElement.Volume * 100) + "\n";
});
#endif
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                response += "volume: 0\n";
            }

            response += (option_random) ? "random: 1\n" : "random: 0\n"; 
            response += (option_single) ? "single: 1\n" : "single: 0\n";
            response += (option_repeat) ? "repeat: 1\n" : "repeat: 0\n";
            response += (option_consume) ? "consume: 1\n" : "consume: 0\n";

            response += "playlist: " + await Dbconnection.ExecuteScalarAsync<int>("SELECT seq FROM sqlite_sequence WHERE name = \"CurrentPlaylist\"") + "\n";
            response += "playlistlength: " + await Dbconnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CurrentPlaylist") + "\n";

            var currentSong = await Dbconnection.FindAsync<CurrentPlaylist>(o => o.CurrentSong == true);

            if (current_state.Equals("play") || current_state.Equals("pause"))
            {
                response += "state: " + current_state + "\n"; 

                response += (currentSong != null) ? "song: " + currentSong.PositionId + "\n" : "song: 0\n";
                response += (currentSong != null) ? "songid: " + currentSong.FileId + "\n" : "songid: 0\n";

                if (currentSong != null)
                {
                    int nextpos = currentSong.PositionId + 1;

                    var nextSong = await Dbconnection.FindAsync<CurrentPlaylist>(o => o.PositionId == nextpos);

                    response += (nextSong != null) ? "nextsong: " + nextSong.PositionId + "\n" : string.Empty;
                    response += (nextSong != null) ? "nextsongid: " + nextSong.FileId + "\n" : string.Empty;
                }
                try
                { 
#if WINDOWS_PHONE_APP
                response += (BackgroundMediaPlayer.Current.Position != null) ? "time: " + BackgroundMediaPlayer.Current.Position.TotalSeconds + "\n" : "time: 0\n";
                response += (BackgroundMediaPlayer.Current.Position != null) ? "elapsed: " + BackgroundMediaPlayer.Current.Position.TotalSeconds + "\n" : "elapsed: 0\n";
#else
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    response += (ChimneyMPDMediaElement.Position != null) ? "time: " + ChimneyMPDMediaElement.Position.TotalSeconds + "\n" : "time: 0\n";
    response += (ChimneyMPDMediaElement.Position != null) ? "elapsed: " + ChimneyMPDMediaElement.Position.TotalSeconds + "\n" : "elapsed: 0\n";
});
#endif
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    response += "time: 0\n";
                    response += "elapsed: 0\n";
                }
                response += (currentSong != null) ? "bitrate: " + currentSong.Bitrate + "\n" : "bitrate: " + 0 + "\n";

                response += "xfade: 0\n";
                response += "mixrampdb: 0.000000\n";
                response += "mixrampdelay: 0\n";
                response += "audio: 16:44000:2\n";
            }
            else
            {
                response += "state: stop\n";
                response += "xfade: 0\n";
            }

            response += (is_db_updating) ? "updating_db: " + db_updating_id + "\n" : string.Empty;

            chimneyMpdServer.AppendResponse(response, e.id, e.position);
        }

        void chimneyMpdServer_OnDefault(object sender, ResponseEventArgs e)
        {
            chimneyMpdServer.AppendResponse(string.Empty, e.id, e.position);
        }

#if WINDOWS_PHONE_APP
        private async void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            var currentPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE CurrentSong = 1")).FirstOrDefault<CurrentPlaylist>();

            List<string> events = new List<string>();

            if (currentPlaylist != null)
            {
                if (!option_single && !option_repeat)
                {
                    var nextPlaylist = (await Dbconnection.QueryAsync<CurrentPlaylist>("SELECT * FROM CurrentPlaylist WHERE PositionId = " + currentPlaylist.PositionId + 1)).FirstOrDefault<CurrentPlaylist>();

                    if(nextPlaylist != null)
                    {
                        Play(nextPlaylist.Uri, nextPlaylist.IsUri);                      

                        currentPlaylist.CurrentSong = false;
                        nextPlaylist.CurrentSong = true;

                        await Dbconnection.UpdateAsync(currentPlaylist);
                        await Dbconnection.UpdateAsync(nextPlaylist);
                    }
                    else
                    {                        
                        Stop();
                    }
                }
                else if (option_repeat)
                {
                    Play(currentPlaylist.Uri, currentPlaylist.IsUri);
                }
                else
                {
                    Stop();
                }

                events.Add("player");

                if (option_consume && !option_repeat)
                {
                    await Dbconnection.QueryAsync<CurrentPlaylist>("UPDATE CurrentPlaylist SET PositionId = PositionId - 1 WHERE PositionId > " + currentPlaylist.PositionId);
                    await Dbconnection.DeleteAsync(currentPlaylist);

                    events.Add("playlist");
                }

                if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs(events));
            }
        }
#endif
        /*
        private void BackgroundMediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
        }
        */

        
        //private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        //{
            /*
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing || 
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused ||
                BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
            {
            */
                //if (OnIdleEvent != null) OnIdleEvent(this, new IdleEventArgs("player"));
            //}
        //}
        

        //private void BackgroundMediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        //{
            //CurrentStatus.Volume = Convert.ToInt32(BackgroundMediaPlayer.Current.Volume * 100);
        //}


        private async void Play(string song, bool IsUri)
        {
            current_state = "play";

#if WINDOWS_PHONE_APP
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet
            {
                {"MePlay", song}, {"IsUri", IsUri}
                });
#else
            try
            {
                StorageFile sf = await StorageFile.GetFileFromPathAsync(song);
                IRandomAccessStream stream = await sf.OpenAsync(FileAccessMode.Read);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.AutoPlay = true;
    ChimneyMPDMediaElement.SetSource(stream, string.Empty);
});
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#endif

            /*
            var stateOption = await Dbconnection.FindAsync<Option>(o => o.Name == "state");
            if (stateOption != null)
            {
                stateOption.ValueString = current_state;

                await Dbconnection.UpdateAsync(stateOption);
            }
            */
        }

        private async void Stop()
        {
            current_state = "stop";

#if WINDOWS_PHONE_APP
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet
            {
                {"Stop", string.Empty}
                });
#else
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Stop();
});
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#endif
            /*

            var stateOption = await Dbconnection.FindAsync<Option>(o => o.Name == "state");
            if (stateOption != null)
            {
                stateOption.ValueString = current_state;

                await Dbconnection.UpdateAsync(stateOption);
            }
            */
        }

        private async void Pause()
        {
            current_state = "pause";

#if WINDOWS_PHONE_APP
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet
            {
                {"Pause", string.Empty}
                });
#else
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Pause();
});
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#endif
            /*
            
            var stateOption = await Dbconnection.FindAsync<Option>(o => o.Name == "state");
            if (stateOption != null)
            {
                stateOption.ValueString = current_state;

                await Dbconnection.UpdateAsync(stateOption);
            }
            */
        }

        private async void PlayOnPause()
        {
            current_state = "play";

#if WINDOWS_PHONE_APP
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet
                {
                    {"MePlayPause", string.Empty}
                    });
#else
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
() =>
{
    ChimneyMPDMediaElement.Play();
});
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#endif
            /*
            var stateOption = await Dbconnection.FindAsync<Option>(o => o.Name == "state");
            if (stateOption != null)
            {
                stateOption.ValueString = current_state;

                await Dbconnection.UpdateAsync(stateOption);
            }
            */
        }

    }
}
