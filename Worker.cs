using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class Worker
{
    public static async Task Work(ConcurrentQueue<string> workQueue, int workerId, IConfigurationRoot config)
    {
        using (var httpClient = new HttpClient())
        {
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
                    var response = await httpClient.SendAsync(request);
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
                }
            }
        }
    }
}