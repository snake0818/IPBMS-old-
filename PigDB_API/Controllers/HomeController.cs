using Microsoft.AspNetCore.Mvc;
using PigDB_API.Services;

namespace PigDB_API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class HomeController(SettingService settingServiceService) : Controller
    {
        private readonly SettingService _settingService = settingServiceService;

        [HttpGet]
        [Route("Status")]
        public IActionResult GetStatus()
        {
            _settingService.ReloadBaseConnect();
            _settingService.ReloadModelConnect();
            return Ok(new
            {
                FolderStatus = new
                {
                    Status = _settingService.IsFolderConnected,
                    Path = _settingService.BasePath,
                    Message = _settingService.IsFolderConnected ? "已連至共享資料夾" : "共享資料夾取得失敗"
                },
                ModelStatus = new
                {
                    Status = _settingService.IsModelConnected,
                    ModelURL = _settingService.ModelUrl,
                    Message = _settingService.IsModelConnected ? "已連至模型端" : "模型端連接失敗"
                }
            });
        }
    }
}
