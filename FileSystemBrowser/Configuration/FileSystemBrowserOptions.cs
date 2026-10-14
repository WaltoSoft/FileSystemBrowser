namespace FileSystemBrowser.Configuration
{
  public class FileSystemBrowserOptions
  {
    public const string SectionName = "FileBrowser";
    public string HomeDirectory { get; set; } = "Homedir";
    public long MaxUploadBytes { get; set; } = 100 * 1024 * 1024; // 100 MB
    public int MaxSearchResults { get; set; } = 500;

  }
}
