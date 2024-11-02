using Azure.Storage.Files.Shares;
using CommandLine;
using Microsoft.Extensions.Logging;

namespace MyPodSync.Sync.Verbs
{
    [Verb("forget")]
    public class Forget
    {
        public async Task Do(ILogger<Forget> logger, ShareClient share)
        {
            var dir = share.GetDirectoryClient("files");

            await DeleteEverythingInThisDirectory(logger, dir);
        }

        private static async Task DeleteEverythingInThisDirectory(ILogger<Forget> logger, ShareDirectoryClient dir)
        {
            var filesAndDirsRequest = dir.GetFilesAndDirectoriesAsync();
            var filesAndDirsPages = await filesAndDirsRequest.AsPages().ToListAsync();

            var directories = filesAndDirsPages.SelectMany(p => p.Values).Where(f => f.IsDirectory);
            foreach (var directory in directories)
            {
                var subdir = dir.GetSubdirectoryClient(directory.Name);
                await DeleteEverythingInThisDirectory(logger, subdir);
                
                logger.LogDebug($"Deleting {subdir.Name}...");
                await subdir.DeleteAsync();
                logger.LogInformation($"Deleted {Spectre.Console.Emoji.Known.OpenFileFolder} {subdir.Name}");
            }

            var files = filesAndDirsPages.SelectMany(p => p.Values).Where(f => !f.IsDirectory);
            foreach (var file in files)
            {
                logger.LogDebug($"Deleting {file.Name}...");
                await dir.DeleteFileAsync(file.Name);
                logger.LogInformation($"Deleted {Spectre.Console.Emoji.Known.PageFacingUp} {file.Name}");
            }
        }
    }
}
