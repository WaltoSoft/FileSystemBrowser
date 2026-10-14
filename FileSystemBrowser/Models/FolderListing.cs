namespace FileSystemBrowser.Models
{
  public record FolderListing(
    string RelativePath,
    List<FileSystemEntry> Entries,
    int FolderCount,
    int FileCount,
    long TotalBytes,
    string? Query = null,
    bool Truncated = false);
}
