import { encodePath } from './paths.js';

// This script is what enables deep linking and back/forward navigation in the app.
// It reads the current view state from the URL, and provides functions to navigate to
// a new view state and update the URL accordingly.
// The `onChange` function is called whenever the view state changes,
// allowing the app to update its UI accordingly.

let onChange = () => {};

// Reads the current view state from the URL.
export function readLocation() {
  const params = new URLSearchParams(location.search);
  return {
    path: params.get('path') ?? '',
    q: params.get('q') ?? ''
  };
}

// URL for a view state; used both for link hrefs and for navigation.
export function browseUrl({ path = '', q = '' } = {}) {
  const params = [];
  if (path) params.push(`path=${encodePath(path)}`);
  if (q) params.push(`q=${encodeURIComponent(q)}`);
  return params.length > 0 ? `?${params.join('&')}` : location.pathname;
}

// Shows a view and records it in browser history.
// replace: overwrite the current history entry instead of adding one.
export function navigate(state, { replace = false } = {}) {
  const url = browseUrl(state);
  const isSameUrl = url === (location.search || location.pathname);

  if (replace || isSameUrl) history.replaceState(null, '', url);
  else history.pushState(null, '', url);

  onChange(readLocation());
}

// Starts routing.  Renders the current URL now, and again on back/forward.
export function startRouter(handler) {
  onChange = handler;
  window.addEventListener('popstate', () => onChange(readLocation()));
  onChange(readLocation());
}
