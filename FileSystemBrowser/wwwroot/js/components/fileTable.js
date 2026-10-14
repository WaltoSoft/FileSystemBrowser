import { el } from '../dom.js';
import { deleteFile, deleteFolder, fileUrl, moveFolder, moveFile, renameFile, renameFolder } from '../api.js';
import { formatBytes, formatDate } from '../format.js';
import { parentPath } from '../paths.js';
import { folderLink } from './folderLink.js';

// The fileTable function generates an HTML table representing a list of file and folder entries.
// It takes an array of entries and an options object as parameters.
// The options object can specify whether to show the location column, which is used to display a column indicating the
// folder containing a search match, and what text to display when the folder is empty.
// The options object includes a callback function to call when changes occur that require the table to be refreshed
// (like renaming, moving, or deleting files / folders).
export function fileTable(entries, { showLocation = false, emptyText = 'This folder is empty.', onChanged } = {}) {
  if (entries.length === 0) {
    return el('p', { className: 'empty' }, emptyText);
  }

  return el('table', { className: 'file-table' },
    el('thead', {},
      el('tr', {},
        el('th', {}, 'Name'),
        showLocation ? el('th', {}, 'Location') : null,
        el('th', { className: 'size' }, 'Size'),
        el('th', {}, 'Modified'),
        el('th', {}, ''))),
    el('tbody', {}, entries.map(entry => entryRow(entry, showLocation, onChanged))));
}

// Creates a table row for a file or folder entry, including its name, size, modified date, and action links.
function entryRow(entry, showLocation, onChanged) {
  const name = entry.isDirectory
    ? folderLink(`📁 ${entry.name}`, entry.relativePath)
    : el('span', {}, `📄 ${entry.name}`);

  const actions = entry.isDirectory
    ? el('span', { className: 'actions' },
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleMoveFolder(entry, onChanged); } }, 'Move'),
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleRenameFolder(entry, onChanged); } }, 'Rename'),
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleRemoveFolder(entry.relativePath, onChanged); } }, 'Delete'))

    : el('span', { className: 'actions' },
        el('a', { href: fileUrl(entry.relativePath), download: entry.name }, 'Download'),
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleMoveFile(entry, onChanged); } }, 'Move'),
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleRenameFile(entry, onChanged); } }, 'Rename'),
        el('a', { href: '#', onclick: e => { e.preventDefault(); handleDeleteFile(entry.relativePath, onChanged); } }, 'Delete'));

  return el('tr', {},
    el('td', { className: 'name' }, name),
    showLocation ? locationCell(entry) : null,
    el('td', { className: 'size' }, formatBytes(entry.size)),
    el('td', {}, formatDate(entry.modifiedUtc)),
    el('td', {}, actions));
}

// Handles renaming a file by prompting the user for a new name and calling the renameFile API function.
async function handleRenameFile(entry, onChanged) {
  const newName = prompt('New file name:', entry.name)?.trim();
  if (!newName || newName === entry.name) return;

  try {
    await renameFile(entry.relativePath, newName);
    onChanged?.();
  } catch (err) {
    alert(`Rename failed: ${err.message}`);
  }
}

// Handles renaming a folder by prompting the user for a new name and calling the renameFolder API function.
async function handleRenameFolder(entry, onChanged) {
  const newName = prompt('New Folder Name:', entry.name)?.trim();
  if (!newName || newName === entry.name) return;

  try {
    await renameFolder(entry.relativePath, newName);
    onChanged?.();
  } catch (err) {
    alert(`Rename failed: ${err.message}`);
  }
}

// Handles deleting a file by calling the deleteFile API function and refreshing the table on success.
async function handleDeleteFile(path, onChanged) {
  try {
    await deleteFile(path);
    onChanged?.();
  } catch (err) {
    alert(`Delete failed: ${err.message}`);
  }
}

// Handles deleting a folder by calling the deleteFolder API function and refreshing the table on success.
async function handleRemoveFolder(path, onChanged) {
  try {
    await deleteFolder(path);
    onChanged?.();
  } catch (err) {
    alert(`Delete failed: ${err.message}`);
  }

}

// Handles moving a file by prompting the user for a new path and calling the moveFile API function.
async function handleMoveFile(entry, onChanged) {
  const newPath = prompt('New file path (enter full path):', entry.name)?.trim();
  if (!newPath || newPath === entry.name) return;

  try {
    await moveFile(entry.relativePath, newPath);
    onChanged?.();
  } catch (err) {
    alert(`Move failed: ${err.message}`);
  }
}

// Handles moving a folder by prompting the user for a new path and calling the moveFolder API function.
async function handleMoveFolder(entry, onChanged) {
  const newPath = prompt('New path:', entry.name)?.trim();
  if (! newPath || newPath === entry.name) return;

  try {
    await moveFolder(entry.relativePath, newPath);
    onChanged?.();
  } catch (err) {
    alert(`Move failed: ${err.message}`);
  }
}


// Creates a TD that represents the folder containing a search match, as a link to that folder.
function locationCell(entry) {
  const folder = parentPath(entry.relativePath);
  return el('td', { className: 'location' }, folderLink(folder || 'Home', folder));
}
