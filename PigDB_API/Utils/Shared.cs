using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace PigDB_API.Utils
{
    public static class Shared
    {

        #region 方法

        // 確保路徑存在
        public static void EnsurePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.WriteLine($"{path} 已創建。");
            }
            else { Console.WriteLine($"{path} 已存在。"); }
        }

        // 當前 unix time
        public static long UnixTime()
        {
            long unixtime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            return unixtime;
        }

        // 格式化當前時間字串(年月日T時分秒)
        public static string FomattedUtcTime()
        {
            DateTime currentUtcTime = DateTime.UtcNow;
            string formattedTime = currentUtcTime.ToString("yyyyMMddTHHmmss");
            return formattedTime;
        }

        // 建立檔案並回傳路徑，預設檔名使用 FomattedUtcTime()
        public static async Task<string> CopyFileStream(IFormFile file, string path, string? FileName = null)
        {
            Directory.CreateDirectory(path); // 確保資料夾存在
            var fileName = $"{FileName ?? FomattedUtcTime() + Path.GetExtension(file.FileName)}"; // 使用時間戳與原始副檔名 作為新檔名
            var filePath = Path.Combine(path, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return filePath;
        }

        #endregion

        // 串流影片檔案的方法
        public static IActionResult StreamVideo(string filePath)
        {
            // 檢查檔案是否存在
            if (!File.Exists(filePath)) { return new NotFoundResult(); }

            // 回傳串流影片檔案 (啟用範圍處理)
            return new PhysicalFileResult(filePath, "video/mp4") { EnableRangeProcessing = true };

            // return PhysicalFile(filePath, "application/dash+xml");

            // 2
            // var videoFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            // return new FileStreamResult(videoFileStream, "video/mp4"){ EnableRangeProcessing = true};

            // 3
            // var fileInfo = new FileInfo(filePath);
            // long fileLength = fileInfo.Length;
            // long start = 0, end = fileLength - 1;

            // if (Request.Headers.ContainsKey("Range"))
            // {
            //     var rangeHeader = Request.Headers.Range.ToString();
            //     var range = rangeHeader.Split(['=', '-']);

            //     start = Convert.ToInt64(range[1]);
            //     if (range.Length > 2 && !string.IsNullOrEmpty(range[2])) { end = Convert.ToInt64(range[2]); }

            //     if (start >= fileLength || end >= fileLength) { return StatusCode(416, "Requested range not satisfiable"); }
            // }

            // var contentLength = end - start + 1;
            // var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            // stream.Seek(start, SeekOrigin.Begin);

            // Response.Headers.ContentRange = $"bytes {start}-{end}/{fileLength}";
            // Response.Headers.AcceptRanges = "bytes";
            // Response.ContentLength = contentLength;

            // return new FileStreamResult(stream, "video/mp4") { EnableRangeProcessing = true };
        }

    }
}