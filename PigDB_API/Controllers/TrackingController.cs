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
        private string ModelUrl;

        public TrackingController(PigDBContext database, SettingService settings)
        {
            _context = database;
            _setting = settings;
            (_imageFolderPath, _videoFolderPath) = ReloadBasePath();
            ModelUrl = _setting.ModelUrl;
        }
        #endregion

        #region 執行追蹤檢測服務
        [HttpGet]
        [Route("{Video_id}")]
        public async Task<IActionResult> GetTrackingService(int Video_id)
        {
            if (_setting.ReloadModelConnect()) ModelUrl = _setting.ModelUrl;

            if (ModelUrl == "|") return BadRequest(new { error = "模型端連接失敗!" });

            string Model_URL = $"{ModelUrl}tracking";
            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync($"{Model_URL}/{Video_id}");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return Ok(result);
            }
            // 捕獲異常並返回錯誤信息
            catch (HttpRequestException httpEx)
            {
                return StatusCode(500, $"模型服務請求錯誤: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"執行追蹤檢測服務時發生錯誤: {ex.Message}");
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
                if (record == null) { return NotFound("該追蹤檢測結果紀錄不存在!"); };
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
        public async Task<IActionResult> UploadRecords(IFormFile ImageFile, IFormFile VideoFile, int Video_id)
        {
            if (ImageFile == null || ImageFile.Length == 0)
                return BadRequest("沒有圖片被上傳!");
            if (VideoFile == null || VideoFile.Length == 0)
                return BadRequest("沒有影片被上傳!");
            try
            {
                // 更新路徑設置
                if (_setting.ReloadBaseConnect())
                    (_imageFolderPath, _videoFolderPath) = ReloadBasePath();

                // 儲存檔案
                string ImageFilePath = await Shared.CopyFileStream(ImageFile, _imageFolderPath);
                string VideoFilePath = await Shared.CopyFileStream(VideoFile, _videoFolderPath);

                // 儲存紀錄到資料庫
                var newRecord = new TrackingRecord
                {
                    ImagePath = ImageFilePath,
                    VideoPath = VideoFilePath,
                    Timestamp = Shared.UnixTime(),
                    VideoId = Video_id,
                };
                _context.TrackingRecords.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "圖片、影片與資訊上傳成功!", newRecord.Id });
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"上傳紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得圖片
        [HttpGet]
        [Route("Record/Image/{Record_id}")]
        public async Task<IActionResult> GetImage(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤檢測結果圖片不存在!"); };
                return PhysicalFile(record.ImagePath, "image/jpeg");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得影片
        [HttpGet]
        [Route("Record/Video/{Record_id}")]
        public async Task<IActionResult> GetVideo(int Record_id)
        {
            try
            {
                var record = await _context.TrackingRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該追蹤檢測結果影片不存在!"); };

                // 回傳影片串流
                string filePath = $"{record.VideoPath}";
                return Shared.StreamVideo(filePath);
                // return PhysicalFile(filePath, "application/dash+xml");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 方法

        // 更新儲存路徑
        private (string, string) ReloadBasePath()
        {
            _setting.ReloadBaseConnect();
            string BasePATH = _setting.BasePath;
            var controllerName = GetType().Name.Replace("Controller", "");
            var imageFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Images");
            var videoFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Videos");
            Shared.EnsurePathExists(imageFolderPath);
            Shared.EnsurePathExists(videoFolderPath);
            return (imageFolderPath, videoFolderPath);
        }

        #endregion

    }
}
