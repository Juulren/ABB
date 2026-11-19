import { background, decode, pageDissolve, skeleton, swell, tabs } from 'asciify-engine/kit';
import './styles.css';
import { ApiClient } from './model/ApiClient.js';
import { TreeModel } from './model/TreeModel.js';
import { TreeView } from './view/TreeView.js';
import { SummaryView } from './view/SummaryView.js';
import { TraversalView } from './view/TraversalView.js';
import { ResultView } from './view/ResultView.js';
import { PaletteView } from './view/PaletteView.js';
import { COMMANDS, TreeController } from './controller/TreeController.js';
import { buttonLoading } from 'asciify-engine/kit';

const $ = (id) => document.getElementById(id);

// ---- Modelo y vistas ----
const model = new TreeModel();
const api = new ApiClient();

const treeView = new TreeView(model, {
  wrap: $('canvasWrap'), sizer: $('sizer'), scene: $('scene'), edges: $('edges'), empty: $('empty'),
});
new SummaryView(model, {
  nodesEl: $('cNodes'), leavesEl: $('cLeaves'), levelsEl: $('cLevels'), plotEl: $('plot'), labelEl: $('chartLabel'),
});
new TraversalView(model, { listEl: $('travList'), titleEl: $('travTitle') });
const resultView = new ResultView(model, {
  titleEl: $('resultTitle'), bodyEl: $('resultBody'), orbEl: $('orb'), orbLabelEl: $('orbLabel'), toastsEl: $('toasts'),
});

// ---- Cambio de vista con transición de página (pageDissolve) ----
const page = pageDissolve($('views'));
let currentView = 'tree';
const navLinks = [...document.querySelectorAll('#nav a')];

async function showView(name, origin) {
  if (name === currentView) return;
  currentView = name;
  await page.swap(() => {
    $('treeViewBox').hidden = name !== 'tree';
    $('printViewBox').hidden = name !== 'print';
    for (const a of navLinks) {
      if (a.dataset.view === name) a.setAttribute('aria-current', 'page');
      else a.removeAttribute('aria-current');
    }
    if (name === 'tree') treeView.fit();
  }, origin);
}

// ---- Controlador ----
const controller = new TreeController({
  api, model, treeView, resultView,
  inputs: { a: $('valA'), b: $('valB') },
  showView,
  printEl: $('printText'),
});

for (const link of navLinks) {
  link.addEventListener('click', (e) => {
    e.preventDefault();
    if (link.dataset.view === 'print') controller.run('print');
    else showView('tree', link);
  });
}

// Pestañas de recorrido
const tabHandle = tabs($('tablist'), {
  panels: 'none',
  onChange: (index) => controller.onTabChange(index),
});
controller.attachTabs(tabHandle);

// Botones con estado de carga
const labels = { insert: 'Insertar', search: 'Buscar', delete: 'Borrar', update: 'Actualizar' };
for (const btn of document.querySelectorAll('[data-op]')) {
  const op = btn.dataset.op;
  if (op in labels) {
    const handle = buttonLoading(btn);
    btn.addEventListener('click', () => controller.run(op, handle, labels[op]));
  } else {
    btn.addEventListener('click', () => controller.run(op));
  }
}
$('valA').addEventListener('keydown', (e) => {
  if (e.key === 'Enter') document.querySelector('[data-op="insert"]').click();
});

// Paleta de comandos (Ctrl+K)
new PaletteView(
  { panel: $('palette'), search: $('paletteSearch'), rowsEl: $('paletteRows'), trigger: $('paletteTrigger') },
  COMMANDS,
  (id) => controller.run(id),
);

// ---- Efectos de Asciify ----
decode($('title'), { duration: 900, settle: 'text' });
for (const a of navLinks) decode(a, { scramble: true, trigger: 'manual', settle: 'text', duration: 600, look: { cellSize: 3 } });

const bg = background($('bg'), { preset: swell, speed: 0.6, opacity: 0.55 });
const bgToggle = $('bgToggle');
bgToggle.addEventListener('click', () => {
  const on = bgToggle.getAttribute('aria-pressed') !== 'true';
  bgToggle.setAttribute('aria-pressed', String(on));
  $('bg').style.display = on ? '' : 'none';
  if (on) bg.play(); else bg.pause();
});

// Esqueleto mientras llega el primer árbol
const skel = skeleton($('skel'));
await controller.init();
await skel.resolve();
$('skel').remove();
