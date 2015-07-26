using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class Channel
    {
        public string channel { get; set; }

        public Channel(List<KeyValuePair<string, string>> keyValuePairList)
        {
            if (keyValuePairList == null) return;

            foreach (var kv in keyValuePairList)
            {
                switch (kv.Key)
                {
                    case "channel":
                        this.channel = kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public Channel(Dictionary<string, string> dictionary)
        {
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "channel":
                        this.channel = dictionary[key];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
