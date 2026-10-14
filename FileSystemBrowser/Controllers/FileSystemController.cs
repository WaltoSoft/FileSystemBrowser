using FileSystemBrowser.Configuration;
using FileSystemBrowser.FileSystem;
using FileSystemBrowser.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace FileSystemBrowser
{

  /// <summary>
  /// REST API controller for file system operations.
  /// The controller is configured in Program.cs to use the FileSystemExceptionHandler to convert exceptions to HTTP responses.
  /// </summary>
  /// <param name="fs">The IFileSystemService implementation that handles file system operations.</param>
  /// <param name="options">The options for configuring the file system browser.</param>
  public class FileSystemController(IFileSystemService fs, IOptions<FileSystemBrowserOptions> options) : Controller
  {
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    // Folder operations -----------------------------------------------------------------------------------------------------

    /// <summary>
    /// GET operation to retrieve the contents of a directory or search for files and folders within a directory.
    /// Used to do one of two things:
    /// 1. Get the contents of a directory (if the query parameter 'q' is not provided).
    /// 2. Search for files and folders within a directory (if the query parameter 'q' is provided).
    /// </summary>
    /// <param name="path">The path of the directory to retrieve or search within.</param>
    /// <param name="q">The optional search query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The folder listing or search results as a <see cref="FolderListing"/>.</returns>
    [HttpGet("api/directories/{**path}")]
    public FolderListing GetFolder(string? path, [FromQuery] string? q, CancellationToken cancellationToken)
    {
      // If the query parameter 'q' is null or whitespace, return the folder listing for the specified path.
      // Otherwise, perform a search within the specified path using the provided query.
      return string.IsNullOrWhiteSpace(q)
        ? fs.GetFolderListing(path ?? string.Empty)
        : fs.Search(path ?? string.Empty, q.Trim(), cancellationToken);
    }

    /// <summary>
    /// PUT operation to create a new directory at the specified relative path.
    /// A PUT operation is used here because the resource (directory) is being created at a specific resource location,
    /// and the client is providing the desired path for the new directory.  
    /// </summary>
    /// <param name="path">The relative path where the new directory will be created.</param>
    /// <returns>The created directory as a <see cref="FileSystemEntry"/>.</returns>
    [HttpPut("api/directories/{**path}")]
    public FileSystemEntry CreateFolder(string path)
    {
      return fs.CreateFolder(path);
    }

    /// <summary>
    /// PATCH operation to move or rename a folder
    /// </summary>
    /// <param name="path">The relative path of the folder to move or rename.</param>
    /// <param name="moveOperation">The move operation details, including the new name or relative path.</param>
    /// <returns>The updated folder as a <see cref="FileSystemEntry"/>.</returns>
    [HttpPatch("api/directories/{**path}")]
    public FileSystemEntry MoveFolder(string path, [FromBody] MoveOperation moveOperation)
    {
      // MoveOperation.Validate ensures that either Name or RelativePath is provided, but not both.
      var destination = moveOperation.Name is not null
          ? WithNewName(path, moveOperation.Name)
          : moveOperation.RelativePath!;

      return fs.MoveFolder(path, destination);
    }

    /// <summary>
    /// DELETE operation to delete a directory at the specified relative path.
    /// </summary>
    /// <param name="path">The relative path of the directory to delete.</param>
    /// <returns>No content if the deletion is successful.</returns>
    [HttpDelete("api/directories/{**path}")]
    public IActionResult DeleteDirectory(string path)
    {
      fs.DeleteFolder(path);
      return NoContent();
    }

    // File Operations ----------------------------------------------------------------------------------------------------------
    /// <summary>
    /// GET operation to retrieve a file contents at the specified relative path.
    /// </summary>
    /// <param name="path">The relative path of the file to retrieve.</param>
    /// <returns>The file as a <see cref="FileSystemEntry"/>.</returns>
    [HttpGet("api/files/{**path}")]
    public IActionResult GetFile(string path)
    {
      var fileDownload = fs.GetFile(path);

      if (! ContentTypes.TryGetContentType(fileDownload.FileName, out var contentType))
      {
        contentType = "application/octet-stream";
      }

      // Don't the let the browser guess the content type.  A
      // An uploaded file must not be sniffed as HTML.
      Response.Headers.XContentTypeOptions = "nosniff";

      // Changes whenever the file's content plausibly changes
      // Enables 304 Not Modified.
      var etag = new EntityTagHeaderValue($"\"{fileDownload.LastModifiedUtc.UtcTicks:x}-{fileDownload.Length:x}\"");

      return PhysicalFile(
        fileDownload.PhysicalPath,
        contentType,
        fileDownload.FileName,
        fileDownload.LastModifiedUtc,
        etag,
        enableRangeProcessing: true
       );
    }

    /// <summary>
    /// PUT operation to upload a file to the specified relative path.
    /// </summary>
    /// <param name="path">The relative path of the file to upload.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The uploaded file as a <see cref="FileSystemEntry"/>.</returns>
    [HttpPut("api/files/{**path}")]
    public async Task<IActionResult> UploadFile(string path, CancellationToken cancellationToken)
    {
      var maxBytes = options.Value.MaxUploadBytes;

      if (Request.ContentLength.HasValue && Request.ContentLength.Value > maxBytes)
      {
        return Problem(statusCode: StatusCodes.Status413PayloadTooLarge, detail: $"File size exceeds the maximum allowed size of {maxBytes} bytes.");
      }

      var overwrite = Request.Headers.IfNoneMatch.ToString() != "*";
      var fileSystemEntry = await fs.WriteFileAsync(path, Request.Body, overwrite, cancellationToken);

      return Created(FileUrl(fileSystemEntry.RelativePath), fileSystemEntry);
    }

    /// <summary>
    /// DELETE operation to delete a file at the specified relative path.
    /// </summary>
    /// <param name="path">The relative path of the file to delete.</param>
    /// <returns>No content if the deletion is successful.</returns>

    [HttpDelete("api/files/{**path}")]
    public IActionResult DeleteFile(string path)
    {
      fs.DeleteFile(path);
      return NoContent();
    }

    /// <summary>
    /// Does one of two things:
    /// 1. Renames a file if the MoveOperation.Name is provided.
    /// 2. Moves a file to a new location if the MoveOperation.RelativePath is provided.
    /// </summary>
    /// <param name="path">The relative path of the file to move or rename.</param>
    /// <param name="moveOperation">The move operation details.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    /// <returns>The moved or renamed file as a <see cref="FileSystemEntry"/>.</returns>
    [HttpPatch("api/files/{**path}")]
    public IActionResult MoveFile(string path, [FromBody] MoveOperation moveOperation, [FromQuery] bool overwrite)
    {
      // MoveOperation.Validate ensures that either Name or RelativePath is provided, but not both.
      var destination = moveOperation.Name is not null
          ? WithNewName(path, moveOperation.Name)
          : moveOperation.RelativePath!;          

      return Ok(fs.MoveFile(path, destination, overwrite));
    }

    private static string FileUrl(string relativePath) =>
      "/api/files/" + string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));

    private static string WithNewName(string relativePath, string newName)
    {
      var trimmed = relativePath.TrimEnd('/');
      var slash = trimmed.LastIndexOf('/');
      return slash < 0 ? newName : $"{trimmed[..slash]}/{newName}";
    }
  }
}

