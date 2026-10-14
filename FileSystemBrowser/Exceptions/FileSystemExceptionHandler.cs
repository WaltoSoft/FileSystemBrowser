using FileSystemBrowser.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace FileBrowser.Web.Exceptions
{
  /// <summary>
  /// Global exception handler.  Exceptions become HTTP responses with a status code and a
  /// JSON body containing a title and detail.  Controllers don't need to catch exceptions; they are handled here.
  /// Response use the standard ProblemDetails format
  /// </summary>
  /// <param name="problemDetails">The ProblemDetails service injected in Program.cs using .AddProblemDetails()</param>
  public class FileSystemExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
  {
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken ct)
    {
      var (status, detail) = ex switch
      {
        BrowserException fb => (fb.StatusCode, fb.Message),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, null),
        BadHttpRequestException bad => (bad.StatusCode, null),
        IOException => (StatusCodes.Status409Conflict, null),
        _ => (0, null),
      };
      if (status == 0) return false;   

      context.Response.StatusCode = status;
      return await problemDetails.TryWriteAsync(new ProblemDetailsContext
      {
        HttpContext = context,
        ProblemDetails = { Status = status, Detail = detail },
      });
    }
  }
}
