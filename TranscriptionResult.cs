using System;

public class TranscriptionResult
{
    public string Filename { get; set; }
    public double FilesizeInMegabytes { get; set; }
    public long Latency { get; set; }
    public TimeSpan AudioDuration { get; set; }
    public double RealtimeSpeedup { get; set; }
}