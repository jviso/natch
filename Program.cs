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
                var totalDuration = TimeSpan.Zero;
                var totalLatency = timer.ElapsedMilliseconds;
                var table = new ConsoleTable("File", "Size (MB)", "Duration", "Latency (ms)", "Realtime Factor");

                foreach (var result in results)
                {
                    table.AddRow(result.Filename, Math.Round(result.FilesizeInMegabytes, 2),
                                 result.AudioDuration.ToString("g"), result.Latency, Math.Round(result.RealtimeSpeedup, 0));

                    totalFilesize += result.FilesizeInMegabytes;
                    totalDuration = totalDuration.Add(result.AudioDuration);
                }

                var totalSpeedup = Math.Round(totalDuration.TotalMilliseconds / totalLatency, 0);
                table.AddRow($"TOTAL ({results.Count} files)", Math.Round(totalFilesize, 2), totalDuration.ToString("g"), totalLatency, totalSpeedup);
                table.Configure(o => o.EnableCount = false);

                Console.WriteLine(TextConstants.TranscriptionComplete);
                table.Write();
            }
        }
    }
}
