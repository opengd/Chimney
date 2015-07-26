using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Chimney.MPD
{

    public class ResponseConfig
    {
        private Dictionary<int, string> _response = new Dictionary<int, string>();
        public string Response
        {      
            get { 
                string responseString = string.Empty;
                
                for (int i = 0; i < _response.Count; i++)
                {
                    string s = _response[i];
                    if (!string.IsNullOrEmpty(s))
                    {
                        responseString += s;
                        if (!s.EndsWith(MPDKeyWords.Response.LINEBREAK)) responseString += MPDKeyWords.Response.LINEBREAK;
                    }
                    if (command_list_ok_begin) responseString += MPDKeyWords.Response.LIST_OK + MPDKeyWords.Response.LINEBREAK;
                }

                if (!error_response)
                {
                    responseString += MPDKeyWords.Response.OK;
                }

                responseString += MPDKeyWords.Response.END;

                return responseString;
                }
            set
            {
                _response.Clear();
                _response.Add(0, value);
            }
        }

        public bool command_list_begin = false;
        public bool command_list_ok_begin = false;
        public bool command_list_end = false;

        public int id = -1;

        public bool error_response = false;

        public bool response_finish = false;
        public bool response_sent = false;
        public bool response_sent_error = false;

        public bool idle = false;

        public int ResponseFinishLenght = 0;

        public StreamSocket socket;


        public int Count
        {
            get { return _response.Count; }
        }

        public bool Empty
        {
            get
            {
                if (_response.Count > 0) return false;
                return true;             
            }
        }

        public ResponseConfig(int id, StreamSocket socket)
        {
            this.id = id;
            this.socket = socket;
        }

        public void AppendResponse(string response, int position)
        {
            //if (_response.ContainsKey(position)) _response[position] = response;
            //else _response.Add(position, response);

            _response[position] = response;

            if (Count >= ResponseFinishLenght) response_finish = true;
        }

        public void AppendResponse(string response)
        {
            //if (_response.ContainsKey(_response.Count)) _response[_response.Count] = response;
            //_response.Add(_response.Count, response);
            _response[_response.Count] = response;


            //if (ResponseFinishLenght == Count) 
            this.response_finish = true;
        }

        public bool RemoveResponse(int reponseid)
        {
            return _response.Remove(reponseid);
        }

        public void ClearResponse()
        {
            _response.Clear();
        }

        public string GetReponse(int reponseid)
        {
            if ((_response.Count - 1) >= reponseid) return _response[reponseid];
            return string.Empty;
        }

        public bool ContinsResponse(string reponsestring)
        {
            return _response.ContainsValue(reponsestring);
        }

        public void ClearConfig()
        {
            ClearResponse();
            this.command_list_begin = false;
            this.command_list_ok_begin = false;
            this.command_list_end = false;

            this.error_response = false;

            this.response_sent = false;
            this.response_sent_error = false;

            this.ResponseFinishLenght = 0;

            this.response_finish = false;
        }

    }

    public class ResponseEventArgs : EventArgs
    {
        public StreamSocket socket;
        public int id = -1;

        public int position = -1;

        public List<string> arguments = new List<string>();

        public ResponseEventArgs(StreamSocket socket, int id, int position)
        {
            this.socket = socket;
            this.id = id;
            this.position = position;
        }

        public ResponseEventArgs(StreamSocket socket, int id, int position, List<string> arguments)
        {
            this.socket = socket;
            this.id = id;
            this.position = position;
            this.arguments = arguments;
        }
    }


    public class CommandListResponseEventArgs : EventArgs
    {
        public StreamSocket socket;
        public bool command_list = false;

        public bool start = false;
        public bool end = false;
        public bool command_list_ok = false;

        public int id = -1;

        public CommandListResponseEventArgs(StreamSocket socket, int id, bool start, bool end, bool command_list_ok)
        {
            this.socket = socket;
            this.start = start;
            this.end = end;
            this.command_list_ok = command_list_ok;
            this.id = id;
        }
    }
    
    public class ChimneyMPDServer : ChimneyMPDBase
    {

        public delegate void CommandListResponseEvent(object sender, CommandListResponseEventArgs e);
        public delegate void ResponseEvent(object sender, ResponseEventArgs e);
        public event ResponseEvent OnDefault;
        public event ResponseEvent OnCurrentSong;
        public event ResponseEvent OnStatus;
        public event ResponseEvent OnStats;
        public event ResponseEvent OnSetVol;
        public event ResponseEvent OnPlay;
        public event ResponseEvent OnStop;
        public event ResponseEvent OnPause;
        public event ResponseEvent OnNext;
        public event ResponseEvent OnPrevious;
        public event ResponseEvent OnClear;
        public event ResponseEvent OnLsInfo;
        public event ResponseEvent OnList;
        public event ResponseEvent OnListAll;
        public event ResponseEvent OnListAllInfo;
        public event ResponseEvent OnAdd;
        public event ResponseEvent OnAddId;
        public event ResponseEvent OnIdle;
        public event ResponseEvent OnNoIdle;
        public event ResponseEvent OnConsume;
        public event ResponseEvent OnSingle;
        public event ResponseEvent OnRepeat;
        public event ResponseEvent OnRandom;
        public event ResponseEvent OnShuffle;
        public event ResponseEvent OnPlaylistInfo;
        public event ResponseEvent OnPlChanges;
        public event ResponseEvent OnMoveId;
        public event ResponseEvent OnDelete;
        public event ResponseEvent OnDeleteId;
        public event ResponseEvent OnPlayId;
        public event ResponseEvent OnFind;
        public event ResponseEvent OnSearch;
        public event ResponseEvent OnUpdate;
        public event ResponseEvent OnSeekCur;


        //
        // Stored Playlist Events
        //
        public event ResponseEvent OnListPlaylist;
        public event ResponseEvent OnListPlaylistInfo;
        public event ResponseEvent OnListPlaylists;
        public event ResponseEvent OnLoad;
        public event ResponseEvent OnPlaylistAdd;
        public event ResponseEvent OnPlaylistClear;
        public event ResponseEvent OnPlaylistDelete;
        public event ResponseEvent OnPlaylistMove;
        public event ResponseEvent OnRename;
        public event ResponseEvent OnRm;
        public event ResponseEvent OnSave;

        //
        // Audio Outputs
        //
        public event ResponseEvent OnOutputs;
        public event ResponseEvent OnToggleOutput;
        public event ResponseEvent OnEnableOutput;
        public event ResponseEvent OnDisableOutput;


        public event CommandListResponseEvent OnCommandListBegin;
        public event CommandListResponseEvent OnCommandListBeginOk;
        public event CommandListResponseEvent OnCommandListEnd;

        int ResponsConfigDictionaryIdCounter = 0;

        public Dictionary<int, ResponseConfig> ResponsConfigDictionary = new Dictionary<int, ResponseConfig>();

        private string password = string.Empty;

        private bool password_confirmed = false;

        string allow = "localhost";

        public async Task<bool> Start(string port, string allow = "localhost", string password = "")
        {
            streamSocketListner = new StreamSocketListener();
            streamSocketListner.ConnectionReceived += streamSocketListner_ConnectionReceived;

            bool suc = false;

            try
            {
                await streamSocketListner.BindServiceNameAsync(port);
                suc = true;
            }
            catch
            {
                suc = false;
            }

            this.password = password;
            password_confirmed = string.IsNullOrEmpty(this.password);

            switch (allow)
            {
                case ("lan"):
                    this.allow = "lan";
                    break;
                case ("any"):
                    this.allow = "any";
                    break;
                default:
                    this.allow = "localhost";
                    break;
            }

            return suc;
        }


        async Task ReadResponse(StreamSocket streamSocket, int responseId)
        {
            try
            {
                DataReader dataReader = new DataReader(streamSocket.InputStream);

                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                bool isSocketOpen = true;

                while (isSocketOpen)
                {
                    bool isFinish = false;
                    string text = string.Empty;

                    while (!isFinish)
                    {
                        if (await dataReader.LoadAsync(65000) == 0)
                        {
                            isSocketOpen = false;
                            isFinish = true;
                        }
                        else
                        {
                            string s = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                            text += s;
                            if (text.EndsWith(MPDKeyWords.Response.LINEBREAK))
                            {
                                isFinish = true;
                            }
                        }
                    }

                    if (isSocketOpen)
                    {
                        bool noidle = ParseResponse(streamSocket, text, responseId);
                        if (!password_confirmed) await SendResponse("ACK [3@0] {password} incorrect password\n", streamSocket);
                        bool suc = (noidle) ? await SendResponse(responseId, true) : await SendResponse(responseId);
                        if (!suc) isSocketOpen = false;
                    }
                }
            }
            catch {
            }
        }

        bool ParseResponse(StreamSocket streamSocket, string response, int responseId)
        {
            //response = response.ToLower();

            string[] commands = response.Split("\n".ToArray(), StringSplitOptions.RemoveEmptyEntries);

            int position = -1;

            bool noidle = false;

            if (!password_confirmed)
            {
                if(commands.Count<string>() > 0 )
                {
                    Tuple<string, List<string>> commandArgs = SplitCommandArgs(commands[0]);

                    if(commandArgs.Item1.Equals("password") && commandArgs.Item2.Count > 0)
                    {
                        password_confirmed = (commandArgs.Item2.FirstOrDefault<string>().Equals(password));
                    }
                }

                if (!password_confirmed) return true;
            }

            foreach (string s in commands)
            {
                string lowers = s.ToLower();
                switch(lowers)
                {
                    case(MPDKeyWords.Client.CommandList.COMMAND_LIST_BEGIN):
                        break;
                    case (MPDKeyWords.Client.CommandList.COMMAND_LIST_END):
                        break;
                    case (MPDKeyWords.Client.CommandList.COMMAND_LIST_OK_BEGIN):
                        break;
                    default:
                        ResponsConfigDictionary[responseId].ResponseFinishLenght++;
                        break;
                } 
            }

            foreach (string command in commands)
            {
                Tuple<string, List<string>> commandArgs = SplitCommandArgs(command);

                bool client_list_command = false;
                
                switch (commandArgs.Item1)
                {
                    case (MPDKeyWords.Client.CommandList.COMMAND_LIST_BEGIN):
                        ResponsConfigDictionary[responseId].command_list_begin = true;
                        ResponsConfigDictionary[responseId].command_list_end = false;
                        if (OnCommandListBegin != null) OnCommandListBegin(this, new CommandListResponseEventArgs(streamSocket, responseId, true, false, false));
                        client_list_command = true;
                        break;
                    case (MPDKeyWords.Client.CommandList.COMMAND_LIST_END):
                        ResponsConfigDictionary[responseId].command_list_end = true;
                        if (OnCommandListEnd != null) OnCommandListEnd(this, new CommandListResponseEventArgs(streamSocket, responseId, false, true, false));
                        client_list_command = true;
                        break;
                    case (MPDKeyWords.Client.CommandList.COMMAND_LIST_OK_BEGIN):
                        ResponsConfigDictionary[responseId].command_list_ok_begin = true;
                        ResponsConfigDictionary[responseId].command_list_end = false;
                        if (OnCommandListBeginOk != null) OnCommandListBeginOk(this, new CommandListResponseEventArgs(streamSocket, responseId, true, false, true));
                        client_list_command = true;
                        break;
                    default:
                        break;
                }
                if (!client_list_command)
                {
                    position++;

                    switch (commandArgs.Item1)
                    {
                        case (MPDKeyWords.Client.Status.STATUS):
                            if (OnStatus != null) OnStatus(this, new ResponseEventArgs(streamSocket, responseId, position));
                            break;
                        case (MPDKeyWords.Client.Status.CURRENTSONG):
                            if (OnCurrentSong != null) OnCurrentSong(this, new ResponseEventArgs(streamSocket, responseId, position));
                            break;
                        case (MPDKeyWords.Client.Status.STATS):
                            if (OnStats != null) OnStats(this, new ResponseEventArgs(streamSocket, responseId, position));
                            break;
                        case (MPDKeyWords.Client.Playback.SETVOL):
                            if (OnSetVol != null) OnSetVol(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.VOLUME):
                            if (OnSetVol != null) OnSetVol(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.PLAY):
                            if (OnPlay != null) OnPlay(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.STOP):
                            if (OnStop != null) OnStop(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.PAUSE):
                            if (OnPause != null) OnPause(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.NEXT):
                            if (OnNext != null) OnNext(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.PREVIOUS):
                            if (OnPrevious != null) OnPrevious(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.CLEAR):
                            if (OnClear != null) OnClear(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.LSINFO):
                            if (OnLsInfo != null) OnLsInfo(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.LIST):
                            if (OnList != null) OnList(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.LISTALL):
                            if (OnListAll != null) OnListAll(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.LISTALLINFO):
                            if (OnListAllInfo != null) OnListAllInfo(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.ADD):
                            if (OnAdd != null) OnAdd(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.ADDID):
                            if (OnAddId != null) OnAddId(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.CONSUME):
                            if (OnConsume != null) OnConsume(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.SINGLE):
                            if (OnSingle != null) OnSingle(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.REPEAT):
                            if (OnRepeat != null) OnRepeat(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.RANDOM):
                            if (OnRandom != null) OnRandom(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.SHUFFLE):
                            if (OnShuffle != null) OnShuffle(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.PLAYLISTINFO):
                            if (OnPlaylistInfo != null) OnPlaylistInfo(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.PLCHANGES):
                            if (OnPlChanges != null) OnPlChanges(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.MOVEID):
                            if (OnMoveId != null) OnMoveId(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.DELETE):
                            if (OnDelete != null) OnDelete(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playlist.DELETEID):
                            if (OnDeleteId != null) OnDeleteId(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.PLAYID):
                            if (OnPlayId != null) OnPlayId(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.SEARCH):
                            if (OnSearch != null) OnSearch(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.FIND):
                            if (OnFind != null) OnFind(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.LISTPLAYLIST):
                            if (OnListPlaylist != null) OnListPlaylist(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.LISTPLAYLISTINFO):
                            if (OnListPlaylistInfo != null) OnListPlaylistInfo(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.LISTPLAYLISTS):
                            if (OnListPlaylists != null) OnListPlaylists(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.LOAD):
                            if (OnLoad != null) OnLoad(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.PLAYLISTADD):
                            if (OnPlaylistAdd != null) OnPlaylistAdd(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.PLAYLISTCLEAR):
                            if (OnPlaylistClear != null) OnPlaylistClear(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.PLAYLISTDELETE):
                            if (OnPlaylistDelete != null) OnPlaylistDelete(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.PLAYLISTMOVE):
                            if (OnPlaylistMove != null) OnPlaylistMove(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.RENAME):
                            if (OnRename != null) OnRename(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.RM):
                            if (OnRm != null) OnRm(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.StoredPlaylist.SAVE):
                            if (OnSave != null) OnSave(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Outputs.OUTPUTS):
                            if (OnOutputs != null) OnOutputs(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Outputs.TOGGLEOUTPUT):
                            if (OnToggleOutput != null) OnToggleOutput(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Outputs.ENABLEOUTPUT):
                            if (OnEnableOutput != null) OnEnableOutput(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Outputs.DISABLEOUTPUT):
                            if (OnDisableOutput != null) OnDisableOutput(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Database.UPDATE):
                            if (OnUpdate != null) OnUpdate(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Playback.SEEKCUR):
                            if (OnSeekCur != null) OnSeekCur(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Status.IDLE):
                            ResponsConfigDictionary[responseId].idle = true;
                            if (OnIdle != null) OnIdle(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        case (MPDKeyWords.Client.Status.NOIDLE):
                            noidle = true;
                            ResponsConfigDictionary[responseId].ClearConfig();
                            ResponsConfigDictionary[responseId].idle = false;
                            if (OnNoIdle != null) OnNoIdle(this, new ResponseEventArgs(streamSocket, responseId, position, commandArgs.Item2));
                            break;
                        default:
                            if (OnDefault != null) OnDefault(this, new ResponseEventArgs(streamSocket, responseId, position));
                            break;
                    }
                }
            }
            return noidle;
        }

        Tuple<string, List<string>> SplitCommandArgs(string command)
        {   
            string com = command.Split(" ".ToArray(), 
                    StringSplitOptions.RemoveEmptyEntries).First<string>();
                            
            com = com.Replace(MPDKeyWords.Response.LINEBREAK, "");

            List<string> arguments = new List<string>();                              


            if (!string.IsNullOrEmpty(com))
            {
                string tempsarg = string.Empty;
                bool appro = false;

                foreach (char c in command.Replace(com, "").ToCharArray())
                {
                    if (c.Equals('"') && !appro) appro = true;
                    else if (c.Equals('"') && appro)
                    {
                        appro = false;
                        if (!string.IsNullOrEmpty(tempsarg) || !string.IsNullOrWhiteSpace(tempsarg)) arguments.Add(tempsarg);
                        tempsarg = string.Empty;
                    }
                    else if (c.Equals(' ') && !appro)
                    {
                        if (!string.IsNullOrEmpty(tempsarg) || !string.IsNullOrWhiteSpace(tempsarg)) arguments.Add(tempsarg);
                        tempsarg = string.Empty;
                    }
                    else if (!c.Equals('\n')) tempsarg += c;
                    else
                    {
                        if(!string.IsNullOrEmpty(tempsarg) || !string.IsNullOrWhiteSpace(tempsarg)) arguments.Add(tempsarg);
                    }
                }

                if (!string.IsNullOrEmpty(tempsarg) || !string.IsNullOrWhiteSpace(tempsarg)) arguments.Add(tempsarg);
            }

            return new Tuple<string,List<string>>(com, arguments);
        }

        async void streamSocketListner_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            ResponsConfigDictionary[ResponsConfigDictionaryIdCounter] = new ResponseConfig(ResponsConfigDictionaryIdCounter, args.Socket);

            bool allowconnection = false;

            switch (this.allow)
            {
                case ("lan"):
                    Tuple<HostName, HostName> mylocal = GetStartEndIP();
                    List<HostName> mylocalfriends = (mylocal != null) ? GetIpList(mylocal) : null;
                    if(mylocalfriends != null)
                    {
                        var lanfriend = mylocalfriends.SingleOrDefault<HostName>(o => o.DisplayName.Equals(args.Socket.Information.RemoteAddress.DisplayName));
                        if (lanfriend != null) allowconnection = true;
                    }
                    if (args.Socket.Information.RemoteAddress.IPInformation != null)
                    {
                        if (args.Socket.Information.LocalAddress.IPInformation.NetworkAdapter.NetworkAdapterId == args.Socket.Information.RemoteAddress.IPInformation.NetworkAdapter.NetworkAdapterId)
                        {
                            allowconnection = true;
                        }
                    }
                    break;
                case ("any"):
                    allowconnection = true;
                    break;
                default:
                    if (args.Socket.Information.RemoteAddress.IPInformation != null)
                    {
                        if (args.Socket.Information.LocalAddress.IPInformation.NetworkAdapter.NetworkAdapterId == args.Socket.Information.RemoteAddress.IPInformation.NetworkAdapter.NetworkAdapterId)
                        {
                            allowconnection = true;
                        }
                    }
                    break;
            }

            if (allowconnection)
            {
                int responseId = ResponsConfigDictionaryIdCounter;

                ResponsConfigDictionaryIdCounter++;

                bool suc = false;

                try
                {
                    suc = await SendResponse(MPDKeyWords.Response.RESPONSE_SUCCESS_CONNECT, args.Socket);
                }
                catch
                {
                    suc = false;
                }

                if (suc)
                {
                    await ReadResponse(args.Socket, responseId);
                }
            }
        }

        public bool AppendResponse(string response, int id, int position)
        {
            if (ResponsConfigDictionary.ContainsKey(id))
            {
                ResponsConfigDictionary[id].AppendResponse(response, position);

                return true;
            }
            return false;
        }

        public bool AppendResponse(string response, int id)
        {
            if (ResponsConfigDictionary.ContainsKey(id))
            {
                ResponsConfigDictionary[id].AppendResponse(response);

                return true;
            }
            return false;
        }

        public bool EventResponse(string response, int id, int position)
        {
            if (ResponsConfigDictionary.ContainsKey(id))
            {
                ResponsConfigDictionary[id].AppendResponse(response, position);

                return true;
            }
            return false;
        }

        public bool ErrorResponse(string response, int id, int position)
        {
            if (ResponsConfigDictionary.ContainsKey(id))
            {
                ResponsConfigDictionary[id].error_response = true;

                ResponsConfigDictionary[id].AppendResponse(response, position);

                return true;
            }
            return false;
        }

        private async Task<bool> SendResponse(int id, bool emptyresponse = false)
        {
            bool suc = false;

            if (ResponsConfigDictionary.ContainsKey(id))
            {
                while (ResponsConfigDictionary[id].response_finish == false) await Task.Delay(5);

                try
                {
                    DataWriter dataWriter = new DataWriter(ResponsConfigDictionary[id].socket.OutputStream);
                    //dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                    dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                    var bytearray = (emptyresponse) ? new byte[]{} : Encoding.GetEncoding("UTF-8").GetBytes(ResponsConfigDictionary[id].Response);

                    //s = Encoding.GetEncoding("UTF-8").(bytearray, 0, bytearray.Length);     

                    //dataWriter.WriteString(s);
                    dataWriter.WriteBytes(bytearray);

                    //dataWriter.WriteString(s);
                    //await dataWriter.FlushAsync();

                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                    dataWriter.Dispose();
                    suc = true;
                }
                catch
                {
                    suc = false;
                }

                ResponsConfigDictionary[id].ClearConfig();
            }

            return suc;
        }



        private async Task<bool> SendResponse(string response, StreamSocket socket)
        {
            string s = response;
            
            bool suc = false;
            try
            {
                DataWriter dataWriter = new DataWriter(socket.OutputStream);
                dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;


                var bytearray = Encoding.GetEncoding("UTF-8").GetBytes(s);

                //s = Encoding.GetEncoding("UTF-8").(bytearray, 0, bytearray.Length);     

                //dataWriter.WriteString(s);
                dataWriter.WriteBytes(bytearray);

                //dataWriter.WriteString(s);
                //await dataWriter.FlushAsync();

                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
                dataWriter.Dispose();
                suc = true;
            }
            catch
            {
                suc = false;
            }

            return suc;
        }


        private HostName GetMyIP()
        {
            var Host = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();

            var Lan = Windows.Networking.Connectivity.NetworkInformation.GetLanIdentifiers();

            
            Windows.Networking.HostName hostname = null;

            
            foreach (Windows.Networking.HostName hn in Host)
            {
                if (hn.IPInformation != null)
                {
                    if (hn.Type == HostNameType.Ipv4 && hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71)
                    {
                        hostname = hn;
                    }
                }
            }
            

            if (hostname == null)
            {
                foreach (Windows.Networking.Connectivity.LanIdentifier l in Lan)
                {
                    foreach (Windows.Networking.HostName h in Host)
                    {
                        if (h.IPInformation != null)
                        {
                            if (h.IPInformation.NetworkAdapter.NetworkAdapterId == l.NetworkAdapterId)
                            {
                                hostname = h;
                            }
                        }
                    }
                }
            }

            return hostname;
        }

        private Tuple<HostName, HostName> GetStartEndIP()
        {
            Windows.Networking.HostName hostname = GetMyIP();

            if (hostname != null)
            {
                string[] ipsplit;
                byte[] ipBytes;
                int bits;
                uint mask;
                byte[] maskBytes;
                byte[] startIPBytes;
                byte[] endIPBytes;

                try
                {
                    ipsplit = hostname.DisplayName.Split(".".ToCharArray(), 4);
                    ipBytes = new byte[] { Convert.ToByte(ipsplit[0]), Convert.ToByte(ipsplit[1]), Convert.ToByte(ipsplit[2]), Convert.ToByte(ipsplit[3]) };
                    bits = Convert.ToInt32(hostname.IPInformation.PrefixLength.ToString());
                    mask = ~(uint.MaxValue >> bits);
                    maskBytes = BitConverter.GetBytes(mask).Reverse().ToArray();
                    startIPBytes = new byte[ipBytes.Length];
                    endIPBytes = new byte[ipBytes.Length];

                    for (int i = 0; i < ipBytes.Length; i++)
                    {
                        startIPBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                        endIPBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }
                }
                catch
                {
                    return null;
                }

                //System.Net.IPAddress ip = new System.Net.IPAddress(new byte[] { 192, 168, 0, 1 });


                // Convert the IP address to bytes.
                //byte[] ipBytes = ip.GetAddressBytes();

                // BitConverter gives bytes in opposite order to GetAddressBytes().

                // Calculate the bytes of the start and end IP addresses.


                // Convert the bytes to IP addresses.
                //IPAddress startIP = new IPAddress(startIPBytes);
                //IPAddress endIP = new IPAddress(endIPBytes);
                if (startIPBytes.Length > 3 && endIPBytes.Length > 3)
                {
                    HostName startIP = new HostName(startIPBytes[0] + "." + startIPBytes[1] + "." + startIPBytes[2] + "." + startIPBytes[3]);
                    HostName endIP = new HostName(endIPBytes[0] + "." + endIPBytes[1] + "." + endIPBytes[2] + "." + endIPBytes[3]);

                    return new Tuple<HostName, HostName>(startIP, endIP);
                }
            }

            return null;
        }

        private List<HostName> GetIpList(Tuple<HostName, HostName> seip)
        {
            List<HostName> ipa = new List<HostName>();

            string[] startIPString;

            byte[] startIPBytes =
                new byte[4];
            try
            {
                startIPString = seip.Item1.DisplayName.Split(".".ToCharArray());

                startIPBytes[0] = Convert.ToByte(startIPString[0]);
                startIPBytes[1] = Convert.ToByte(startIPString[1]);
                startIPBytes[2] = Convert.ToByte(startIPString[2]);
                startIPBytes[3] = Convert.ToByte(startIPString[3]);
            }
            catch
            {
                return ipa;
            }

            string[] endIPString;

            byte[] endIPBytes =
                new byte[4];

            try
            {
                endIPString = seip.Item2.DisplayName.Split(".".ToCharArray());

                endIPBytes[0] = Convert.ToByte(endIPString[0]);
                endIPBytes[1] = Convert.ToByte(endIPString[1]);
                endIPBytes[2] = Convert.ToByte(endIPString[2]);
                endIPBytes[3] = Convert.ToByte(endIPString[3]);
            }
            catch
            {
                return ipa;
            }

            if (startIPBytes.Length == 4 && endIPBytes.Length == 4)
            {
                int s0 = startIPBytes[0];
                while (s0 <= endIPBytes[0])
                {
                    int s1 = startIPBytes[1];
                    while (s1 <= endIPBytes[1])
                    {
                        int s2 = startIPBytes[2];
                        while (s2 <= endIPBytes[2])
                        {
                            int s3 = startIPBytes[3] + 1;
                            while (s3 < endIPBytes[3])
                            {
                                ipa.Add(new HostName(s0 + "." + s1 + "." + s2 + "." + s3));
                                s3++;
                            }
                            s2++;
                        }
                        s1++;
                    }
                    s0++;
                }
            }

            return ipa;
        }
    }
}
