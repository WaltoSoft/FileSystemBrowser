using FileSystemBrowser.Exceptions;

namespace FileSystemBrowser.FileSystem
{
  /// <summary>
  /// Resolves relative and full file paths within a specified home directory.
  /// Some additional path resolution features as well
  /// </summary>
  public class PathResolver
  {
    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    private readonly StringComparison _comparison;

    /// <summary>
    /// Constructor sets up the home directory as well as the provides a mechanism for comparing paths.
    /// On Windows, the default comparison is case-insensitive, while on Linux and MacOS it is case-sensitive.
    /// </summary>
    /// <param name="homeDirectory">The home directory for the path resolver.</param>
    /// <param name="comparison">Optional string comparison type for path comparisons.</param>
    public PathResolver(string homeDirectory, StringComparison? comparison = null)
    {
      ArgumentException.ThrowIfNullOrEmpty(homeDirectory, nameof(homeDirectory));

      RootPath = PathWithTrailingSeparator(Path.GetFullPath(homeDirectory));

      _comparison = comparison ?? (OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal);
    }

    // The home directory
    public string RootPath { get; }

    /// <summary>
    /// Resolves the full file path for the specified relative path within the home directory.
    /// Throws exceptions if the path is not relative or if it is outside the home directory.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <returns>The full path corresponding to the relative path.</returns>
    public string ResolveFullPath(string relativePath)
    {
      ArgumentNullException.ThrowIfNull(relativePath, nameof(relativePath));

      var relative = relativePath.Replace('\\', '/').TrimStart('/');

      // Relative Path must be relative
      if (Path.IsPathRooted(relative))
      {
        throw new PathNotRelativeException(relativePath);
      }

      // Validate the relative path for invalid segments and characters
      ValidateRelativePath(relative);

      // Build the full path by combining the root path and the relative path
      var combinedPath = Path.GetFullPath(Path.Combine(RootPath, relative));

      // Extra defense, technically the validation above should prevent this, but just in case.
      if (!IsWithinRootPath(combinedPath))
      {
        throw new PathOutsideRootException(relativePath, RootPath);
      }

      return combinedPath;
    }

    /// <summary>
    /// Resolves the relative path for the specified full path within the home directory.
    /// Throws an exception if the full path is outside the home directory.
    /// </summary>
    /// <param name="fullPath">The full path to resolve.</param>
    /// <returns>The relative path corresponding to the full path.</returns>
    public string ResolveRelativePath(string fullPath)
    {
      ArgumentNullException.ThrowIfNull(fullPath, nameof(fullPath));

      var normalized = Path.GetFullPath(fullPath);

      if (!IsWithinRootPath(normalized))
      {
        throw new PathOutsideRootException(fullPath, RootPath);
      }

      var relative = Path.GetRelativePath(RootPath, normalized).Replace('\\', '/');

      return relative == "." ? "" : relative;
    }

    /// <summary>
    /// Checks if two paths are the same, ignoring any trailing directory separators and using the configured string comparison.
    /// </summary>
    /// <param name="path1">The first path to compare.</param>
    /// <param name="path2">The second path to compare.</param>
    /// <returns>True if the paths are the same, false otherwise.</returns>
    public bool IsSamePath(string path1, string path2) =>
      string.Equals(Path.TrimEndingDirectorySeparator(path1), Path.TrimEndingDirectorySeparator(path2), _comparison);

    /// <summary>
    /// Checks if a path is the same as or inside another path, using the configured string comparison.
    /// </summary>
    /// <param name="fullPath">The path to check.</param>
    /// <param name="ancestorPath">The potential ancestor path.</param>
    /// <returns>True if the path is the same as or inside the ancestor path, false otherwise.</returns>
    public bool IsSamePathOrInside(string fullPath, string ancestorPath) =>
      PathWithTrailingSeparator(fullPath).StartsWith(PathWithTrailingSeparator(ancestorPath), _comparison);

    /// <summary>
    /// Checks if a full path is the same as the root path, using the configured string comparison.
    /// </summary>
    /// <param name="fullPath">The full path to check.</param>
    /// <returns>True if the full path is the same as the root path, false otherwise.</returns>
    public bool IsRoot(string fullPath) => IsSamePath(fullPath, RootPath);

    private static void ValidateRelativePath(string relativePath)
    {
      foreach (var segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
      {
        if (segment == "." || segment == "..")
        {
          throw new InvalidPathException($"Relative navigation segments ('.' or '..') are not allowed in the path '{relativePath}'.");
        }

        if (segment.IndexOfAny(InvalidNameChars) >= 0)
        {
          throw new InvalidPathException($"The path segment '{segment}' contains invalid characters.");
        }

        if (OperatingSystem.IsWindows() && (segment.EndsWith('.') || segment.EndsWith(' ')))
        {
          throw new InvalidPathException($"The path segment '{segment}' ends with a space or dot, which is not allowed on Windows.");
        }

      }
    }

    private bool IsWithinRootPath(string path) =>
      PathWithTrailingSeparator(path).StartsWith(RootPath, _comparison);

    private static string PathWithTrailingSeparator(string path) =>
      Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
  }
}
