# FileSystemBrowser

A small ASP.NET Core 8 web app for browsing and managing files inside a configured root directory.

It provides:
- A browser UI served from `wwwroot`
- A REST API for directory and file operations
- Path-safety checks to keep all operations inside a configured home directory
- ProblemDetails-based error responses

## Features

- List folder contents
- Search files/folders within a folder
- Create, rename, move, and delete folders
- Upload, download, rename, move, and delete files
- Upload size limit and search-result limit via configuration
- ETag and range support for file downloads

## Tech Stack

- .NET 8 (`net8.0`)
- ASP.NET Core (controllers + static files)
- Vanilla JavaScript frontend

## Project Structure

- `FileSystemBrowser/` - ASP.NET Core app
- `FileSystemBrowser/wwwroot/` - frontend assets (`index.html`, JS, CSS)
- `FileSystemBrowser/Controllers/FileSystemController.cs` - REST API endpoints
- `FileSystemBrowser/FileSystem/` - file system service and path resolver
- `FileSystemBrowser/Exceptions/` - custom exceptions and global exception handler
- `FileSystemBrowser/Homedir/` - default managed root folder (local/dev)

## Prerequisites

- .NET SDK 8.0+

Check your SDK:

```bash
dotnet --version
```

## Run Locally

From the app directory:

```bash
cd FileSystemBrowser
dotnet run
```

Default development URLs (from launch settings):
- `https://localhost:7146`
- `http://localhost:5120`

The UI is available at:
- `https://localhost:7146/`
- `http://localhost:5120/`

## Configuration

Configuration is in `FileSystemBrowser/appsettings.json`.

```json
"FileSystemBrowser": {
  "HomeDirectory": "Homedir",
  "MaxUploadBytes": 104857600,
  "MaxSearchResults": 500
}
```

### Settings

- `HomeDirectory`: Relative (or absolute) root folder for all operations.
  - Relative paths are resolved from the app content root.
- `MaxUploadBytes`: Maximum allowed upload payload size in bytes.
- `MaxSearchResults`: Cap on search results returned by the API.

## API Overview

Base routes:
- `GET /api/directories/{**path}`: List a directory
- `GET /api/directories/{**path}?q=query`: Search within a directory
- `PUT /api/directories/{**path}`: Create a directory
- `PATCH /api/directories/{**path}`: Rename/move a directory
- `DELETE /api/directories/{**path}`: Delete a directory

- `GET /api/files/{**path}`: Download a file
- `PUT /api/files/{**path}`: Upload a file
- `PATCH /api/files/{**path}?overwrite=true|false`: Rename/move a file
- `DELETE /api/files/{**path}`: Delete a file

### Move/Rename Request Body

Use exactly one of `name` or `relativePath`:

```json
{ "name": "new-name.txt" }
```

or

```json
{ "relativePath": "folder/subfolder/new-name.txt" }
```

## Error Handling

API errors are returned as RFC 7807 ProblemDetails JSON with appropriate status codes (for example 400, 403, 404, 409, 412, 413).

### HTTPS certificate warning on Linux

If HTTPS is untrusted locally, either trust the dev cert or use the HTTP URL for local development.

## Development Notes

- The app serves static UI files from `wwwroot`.
- `UseDefaultFiles()` is enabled so `/` maps to `index.html`.

## License

No license file is currently included in this repository.
