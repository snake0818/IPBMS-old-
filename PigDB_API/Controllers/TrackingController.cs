using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PigDB_API.Data;
using PigDB_API.Models;
using PigDB_API.Services;
using PigDB_API.Utils;

namespace PigDB_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackingController : Controller
    {
        #region 建構

        private readonly PigDBContext _context; // 宣告 PigDB 物件變數
        private readonly SettingService _setting;
        private string _imageFolderPath;
        private string _videoFolderPath;
        private string _dataFolderPath;
        private string ModelUrl;

        public TrackingController(PigDBContext database, SettingService settings)
        {
            _context = database;
            _setting = settings;
            (_imageFolderPath, _videoFolderPath, _dataFolderPath) = ReloadBasePath();
            ModelUrl = _setting.ModelUrl;
        }
        #endregion

        #region 執行追蹤檢測服務
        [HttpGet]
        [Route("{Video_id}")]
        // public async Task<IActionResult> GetTrackingService(int Video_id)
        // {
        //     if (_setting.ReloadModelConnect()) ModelUrl = _setting.ModelUrl;

        //     if (ModelUrl == "|") return BadRequest(new { error = "模型端連接失敗!" });

        //     try
        //     {
        //         using HttpClient client = new();
        //         var response = await client.GetAsync($"{ModelUrl}tracking/{Video_id}");
        //         response.EnsureSuccessStatusCode();
        //         var result = await response.Content.ReadAsStringAsync();
        //         return Ok(result);
        //     }
        //     // 捕獲異常並返回錯誤信息
        //     catch (HttpRequestException httpEx)
        //     {
        //         return StatusCode(500, $"模型服務請求錯誤: {httpEx.Message}");
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, $"執行追蹤檢測服務時發生錯誤: {ex.Message}");
        //     }
        // }
        public async Task GetTrackingService(int Video_id)
        {
            if (_setting.ReloadModelConnect()) ModelUrl = _setting.ModelUrl;
            if (ModelUrl == "|")
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("模型端連接失敗!");
                return;
            }

            // 設定 SSE 標頭
            Response.Headers.Append("Content-Type", "text/event-stream; charset=utf-8");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // 忽略 SSL 驗證
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true };
            var client = new HttpClient(handler);

            try
            {
                var response = await client.GetAsync($"{ModelUrl}tracking/{Video_id}", HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    await SSESendMessageAsync($"模型服務請求錯誤: {response.ReasonPhrase}");
                    Response.StatusCode = StatusCodes.Status502BadGateway;
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrEmpty(line)) // 逐行讀取模型 API 的 SSE 輸出
                        await SSESendMessageAsync(line); // 將分段數據作為 SSE 事件傳輸
                }
            }
            // 捕獲異常並返回錯誤信息
            catch (HttpRequestException httpEx)
            {
                await SSESendMessageAsync($"模型服務請求錯誤: {httpEx.Message}");
                Response.StatusCode = StatusCodes.Status501NotImplemented;
            }
            catch (Exception ex)
            {
                await SSESendMessageAsync($"執行追蹤檢測服務時發生錯誤: {ex.Message}");
                Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }

        // 以 SSE 格式發送訊息
        private async Task SSESendMessageAsync(string message)
        {
            await Response.WriteAsync($"data: {message}\n\n");
            await Response.Body.FlushAsync(); // 確保即時傳輸
        }
        #endregion

        #region 轉發即時辨識影像串流
        [Route("Streaming")]
        [HttpGet]
        public async Task ForwardVideoStream()
        {
            Response.ContentType = "text/plain; charset=utf-8";
            if (_setting.ReloadModelConnect()) ModelUrl = _setting.ModelUrl;
            if (ModelUrl == "|")
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsync("模型端連接失敗!");
                return;
            }

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{ModelUrl}/video_stream", HttpCompletionOption.ResponseHeadersRead);

            // 檢查是否成功，若無內容則回傳提示訊息
            if (!response.IsSuccessStatusCode)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                await Response.WriteAsync("即時辨識影像路由連接失敗!");
                return;
            }

            // 設定回應的內容類型
            Response.ContentType = "multipart/x-mixed-replace; boundary=frame";

            // 讀取串流並將內容寫回給客戶端
            try
            {
                var stream = await response.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(Response.Body); // 持續將服務端的串流資料寫入回應，保持連接直到結束
            }
            catch (Exception ex)
            {
                Console.WriteLine($"串流中發生錯誤: {ex.Message}");
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Response.WriteAsync("串流過程中發生錯誤！");
            }
        }
        #endregion

        #region 紀錄清單
        [HttpGet]
        [Route("List")]
        public async Task<IActionResult> GetRecordList()
        {
            try
            {
                var records = await _context.TrackingRecords
                    .Select(r => new { r.Id, r.VideoId, r.Timestamp, })
                    .ToListAsync();
                if (records == null || records.Count == 0) { return NotFound("尚未有任何追蹤檢測紀錄資訊!"); };
                return Ok(records);
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄清單時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得紀錄
        [HttpGet]
        [Route("Record/{Record_id}")]
        public async Task<IActionResult> GetRecord(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .Select(r => new { r.Id, r.VideoId, r.Timestamp, })
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤結果紀錄不存在!"); };
                return Ok(record);
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 新增紀錄
        [HttpPost]
        [Route("Record/{Video_id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRecords(int Video_id, IFormFile VideoFile, IFormFile DataFile, IFormFile ImageFile)
        {
            if (ImageFile == null || ImageFile.Length == 0) return BadRequest("沒有圖片檔案被上傳!");
            if (VideoFile == null || VideoFile.Length == 0) return BadRequest("沒有影片檔案被上傳!");
            if (DataFile == null || DataFile.Length == 0) return BadRequest("沒有數據檔案被上傳!");

            // 確認檔案類型是否正確
            if (!Path.GetExtension(DataFile.FileName).Equals(".json", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("檔案必須為 JSON 格式!");

            try
            {
                // 更新路徑設置
                if (_setting.ReloadBaseConnect())
                    (_imageFolderPath, _videoFolderPath, _dataFolderPath) = ReloadBasePath();

                // 儲存檔案
                string ImageFilePath = await Shared.CopyFileStream(ImageFile, _imageFolderPath);
                string VideoFilePath = await Shared.CopyFileStream(VideoFile, _videoFolderPath);
                string DataFilePath = await Shared.CopyFileStream(DataFile, _dataFolderPath);

                // 儲存紀錄到資料庫
                var newRecord = new TrackingRecord
                {
                    ImagePath = ImageFilePath,
                    VideoPath = VideoFilePath,
                    DataPath = DataFilePath,
                    Timestamp = Shared.UnixTime(),
                    VideoId = Video_id,
                };
                _context.TrackingRecords.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "圖片、影片與數據檔案資訊上傳成功!", newRecord.Id });
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"上傳紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得圖片
        [HttpGet]
        [Route("Image/{Record_id}")]
        public async Task<IActionResult> GetImage(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤檢測結果不存在!"); };
                return PhysicalFile(record.ImagePath, "image/jpeg");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得影片
        [HttpGet]
        [Route("Video/{Record_id}")]
        public async Task<IActionResult> GetVideo(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤檢測結果不存在!"); };

                // 回傳影片串流
                return Shared.StreamVideo(record.VideoPath);
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得數據
        [HttpGet]
        [Route("Data/{Record_id}")]
        public async Task<IActionResult> GetData(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤檢測結果不存在!"); };
                return PhysicalFile(record.DataPath, "application/json");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄數據時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 方法

        // 更新儲存路徑
        private (string, string, string) ReloadBasePath()
        {
            _setting.ReloadBaseConnect();
            string BasePATH = _setting.BasePath;
            var controllerName = GetType().Name.Replace("Controller", "");
            var imageFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Images");
            var videoFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Videos");
            var dataFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Data");
            Shared.EnsurePathExists(imageFolderPath);
            Shared.EnsurePathExists(videoFolderPath);
            Shared.EnsurePathExists(dataFolderPath);
            return (imageFolderPath, videoFolderPath, dataFolderPath);
        }

        #endregion

    }
}
