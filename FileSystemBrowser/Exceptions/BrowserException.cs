namespace FileSystemBrowser.Exceptions
{
  /// <summary>
  /// Base class for exceptions thrown by the FileSystemBrowser.  These exceptions are handled by the
  /// FileSystemExceptionHandler and converted to HTTP responses with a status code and
  /// a JSON body containing a title and detail.  
  /// </summary>
  /// <param name="message"></param>
  /// <param name="statusCode"></param>
  public abstract class BrowserException(string message, int statusCode) : Exception(message)
  {
    public int StatusCode { get; } = statusCode;
  }
}
