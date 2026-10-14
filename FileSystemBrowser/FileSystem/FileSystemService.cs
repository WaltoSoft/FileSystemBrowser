using FileSystemBrowser.Configuration;
using FileSystemBrowser.Exceptions;
using FileSystemBrowser.Models;
using Microsoft.Extensions.Options;
using System.IO.Enumeration;
using EnumFileSystemEntry = System.IO.Enumeration.FileSystemEntry;
using FileSystemEntry = FileSystemBrowser.Models.FileSystemEntry;

namespace FileSystemBrowser.FileSystem
{
  /// <summary>
  /// Provides file and folder operations such as listing, searching, creating, moving, and deleting files and
  /// directories within a configurable root.
  /// </summary>
  /// <param name="resolver">The resolver used to convert relative paths to absolute paths.</param>
  /// <param name="options">The options that configure the behavior of the file system service.</param>
  public class FileSystemService(PathResolver resolver, IOptions<FileSystemBrowserOptions> options, ILogger<FileSystemService> logger) : IFileSystemService
  {
    private const int CopyBufferSize = 81_920;

    private const FileAttributes SkippedAttributes =
      FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint;

    private static readonly EnumerationOptions ListingOptions = new ()
    {
      RecurseSubdirectories = false,
      IgnoreInaccessible = true,
      AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint
    };

    private static readonly EnumerationOptions SearchOptions = new()
    {
      RecurseSubdirectories = true,
      IgnoreInaccessible = true,
      AttributesToSkip = SkippedAttributes, 
    };


// Folder Operations-----------------------------------------------------------------------------------------------
    /// <summary>
    /// Creates a new folder at the specified relative path. Throws exceptions if a file or directory already exists at that path.
    /// </summary>
    /// <param name="relativePath">The relative path where the new folder will be created.</param>
    /// <returns>The created folder as a <see cref="FileSystemEntry"/>.</returns>
    public FileSystemEntry CreateFolder(string relativePath)
    {
      //Get the target full folder path
      var fullPath = resolver.ResolveFullPath(relativePath);

      //Make sure the target path doesn't already exist as a file.
      if (File.Exists(fullPath))
      {
        logger.LogError($"Create Folder failed.  A file already exists at the path: {fullPath}");
        throw new FileConflictException(relativePath);
      }

      //Make sure the target folder path doesn't already exist
      if (Directory.Exists(fullPath))
      {
        logger.LogError($"Create Folder failed.  A folder already exists at the path: {fullPath}");
        throw new Exceptions.DirectoryExistsException(relativePath);
      }

      Directory.CreateDirectory(fullPath);

      return new FileSystemEntry(
        Name: Path.GetFileName(fullPath),
        RelativePath: resolver.ResolveRelativePath(fullPath),
        IsDirectory: true,
        Size: null,
        ModifiedUtc: Directory.GetLastWriteTimeUtc(fullPath)
      );
    }

    /// <summary>
    /// Deletes the folder at the specified relative path
    /// </summary>
    /// <param name="relativePath">The relative path where the new folder will be delete.</param>
    public void DeleteFolder(string relativePath)
    {
      //Get the target full folder path
      var fullPath = resolver.ResolveFullPath(relativePath);

      //If the folder doesn't exist, return the appropriate exception
      if (!Directory.Exists(fullPath))
      {
        if(File.Exists(fullPath))
        {
          logger.LogError($"Delete Folder failed.  A file exists at the path: {fullPath}");
          throw new FileConflictException(relativePath);
        }
        else
        {
          logger.LogError($"Delete Folder failed.  The folder does not exist at the path: {fullPath}");
          throw new Exceptions.DirectoryNotFoundException(relativePath);
        }
      }

      Directory.Delete(fullPath, recursive: true);
    }

