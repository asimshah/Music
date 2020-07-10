using Fastnet.Core;
using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RagaGenerator
{
    public class CarnaticGenerator
    {
        private readonly string destination;
        private readonly string source;
        public CarnaticGenerator(string source, string destination)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("message", nameof(source));
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("message", nameof(destination));
            }

            this.source = source;
            this.destination = destination;
        }
        public async Task<IEnumerable<string>> RunAsync()
        {
            var listEntries = new List<string>();
            var lines = await File.ReadAllLinesAsync(source);
            foreach (var line in lines)
            {
                var text = line.Trim();
                if(text.Length > 0)
                {
                    var parts = text.Split("---", StringSplitOptions.RemoveEmptyEntries);
                    var ragaName = parts[0].Trim().ToLower();
                    ragaName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ragaName);
                    Debug.WriteLine(ragaName);
                    listEntries.Add(ragaName);
                }
            }
            return listEntries.OrderBy(x => x);
        }
        public void WriteRagaNames(IEnumerable<string> names)
        {
            var ragaNames = new List<RagaName>();
            foreach (var name in names)
            {
                var rn = new RagaName { Name = name };
                ragaNames.Add(rn);
            }
            var wrapper = new { RagaNames = ragaNames };
            var jsonText = wrapper.ToJson(true);
            File.WriteAllText(destination, jsonText);
        }
    }
}
