using FFmpeg.NET;

namespace SpookyLlamaVideoService;

public sealed class FFmpegVideoComposer : IVideoComposer
{
    private readonly Engine ffmpeg = new(ResolveFFmpegPath());

    public async Task<byte[]> ComposeAsync(
        byte[] image,
        byte[] audio,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfZero(image.Length);
        ArgumentOutOfRangeException.ThrowIfZero(audio.Length);

        var workingDirectory = Directory.CreateTempSubdirectory("spooky-llama-video-");
        try
        {
            var imagePath = Path.Combine(workingDirectory.FullName, "frame.png");
            var audioPath = Path.Combine(workingDirectory.FullName, "narration.wav");
            var videoPath = Path.Combine(workingDirectory.FullName, "video.mp4");
            await File.WriteAllBytesAsync(imagePath, image, cancellationToken);
            await File.WriteAllBytesAsync(audioPath, audio, cancellationToken);

            const string arguments =
                "-y -loop 1 -framerate 30 -i frame.png -i narration.wav " +
                "-map 0:v:0 -map 1:a:0 -c:v libx264 -tune stillimage " +
                "-c:a aac -b:a 192k -vf \"scale=ceil(iw/2)*2:ceil(ih/2)*2\" " +
                "-pix_fmt yuv420p -shortest -movflags +faststart video.mp4";
            await ffmpeg.ExecuteAsync(arguments, workingDirectory.FullName, cancellationToken);

            return await File.ReadAllBytesAsync(videoPath, cancellationToken);
        }
        finally
        {
            workingDirectory.Delete(recursive: true);
        }
    }

    private static string ResolveFFmpegPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var executableName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var pathDirectories = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? [];
        var executablePath = pathDirectories
            .Select(directory => Path.Combine(directory, executableName))
            .FirstOrDefault(File.Exists);

        return executablePath
            ?? throw new InvalidOperationException(
                "FFmpeg was not found. Install it or set FFMPEG_PATH.");
    }
}