    /// <summary>
    /// Gets a listing of folders and files at the specified relative path.
    /// Throws exceptions if the path does not exist or is a file.
    /// </summary>
    /// <param name="relativePath">The relative path to get the folder listing from.</param>
    /// <returns>The folder listing as a <see cref="FolderListing"/>.</returns>
    public FolderListing GetFolderListing(string relativePath)
    {
      //Get the target full folder path
      var fullPath = resolver.ResolveFullPath(relativePath);
      var directoryInfo = new DirectoryInfo(fullPath);

      // If the folder doesn't exist, return the appropriate exception
      if (! directoryInfo.Exists)
      {
        if (File.Exists(fullPath))
        {
          logger.LogError($"Get Folder Listing failed.  A file exists at the path: {fullPath}");
          throw new FileConflictException(relativePath);
        }
        else
        {
          logger.LogError($"Get Folder Listing failed.  The folder does not exist at the path: {fullPath}");
          throw new Exceptions.DirectoryNotFoundException(relativePath);
        }
      }

      // Normalize the relative path and prepare a prefix for entries
      var normalizedRelativePath = resolver.ResolveRelativePath(directoryInfo.FullName);
      var relativePathPrefix = normalizedRelativePath.Length == 0 ? "" : normalizedRelativePath + "/";

      var entries = new List<FileSystemEntry>();
      var folderCount = 0;
      var fileCount = 0;
      var totalBytes = 0L;

      // Enumerate the file system entries in the directory, and build the listing of entries,
      // while counting folders, files, and total bytes.
      foreach (var info in directoryInfo.EnumerateFileSystemInfos("*", ListingOptions))
      {
        switch (info)
        {
          case DirectoryInfo dirInfo:
            folderCount++;
            entries.Add(new FileSystemEntry(
              dirInfo.Name,
              relativePathPrefix + dirInfo.Name,
              IsDirectory: true,
              Size: null,
              ModifiedUtc: dirInfo.LastWriteTimeUtc));
            break;

          case FileInfo file when !IsUploadingFile(file.Name):
            fileCount++;
            totalBytes += file.Length;
            entries.Add(new FileSystemEntry(
              file.Name,
              relativePathPrefix + file.Name,
              IsDirectory: false,
              Size: file.Length,
              ModifiedUtc: file.LastWriteTimeUtc));
            break;
        }
      }

      // Sort the entries so that directories come first, followed by files, both in alphabetical order.
      entries.Sort(CompareFileSystemEntries);

      return new FolderListing(
        normalizedRelativePath,
        entries,
        folderCount,
        fileCount,
        totalBytes
      );
    }

    /// <summary>
    /// Does one of two things:  Moves a folder to a new location, or renames a folder.
    /// Throws exceptions if the source folder does not exist, if the destination folder already exists,
    /// or if the destination is inside the source.
    /// </summary>
    /// <param name="sourceRelativePath">The relative path of the source folder to move.</param>
    /// <param name="destinationRelativePath">The relative path of the destination folder.</param>
    /// <returns>The moved folder as a <see cref="FileSystemEntry"/>.</returns>
    public FileSystemEntry MoveFolder(string sourceRelativePath, string destinationRelativePath)
    {
      // Get the full paths for the source and destination folders
      var sourceFullPath = resolver.ResolveFullPath(sourceRelativePath);
      var destinationFullPath = resolver.ResolveFullPath(destinationRelativePath);

      // Ensure that the source folder isn't the home directory, which cannot be moved
      if (resolver.IsRoot(sourceFullPath))
      {
        logger.LogError($"Move Folder failed.  The home directory cannot be moved: {sourceFullPath}");
        throw new DirectoryMoveConflictException("The home directory cannot be moved.");
      }

      // If the source and destinatoin paths are the same, there is nothing to do,
      // so return the source folder as a FileSystemEntry
      if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.Ordinal))
      {
        return ToFileSystemEntry(new DirectoryInfo(sourceFullPath));
      }

      // Determine if the move is a case-only rename (e.g., "Folder" to "folder")
      var caseOnlyRename = resolver.IsSamePath(sourceFullPath, destinationFullPath);

