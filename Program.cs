using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            var timer = new Stopwatch();
            timer.Start();

            foreach (var value in Enumerable.Range(0, int.Parse(config["workers"])))
            {
                tasks.Add(Worker.Work(workQueue, value, config));
            }

            await Task.WhenAll(tasks);

            timer.Stop();
            Console.WriteLine($"Transcribed {files.Count()} files in {timer.ElapsedMilliseconds / 1000} seconds.");
        }
    }
}
