namespace PigDB_API.Services
{
  public class SettingService
  {
    public string BasePath { get; private set; }
    public string ModelUrl { get; private set; }
    public bool IsFolderConnected { get; private set; }
    public bool IsModelConnected { get; private set; }
    private readonly IConfiguration _configuration;

    public SettingService(IConfiguration configuration)
    {
      _configuration = configuration;
      BasePath = LoadBasePath();
      ModelUrl = LoadModelUrl();
      Log();
    }
    // 載入共享資料夾路徑及模型端連接路徑
    private string LoadBasePath()
    {
      var appSettingsBasePath = _configuration["BasePath"];
      (IsFolderConnected, string path) = CheckPath(appSettingsBasePath);
      return path;
    }
    private string LoadModelUrl()
    {
      var appSettingsModelURL = _configuration["ModelUrl"];
      (IsModelConnected, string url) = CheckConnection(appSettingsModelURL).GetAwaiter().GetResult();
      return url;
    }

    // 檢查共享資料夾狀態及路徑
    private static (bool, string) CheckPath(string? PATH)
    {
      bool status = false;
      string path = Path.Combine(AppContext.BaseDirectory, "PigDB");

      if (!string.IsNullOrEmpty(PATH) && Directory.Exists(PATH))
      {
        status = true;
        path = PATH;
      }

      Directory.CreateDirectory(path);
      return (status, path);
    }

    // 檢查模型端連接狀態及路徑
    private static async Task<(bool, string)> CheckConnection(string? URL)
    {
      bool status = false;
      string url = "|";

      if (!string.IsNullOrEmpty(URL))
      {
        HttpClient httpClient = new();
        try
        {
          HttpResponseMessage response = await httpClient.GetAsync(URL);
          status = true;
          url = URL;
        }
        catch (HttpRequestException ex) { Console.WriteLine($"連線錯誤：{ex.Message}"); }
      }

      return (status, url);
    }

    // 添加日誌以確認
    private void Log()
    {
      Console.WriteLine($"FolderConnected: {IsFolderConnected}, BasePath: {BasePath}");
      Console.WriteLine($"ModelConnected:  {IsModelConnected}, ModelUrl: {ModelUrl}");
    }

    // 重新檢驗連接並回傳是否發生改變
    public bool ReloadBaseConnect()
    {
      var before = IsFolderConnected;
      BasePath = LoadBasePath();
      Log();
      return before == IsFolderConnected;
    }
    public bool ReloadModelConnect()
    {
      var before = IsModelConnected;
      ModelUrl = LoadModelUrl();
      Log();
      return before == IsModelConnected;
    }

  }
}