      // If it's not a case-only rename, perform additional checks to ensure the move is valid
      if (!caseOnlyRename)
      {
        if (resolver.IsSamePathOrInside(destinationFullPath, sourceFullPath))
        {
          throw new DirectoryMoveConflictException("A folder cannot be moved into itsself.");
        }

        if (Directory.Exists(destinationFullPath) || File.Exists(destinationFullPath))
        {
          throw new DirectoryMoveConflictException($"'{destinationFullPath}' already exists.");
        }
      }

      // Ensure that the parent folder of the destination exists before moving
      EnsureParentFolderExists(destinationFullPath);

      Directory.Move(sourceFullPath, destinationFullPath);

      return ToFileSystemEntry(new DirectoryInfo(destinationFullPath));
    }

    /// <summary>
    /// Searches for files and folders within the specified relative path that match the given query.
    /// </summary>
    /// <param name="relativePath">The relative path to get the folder listing from.</param>
    /// <param name="query">The query to search files for, allows wildcars</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The folder listing as a <see cref="FolderListing"/>.</returns>
    public FolderListing Search(string relativePath, string query, CancellationToken cancellationToken = default)
    {
      //Ensure a query was passed
      ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

      //Get the target full folder path
      var fullPath = resolver.ResolveFullPath(relativePath);

      //If the folder doesn't exist, return the appropriate exception
      if (!Directory.Exists(fullPath))
      {
        if (File.Exists(fullPath))
        {
          logger.LogError($"Search failed.  A file exists at the path: {fullPath}");
          throw new FileConflictException(fullPath);
        }
        else
        {
          logger.LogError($"Search failed.  The folder does not exist at the path: {fullPath}");
          throw new Exceptions.DirectoryNotFoundException(fullPath);
        }
      }

      // Ensure that the query is wrapped in wildcards if it doesn't already contain them
      var searchPattern = ToSearchPattern(query);
      var maxResults = options.Value.MaxSearchResults;

      // Predicate to determine if a file system entry matches the search pattern and is not an uploading file
      bool IsMatch(ref EnumFileSystemEntry entry)
      {
        cancellationToken.ThrowIfCancellationRequested();
        return FileSystemName.MatchesSimpleExpression(searchPattern, entry.FileName, ignoreCase: true)
          && (entry.IsDirectory || ! IsUploadingFile(entry.FileName.ToString()));
      }

      // Enumerate the file system entries that match the search pattern, up to maxResults + 1 to check for truncation
      var matches = new FileSystemEnumerable<FileSystemEntry>(
        fullPath,
        (ref EnumFileSystemEntry entry) => ToFileSystemEntry(ref entry),
        SearchOptions)
      {
        ShouldIncludePredicate = IsMatch,
      };

      // Take the first maxResults + 1 entries to determine if the results are truncated
      // if there are more than maxResults, it means that we are truncating the results
      var entries = matches.Take(maxResults + 1).ToList();
      var truncated = entries.Count > maxResults;

      // if we are truncating, remove the extra entry we included.
      if (truncated)
      {
        entries.RemoveAt(entries.Count - 1);
      }

      // Sort the entries so that directories come first, followed by files, both in alphabetical order.
      entries.Sort(CompareFileSystemEntries);

      return new FolderListing(
        resolver.ResolveRelativePath(fullPath),
        entries,
        FolderCount: entries.Count(e => e.IsDirectory),
        FileCount: entries.Count(e => !e.IsDirectory),
        TotalBytes: entries.Where(e => !e.IsDirectory).Sum(e => e.Size ?? 0),
        Query: query,
        Truncated: truncated
      );
    }

    // File Operations-------------------------------------------------------------------------------------------------
    /// <summary>
    /// Deletes the file at the specified relative path. Throws an exception if the file does not exist.
    /// </summary>
    /// <param name="relativePath">The relative path to the file to delete.</param>
    public void DeleteFile(string relativePath)
    {
      var fullPath = resolver.ResolveFullPath(relativePath);
      if (!File.Exists(fullPath))
      {
        logger.LogError($"Delete File failed.  The file does not exist at the path: {fullPath}");
        throw new Exceptions.FileNotFoundException(relativePath);
      }

      File.Delete(fullPath);
    }

    /// <summary>
    /// Gets the details of the file at the specified relative path in order to be downloaded.
    /// Throws an exception if the file does not exist.
    /// </summary>
    /// <param name="relativePath">The relative path to the file to get.</param>
    /// <returns>The file download information as as a <see cref="FileDownload"/>.</returns>
    public FileDownload GetFile(string relativePath)
    {
      // Resolve the full path of the file 
      var fullPath = resolver.ResolveFullPath(relativePath);
      var file = new FileInfo(fullPath);

      // If the file does not exist, throw the appropriate exception.
      if (!file.Exists)
      {
        if (Directory.Exists(fullPath))
        {
          logger.LogError($"Get File failed.  A directory exists at the path: {fullPath}");
          throw new DirectoryConflictException(relativePath);
        }
        else
        {
          logger.LogError($"Get File failed.  The file does not exist at the path: {fullPath}");
          throw new Exceptions.FileNotFoundException(relativePath);
        }
      }

      return new FileDownload
      (
        file.FullName,
        file.Name,
        file.Length,
        new DateTimeOffset(file.LastWriteTimeUtc)
      );
    }

    /// <summary>
    /// Does one of two things:  Moves a file to a new location, or renames a file.
    /// </summary>
    /// <param name="sourceRelativePath">The relative path to the source file.</param>
    /// <param name="destinationRelativePath">The relative path to the destination file.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
    /// <returns>The file new file information as a <see cref="FileSystemEntry"/>.</returns>
    public FileSystemEntry MoveFile(string sourceRelativePath, string destinationRelativePath, bool overwrite)
    {
      // Get the full paths for the source and destination files
      var sourceFullPath = resolver.ResolveFullPath(sourceRelativePath);
      var destinationFullPath = resolver.ResolveFullPath(destinationRelativePath);

      // Ensure that the source file exists, throwing the appropriate exception if it does not
      if (!File.Exists(sourceFullPath))
      {
        if (Directory.Exists(destinationFullPath))
        {
          logger.LogError($"Move File failed.  A directory exists at the source path: {sourceFullPath}");
          throw new DirectoryConflictException(sourceRelativePath);
        }
        else
        {
          logger.LogError($"Move File failed.  The source file does not exist at the path: {sourceFullPath}");
          throw new Exceptions.FileNotFoundException(sourceRelativePath);
        }
      }

      // Ensure the destination file path is valid, throwing exceptions if it is not
      ValidateFileTarget(destinationFullPath);

      // if the source and destination paths are the same, there is nothing to do, so return the source file as a FileSystemEntry
      if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.Ordinal))
      {
        // There is nothing to do.
        return ToFileSystemEntry(new FileInfo(sourceFullPath));
      }

      // Determine if the move is a case-only rename (e.g., "File.txt" to "file.txt")
      var caseOnlyRename = resolver.IsSamePath(sourceFullPath, destinationFullPath);

      // If it's not a case-only rename, check if the destination file already exists and throw an exception if it does
      if (!overwrite && !caseOnlyRename && File.Exists(destinationFullPath))
      {
        throw new FileExistsException(destinationFullPath);
      }

      try
      {
        File.Move(sourceFullPath, destinationFullPath, overwrite && !caseOnlyRename);
      }
      catch (IOException) when (!overwrite && !caseOnlyRename && File.Exists(destinationFullPath))
      {
        throw new FileExistsException(destinationFullPath);
      }

      return ToFileSystemEntry(new FileInfo(destinationFullPath));
    }

    /// <summary>
    /// Writes the provided content stream to a file at the specified relative path.
    /// </summary>
    /// <param name="relativePath">The relative path to the file.</param>
    /// <param name="content">The content stream to write to the file.</param>
    /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The file information as a <see cref="FileSystemEntry"/>.</returns>
    public async Task<FileSystemEntry> WriteFileAsync(string relativePath, Stream content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
      // Resolve the full path of the file and extract the file name and directory
      var fullPath = resolver.ResolveFullPath(relativePath);
      var fileName = Path.GetFileName(fullPath);
      var directory = Path.GetDirectoryName(fullPath);

      if (string.IsNullOrWhiteSpace(fileName))
      {
        throw new InvalidPathException("A file name is required.");
      }

      if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
      {
        throw new InvalidPathException($"The file name '{relativePath}' contains invalid characters.");
      }

      if (Directory.Exists(fullPath))
      {
        throw new DirectoryConflictException(fullPath);
      }

      if (!Directory.Exists(directory))
      {
        throw new Exceptions.DirectoryNotFoundException(directory);
      }

      if (!overwrite && File.Exists(fullPath))
      {
        throw new FilePreconditionFailedException(relativePath);
      }

      // Get a temporary file path for writing the content
      // This ensures that if the write operation fails, we don't end up with a partially written file
      // and that readers don't see a partially written file.
      var tempFilePath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

      try
      {
        // Write the content to the temporary file 
        await using (var outputStream = new FileStream(tempFilePath, new FileStreamOptions
        {
          Mode = FileMode.CreateNew,
          Access = FileAccess.Write,
          Share = FileShare.None,
          BufferSize = CopyBufferSize,
          Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        }))
        {
          await content.CopyToAsync(outputStream, cancellationToken);
        }

        // Move the temporary file to the final destination
        File.Move(tempFilePath, fullPath, overwrite);
      }
      finally
      {
        // Clean up the temporary file if it still exists
        if (File.Exists(tempFilePath))
        {
          File.Delete(tempFilePath);
        }
      }

      return ToFileSystemEntry(new FileInfo(fullPath));
    }


