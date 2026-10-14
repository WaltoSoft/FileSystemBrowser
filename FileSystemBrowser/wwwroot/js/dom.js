// Creates a DOM element with the specified tag, properties, and children.
// The `props` parameter is an object that sets the DOM properties of the element,
// and any keys starting with "on" are registered as event listeners.
// The `children` parameter can be nodes, strings, arrays of either, or null / undefined(which are skipped).
// Strings are inserted as text nodes, preventing untrusted values from injecting markup.
// This function is how ALL DOM elements are created in this app,
// so that we can easily change the implementation later if needed.
export function el(tag, props = {}, ...children) {
  const element = document.createElement(tag);

  for (const [key, value] of Object.entries(props)) {
    if (key.startsWith('on')) {
      element.addEventListener(key.slice(2).toLowerCase(), value);
    } else {
      element[key] = value;
    }
  }

  element.append(...children.flat().filter(child => child != null));
  return element;
}
