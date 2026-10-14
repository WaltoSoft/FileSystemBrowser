import { newFolder } from '../api.js';
import { el } from '../dom.js';

// Handles the creation of a new folder by prompting the user for a folder name and calling the newFolder API function.
async function handleNewFolderClick(getPath, onChange) {
  const newFolderName = prompt("New Folder Name");
  if (!newFolderName) return;
  const newFolderPath = `${getPath()}/${newFolderName}`;

  try {
    await newFolder(newFolderPath);
    onChange?.();
  } catch (err) {
    alert(`Create folder failed: ${err.message}`);
  }
}

// Creates a button element that, when clicked, prompts the user to create a new folder in the current directory.
export function createNewFolderButton({ getFolder, onCreated }) {
  return el('button', { type: 'button', onclick: () => handleNewFolderClick(getFolder, onCreated) }, 'New folder');
}