// Private Helper Methods---------------------------------------------------------------------------------------------
    private void ValidateFileTarget(string fullPath)
    {
      if (string.IsNullOrEmpty(Path.GetFileName(fullPath)))
      {
        throw new InvalidPathException("A file name is required.");
      }

      if (Directory.Exists(fullPath))
      {
        throw new DirectoryConflictException(fullPath); 
      }

      var parentDirectory = Path.GetDirectoryName(fullPath);
      if (!Directory.Exists(parentDirectory))
      {
        throw new Exceptions.DirectoryNotFoundException(parentDirectory);
      }

    }

    private void EnsureParentFolderExists(string fullPath)
    {
      var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(fullPath))!;

      if(!Directory.Exists(parent))
      {
        throw new Exceptions.DirectoryNotFoundException(resolver.ResolveRelativePath(parent));
      }

    }

    private FileSystemEntry ToFileSystemEntry(DirectoryInfo directoryInfo)
    {
      return new FileSystemEntry(
        Name: directoryInfo.Name,
        RelativePath: resolver.ResolveRelativePath(directoryInfo.FullName),
        IsDirectory: true,
        Size: null,
        ModifiedUtc: directoryInfo.LastWriteTimeUtc
      );
    }

    private FileSystemEntry ToFileSystemEntry(FileInfo fileInfo)
    {
      return new FileSystemEntry(
        Name: fileInfo.Name,
        RelativePath: resolver.ResolveRelativePath(fileInfo.FullName),
        IsDirectory: false,
        Size: fileInfo.Length,
        ModifiedUtc: fileInfo.LastWriteTimeUtc
      );
    }

    private FileSystemEntry ToFileSystemEntry(ref EnumFileSystemEntry entry)
    {
      return new FileSystemEntry(
        Name: entry.FileName.ToString(),
        RelativePath: resolver.ResolveRelativePath(entry.ToFullPath()),
        IsDirectory: entry.IsDirectory,
        Size: entry.IsDirectory ? null : entry.Length,
        ModifiedUtc: entry.LastWriteTimeUtc.UtcDateTime
      );
    }

    private static int CompareFileSystemEntries(FileSystemEntry x, FileSystemEntry y) =>
      x.IsDirectory != y.IsDirectory
        ? (x.IsDirectory ? -1 : 1)
        : StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);

    private static bool IsUploadingFile(string name) =>
      name.StartsWith(".") && name.EndsWith(".tmp", StringComparison.Ordinal);

    private static string ToSearchPattern(string query) =>
      query.IndexOfAny(['*', '?']) >= 0 ? query : $"*{query}*";
  }
}
