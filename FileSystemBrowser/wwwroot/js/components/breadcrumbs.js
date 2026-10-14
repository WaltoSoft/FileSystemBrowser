import { el } from '../dom.js';
import { folderLink } from './folderLink.js';

// Builds a breadcrumb navigation element based on the provided path.
// The path is split into segments, and each segment is represented as a link or bold text if it's the current segment.
export function breadcrumbs(path) {
  const segments = path ? path.split('/') : [];
  const items = [segments.length > 0 ? folderLink('Home', '') : el('strong', {}, 'Home')];

  segments.forEach((segment, i) => {
    const isCurrent = i === segments.length - 1;
    items.push(el('span', { className: 'separator' }, '/'));
    items.push(isCurrent
      ? el('strong', {}, segment)
      : folderLink(segment, segments.slice(0, i + 1).join('/')));
  });

  return el('nav', { className: 'breadcrumbs' }, items);
}
