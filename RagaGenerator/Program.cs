using Fastnet.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RagaGenerator
{
    class Program
    {
        const string source = @"C:\devroot\Music\RagaGenerator\WikiList.txt";
        const string destination = @"C:\temp\raganames.json";
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var generator = new Generator(source, destination);
            await generator.RunAsync();
        }
    }
    public class RagaName
    {
        public string Name { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
    }
    public class Generator
    {
        private readonly string source;
        private readonly string destination;

        public Generator(string source, string destination)
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
        public async Task RunAsync()
        {
            var listEntries = new List<string>();
            var lines = await File.ReadAllLinesAsync(source);
            foreach(var line in lines)
            {

                if(line.StartsWith("<li>"))
                {
                    listEntries.Add(line);
                }
                else
                {
                    //Debug.WriteLine($"discarded: {line}");
                }
            }
            var names = Process(listEntries);
            WriteRagaNames(names, destination);
            Debug.WriteLine($"{destination} written");
        }
        private void WriteRagaNames(IEnumerable<string> names, string destination)
        {
            var ragaNames = new List<RagaName>();
            foreach(var name in names)
            {
                var rn = new RagaName { Name = name };
                ragaNames.Add(rn);
            }
            var wrapper = new { RagaNames = ragaNames };
            var jsonText = wrapper.ToJson(true);
            File.WriteAllText(destination, jsonText);
        }
        private IEnumerable<string> Process(List<string> lines)
        {
            var firstRegex = new Regex(@"^<li>([a-z0-9ā\-/,’'\(\)\s]+)[</li>|<sup]", RegexOptions.IgnoreCase);
            var secondRegex = new Regex(@"^<li>([a-z0-9ā\-/.,’'\(\)\s]*)<a.+>([a-z0-9ā\-/,’'\(\)\s]+)</a>([a-z0-9ā\-/,’'\(\)\s]*)</li>", RegexOptions.IgnoreCase);
            (bool success, string value) firstMatch(string line)
            {
                if (!line.Contains("<a", StringComparison.CurrentCultureIgnoreCase))
                {
                    var m = firstRegex.Match(line);
                    if (m.Success)
                    {
                        var val = m.Groups[1].Value;
                        //names.Add(val.Trim());
                        return (true, val.Trim());
                    }
                }
                return (false, string.Empty);
            }
            (bool success, string value) secondMatch(string line)
            {
                var m = secondRegex.Match(line);
                if (m.Success)
                {
                    var parts = new List<string>();
                    for(int index = 1;index < m.Groups.Count;++index)
                    {
                        var t = m.Groups[index].Value.Trim();
                        if (t.Length > 0)
                        {
                            parts.Add(t);
                        }
                    }
                    var val = string.Join(" ", parts).Replace("( ", "(").Replace(" )", ")");
                    //names.Add(val.Trim());
                    return (true, val);
                }
                return (false, string.Empty);
            }
            var names = new List<string>();
            var removeSup = new Regex(@"<sup.*</sup>");
            foreach(var l in lines)
            {
                var line = removeSup.Replace(l, string.Empty);
                var (success, value) = firstMatch(line);
                if(success)
                {
                    names.Add(value);
                }
                else
                {
                    (success, value) = secondMatch(line);
                    if (success)
                    {
                        names.Add(value);
                    }
                }
                if (!success)
                {
                    Debug.WriteLine($"discarded: {line}");
                }
            }
            return names;
        }


    }

}
