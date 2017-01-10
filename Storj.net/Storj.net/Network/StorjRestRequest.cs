using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Storj.net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Storj.net.Network
{
    class StorjRestRequest
    {
        [JsonIgnore]
        public string Endpoint { get; private set; }

        [JsonIgnore]
        public Method Method { get; private set; }

        [JsonIgnore]
        public List<KeyValuePair<string, string>> HeaderAttributes { get; private set; }

        [JsonProperty("__nonce")]
        public string Nonce { get; private set; }

        public StorjRestRequest()
        {
            SetProperties();
            Nonce = Guid.NewGuid().ToString();  
        }

        private void SetProperties()
        {
            if (this.GetType().GetCustomAttributes(typeof(EndpointAttribute), true).Count() == 0)
                return;

            EndpointAttribute endpointAttribute = (EndpointAttribute)this.GetType().GetCustomAttributes(typeof(EndpointAttribute), true).First();
            Endpoint = endpointAttribute.Endpoint;
            Method = endpointAttribute.Method;

            if (this.GetType().GetCustomAttributes(typeof(HeaderAttribute), true).Count() == 0)
                return;

            HeaderAttribute headerAttribute = (HeaderAttribute)this.GetType().GetCustomAttributes(typeof(HeaderAttribute), true).First();
            this.HeaderAttributes = headerAttribute.Attributes;
        }

        internal void ResolveEndpointVars()
        {
            Regex regex = new Regex(@"\{[a-zA-Z0-9_]+\}");
            foreach (Match match in regex.Matches(Endpoint))
            {
                string matchValue = match.Value;
                string name = matchValue.Substring(1, matchValue.Length - 2);

                PropertyInfo property = this.GetType().GetProperty(name);

                if (property == null)
                    continue;

                if (property.GetValue(this) == null)
                    continue;

                string value = this.GetType().GetProperty(name).GetValue(this).ToString();

                Endpoint = Endpoint.Replace(matchValue, value);
            }
        }

        internal void ResolveHeaderVars()
        {
            if (HeaderAttributes == null)
            {
                HeaderAttributes = new List<KeyValuePair<string, string>>();
                return;
            }

            List<KeyValuePair<string, string>> newHeaderAttributes = new List<KeyValuePair<string, string>>();
            Regex regex = new Regex(@"\{[a-zA-Z0-9_]+\}");
            foreach (KeyValuePair<string, string> attribute in HeaderAttributes)
            {
                string attributeValue = attribute.Value;
                foreach (Match match in regex.Matches(attributeValue))
                {
                    string matchValue = match.Value;
                    string name = matchValue.Substring(1, matchValue.Length - 2);

                    PropertyInfo property = this.GetType().GetProperty(name);

                    if (property == null)
                        continue;

                    if (property.GetValue(this) == null)
                        continue;

                    string value = this.GetType().GetProperty(name).GetValue(this).ToString();

                    attributeValue = attributeValue.Replace(matchValue, value);
                }

                newHeaderAttributes.Add(new KeyValuePair<string, string>(attribute.Key, attributeValue));
            }

            HeaderAttributes = newHeaderAttributes;
        }

        public string ToDataString()
        {
            if (IsQueryOnly())
                return "__nonce=" + Nonce;
            else
                return JObject.FromObject(this).ToString(Formatting.None);
        }

        public bool IsQueryOnly()
        {
            return JObject.FromObject(this).Count == 1;
        }
    }
}
