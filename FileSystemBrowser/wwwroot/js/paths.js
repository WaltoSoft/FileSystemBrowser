// Helpers for client-relative paths ("docs/2024/report.txt").
// Paths use "/" separators, with no leading or trailing slash; "" is the root.

//Encodes each segment for use in a URL, keeping the slashes: "a b/c#1.txt" -> "a%20b/c%231.txt"
export function encodePath(path) {
  return path.split('/').filter(Boolean).map(encodeURIComponent).join('/');
}

// Joins a folder path and a name: ("docs", "a.txt") -> "docs/a.txt"; ("", "a.txt") -> "a.txt"
export function joinPath(folder, name) {
  return folder ? `${folder}/${name}` : name;
}

// The folder part of a path: "docs/2024/a.txt" -> "docs/2024"; "a.txt" -> ""
export function parentPath(path) {
  const slash = path.lastIndexOf('/');
  return slash < 0 ? '' : path.slice(0, slash);
}
