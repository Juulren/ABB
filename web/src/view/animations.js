// Vista: secuencias de resaltado sobre el árbol. Con prefers-reduced-motion se muestran
// de golpe, sin pausas.

const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, reduced() ? 0 : ms));

/** Resalta una ruta nodo por nodo. */
export async function walk(view, values, cls = 'path', step = 340) {
  for (const v of values) {
    view.mark(v, cls);
    await sleep(step);
  }
}

/** Numera los nodos en el orden de visita de un recorrido. */
export async function numberInOrder(view, values, step = 240) {
  view.clearMarks();
  for (let i = 0; i < values.length; i++) {
    view.mark(values[i], 'path');
    view.badge(values[i], i + 1);
    await sleep(step);
  }
}
