import { disintegrate } from 'asciify-engine/kit';
import { ApiError } from '../model/ApiClient.js';
import { numberInOrder, sleep, walk } from '../view/animations.js';

// Controlador (cliente): recibe eventos de la interfaz, llama a la API y actualiza el modelo.
// Las vistas reaccionan al modelo. Aquí no se calcula ninguna operación del árbol.

export const COMMANDS = [
  { id: 'print', title: '1. Imprimir los elementos del árbol' },
  { id: 'search', title: '2. Buscar un elemento', needs: 1 },
  { id: 'insert', title: '3. Insertar un elemento', needs: 1 },
  { id: 'delete', title: '4. Borrar un elemento', needs: 1 },
  { id: 'levelorder', title: '5. Recorrido en anchura (por niveles)' },
  { id: 'preorder', title: '6. Recorrido en preorden' },
  { id: 'postorder', title: '7. Recorrido en postorden' },
  { id: 'inorder', title: '8. Recorrido en inorden' },
  { id: 'levels', title: '9. Número de niveles del árbol' },
  { id: 'level', title: '10. Nivel de un nodo', needs: 1 },
  { id: 'extremes', title: '11. Valor mínimo y máximo' },
  { id: 'stats', title: '12. Contar nodos y hojas' },
  { id: 'ancestors', title: '13. Ancestros de un nodo', needs: 1 },
  { id: 'balance', title: '14. Verificar si está balanceado' },
  { id: 'lca', title: '15. Ancestro común más bajo de dos nodos', needs: 2 },
  { id: 'update', title: 'Actualizar un elemento (CRUD)', needs: 2, aux: true },
  { id: 'seed', title: 'Cargar árbol de ejemplo', aux: true },
  { id: 'clear', title: 'Vaciar el árbol', aux: true },
];

export const TRAVERSAL_TABS = [
  { id: 'preorder', label: 'Preorden' },
  { id: 'inorder', label: 'Inorden' },
  { id: 'postorder', label: 'Postorden' },
  { id: 'levelorder', label: 'Por niveles' },
];

const MIN = -999;
const MAX = 999;

class UserError extends Error {
  constructor(message, input) {
    super(message);
    this.input = input;
  }
}

export class TreeController {
  /**
   * @param {object} deps
   * @param {import('../model/ApiClient.js').ApiClient} deps.api
   * @param {import('../model/TreeModel.js').TreeModel} deps.model
   * @param {import('../view/TreeView.js').TreeView} deps.treeView
   * @param {import('../view/ResultView.js').ResultView} deps.resultView
   * @param {{a: HTMLInputElement, b: HTMLInputElement}} deps.inputs
   * @param {(name: string, origin?: Element) => Promise<void>} deps.showView
   * @param {HTMLElement} deps.printEl
   */
  constructor({ api, model, treeView, resultView, inputs, showView, printEl }) {
    Object.assign(this, { api, model, treeView, resultView, inputs, showView, printEl });
    this.busy = false;
    this.tabs = null;
    this.currentTab = -1;

    treeView.onSelect = (value) => {
      inputs.a.value = String(value);
    };

    this.handlers = {
      print: () => this.print(),
      search: (a) => this.search(a),
      insert: (a) => this.insert(a),
      delete: (a) => this.remove(a),
      levelorder: () => this.traversal('levelorder'),
      preorder: () => this.traversal('preorder'),
      postorder: () => this.traversal('postorder'),
      inorder: () => this.traversal('inorder'),
      levels: () => this.levels(),
      level: (a) => this.level(a),
      extremes: () => this.extremes(),
      stats: () => this.stats(),
      ancestors: (a) => this.ancestors(a),
      balance: () => this.balance(),
      lca: (a, b) => this.lca(a, b),
      update: (a, b) => this.update(a, b),
      seed: () => this.seed(),
      clear: () => this.clear(),
    };
  }

  attachTabs(handle) {
    this.tabs = handle;
  }

  /** Lo llaman las pestañas cuando la persona elige un recorrido con el ratón o el teclado. */
  onTabChange(index) {
    if (index === this.currentTab) return;
    if (this.busy) {
      // La pestaña ya se movió. Se devuelve a la del recorrido que sigue en pantalla.
      if (this.currentTab >= 0) this.tabs?.select(this.currentTab);
      this.resultView.toast('Operación en curso', 'Espera a que termine la animación.');
      return;
    }
    this.run(TRAVERSAL_TABS[index].id);
  }

  async init() {
    this.busy = true;
    this.model.setConnection('loading');
    try {
      this.model.setTree(await this.api.getTree());
      await this.refreshSummary();
      this.model.setConnection('ok');
      this.model.setResult({ title: 'Listo', lines: ['Inserta un valor o carga el árbol de ejemplo para empezar.'] });
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
    }
  }

