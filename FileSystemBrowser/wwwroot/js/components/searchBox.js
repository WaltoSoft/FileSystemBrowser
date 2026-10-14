import { el } from '../dom.js';

const DEBOUNCE_MS = 300;

/**
 * Search input. Searches as you type (debounced), immediately on Enter,
 * and clears on Escape. Created once and kept across renders so it never loses focus.
 *
 * Returns { element, setQuery } — setQuery syncs the box with the URL (e.g. after Back)
 * without disturbing the user while they type.
 */

// Creates a search box component that allows users to search for files and folders.
// The search box triggers the onSearch callback when the user types, presses Enter, or clears the input.
// The search input is debounced to avoid excessive calls to the onSearch callback while typing.
export function createSearchBox({ onSearch }) {
  let timer = null;

  const submit = value => {
    clearTimeout(timer);
    onSearch(value.trim());
  };

  const input = el('input', {
    type: 'search',
    placeholder: 'Search this folder and its subfolders (* and ? are wildcards)',
    ariaLabel: 'Search files and folders',
    autocomplete: 'off',
    oninput: () => {
      clearTimeout(timer);
      timer = setTimeout(() => submit(input.value), DEBOUNCE_MS);
    },
    onkeydown: e => {
      if (e.key === 'Enter') {
        submit(input.value);
      } else if (e.key === 'Escape' && input.value) {
        e.preventDefault();
        input.value = '';
        submit('');
      }
    },
  });

  return {
    element: el('div', { className: 'search' }, input),
    setQuery(q) {
      if (input.value.trim() !== q) input.value = q;
    },
  };
}
