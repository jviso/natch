using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class Worker
{
    public static async Task Work(ConcurrentQueue<string> workQueue, IConfigurationRoot config, ConcurrentBag<TranscriptionResult> results)
    {
        using (var httpClient = new HttpClient())
        {
            var timer = new Stopwatch();
            while (workQueue.TryDequeue(out string audioFilename))
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(config["baseUrl"]),
                    Method = HttpMethod.Post,
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(config["auth"])));

                var fileId = Path.GetFileNameWithoutExtension(audioFilename);
                var transcriptFilename = $"{config["outputDir"]}/{fileId}.txt";

                using (var writer = File.CreateText(transcriptFilename))
                {
                    var bytes = await File.ReadAllBytesAsync(audioFilename);
                    request.Content = new ByteArrayContent(bytes);

                    Console.WriteLine($"Sending file {fileId} to Deepgram Brain...");
                    timer.Start();
                    var response = await httpClient.SendAsync(request);
                    timer.Stop();
                    Console.WriteLine($"Received transcript for file {fileId} from Deepgram Brain.");

                    var transcript = "";
                    if (bool.Parse(config["parseForTranscript"]))
                    {
                        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                        transcript = doc.RootElement
                            .GetProperty("results")
                            .GetProperty("channels")[0]
                            .GetProperty("alternatives")[0]
                            .GetProperty("transcript")
                            .GetRawText();
                    }
                    else transcript = await response.Content.ReadAsStringAsync();

                    await writer.WriteAsync(transcript);
                    results.Add(new TranscriptionResult
                    { 
                        Filename = fileId,
                        Filesize = bytes.Length / 1_048_576d,
                        TranscriptionLatency = timer.ElapsedMilliseconds
                    });
                }
            }
        }
    }
}