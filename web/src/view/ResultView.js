import { statusOrb, toastStack } from 'asciify-engine/kit';

// Vista: panel de resultado de la última operación, avisos (toastStack) y estado de la API (statusOrb).

export class ResultView {
  /** @param {import('../model/TreeModel.js').TreeModel} model */
  constructor(model, { titleEl, bodyEl, orbEl, orbLabelEl, toastsEl }) {
    this.titleEl = titleEl;
    this.bodyEl = bodyEl;
    this.orbLabelEl = orbLabelEl;
    this.orb = statusOrb(orbEl, { ring: true });
    this.toasts = toastStack(toastsEl, { max: 3, duration: 4500 });

    model.on('result', () => this.renderResult(model.result));
    model.on('connection', () => this.renderConnection(model.connection));
  }

  renderResult(result) {
    this.titleEl.textContent = result.title;
    this.titleEl.dataset.tone = result.tone ?? 'ok';
    this.bodyEl.replaceChildren(
      ...(result.lines ?? []).map((line) => {
        const p = document.createElement('p');
        p.textContent = line;
        return p;
      }),
    );
  }

  renderConnection(state) {
    const labels = { loading: 'Consultando…', ok: 'API conectada', error: 'Sin conexión', idle: 'API' };
    this.orbLabelEl.textContent = labels[state] ?? 'API';
    if (state === 'loading') this.orb.loading();
    else if (state === 'ok') this.orb.success();
    else if (state === 'error') this.orb.error();
    else this.orb.idle();
  }

  toast(title, body) {
    this.toasts.push({ title, body });
  }
}
