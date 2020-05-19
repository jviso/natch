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
                Console.WriteLine(TextConstants.TranscriptionComplete);
                var table = new ConsoleTable("File", "Size (MB)", "Duration", "Transcription Latency (ms)");
                foreach (var result in results)
                {
                    table.AddRow(result.Filename, Math.Round(result.FilesizeInMegabytes, 2), result.AudioDuration.ToString("g"), result.TranscriptionLatency);
                    totalFilesize += result.FilesizeInMegabytes;
                    totalDuration = totalDuration.Add(result.AudioDuration);
                }
                table.AddRow($"TOTAL ({results.Count} files)", Math.Round(totalFilesize, 2), totalDuration.ToString("g"), timer.ElapsedMilliseconds);
                table.Configure(o => o.EnableCount = false);
                table.Write();
                Console.WriteLine($"⟹    Transcribed {totalDuration.ToString("g")} of audio in {timer.ElapsedMilliseconds / 1000d} seconds: {Math.Round(totalDuration.TotalMilliseconds / timer.ElapsedMilliseconds, 0)}x speed-up");
            }
            else
                Console.WriteLine($"Handled {files.Count()} files in {timer.ElapsedMilliseconds / 1000} seconds.");
        }
    }
}
