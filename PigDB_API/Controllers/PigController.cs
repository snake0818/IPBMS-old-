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
    public class PigController : Controller
    {
        #region 建構

        private readonly PigDBContext _context; // 宣告 PigDB 物件變數
        private readonly SettingService _setting;
        private string _pigImageFolderPath;
        private string _pigDatasFolderPath;

        public PigController(PigDBContext database, SettingService settings)
        {
            _context = database;
            _setting = settings;
            (_pigImageFolderPath, _pigDatasFolderPath) = ReloadBasePath();
        }

        #endregion

        #region 新增豬隻紀錄
        [HttpPost]
        [Route("Record/{Record_id}")]
        public async Task<IActionResult> UploadRecords(IFormFile ImageFile, IFormFile JsonFile, int Record_id)
        {
            if (ImageFile == null || ImageFile.Length == 0)
                return BadRequest("沒有上傳圖片檔案!");
            if (JsonFile == null || JsonFile.Length == 0)
                return BadRequest("沒有上傳 JSON 檔案!");
            // 確認檔案類型是否正確
            if (!Path.GetExtension(JsonFile.FileName).Equals(".json", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("檔案必須為 JSON 格式!");

            try
            {
                // 儲存檔案
                string ImageFilePath = await Shared.CopyFileStream(ImageFile, _pigImageFolderPath, $"R{Record_id}N{ImageFile.FileName}");
                string JsonFilePath = await Shared.CopyFileStream(JsonFile, _pigDatasFolderPath, $"R{Record_id}N{JsonFile.FileName}");

                // 儲存紀錄到資料庫
                var newRecord = new Pig
                {
                    ImagePath = ImageFilePath,
                    DataPath = JsonFilePath,
                    RecordId = Record_id,
                };
                _context.Pigs.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "豬隻圖片與資料上傳成功!", newRecord.Id });
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"上傳紀錄時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得豬隻圖片
        [HttpGet]
        [Route("Image/{Pig_id}")]
        public async Task<IActionResult> GetImage(int Pig_id)
        {
            try
            {
                var record = await _context.Pigs
                    .FirstOrDefaultAsync(r => r.Id == Pig_id);
                if (record == null) { return NotFound("該估算豬隻結果圖片不存在!"); };
                return PhysicalFile(record.ImagePath, "image/jpeg");
            }
            // 捕捉例外並回傳 500 狀態碼
            catch (Exception ex) { return StatusCode(500, $"取得紀錄圖片時發生錯誤: {ex.Message}"); }
        }
        #endregion

        #region 取得豬隻資料
        [HttpGet]
        [Route("Data/{Pig_id}")]
        public async Task<IActionResult> GetData(int Pig_id)
        {
            try
            {
                var record = await _context.Pigs
                    .FirstOrDefaultAsync(r => r.Id == Pig_id);
                if (record == null) { return NotFound("該估算豬隻結果資訊不存在!"); };
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
            var annotationImageFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Images");
            var annotationDatasFolderPath = Path.Combine(BasePATH, "Sources", controllerName, "Data");
            Shared.EnsurePathExists(annotationImageFolderPath);
            Shared.EnsurePathExists(annotationDatasFolderPath);
            return (annotationImageFolderPath, annotationDatasFolderPath);
        }

        #endregion

    }
}
