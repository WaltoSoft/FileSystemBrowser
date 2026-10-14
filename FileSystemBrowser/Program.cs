using FileBrowser.Web.Exceptions;
using FileSystemBrowser.Configuration;
using FileSystemBrowser.FileSystem;
using Microsoft.Extensions.Options;

namespace FileSystemBrowser
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.

      builder.Services.AddControllers();
      builder.Services.AddProblemDetails();
      builder.Services.AddExceptionHandler<FileSystemExceptionHandler>();

      builder.Services.AddOptions<FileSystemBrowserOptions>()
        .Bind(builder.Configuration.GetSection("FileSystemBrowser"))
        .PostConfigure<IHostEnvironment>((o, env) =>
          o.HomeDirectory = Path.GetFullPath(Path.Combine(env.ContentRootPath, o.HomeDirectory)))
      .Validate(o => Directory.Exists(o.HomeDirectory), "Home Directory does not exist.")
      .ValidateOnStart();

      builder.Services.AddSingleton(sp =>
          new PathResolver(sp.GetRequiredService<IOptions<FileSystemBrowserOptions>>().Value.HomeDirectory));
      builder.Services.AddSingleton<IFileSystemService, FileSystemService>();


      var app = builder.Build();

      // Configure the HTTP request pipeline.

      app.UseExceptionHandler();

      app.UseHttpsRedirection();

      app.UseDefaultFiles();

      app.UseStaticFiles();

      app.MapControllers();

      app.Run();
    }
  }
}
