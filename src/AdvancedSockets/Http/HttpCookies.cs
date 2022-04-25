using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSockets.Http
{
    public class HttpCookies
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

        public HttpCookies()
        {
            data = new List<KeyValuePair<string, string>>();   
        }

        public void Add(string name, string value)
        {
            data.Add(new KeyValuePair<string, string>(name, value));
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
                    throw new InvalidOperationException($"Unable to convert HttpCookies to dictionary. An entry with key '{entry.Key}' already exists");
                }
                else
                {
                    result.Add(entry.Key, entry.Value);
                }
            }

            return result;
        }
        public override string ToString()
        {
            var result = "";

            for (var i=0; i<data.Count; i++)
            {
                var key = data[i].Key;
                var value = data[i].Value;

                result += $"{key}={value}";

                if (i < data.Count)
                {
                    result += ";";
                }
            }

            return result;
        }
    }
}