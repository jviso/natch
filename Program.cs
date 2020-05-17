using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using Microsoft.Extensions.Configuration;

namespace Natch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false)
                .Build();

            var files = Directory.EnumerateFiles(config["inputDir"]);
            var outputDir = Directory.CreateDirectory(config["outputDir"]);

            var workQueue = new ConcurrentQueue<string>();
            foreach (var value in files)
            {
                workQueue.Enqueue(value);
            }

            var tasks = new List<Task>();
            var results = new ConcurrentBag<TranscriptionResult>();

            var timer = new Stopwatch();
            timer.Start();

            foreach (var value in Enumerable.Range(0, int.Parse(config["workers"])))
            {
                tasks.Add(Worker.Work(workQueue, config, results));
            }

            await Task.WhenAll(tasks);

            timer.Stop();
            if (bool.Parse(config["demoMode"]))
            {
                var totalFilesize = 0d;
                Console.WriteLine(TextConstants.TranscriptionComplete);
                var table = new ConsoleTable("File", "Size (MB)", "Transcription Latency (ms)");
                foreach (var result in results)
                {
                    table.AddRow(result.Filename, Math.Round(result.Filesize, 2), result.TranscriptionLatency);
                    totalFilesize += result.Filesize;
                }
                table.AddRow("TOTAL", Math.Round(totalFilesize, 2), timer.ElapsedMilliseconds);
                table.Configure(o => o.EnableCount = false);
                table.Write();
            }
            else
                Console.WriteLine($"Handled {files.Count()} files in {timer.ElapsedMilliseconds / 1000} seconds.");
        }
    }
}
