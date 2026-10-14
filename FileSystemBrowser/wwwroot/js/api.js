import { encodePath } from './paths.js';

// ---- Errors ----------------------------------------------------------------------------------------

// An error returned by the API, with a status code and an optional problem details object.
export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Request failed (${status})`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

// ---- URL Builders ----------------------------------------------------------------------------------
// Returns the URL of a directory's listing or search endpoint.
function directoryUrl(path) {
  return `/api/directories/${encodePath(path)}`;
}

// Returns the URL of a file's endpoint, optionally with an overwrite query parameter.
export function fileUrl(path, { overwrite = false } = {}) {
  return `/api/files/${encodePath(path)}${overwrite ? '?overwrite=true' : ''}`;
}

// ---- Request helpers -------------------------------------------------------------------------------
async function readResponse(response) {
  const payload = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) throw new ApiError(response.status, payload);
  return payload;
}

async function getJson(url, signal) {
  return readResponse(await fetch(url, { signal, headers: { Accept: 'application/json' } }));
}

async function sendJson(method, url, body) {
  return readResponse(await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }));
}

// ---- API Operations---------------------------------------------------------------------------------

// Retrieves a directory's listing or search results. The optional `q` parameter is a search query.
export function getDirectory(path, { q = '', signal } = {}) {
  const query = q ? `?q=${encodeURIComponent(q)}` : '';
  return getJson(directoryUrl(path) + query, signal);
}

// Uploads a file to the specified path. The `overwrite` option determines whether to overwrite an existing file.
// The `onProgress` callback is called with the upload progress(0 to 1).
// The `signal` option allows aborting the upload.
export function uploadFile(path, file, { overwrite = false, onProgress, signal } = {}) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', fileUrl(path));
    xhr.responseType = 'json';
    if (!overwrite) xhr.setRequestHeader('If-None-Match', '*');

    xhr.upload.onprogress = e => {
      if (e.lengthComputable) onProgress?.(e.loaded / e.total);
    };
    xhr.onload = () =>
      xhr.status >= 200 && xhr.status < 300
        ? resolve(xhr.response)
        : reject(new ApiError(xhr.status, xhr.response));
    xhr.onerror = () => reject(new ApiError(0, { title: 'Network error' }));
    xhr.onabort = () => reject(new DOMException('Upload cancelled', 'AbortError'));
    signal?.addEventListener('abort', () => xhr.abort(), { once: true });

    xhr.send(file);
  });
}

// Moves a file or folder to a new path. The `overwrite` option determines whether to overwrite an existing file or folder.
export function moveFile(path, newPath, { overwrite = false } = {}) {
  return sendJson('PATCH', fileUrl(path, { overwrite }), { relativePath: newPath });
}

// Moves a folder to a new path. The `overwrite` option determines whether to overwrite an existing folder.
export function moveFolder(path, newPath, { overwrite = false } = {}) {
  return sendJson('PATCH', directoryUrl(path, { overwrite }), { relativePath: newPath });
}

// Renames a file or folder. The `overwrite` option determines whether to overwrite an existing file or folder with the new name.
export function renameFile(path, newName, { overwrite = false } = {}) {
  return sendJson('PATCH', fileUrl(path, { overwrite }), { name: newName });
}

// Renames a folder. The `overwrite` option determines whether to overwrite an existing folder with the new name.
export function renameFolder(path, newName, { overwrite = false } = {}) {
  return sendJson('PATCH', directoryUrl(path, { overwrite }), { name: newName });
}

// Deletes a file at the specified path.
export function deleteFile(path) {
  return sendJson('DELETE', fileUrl(path));
}

// Creates a new folder at the specified path.
export function newFolder(path) {
  return sendJson('PUT', directoryUrl(path));
}

// Deletes a folder at the specified path.
export function deleteFolder(path) {
  return sendJson('DELETE', directoryUrl(path));
}
