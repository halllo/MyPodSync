using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CommandLine;
using Microsoft.Extensions.Logging;
using NeoSmart.PrettySize;

namespace MyPodSync.Sync.Verbs
{
    [Verb("sync")]
    public class Sync
    {
        [Option('s', longName: "source", Required = true)]
        public string SourceFolder { get; set; } = null!;

        public async Task Do(ILogger<Forget> logger, ShareClient share)
        {
            var dir = share.GetDirectoryClient("files");

            foreach (var sourceFile in Directory
                .GetFiles(SourceFolder, "*.*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f)))
            {
                logger.LogDebug($"Uploading {sourceFile.Name}...");

                var sourceFileFolder = sourceFile.Directory!.FullName;
                if (sourceFileFolder.StartsWith(SourceFolder))
                {
                    var folder = sourceFileFolder.Substring(SourceFolder.Length).Trim(['\\']);
                    var subdir = dir.GetSubdirectoryClient(folder);
                    if (!(await subdir.ExistsAsync()).Value)
                    {
                        await subdir.CreateAsync();
                        logger.LogInformation($"Created {Spectre.Console.Emoji.Known.OpenFileFolder} {folder}");
                    }

                    var file = subdir.GetFileClient(sourceFile.Name);
                    var fileExists = (await file.ExistsAsync()).Value;
                    if (!fileExists || (await file.GetPropertiesAsync()).Value.ContentLength != sourceFile.Length)
                    {
                        using var targetStream = file.OpenWrite(false, 0, new ShareFileOpenWriteOptions { MaxSize = sourceFile.Length });
                        using var sourceStream = sourceFile.OpenRead();
                        await sourceStream.CopyToAsync(targetStream);
                        logger.LogInformation($"Uploaded {Spectre.Console.Emoji.Known.PageFacingUp} {sourceFile.Name} ({new PrettySize(sourceFile.Length)})");
                    }
                    else
                    {
                        logger.LogInformation($"Skipping {Spectre.Console.Emoji.Known.PageFacingUp} {sourceFile.Name}.");
                    }
                }
                else
                {
                    throw new ArgumentException("Folder prefix inconsistent.");
                }
            }
        }
    }
}
