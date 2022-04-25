using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSockets.Http
{
    public class HttpQuery
    {
        private List<KeyValuePair<string, string>> data;

        public int Count
        {
            get { return data.Count; }
        }

        public string this[string name]
        {
            get
            {
                var entry = data.FirstOrDefault(x => x.Key == name);

                if (entry.Value == null)
                {
                    return null;
                }
                else
                {
                    return entry.Value;
                }
            }
        }

        public HttpQuery()
        {
            data = new List<KeyValuePair<string, string>>();   
        }

        public void Add(string name, string value)
        {
            data.Add(new KeyValuePair<string, string>(name, value));
        }
        public bool Contains(string name)
        {
            return this[name] != null;
        }

        public override string ToString()
        {
            var result = "";

            if (data.Count > 0)
            {
                result += "?";

                for (var i=0; i<data.Count; i++)
                {
                    var entry = data[i];

                    result += $"{entry.Key}={entry.Value}";

                    if (i < data.Count - 1)
                    {
                        result += "&";
                    }
                }
            }

            return result;
        }
        public List<KeyValuePair<string, string>> ToList()
        {
            return data;
        }
        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>();

            foreach (var entry in data)
            {
                if (result.ContainsKey(entry.Key))
                {
                    throw new InvalidOperationException($"Unable to convert HttpQuery to dictionary. An entry with key '{entry.Key}' already exists");
                }
                else
                {
                    result.Add(entry.Key, entry.Value);
                }
            }

            return result;
        }
    }
}