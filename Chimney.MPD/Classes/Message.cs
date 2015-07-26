using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class Message
    {
        public string message { get; set; }
        public Channel channel { get; set; }
        
        public Message(List<KeyValuePair<string, string>> keyValuePairList)
        {
            if (keyValuePairList == null) return;

            foreach (var kv in keyValuePairList)
            {
                switch (kv.Key)
                {
                    case "message":
                        this.message = kv.Value;
                        break;
                    case "channel":
                        this.channel = new Channel(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string> ("channel", kv.Value ) });
                        break;
                    default:
                        break;
                }
            }
        }

        public Message(Dictionary<string, string> dictionary)
        {
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "message":
                        this.message = dictionary[key];
                        break;
                    case "channel":
                        this.channel = new Channel(new Dictionary<string, string>() { { "channel", dictionary[key] } });
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
