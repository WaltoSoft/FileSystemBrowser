import { el } from '../dom.js';
import { uploadFile } from '../api.js';
import { joinPath } from '../paths.js';

// The createUploadButton function creates an upload button that allows users to select a file and upload it
// to a specified folder.It takes two parameters: getFolder, a function that returns the current folder path,
// and onUploaded, a callback function that is called after a successful upload.
// The function creates a hidden file input element and a visible button.When the button is clicked,
// it triggers the file input click event.When a file is selected, it attempts to upload the file to the specified folder.
// If the file already exists, it prompts the user for confirmation before overwriting it.If the upload is successful,
// it calls the onUploaded callback; otherwise, it displays an error message.
export function createUploadButton({ getFolder, onUploaded }) {
  const input = el('input', {
    type: 'file',
    hidden: true,
    onchange: async () => {
      const file = input.files[0];
      input.value = '';   // allow choosing the same file again
      if (!file) return;

      const path = joinPath(getFolder(), file.name);
      try {
        try {
          await uploadFile(path, file);
        } catch (err) {
          // 412: the file already exists.
          if (err.status !== 412 || !confirm(`"${file.name}" already exists. Replace it?`)) throw err;
          await uploadFile(path, file, { overwrite: true });
        }
        onUploaded();
      } catch (err) {
        if (err.status !== 412) alert(`Upload failed: ${err.message}`);
      }
    },
  });

  return el('span', {},
    el('button', { type: 'button', onclick: () => input.click() }, 'Upload file'),
    input);
}
