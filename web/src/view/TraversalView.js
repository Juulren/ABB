import { pill } from 'asciify-engine/kit';

// Vista: muestra el resultado de un recorrido como una fila de pastillas (pill de Asciify).

export class TraversalView {
  /** @param {import('../model/TreeModel.js').TreeModel} model */
  constructor(model, { listEl, titleEl }) {
    this.listEl = listEl;
    this.titleEl = titleEl;
    this.handle = null;
    model.on('traversal', () => this.render(model.traversal));
  }

  render({ name, values }) {
    this.handle?.destroy();
    this.handle = null;
    this.titleEl.textContent = name ? `${name}: ${values.length} elemento${values.length === 1 ? '' : 's'}` : 'Elige un recorrido';
    this.listEl.replaceChildren();
    if (!values.length) {
      const vacio = document.createElement('span');
      vacio.className = 'muted';
      vacio.textContent = name ? 'El árbol está vacío.' : '';
      this.listEl.append(vacio);
      return;
    }
    const items = values.map((v) => {
      const el = document.createElement('span');
      el.className = 'pill';
      el.dataset.tone = 'neutral';
      el.textContent = String(v);
      this.listEl.append(el);
      return el;
    });
    this.handle = pill(items);
  }
}
