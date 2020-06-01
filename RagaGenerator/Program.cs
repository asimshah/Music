using Fastnet.Core;
using Fastnet.Music.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RagaGenerator
{
    class Program
    {
        const string hindustaniSource = @"C:\devroot\Music\RagaGenerator\WikiList.txt";
        const string carnaticSource = @"C:\devroot\Music\RagaGenerator\originalCarnaticList.txt";
        const string hindustaniDestination = @"C:\temp\raganames.json";
        const string carnaticDestination = @"C:\temp\carnaticRagaNames.json";

        const string liveRaganames = @"C:\devroot\Music\Fastnet.Apollo.Web\raganames.json";
        const string mergedOutput = @"C:\temp\merged-names.json";
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //var generator = new HindustaniGenerator(hindustaniSource, destination);
            //await generator.RunAsync();
            //var generator = new CarnaticGenerator(carnaticSource, hindustaniDestination);
            //var list = await generator.RunAsync();
            //generator.WriteRagaNames(list);
            var merger = new Merger(liveRaganames, carnaticDestination, mergedOutput);
            await merger.RunAsync();
        }
    }
    class Merger
    {
        private readonly string liveRaganames;
        private readonly string carnaticSource;
        private readonly string mergedOutput;
        public Merger(string liveRaganames, string carnaticSource, string mergedOutput)
        {
            this.liveRaganames = liveRaganames;
            this.carnaticSource = carnaticSource;
            this.mergedOutput = mergedOutput;
        }
        public async Task RunAsync()
        {
            var liveNames = await GetLiveNames();
            var ici = new IndianClassicalInformation();// so we can use the lookup technique
            ici.RagaNames = liveNames;
            var caranaticNames = await GetCarnaticNames();
            foreach(var cn in caranaticNames)
            {
                var alphamericName = cn.Name.ToAlphaNumerics();
                if(ici.Lookup.ContainsKey(alphamericName))
                {
                    Debug.WriteLine($"{cn.Name} already exists!!!!!!!");
                    var en = ici.Lookup[alphamericName];
                    if(en.Name != cn.Name)
                    {
                        Debug.WriteLine($"{cn.Name} id {en.Name} needs to be added as an alias");
                    }
                }
                else
                {
                    Debug.WriteLine($"{cn.Name} to be added ...");
                    liveNames.Add(cn);
                }
            }
            var jsonText = liveNames.OrderBy(x => x.Name).ToJson();
            await File.WriteAllTextAsync(mergedOutput, jsonText);
        }
        private async Task<List<RagaName>> GetCarnaticNames()
        {
            var jsonText = await File.ReadAllTextAsync(carnaticSource);
            var jo = JObject.Parse(jsonText);
            var nameJson = jo["RagaNames"].ToString();
            return nameJson.ToInstance<List<RagaName>>();
        }
        private async Task<List<RagaName>> GetLiveNames()
        {
            var jsonText = await File.ReadAllTextAsync(liveRaganames);
            var jo = JObject.Parse(jsonText);
            var nameJson = jo["IndianClassicalInformation"]["RagaNames"].ToString();
            return nameJson.ToInstance<List<RagaName>>();
        }
    }
}
