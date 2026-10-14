namespace FileSystemBrowser.Models
{
  public record FileSystemEntry(
    string Name,
    string RelativePath,
    bool IsDirectory,
    long? Size,
    DateTime ModifiedUtc
  );
}
