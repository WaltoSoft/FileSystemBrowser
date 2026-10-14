import { el } from './dom.js';
import { getDirectory } from './api.js';
import { startRouter, navigate } from './router.js';
import { breadcrumbs } from './components/breadcrumbs.js';
import { statsBar } from './components/statsBar.js';
import { fileTable } from './components/fileTable.js';
import { createSearchBox } from './components/searchBox.js';
import { searchSummary } from './components/searchSummary.js';
import { createUploadButton } from './components/uploadButton.js';
import { createNewFolderButton } from './components/newFolderButton.js';

// This script is the main entry point for the app.
// It sets up the UI and routing, and handles navigation and search requests.

// The loading message is only shown if the request takes longer than this, to avoid flicker on fast responses.
const LOADING_DELAY_MS = 200;

// The current view state, as read from the URL.  Updated on navigation and search.
let current = { path: '', q: '' };

// The current in-flight request, if any.  Used to cancel slow requests when a new navigation or search occurs.
let inFlight = null;

// The search box is created once and lives outside the re-rendered area,
// so re-rendering results never steals focus or the cursor while typing.
const searchBox = createSearchBox({
  onSearch: q => {
    if (q === current.q) return;
    // Starting a search adds a history entry (Back returns to the folder);
    // refining an existing search replaces it, so each keystroke isn't a Back step.
    navigate({ path: current.path, q }, { replace: Boolean(current.q && q) });
  },
});

// Create the upload button element
const uploadButton = createUploadButton({
  getFolder: () => current.path,
  onUploaded: () => show(current),
});


// Create the new folder button element
const newFolderButton = createNewFolderButton({
  getFolder: () => current.path,
  onCreated: () => show(current),
});

// Create the content area section where the breadcrumbs, stats, and file table will be rendered.
const content = el('section', { className: 'content' });

// Render the toolbar and content area into the app container.
document.getElementById('app').replaceChildren(
  el('div', { className: 'toolbar' }, searchBox.element, uploadButton, newFolderButton),
  content);

// Replaces the content area, skipping null parts (replaceChildren would render them as "null").
function render(...nodes) {
  content.replaceChildren(...nodes.filter(Boolean));
}

// Loads and renders the view described by the URL: a folder listing or a search.
async function show(state) {
  current = state;
  searchBox.setQuery(state.q);

  // A newer navigation or search always wins: cancel the previous request so a slow,
  // stale response can never overwrite what the user asked for most recently.
  inFlight?.abort();
  const controller = new AbortController();
  inFlight = controller;

  // Only show a loading message for slow responses, so fast navigation doesn't flicker.
  const loadingTimer = setTimeout(() => {
    render(
      breadcrumbs(state.path),
      el('p', { className: 'status' }, state.q ? 'Searching…' : 'Loading…'));
  }, LOADING_DELAY_MS);

  try {
    // Fetch the directory listing or search results from the server.
    const listing = await getDirectory(state.path, { q: state.q, signal: controller.signal });
    const isSearch = Boolean(listing.query);

    // Render the breadcrumbs, search summary (if searching), stats bar, and file table.
    render(
      breadcrumbs(listing.relativePath),
      isSearch ? searchSummary(listing) : null,
      statsBar({ folderCount: listing.folderCount, fileCount: listing.fileCount, totalBytes: listing.totalBytes }),
      fileTable(listing.entries, {
        showLocation: isSearch,
        emptyText: isSearch ? 'No matches.' : 'This folder is empty.',
        onChanged: () => show(current),
      }));
  } catch (err) {
    if (err.name === 'AbortError') return;
    render(breadcrumbs(state.path), el('p', { className: 'status error' }, err.message));
  } finally {
    clearTimeout(loadingTimer);
  }
}

// Start the router, which will call `show` with the current URL state now and on back/forward navigation.
startRouter(show);
