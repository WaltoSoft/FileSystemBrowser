import { el } from '../dom.js';
import { formatBytes, pluralize } from '../format.js';

// The statsBar function creates a paragraph element that displays the number of folders, files, and total bytes in a human-readable format.
export function statsBar({ folderCount, fileCount, totalBytes }) {
  return el('p', { className: 'stats' },
    `${pluralize(folderCount, 'folder')} · ${pluralize(fileCount, 'file')} · ${formatBytes(totalBytes)}`);
}
