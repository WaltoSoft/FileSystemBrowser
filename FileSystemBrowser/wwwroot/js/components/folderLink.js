import { el } from '../dom.js';
import { browseUrl, navigate } from '../router.js';

const isPlainClick = e =>
  e.button === 0 && !e.ctrlKey && !e.metaKey && !e.shiftKey && !e.altKey;

// The folderLink function creates an anchor element (<a>) that links to a folder path.
export function folderLink(label, path) {
  return el('a', {
    href: browseUrl({ path }),
    onclick: e => {
      if (!isPlainClick(e)) return;
      e.preventDefault();
      navigate({ path });
    },
  }, label);
}
