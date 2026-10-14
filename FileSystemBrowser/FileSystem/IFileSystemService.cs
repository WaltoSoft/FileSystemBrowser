using FileSystemBrowser.Models;

namespace FileSystemBrowser.FileSystem
{
  public interface IFileSystemService
  {
    // Folder operations
    FileSystemEntry CreateFolder(string relativePath);
    void DeleteFolder(string relativePath);
    FolderListing GetFolderListing(string relativePath);
    FileSystemEntry MoveFolder(string sourceRelativePath, string destinationRelativePath);
    FolderListing Search(string relativePath, string query, CancellationToken ct);

    // File Operations
    void DeleteFile(string relativePath);
    FileDownload GetFile(string relativePath);
    FileSystemEntry MoveFile(string sourceRelativePath, string destinationRelativePath, bool overwrite);
    Task<FileSystemEntry> WriteFileAsync(string relativePath, Stream content, bool overwrite = false, CancellationToken cancellationToken = default);

  }
}
