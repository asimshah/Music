using System.Collections.Generic;
using System.Diagnostics;
using Fastnet.Core;

namespace Fastnet.Music.Core
{
    public class IndianClassicalInformation
    {
        public IEnumerable<RagaName> RagaNames { get; set; }
        public IDictionary<string, RagaName> Lookup { get; private set; }
        public void PrepareNames()
        {
            if (Lookup == null)
            {
                var dict = new Dictionary<string, RagaName>();
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
                    catch (System.Exception)
                    {
                        Debugger.Break();
                        throw;
                    }
                }
                Lookup = dict;
            }
        }
    }
}