  /**
   * Ejecuta una operación por id. `button` es el handle de buttonLoading cuando viene de un botón.
   */
  async run(id, button, label) {
    if (this.busy) {
      this.resultView.toast('Operación en curso', 'Espera a que termine la animación.');
      return;
    }
    const cmd = COMMANDS.find((c) => c.id === id);
    if (!cmd) return;

    this.busy = true;
    button?.start(label);
    let ok = false;
    try {
      const a = cmd.needs >= 1 ? this.parse(this.inputs.a, 'Valor') : undefined;
      const b = cmd.needs >= 2 ? this.parse(this.inputs.b, 'Segundo valor') : undefined;
      this.model.setConnection('loading');
      this.treeView.clearMarks();
      if (id !== 'print') await this.showView('tree');
      await this.handlers[id](a, b);
      this.model.setConnection('ok');
      ok = true;
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
      if (button) {
        if (ok) button.succeed(label); else button.fail(label);
        setTimeout(() => button.reset(label), 900);
      }
    }
  }

  // ---- utilidades ----

  parse(input, nombre) {
    const texto = input.value.trim();
    if (!/^-?\d+$/.test(texto) || Number(texto) < MIN || Number(texto) > MAX) {
      throw new UserError(`"${nombre}" debe ser un entero entre ${MIN} y ${MAX}.`, input);
    }
    return Number(texto);
  }

  fail(error) {
    if (error instanceof UserError) {
      this.model.setConnection('ok');
      this.model.setResult({ title: 'Revisa el valor', lines: [error.message], tone: 'warn' });
      this.resultView.toast('Revisa el valor', error.message);
      error.input.focus();
      return;
    }
    if (error instanceof ApiError) {
      this.model.setConnection(error.status === 0 ? 'error' : 'ok');
      this.model.setResult({ title: error.title, lines: [error.detail], tone: error.status === 0 ? 'error' : 'warn' });
      this.resultView.toast(error.title, error.detail);
      return;
    }
    console.error(error);
    this.model.setConnection('error');
    this.model.setResult({ title: 'Error inesperado', lines: [String(error?.message ?? error)], tone: 'error' });
    this.resultView.toast('Error inesperado', String(error?.message ?? error));
  }

  /** Una animación que falla no debe impedir que se dibuje el árbol que ya cambió en el servidor. */
  async safe(animation) {
    try {
      await animation();
    } catch (e) {
      console.error(e);
    }
  }

  /** Si el resumen falla después de un cambio, el cambio ya se hizo: no se reporta como error. */
  async safeSummary() {
    await this.safe(() => this.refreshSummary());
  }

  async refreshSummary() {
    const [stats, levels] = await Promise.all([this.api.stats(), this.api.levels()]);
    this.model.setSummary(stats, levels);
  }

  ruta(valores) {
    return valores.join(' → ');
  }

  // ---- operaciones ----

  async print() {
    const r = await this.api.print();
    this.printEl.textContent = r.text;
    await this.showView('print');
    this.model.setResult({ title: 'Impresión del árbol', lines: ['El árbol se muestra girado: el hijo derecho arriba y el izquierdo abajo.'] });
  }

  async search(a) {
    const r = await this.api.search(a);
    await walk(this.treeView, r.path);
    if (r.found) {
      this.treeView.mark(a, 'found');
      this.model.setResult({
        title: `El valor ${a} existe`,
        lines: [`Nivel ${r.level}.`, `Ruta: ${this.ruta(r.path)}`],
      });
    } else {
      if (r.path.length) this.treeView.mark(r.path.at(-1), 'miss');
      this.model.setResult({
        title: `El valor ${a} no está en el árbol`,
        lines: [r.path.length ? `Ruta recorrida: ${this.ruta(r.path)}` : 'El árbol está vacío.'],
        tone: 'warn',
      });
    }
  }

  async insert(a) {
    const r = await this.api.insert(a);
    await this.safe(() => walk(this.treeView, r.path.slice(0, -1)));
    this.model.setTree(r.tree);
    this.treeView.mark(a, 'found');
    await this.safeSummary();
    this.model.setResult({
      title: `Se insertó el ${a}`,
      lines: [`Ruta de inserción: ${this.ruta(r.path)}`],
    });
  }

  async remove(a) {
    const r = await this.api.remove(a);
    const casos = {
      hoja: 'Era una hoja: se quitó sin más.',
      unHijo: 'Tenía un hijo: el hijo ocupó su lugar.',
      dosHijos: `Tenía dos hijos: se reemplazó por su sucesor inorden (${r.successor}).`,
    };
    await this.safe(async () => {
      await walk(this.treeView, r.path);
      this.treeView.mark(a, 'target');
      if (r.case === 'dosHijos') await walk(this.treeView, r.successorPath.slice(1), 'successor');
      const el = this.treeView.nodeEl(a);
      // Sin un campo que dibuje, disintegrate podría no resolverse: se limita la espera.
      if (el) {
        await Promise.race([
          disintegrate(el, { duration: 620, collapse: false, remove: false }),
          new Promise((resolve) => setTimeout(resolve, 1800)),
        ]);
      }
    });
    this.model.setTree(r.tree);
    await this.safeSummary();
    this.model.setResult({
      title: `Se borró el ${a}`,
      lines: [casos[r.case] ?? '', `Ruta hasta el nodo: ${this.ruta(r.path)}`],
    });
  }

