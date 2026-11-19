// Vista: dibuja el árbol. Nodos HTML (div) sobre un SVG de aristas, con un solo sistema de
// coordenadas en píxeles. Todo el conjunto se escala con un único transform.

const SVG_NS = 'http://www.w3.org/2000/svg';
const COL = 58;   // separación horizontal entre índices inorden
const ROW = 80;   // separación vertical entre niveles
const R = 23;     // radio del nodo
const PAD = 34;
const MARKS = ['path', 'found', 'miss', 'successor', 'target'];

export class TreeView {
  /** @param {import('../model/TreeModel.js').TreeModel} model */
  constructor(model, { wrap, sizer, scene, edges, empty }) {
    this.model = model;
    this.wrap = wrap;
    this.sizer = sizer;
    this.scene = scene;
    this.edges = edges;
    this.empty = empty;
    this.dom = new Map();
    this.edgeDom = new Map();
    this.size = { w: 1, h: 1 };
    this.onSelect = null;

    model.on('tree', () => this.render());
    new ResizeObserver(() => this.fit()).observe(wrap);

    scene.addEventListener('click', (e) => {
      const el = e.target.closest('.nodo');
      if (el && this.onSelect) this.onSelect(Number(el.dataset.value));
    });
  }

  nodeEl(value) {
    return this.dom.get(value) ?? null;
  }

  render() {
    const nodes = this.model.nodes;
    this.empty.hidden = nodes.length > 0;

    const byValue = new Map(nodes.map((n) => [n.value, n]));
    const maxLevel = nodes.reduce((m, n) => Math.max(m, n.level), 1);
    const w = Math.max(nodes.length, 1) * COL + PAD * 2;
    const h = maxLevel * ROW + PAD;
    this.size = { w, h };
    const pos = (n) => ({ x: PAD + n.inorderIndex * COL + COL / 2, y: PAD / 2 + (n.level - 1) * ROW + R });

    for (const [value, el] of this.dom) {
      if (!byValue.has(value)) { el.remove(); this.dom.delete(value); }
    }
    for (const [value, el] of this.edgeDom) {
      if (!byValue.has(value) || byValue.get(value).parent == null) { el.remove(); this.edgeDom.delete(value); }
    }

    for (const n of nodes) {
      const { x, y } = pos(n);
      let el = this.dom.get(n.value);
      if (!el) {
        el = document.createElement('div');
        el.className = 'nodo enter';
        el.dataset.value = String(n.value);
        el.textContent = String(n.value);     // textContent: un valor nunca se interpreta como HTML
        this.scene.append(el);
        this.dom.set(n.value, el);
      }
      el.style.left = `${x - R}px`;
      el.style.top = `${y - R}px`;
    }

    this.edges.setAttribute('width', w);
    this.edges.setAttribute('height', h);
    this.scene.style.width = `${w}px`;
    this.scene.style.height = `${h}px`;

    for (const n of nodes) {
      if (n.parent == null) continue;
      const p = pos(byValue.get(n.parent));
      const c = pos(n);
      const d = `M ${p.x} ${p.y} L ${c.x} ${c.y}`;
      let edge = this.edgeDom.get(n.value);
      if (!edge) {
        edge = document.createElementNS(SVG_NS, 'path');
        this.edges.append(edge);
        this.edgeDom.set(n.value, edge);
      }
      edge.setAttribute('d', d);
      edge.style.setProperty('d', `path('${d}')`);
    }

    this.fit();
  }

  fit() {
    const { w, h } = this.size;
    const aw = this.wrap.clientWidth - 8;
    const ah = this.wrap.clientHeight - 8;
    let s = Math.min(1.35, aw / w, ah / h);   // un árbol pequeño se amplía un poco para llenar el escenario
    if (!Number.isFinite(s) || s <= 0) s = 1;
    s = Math.max(s, 0.4);    // por debajo de esto el contenedor se desplaza
    this.scene.style.transform = `scale(${s})`;
    this.sizer.style.width = `${w * s}px`;
    this.sizer.style.height = `${h * s}px`;
  }

  mark(value, cls) {
    this.dom.get(value)?.classList.add(cls);
    this.edgeDom.get(value)?.classList.add(cls);
  }

  badge(value, text, tone) {
    const el = this.dom.get(value);
    if (!el) return;
    el.dataset.order = String(text);
    if (tone) el.dataset.tone = tone;
  }

  clearMarks() {
    for (const el of [...this.dom.values(), ...this.edgeDom.values()]) {
      el.classList.remove(...MARKS);
      delete el.dataset.order;
      delete el.dataset.tone;
    }
  }
}
