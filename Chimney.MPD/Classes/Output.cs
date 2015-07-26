using System;
using System.Collections.Generic;

namespace Chimney.MPD.Classes
{
    public class Output
    {
        public string outputname { get; set; }
        public bool outputenabled { get; set; }
        public string outputid { get; set; }

        public Output()
        {

        }

        public Output(List<KeyValuePair<string, string>> keyValuePair)
        {
            if (keyValuePair == null) return;

            foreach(var kv in keyValuePair)
            {
                switch (kv.Key)
                {
                    case "outputname":
                        this.outputname = kv.Value;
                        break;
                    case "outputenabled":
                        this.outputenabled = Convert.ToBoolean(Convert.ToInt32(kv.Value));
                        break;
                    case "outputid":
                        this.outputid = kv.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public Output(Dictionary<string, string> dictionary)
        {
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "outputname":
                        this.outputname = dictionary[key];
                        break;
                    case "outputenabled":
                        this.outputenabled = Convert.ToBoolean(Convert.ToInt32(dictionary[key]));
                        break;
                    case "outputid":
                        this.outputid = dictionary[key];
                        break;
                    default:
                        break;
                }
            }
        }

        public override string ToString()
        {
            string returnstring = string.Empty;
            returnstring += "outputid: " + this.outputid + "\n";
            returnstring += "outputname: " + this.outputname + "\n";
            returnstring += "outputenabled: " + Convert.ToInt32(this.outputenabled) + "\n";

            return returnstring;
        }

        public string ToJson()
        {
            return "{\n" + "\"outputname\": " + "\"" + this.outputname + "\"," +
                "\n\"outputenabled\": " + "\"" + this.outputname + "\"," +
                "\n\"outputid\": " + "\"" + this.outputid + "\"" + "\n}";
        }
    }

}
