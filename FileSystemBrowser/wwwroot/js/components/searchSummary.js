import { el } from '../dom.js';

// The searchSummary function creates a summary element that displays the search query,
// the relative path, and a warning if the results are truncated.
export function searchSummary({ query, relativePath, entries, truncated }) {
  return el('p', { className: 'status' },
    `Results for “${query}” in ${relativePath || 'Home'}`,
    truncated
      ? el('strong', { className: 'warning' },
          ` · showing the first ${entries.length}; refine your search to see more`)
      : null);
}
