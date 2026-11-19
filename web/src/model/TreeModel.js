// Modelo (cliente): estado de la aplicación y lista de suscriptores (patrón observador).
// Las vistas escuchan eventos; ninguna llama a otra vista ni a la API.

export class TreeModel {
  #listeners = new Map();

  nodes = [];
  count = 0;
  stats = { nodes: 0, leaves: 0, internalNodes: 0 };
  levels = 0;
  perLevel = [];
  traversal = { name: null, values: [] };
  result = null;
  connection = 'idle';

  on(event, fn) {
    if (!this.#listeners.has(event)) this.#listeners.set(event, new Set());
    this.#listeners.get(event).add(fn);
    return () => this.#listeners.get(event).delete(fn);
  }

  #emit(event, payload = this) {
    for (const fn of this.#listeners.get(event) ?? []) fn(payload);
  }

  setTree(tree) {
    this.nodes = tree.nodes;
    this.count = tree.count;
    this.#emit('tree');
  }

  setSummary(stats, levels) {
    this.stats = stats;
    this.levels = levels.levels;
    this.perLevel = levels.perLevel;
    this.#emit('summary');
  }

  setTraversal(name, values) {
    this.traversal = { name, values };
    this.#emit('traversal');
  }

  /** @param {{title: string, lines?: string[], tone?: 'ok'|'warn'|'error'}} result */
  setResult(result) {
    this.result = result;
    this.#emit('result');
  }

  setConnection(state) {
    this.connection = state;
    this.#emit('connection');
  }
}
