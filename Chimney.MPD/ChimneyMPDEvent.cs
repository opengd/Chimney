using Chimney.MPD.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chimney.MPD
{
    public class PlaylistEventArgs : EventArgs
    {
        public List<SongTag> playlist;
        public List<SongTag> playlistchanges;
        public Status status;
        public SongTag currentsong;

        public PlaylistEventArgs(List<SongTag> playlist, List<SongTag> playlistchanges, Status status, SongTag currentsong)
        {
            this.playlist = playlist;
            this.playlistchanges = playlistchanges;
            this.status = status;
            this.currentsong = currentsong;

        }
    }

    public class StoredPlaylistEventArgs : EventArgs
    {
        public List<StoredPlaylist> storedplaylists;

        public StoredPlaylistEventArgs(List<StoredPlaylist> storedplaylists)
        {
            this.storedplaylists = storedplaylists;
        }
    }

    public class StatusEventArgs : EventArgs
    {
        public Status status;

        public StatusEventArgs(Status status)
        {
            this.status = status;
        }
    }

    public class StatsEventArgs : EventArgs
    {
        public Stats stats;

        public StatsEventArgs(Stats stats)
        {
            this.stats = stats;
        }
    }

    public class UpdateEventArgs : EventArgs
    {
        public Status status;
        public bool IsUpdating;
        public Stats stats;

        public UpdateEventArgs(Status status, bool IsUpdating, Stats stats)
        {
            this.status = status;
            this.IsUpdating = IsUpdating;
            this.stats = stats;
        }
    }

    public class PlayerEventArgs : EventArgs
    {
        public SongTag currentsong;
        public Status status;

        public PlayerEventArgs(SongTag currentsong, Status status)
        {
            this.currentsong = currentsong;
            this.status = status;
        }
    }

    public class OutputEventArgs : EventArgs
    {
        public List<Output> outputs;

        public OutputEventArgs(List<Output> outputs)
        {
            this.outputs = outputs;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public List<Message> messages;

        public MessageEventArgs(List<Message> messages)
        {
            this.messages = messages;
        }
    }

    public class ChimneyMPDEvent : ChimneyMPDClient
    {
        public delegate void EventHandler(object sender, EventArgs e);
        public delegate void PlaylistEventHandler(object sender, PlaylistEventArgs e);
        public delegate void StatusEventHandler(object sender, StatusEventArgs e);
        public delegate void PlayerEventHandler(object sender, PlayerEventArgs e);
        public delegate void OutputEventHandler(object sender, OutputEventArgs e);
        public delegate void StoredPlaylistEventHandler(object sender, StoredPlaylistEventArgs e);
        public delegate void UpdateEventHandler(object sender, UpdateEventArgs e);
        public delegate void StatsEventHandler(object sender, StatsEventArgs e);
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public event StatsEventHandler OnDatabase;
        public event UpdateEventHandler OnUpdate;
        public event StoredPlaylistEventHandler OnStoredPlaylist;
        public event PlaylistEventHandler OnPlaylist;
        public event PlayerEventHandler OnPlayer;
        public event StatusEventHandler OnMixer;
        public event OutputEventHandler OnOutput;
        public event StatusEventHandler OnOptions;
        public event EventHandler OnSticker;
        public event EventHandler OnSubscription;
        public event MessageEventHandler OnMessage;

        //private bool idle = true;

        //
        // Connect to MPD server
        //

        //ChimneyMPDClient actionChimeny;
        public bool silent = false;

        Status status;

        public async Task<bool> RefreshConnection(Status status = null)
        {
            idle = false;
            bool suc = await Start(status, true);
            return suc;
        }

        public async Task<bool> Start(Status status = null, bool silent = false)
        {
            var success = await Connect(this.host, this.port, this.password, silent);

            if (success)
            {
                if (status == null) this.status = await GetStatus();
                else this.status = status;

                idle = true;

                IdleEventLoop();
            }
            else idle = false;

            return success;
        }

        void actionChimeny_ConnectionProblem(object sender, EventArgs e)
        {
            connectionproblem = true;
            SendConnectionProblem();
        }

        public async Task Stop(bool connectionproblem = false)
        {
            this.connectionproblem = connectionproblem;
            idle = false;
            if(!this.connectionproblem) 
                await this.NoIdle();
            await this.Close(this.connectionproblem);
        }

        private async Task ActionOnEventLoop()
        {

            Status status = null;
            Stats stats = null;
            SongTag currentsong = null;

            for (int i = 0; i < responselist.Count; i++)
            {
                string response = responselist[i];

                switch (response)
                {
                    case ("database"):
                        if (stats == null) stats = await this.Stats();
                        if (OnDatabase != null) OnDatabase(this, new StatsEventArgs(stats));
                        break;
                    case ("update"):
                        if (status == null) status = await this.GetStatus();
                        if (stats == null) stats = await this.Stats();
                        if (OnUpdate != null) OnUpdate(this, new UpdateEventArgs(status, status.IsDbUpdating, stats));
                        break;
                    case ("stored_playlist"):
                        if (OnStoredPlaylist != null) OnStoredPlaylist(this, new StoredPlaylistEventArgs(await this.ListPlaylists()));
                        break;
                    case ("playlist"):
                        if (currentsong == null) currentsong = await this.CurrentSong();
                        if (status == null) status = await this.GetStatus();
                        List<SongTag> playlist = await this.Playlist();
                        List<SongTag> playlistchanges = await this.PlaylistChanges(this.status.Playlist);
                        if (OnPlaylist != null) OnPlaylist(this, new PlaylistEventArgs(playlist, playlistchanges, status, currentsong));
                        break;
                    case ("player"):
                        if (currentsong == null) currentsong = await this.CurrentSong();
                        if (status == null) status = await this.GetStatus();
                        if (OnPlayer != null) OnPlayer(this, new PlayerEventArgs(currentsong, status));
                        break;
                    case ("mixer"):
                        if (status == null) status = await this.GetStatus();
                        if (OnMixer != null) OnMixer(this, new StatusEventArgs(status));
                        break;
                    case ("output"):
                        if (OnOutput != null) OnOutput(this, new OutputEventArgs(await this.Outputs()));
                        break;
                    case ("options"):
                        if (status == null) status = await this.GetStatus();
                        if (OnOptions != null) OnOptions(this, new StatusEventArgs(status));
                        break;
                    case ("sticker"):
                        if (OnSticker != null) OnSticker(this, new EventArgs());
                        break;
                    case ("subscription"):
                        //if (OnSubscription != null) OnSubscription(this, new EventArgs());
                        break;
                    case ("message"):
                        //if (OnMessage != null) OnMessage(this, new MessageEventArgs(await ReadMessages()));
                        break;
                    default:
                        break;
                }
            }
            if (status != null) this.status = status;

        }

        List<string> responselist;

        private async Task IdleEventLoop()
        {
            int attemps = 3;

            while(idle)
            {
                List<string> tempresponselist = await Idle();

                if (tempresponselist.Count > 0)
                {
                    await NoIdle();
                    responselist = tempresponselist;
                    await ActionOnEventLoop();
                    attemps = 3;
                    idle = true;
                }
                else if (attemps > 0 && idle)
                {
                    await Connect(this.host, this.port, this.password, true);
                    attemps--;
                }
                else if(idle)
                {
                    idle = false;
                    attemps = 3;
                    connectionproblem = true;
                    SendConnectionProblem();
                } 
            }
        }

    }
}
