import { blockChart, odometer } from 'asciify-engine/kit';

// Vista: contadores (odómetro de Asciify) y gráfica de nodos por nivel (blockChart).

export class SummaryView {
  /** @param {import('../model/TreeModel.js').TreeModel} model */
  constructor(model, { nodesEl, leavesEl, levelsEl, plotEl, labelEl }) {
    this.labelEl = labelEl;
    this.defaultLabel = labelEl.textContent;
    this.nodes = odometer(nodesEl, { value: 0 });
    this.leaves = odometer(leavesEl, { value: 0 });
    this.levels = odometer(levelsEl, { value: 0 });
    this.chart = blockChart(plotEl, {
      values: [0],
      labels: ['-'],
      lazy: false,
      onHover: (i, v, label) => {
        this.labelEl.textContent = i === null ? this.defaultLabel : `${label}: ${v} nodo${v === 1 ? '' : 's'}`;
      },
    });
    this.plotEl = plotEl;

    // Sin un contexto WebGL2 con GPU el blockChart no dibuja. En ese caso se usan barras HTML.
    this.fallback = !this.chart.supported || !this.chart.field;
    if (this.fallback) {
      try { this.chart.destroy(); } catch { /* sin efecto */ }
      this.chart = null;
      plotEl.classList.add('plot-fallback');
    }
    model.on('summary', () => this.render(model));
  }

  render(model) {
    this.nodes.set(model.stats.nodes);
    this.leaves.set(model.stats.leaves);
    this.levels.set(model.levels);
    const values = model.perLevel.length ? model.perLevel : [0];
    const labels = model.perLevel.length ? model.perLevel.map((_, i) => `Nivel ${i + 1}`) : ['-'];
    if (this.fallback) this.renderBars(values, labels);
    else this.chart.set(values, labels);
  }

  renderBars(values, labels) {
    const max = Math.max(...values, 1);
    this.plotEl.replaceChildren(
      ...values.map((v, i) => {
        const bar = document.createElement('div');
        bar.className = 'bar';
        bar.style.height = `${Math.max(4, (v / max) * 100)}%`;
        bar.title = `${labels[i]}: ${v} nodo${v === 1 ? '' : 's'}`;
        return bar;
      }),
    );
  }
}
