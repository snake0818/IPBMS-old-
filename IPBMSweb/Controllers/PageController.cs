using IPBMSweb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace IPBMSweb.Controllers
{
    public class PageController(ILogger<PageController> logger) : Controller
    {
        private readonly ILogger<PageController> _logger = logger;
        private static readonly string BaseAPIurl = "http://140.137.41.136:1380/IPBMS/OinkAPI/api/";
        // private static readonly string BaseAPIurl = "http://localhost:5052/api/";
        private readonly string APIurl_imageMedia = $"{BaseAPIurl}Media/Image/";
        private readonly string APIurl_videoMedia = $"{BaseAPIurl}Media/Video/";
        private readonly string APIurl_estimate = $"{BaseAPIurl}Estimate/";
        private readonly string APIurl_tracking = $"{BaseAPIurl}Tracking/";

        #region 頁面載入

        [HttpGet]
        public IActionResult Index() { return View(); }

        [HttpGet]
        [Route("/Service/PigSizeEstimation")]
        public IActionResult Estimate() { return View("PigSizeEstimation"); }

        [HttpGet]
        [Route("/Service/PigActivityTracking")]
        public IActionResult Tracking() { return View("PigActivityTracking"); }

        #region 錯誤處理
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() { return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); }
        #endregion

        #endregion

        #region 媒體檔案請求

        #region 取得圖片
        [HttpGet]
        [Route("Image")]
        public async Task<IActionResult> GetImage(int imageId)
        {
            using HttpClient client = new();
            try
            {
                HttpResponseMessage res = await client.GetAsync($"{APIurl_imageMedia}/{imageId}");
                res.EnsureSuccessStatusCode();
                var imageStream = await res.Content.ReadAsStreamAsync();
                return File(imageStream, "image/jpeg");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        #endregion

        #region 上傳圖片
        [HttpPost]
        [Route("Image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("未選擇上傳圖片檔案.");

            using var content = new MultipartFormDataContent();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            content.Add(new ByteArrayContent(ms.ToArray()), "file", file.FileName);
            
            using HttpClient client = new();
            HttpResponseMessage? temp = null;
            try
            {
                var res = await client.PostAsync(APIurl_imageMedia, content);
                temp = res;
                res.EnsureSuccessStatusCode();
                var result = await res.Content.ReadAsStringAsync();
                return Ok(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Internal server error, {temp}");
            }
        }
        #endregion

        #region 取得影片
        [HttpGet]
        [Route("Video")]
        public async Task<IActionResult> GetVideo(int videoId)
        {
            using HttpClient client = new();
            try
            {
                var res = await client.GetAsync($"{APIurl_videoMedia}/{videoId}");
                res.EnsureSuccessStatusCode();
                var videoStream = await res.Content.ReadAsStreamAsync();
                return File(videoStream, "video/mp4");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region 上傳影片
        [HttpPost]
        [Route("Video")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("未選擇上傳影片檔案");

            using var content = new MultipartFormDataContent();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            content.Add(new ByteArrayContent(ms.ToArray()), "file", file.FileName);

            using HttpClient client = new();
            try
            {
                var res = await client.PostAsync(APIurl_videoMedia, content);
                res.EnsureSuccessStatusCode();
                var result = await res.Content.ReadAsStringAsync();
                return Ok(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        #endregion

        #endregion

    }
}
