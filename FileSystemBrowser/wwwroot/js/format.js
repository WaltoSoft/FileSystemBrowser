const units = ['B', 'KB', 'MB', 'GB', 'TB'];

// Finds the apporpriate unit for the given number of bytes and returns a string with one decimal place.
// Example: 482113 -> "470.8 KB". Returns "" for null (folders have no size).
export function formatBytes(bytes) {
  if (bytes == null) return '';

  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }
  return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}

// ISO UTC timestamp -> the user's local date and time.
export function formatDate(iso) {
  return new Date(iso).toLocaleString();
}

// Examples: (1, "folder") -> "1 folder"; (3, "folder") -> "3 folders"
export function pluralize(count, noun) {
  return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
