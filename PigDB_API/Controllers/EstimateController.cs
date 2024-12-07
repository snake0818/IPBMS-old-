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
    public class EstimateController : Controller
    {
        #region 建構

        private readonly PigDBContext _context; // 宣告 PigDB 物件變數
        private readonly SettingService _setting;
        private string _estimateImageFolderPath;
        private string _estimateDataFolderPath;
        private string ModelUrl;

        public EstimateController(PigDBContext database, SettingService settings)
        {
            _context = database;
            _setting = settings;
            (_estimateImageFolderPath, _estimateDataFolderPath) = ReloadBasePath();
            ModelUrl = _setting.ModelUrl;
        }

        #endregion

        #region 執行估測服務
        [HttpGet]
        [Route("{Image_id}")]
        public async Task<IActionResult> GetEstimateService(int Image_id)
        {
            if (_setting.ReloadModelConnect()) ModelUrl = _setting.ModelUrl;

            if (ModelUrl == "|") return BadRequest(new { error = "模型端連接失敗!" });

            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync($"{ModelUrl}estimate/{Image_id}");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return Ok(result);
            }
            // 捕獲異常並返回錯誤信息
            catch (HttpRequestException httpEx)
            {
                return StatusCode(501, $"模型服務請求錯誤: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"執行估測服務時發生錯誤: {ex.Message}");
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
                var records = await _context.EstimateRecords
                    .Select(r => new { r.Id, r.ImageId, r.Timestamp, })
                    .ToListAsync();
                if (records == null || records.Count == 0) { return NotFound("尚未有任何估算紀錄資訊!"); };
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
                var record = await _context.EstimateRecords
                    .Select(r => new
                    {
                        r.Id,
                        r.ImageId,
                        PigRecords = _context.Pigs
                            .Where(r => r.RecordId == Record_id)
                            .Select(r => r.Id)
                            .ToList(),
                        r.Timestamp,
                    })
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該估測結果紀錄不存在!"); };
                return Ok(record);
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 新增估測紀錄
        [HttpPost]
        [Route("Record/{Image_id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRecords(int Image_id, IFormFile JsonFile, IFormFile ImageFile, IFormFile DepthMapFile)
        {
            if (ImageFile == null || ImageFile.Length == 0)
                return BadRequest("沒有上傳圖片檔案!");
            if (DepthMapFile == null || DepthMapFile.Length == 0)
                return BadRequest("沒有上傳深度圖檔案!");
            if (JsonFile == null || JsonFile.Length == 0)
                return BadRequest("沒有上傳 JSON 檔案!");

            // 確認檔案類型是否正確
            if (!Path.GetExtension(JsonFile.FileName).Equals(".json", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("檔案必須為 JSON 格式!");

            try
            {
                // 更新路徑設置
                if (_setting.ReloadBaseConnect())
                    (_estimateImageFolderPath, _estimateDataFolderPath) = ReloadBasePath();

                // 儲存檔案
                string ImageFilePath = await Shared.CopyFileStream(ImageFile, _estimateImageFolderPath);
                string DepthMapFilePath = await Shared.CopyFileStream(DepthMapFile, _estimateImageFolderPath);
                string JsonFilePath = await Shared.CopyFileStream(JsonFile, _estimateDataFolderPath);

                // 儲存紀錄到資料庫
                var newRecord = new EstimateRecord
                {
                    DataPath = JsonFilePath,
                    ImagePath = ImageFilePath,
                    DepthMapPath = DepthMapFilePath,
                    Timestamp = Shared.UnixTime(),
                    ImageId = Image_id,
                };
                _context.EstimateRecords.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "估測圖片與資料上傳成功!", newRecord.Id });
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"上傳紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得紀錄圖片
        [HttpGet]
        [Route("Image/{Record_id}")]
        public async Task<IActionResult> GetRecordImage(int Record_id)
        {
            try
            {
                var record = await _context.EstimateRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該估測紀錄結果不存在!"); };
                return PhysicalFile(record.ImagePath, "image/jpeg");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得紀錄深度圖
        [HttpGet]
        [Route("DepthMap/{Record_id}")]
        public async Task<IActionResult> GetRecordDepthMap(int Record_id)
        {
            try
            {
                var record = await _context.EstimateRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該估測紀錄結果不存在!"); };
                return PhysicalFile(record.DepthMapPath, "image/jpeg");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄深度圖時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得紀錄資料
        [HttpGet]
        [Route("Data/{Record_id}")]
        public async Task<IActionResult> GetRecordData(int Record_id)
        {
            try
            {
                var record = await _context.EstimateRecords
                    .FirstOrDefaultAsync(r => r.Id == Record_id);
                if (record == null) { return NotFound("該估測紀錄結果資訊不存在!"); };
                return PhysicalFile(record.DataPath, "application/json");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄資料時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 方法

        // 更新儲存路徑
        private (string, string) ReloadBasePath()
        {
            string BasePATH = _setting.BasePath;
            var controllerName = GetType().Name.Replace("Controller", "");
            var estimateImageFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Images");
            var estimateDataFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Data");
            Shared.EnsurePathExists(estimateImageFolderPath);
            Shared.EnsurePathExists(estimateDataFolderPath);
            return (estimateImageFolderPath, estimateDataFolderPath);
        }

        #endregion

    }
}