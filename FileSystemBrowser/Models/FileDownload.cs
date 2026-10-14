namespace FileSystemBrowser.Models
{
  public record FileDownload(
    string PhysicalPath, string FileName, long Length, DateTimeOffset LastModifiedUtc);
}
