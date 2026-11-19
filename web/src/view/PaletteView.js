import { commandPalette } from 'asciify-engine/kit';

// Vista: paleta de comandos con las 15 operaciones (Ctrl+K). Solo muestra y filtra filas;
// quien decide qué hacer es el controlador (onRun).

export class PaletteView {
  constructor({ panel, search, rowsEl, trigger }, commands, onRun) {
    this.search = search;
    this.rowsEl = rowsEl;
    this.commands = commands;
    this.visible = [];
    this.renderRows('');

    this.handle = commandPalette(panel, {
      trigger,
      onRun: (_, row) => onRun(row.dataset.id),
      onOpenChange: (open) => {
        if (open) { search.value = ''; this.renderRows(''); }
      },
    });

    search.addEventListener('input', () => this.renderRows(search.value));
  }

  renderRows(query) {
    const q = query.trim().toLowerCase();
    this.visible = this.commands.filter((c) => c.title.toLowerCase().includes(q));
    this.rowsEl.replaceChildren(
      ...this.visible.map((c) => {
        const row = document.createElement('div');
        row.setAttribute('role', 'option');
        row.id = `op-${c.id}`;
        row.dataset.id = c.id;
        row.textContent = c.title;
        if (c.aux) row.dataset.aux = 'true';
        return row;
      }),
    );
  }
}
