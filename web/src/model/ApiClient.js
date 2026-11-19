// Modelo (cliente): único lugar que habla con la API REST.

export class ApiError extends Error {
  constructor(status, title, detail) {
    super(detail || title);
    this.status = status;
    this.title = title;
    this.detail = detail;
  }
}

async function request(method, url, body) {
  let res;
  try {
    res = await fetch(url, {
      method,
      headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'Sin conexión', 'No se pudo conectar con la API. Verifica que esté en ejecución.');
  }

  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = null;
  }

  if (!res.ok) {
    const detail = data?.detail ?? (data?.errors ? 'El valor enviado no es válido.' : 'La API respondió con un error.');
    throw new ApiError(res.status, data?.title ?? res.statusText, detail);
  }
  return data;
}

export class ApiClient {
  constructor(base = '/api/tree') {
    this.base = base;
  }

  getTree() { return request('GET', this.base); }
  print() { return request('GET', `${this.base}/print`); }
  search(value) { return request('GET', `${this.base}/nodes/${value}`); }
  insert(value) { return request('POST', `${this.base}/nodes`, { value }); }
  update(value, newValue) { return request('PUT', `${this.base}/nodes/${value}`, { newValue }); }
  remove(value) { return request('DELETE', `${this.base}/nodes/${value}`); }
  traversal(name) { return request('GET', `${this.base}/traversals/${name}`); }
  levels() { return request('GET', `${this.base}/levels`); }
  level(value) { return request('GET', `${this.base}/nodes/${value}/level`); }
  extremes() { return request('GET', `${this.base}/extremes`); }
  stats() { return request('GET', `${this.base}/stats`); }
  ancestors(value) { return request('GET', `${this.base}/nodes/${value}/ancestors`); }
  balance() { return request('GET', `${this.base}/balance`); }
  lca(a, b) { return request('GET', `${this.base}/lca?a=${a}&b=${b}`); }
  clear() { return request('DELETE', this.base); }
  seed() { return request('POST', `${this.base}/seed`); }
}
