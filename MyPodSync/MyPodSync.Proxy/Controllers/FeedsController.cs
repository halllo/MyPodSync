using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MyPodSync.Proxy.Controllers
{
    [ApiController]
    [Route("feeds")]
    public class FeedsController : ControllerBase
    {
        private readonly ShareClient share;
        private readonly ILogger<FeedsController> _logger;

        public FeedsController(ShareClient share, ILogger<FeedsController> logger)
        {
            this.share = share;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var dir = share.GetDirectoryClient("files");
            var filesAndDirsRequest = dir.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken);
            var filesAndDirsPages = await filesAndDirsRequest.AsPages().ToListAsync();
            var files = filesAndDirsPages.SelectMany(p => p.Values).Where(f => !f.IsDirectory).ToList();

            return Ok(new
            {
                feeds = files.Select(f => new
                {
                    f.Name,
                })
            });
        }

        [HttpGet]
        [Route("{feed}")]
        public async Task<IActionResult> Get(string feed, CancellationToken cancellationToken)
        {
            ValidatePath(feed);

            var dir = share.GetDirectoryClient("files");
            var file = dir.GetFileClient($"{feed}");

            try
            {
                var stream = await file.OpenReadAsync();
                return new FileStreamResult(stream, new MediaTypeHeaderValue("application/rss+xml"));
            }
            catch (Exception)
            {
                return BadRequest("Could not open this feed. Make sure it exists.");
            }
        }

        [HttpGet]
        [Route("{feed}/{enclosure}")]
        public async Task<IActionResult> Get(string feed, string enclosure, CancellationToken cancellationToken)
        {
            ValidatePath(feed, enclosure);

            var dir = share.GetDirectoryClient("files");
            var file = dir.GetFileClient($"{Path.GetFileNameWithoutExtension(feed)}/{enclosure}");

            try
            {
                var properties = await file.GetPropertiesAsync(cancellationToken: cancellationToken);
                var stream = await file.OpenReadAsync();
                return new FileStreamResult(stream, new MediaTypeHeaderValue("audio/mpeg"));

            }
            catch (Exception)
            {
                return BadRequest("Could not open this feed's enclosure. Make sure it exists.");
            }
        }

        private static void ValidatePath(params string[] paths)
        {
            if (paths.Any(path
                => Path.GetInvalidPathChars().Any(path.Contains)
                || Path.GetInvalidFileNameChars().Any(path.Contains)
                || path.Contains("..")))
            {
                throw new ArgumentException("Invalid path.");
            }
        }
    }
}
