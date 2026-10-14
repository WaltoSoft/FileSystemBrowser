namespace FileSystemBrowser.Exceptions
{
  public class PathNotRelativeException(string? path)
      : BrowserException($"The path '{path}' is not relative.", StatusCodes.Status403Forbidden);

  public class PathOutsideRootException(string? path, string rootPath)
      : BrowserException($"The path '{path}' is outside the root directory '{rootPath}'.", StatusCodes.Status403Forbidden);

  public class InvalidPathException(string message)
      : BrowserException(message, StatusCodes.Status400BadRequest);

  public class DirectoryNotFoundException(string? path)
      : BrowserException($"The directory '{path}' was not found.", StatusCodes.Status404NotFound);

  public class DirectoryConflictException(string? path)
      : BrowserException($"The directory '{path}' already exists as a directory.", StatusCodes.Status409Conflict);

  public class DirectoryMoveConflictException(string message)
      : BrowserException(message, StatusCodes.Status409Conflict); 

  public class DirectoryExistsException(string? path)
      : BrowserException($"The directory '{path}' already exists.", StatusCodes.Status409Conflict);

  public class FileConflictException(string? path)
      : BrowserException($"The file '{path}' already exists as a file.", StatusCodes.Status409Conflict);

  public class FileExistsException(string? path)
      : BrowserException($"The file '{path}' already exists.", StatusCodes.Status409Conflict);

  public class FilePreconditionFailedException(string? path)
      : BrowserException($"The file '{path}' already exists.", StatusCodes.Status412PreconditionFailed);

  public class FileNotFoundException(string? path)
      : BrowserException($"The file '{path}' was not found.", StatusCodes.Status404NotFound);


}
