using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fastnet.Core;
using Newtonsoft.Json;

namespace Fastnet.Music.Core
{
    public class IndianClassicalInformation
    {
        [JsonIgnore]
        private IDictionary<string, RagaName> lookup;
        public IEnumerable<RagaName> RagaNames { get; set; }
        //public RagaName[] RagaNames { get; set; }
        [JsonIgnore]
        public IDictionary<string, RagaName> Lookup
        {
            get
            {
                if(lookup == null)
                {
                    PrepareNames();
                }
                return lookup;
            }
            private set => lookup = value;
        }
        private void PrepareNames()
        {
            if (lookup == null)
            {
                var dict = new Dictionary<string, RagaName>(StringComparer.CurrentCultureIgnoreCase);
                foreach (var rn in RagaNames)
                {
                    try
                    {
                        dict.Add(rn.Name.ToAlphaNumerics(), rn);
                        foreach (var alias in rn.Aliases)
                        {
                            dict.Add(alias.ToAlphaNumerics(), rn);
                        }
                    }
                    catch (Exception)
                    {
                        Debugger.Break();
                        throw;
                    }
                }
                lookup = dict;
            }
        }
    }
}