  async update(a, b) {
    const r = await this.api.update(a, b);
    this.model.setTree(r.tree);
    this.treeView.mark(b, 'found');
    await this.safeSummary();
    this.model.setResult({
      title: `Se cambió el ${a} por el ${b}`,
      lines: ['En un ABB el cambio equivale a borrar el valor anterior e insertar el nuevo.'],
    });
  }

  async seed() {
    this.model.setTree(await this.api.seed());
    await this.safeSummary();
    this.model.setResult({ title: 'Árbol de ejemplo cargado', lines: ['10 nodos: 50, 30, 70, 20, 40, 60, 80, 35, 45, 65.'] });
  }

  async clear() {
    this.model.setTree(await this.api.clear());
    this.model.setTraversal(null, []);
    await this.safeSummary();
    this.model.setResult({ title: 'Árbol vaciado', lines: ['No quedan nodos.'] });
  }

  async traversal(name) {
    const index = TRAVERSAL_TABS.findIndex((t) => t.id === name);
    this.currentTab = index;
    this.tabs?.select(index);
    const r = await this.api.traversal(name);
    const label = TRAVERSAL_TABS[index].label;
    this.model.setTraversal(label, r.values);
    await numberInOrder(this.treeView, r.values);
    this.model.setResult({
      title: `Recorrido ${label.toLowerCase()}`,
      lines: [r.values.length ? r.values.join(', ') : 'El árbol está vacío.'],
    });
  }

  async levels() {
    const r = await this.api.levels();
    this.model.setResult({
      title: r.levels === 0 ? 'El árbol está vacío' : `El árbol tiene ${r.levels} nivel${r.levels === 1 ? '' : 'es'}`,
      lines: r.perLevel.map((n, i) => `Nivel ${i + 1}: ${n} nodo${n === 1 ? '' : 's'}`),
    });
  }

  async level(a) {
    const [r, s] = await Promise.all([this.api.level(a), this.api.search(a)]);
    await walk(this.treeView, s.path);
    this.treeView.mark(a, 'found');
    this.model.setResult({
      title: `El ${a} está en el nivel ${r.level}`,
      lines: ['La raíz está en el nivel 1.', `Ruta: ${this.ruta(s.path)}`],
    });
  }

  async extremes() {
    const r = await this.api.extremes();
    await walk(this.treeView, r.minPath);
    this.treeView.mark(r.min, 'found');
    await sleep(500);
    this.treeView.clearMarks();
    await walk(this.treeView, r.maxPath);
    this.treeView.mark(r.max, 'found');
    this.model.setResult({
      title: `Mínimo ${r.min}, máximo ${r.max}`,
      lines: [`Ruta al mínimo: ${this.ruta(r.minPath)}`, `Ruta al máximo: ${this.ruta(r.maxPath)}`],
    });
  }

  async stats() {
    const r = await this.api.stats();
    this.model.setResult({
      title: `${r.nodes} nodo${r.nodes === 1 ? '' : 's'}, ${r.leaves} hoja${r.leaves === 1 ? '' : 's'}`,
      lines: [`Nodos internos: ${r.internalNodes}.`],
    });
  }

  async ancestors(a) {
    const r = await this.api.ancestors(a);
    await walk(this.treeView, r.ancestors);
    this.treeView.mark(a, 'found');
    this.model.setResult({
      title: r.ancestors.length ? `Ancestros del ${a}` : `El ${a} es la raíz`,
      lines: [r.ancestors.length ? this.ruta(r.ancestors) : 'La raíz no tiene ancestros.'],
    });
  }

  async balance() {
    const r = await this.api.balance();
    for (const f of r.factors) this.treeView.badge(f.value, f.factor, Math.abs(f.factor) > 1 ? 'bad' : 'ok');
    this.model.setResult({
      title: r.balanced ? 'El árbol está balanceado' : 'El árbol no está balanceado',
      lines: [
        'Criterio AVL: en cada nodo, la diferencia entre la altura del subárbol izquierdo y la del derecho es como máximo 1.',
        'El número sobre cada nodo es su factor de equilibrio.',
      ],
      tone: r.balanced ? 'ok' : 'warn',
    });
  }

  async lca(a, b) {
    const r = await this.api.lca(a, b);
    await walk(this.treeView, r.pathA);
    await walk(this.treeView, r.pathB);
    this.treeView.mark(r.value, 'found');
    this.model.setResult({
      title: `Ancestro común más bajo de ${a} y ${b}: ${r.value}`,
      lines: [`Ruta al ${a}: ${this.ruta(r.pathA)}`, `Ruta al ${b}: ${this.ruta(r.pathB)}`],
    });
  }
}
