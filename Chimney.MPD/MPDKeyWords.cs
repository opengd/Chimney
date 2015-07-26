using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Chimney.MPD
{
    public static class MPDKeyWords
    {
        public static class Send
        {
            public const string SEPERATION = " ";
            public const string QUOTATION_MARK = "\"";

            public static string Encode(string cmd)
            {
                return Encode(cmd, new List<string>(), new List<string>());
            }

            public static string Encode(string cmd, string args)
            {
                return Encode(cmd, new List<string>() { args }, new List<string>());
            }

            public static string Encode(string cmd, List<string> args)
            {
                return Encode(cmd, args, new List<string>());
            }

            public static string Encode(string cmd, List<string> args, List<string> quoted_args, bool reversarguments = false)
            {
                string send = cmd;

                var args_send = string.Empty;
                var quoted_args_send = string.Empty;

                if (args.Count > 0)
                {
                    for (var i = 0; i < args.Count; i++)
                    {
                        args_send += args[i];
                        if (i < args.Count - 1)
                            args_send += MPDKeyWords.Send.SEPERATION;
                    }
                }

                if (quoted_args.Count > 0)
                {
                    for (var i = 0; i < quoted_args.Count; i++)
                    {
                        quoted_args_send += MPDKeyWords.Send.QUOTATION_MARK 
                            + quoted_args[i] 
                            + MPDKeyWords.Send.QUOTATION_MARK;
                        if (i < quoted_args.Count - 1)
                            quoted_args_send += MPDKeyWords.Send.SEPERATION;
                    }
                }

                if(reversarguments)
                {
                    if(quoted_args.Count > 0)
                        send += MPDKeyWords.Send.SEPERATION + quoted_args_send;
                    if(args.Count > 0)
                        send += MPDKeyWords.Send.SEPERATION + args_send;
                }
                else
                {
                    if (args.Count > 0)
                        send += MPDKeyWords.Send.SEPERATION + args_send;
                    if (quoted_args.Count > 0)
                        send += MPDKeyWords.Send.SEPERATION + quoted_args_send;
                }

                return send + MPDKeyWords.Response.END;
            }
        }

        public static class Response
        {
            public const string KEYVALUESEPERATION = ":";
            public const string SPACE = " ";
            public const string OK = "OK";
            public const string OK_LINEBREAK = MPDKeyWords.Response.OK + MPDKeyWords.Response.LINEBREAK;
            public const string ACK = "ACK";
            public const string LIST_OK = "list_OK";

            public const string SUCCESS_CONNECT = "OK";
            public const string RESPONSE_SUCCESS_CONNECT = "OK MPD 1.0 Chimney" + END;

            public const string LINEBREAK = "\n";

            public const string END = "\n";

            public const string UPDATING_DB = "updating_db";

            public struct Sections
            {
                public struct Outer
                {
                    public const string PLAYLIST = "playlist";
                    public const string DIRECTORY = "directory";
                    public const string FILE = "file";
                    public const string OUTPUTID = "outputid";
                }

                public struct Inner
                {
                    public const string ARTIST = "Artist";
                    public const string ALBUM = "Album";
                    public const string GENRE = "Genre";
                }
            }

            public static async Task<List<List<KeyValuePair<string, string>>>> Encode(string value)
            {
                return GetResponseObjects(await GetResponseKeyValuePairList(value));
            }

            private static async Task<List<KeyValuePair<string, string>>> GetResponseKeyValuePairList(string responseString)
            {
                var list = new List<KeyValuePair<string, string>>();

                if (!string.IsNullOrEmpty(responseString))
                {
                    using (var stringReader = new StringReader(responseString))
                    {
                        string line = string.Empty;
                        while (!string.IsNullOrEmpty(line = await stringReader.ReadLineAsync()) 
                            && !line.Equals(MPDKeyWords.Response.OK))
                        {
                            var pair = line.Split(MPDKeyWords.Response.KEYVALUESEPERATION.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            
                            var key = (pair.Length > 0) 
                                ? pair[0] 
                                : string.Empty;

                            if (pair.Length > 1)
                                pair[1] = pair[1].TrimStart(MPDKeyWords.Response.SPACE.ToCharArray());

                            var value = string.Empty;

                            for(var i = 1; i < pair.Length; i++)
                            {
                                value += pair[i];
                                if(i != pair.Length - 1) value += MPDKeyWords.Response.KEYVALUESEPERATION;
                            }

                            list.Add(new KeyValuePair<string, string>(key, value));
                        }
                    }
                }

                return list;
            }

            private static List<List<KeyValuePair<string, string>>> GetResponseObjects(List<KeyValuePair<string, string>> keyValuePairList)
            {
                var outerList = new List<List<KeyValuePair<string, string>>>();

                if (keyValuePairList != null)
                {
                    var innerList = new List<KeyValuePair<string, string>>();
                    var outer = false;
                    var i = 0;
                    foreach (var keyValuePair in keyValuePairList)
                    {
                        if ((i == 0 || outer) 
                            && (keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Outer.DIRECTORY)
                            || keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Outer.FILE)
                            || keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Outer.PLAYLIST)
                            || keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Outer.OUTPUTID)))
                        {
                            outer = true;
                            if (innerList.Count > 0) 
                                outerList.Add(innerList);
                            innerList = new List<KeyValuePair<string, string>>();
                        }
                        else if (!outer && 
                                (keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Inner.ARTIST)
                                || keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Inner.ALBUM)
                                || keyValuePair.Key.Equals(MPDKeyWords.Response.Sections.Inner.GENRE)))
                        {
                            outer = false;
                            if (innerList.Count > 0) 
                                outerList.Add(innerList);
                            innerList = new List<KeyValuePair<string, string>>();
                        }

                        innerList.Add(keyValuePair);
                        i++;

                    }
                    if (innerList.Count > 0)
                        outerList.Add(innerList);
                }

                return outerList;
            }
        }

        public static class Client
        {
            public struct Connection
            {
                public const string CLOSE = "close";
                public const string KILL = "kill";
                public const string PASSWORD = "password";
                public const string PING = "ping";
            }

            public struct CommandList
            {
                public const string COMMAND_LIST_OK_BEGIN = "command_list_ok_begin";
                public const string COMMAND_LIST_BEGIN = "command_list_begin";
                public const string COMMAND_LIST_END = "command_list_end";
            }

            public struct Status
            {
                public const string CLEARERROR = "clearerror";
                public const string CURRENTSONG = "currentsong";
                public const string IDLE = "idle";
                public const string NOIDLE = "noidle";
                public const string STATUS = "status";
                public const string STATS = "stats";
            }

            public struct Playback
            {
                public const string CONSUME = "consume";
                public const string CROSSFADE = "crossfade";
                public const string MIXRAMPDB = "mixrampdb";
                public const string MIXRAMPDELAY = "mixrampdelay";
                public const string RANDOM = "random";
                public const string REPEAT = "repeat";
                public const string SETVOL = "setvol";
                public const string SINGLE = "single";
                public const string REPLAY_GAIN_MODE = "replay_gain_mode";
                public const string REPLAY_GAIN_STATUS = "replay_gain_status";
                public const string VOLUME = "volume"; // DO NOT USE

                public const string NEXT = "next";
                public const string PAUSE = "pause";
                public const string PLAY = "play";
                public const string PLAYID = "playid";
                public const string PREVIOUS = "previous";
                public const string SEEK = "seek";
                public const string SEEKID = "seekid";
                public const string SEEKCUR = "seekcur";
                public const string STOP = "stop";
            }

            public struct Playlist
            {
                public const string ADD = "add";
                public const string ADDID = "addid";
                public const string CLEAR = "clear";
                public const string DELETE = "delete";
                public const string DELETEID = "deleteid";
                public const string MOVE = "move";
                public const string MOVEID = "moveid";
                public const string PLAYLIST = "playlist"; //DO NOT USE 
                public const string PLAYLISTFIND = "playlistfind";
                public const string PLAYLISTID = "playlistid";
                public const string PLAYLISTINFO = "playlistinfo";
                public const string PLAYLISTSEARCH = "playlistsearch";
                public const string PLCHANGES = "plchanges";
                public const string PLCHANGESPOSID = "plchangesposid";
                public const string PRIO = "prio";
                public const string PRIOID = "prioid";
                public const string SHUFFLE = "shuffle";
                public const string SWAP = "swap";
                public const string SWAPID = "swapid";
            }

            public struct StoredPlaylist
            {
                public const string LISTPLAYLIST = "listplaylist";
                public const string LISTPLAYLISTINFO = "listplaylistinfo";
                public const string LISTPLAYLISTS = "listplaylists";
                public const string LOAD = "load";
                public const string PLAYLISTADD = "playlistadd";
                public const string PLAYLISTCLEAR = "playlistclear";
                public const string PLAYLISTDELETE = "playlistdelete";
                public const string PLAYLISTMOVE = "playlistmove";
                public const string RENAME = "rename";
                public const string RM = "rm";
                public const string SAVE = "save";
            }

            public struct Database
            {
                public const string COUNT = "count";
                public const string FIND = "find";
                public const string FINDIN = "findin";
                public const string FINDADD = "findadd";
                public const string LIST = "list";
                public const string LISTALL = "listall";
                public const string LISTALLINFO = "listallinfo";
                public const string LSINFO = "lsinfo";
                public const string READCOMMENTS = "readcomments";
                public const string SEARCH = "search";
                public const string SEARCHIN = "searchin";
                public const string SEARCHADD = "searchadd";
                public const string SEARCHADDPL = "searchaddpl";
                public const string UPDATE = "update";
                public const string RESCAN = "rescan";
            }

            public struct Stickers
            {
                public const string STICKER_GET = "sticker get";
                public const string STICKER_SET = "sticker set";
                public const string STICKER_DELETE = "sticker delete";
                public const string STICKER_LIST = "sticker list";
                public const string STICKER_FIND = "sticker find";
            }

            public struct Outputs
            {
                public const string DISABLEOUTPUT = "disableoutput";
                public const string ENABLEOUTPUT = "enableoutput";
                public const string TOGGLEOUTPUT = "toggleoutput";
                public const string OUTPUTS = "outputs";
            }

            public struct Reflection
            {
                public const string CONFIG = "config";
                public const string COMMANDS = "commands";
                public const string NOTCOMMANDS = "notcommands";
                public const string TAGTYPES = "tagtypes";
                public const string URLHANDLERS = "urlhandlers";
                public const string DECODERS = "decoders";
            }
            
            public struct ClientToClient
            {
                public const string SUBSCRIBE = "subscribe";
                public const string UNSUBSCRIBE = "unsubscribe";
                public const string CHANNELS = "channels";
                public const string READMESSAGES = "readmessages";
                public const string SENDMESSAGE = "sendmessage";
            }
        }
    }
}
