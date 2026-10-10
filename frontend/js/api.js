const API_BASE_URL = window.API_BASE_URL || "http://localhost:5090/api";

const Sesion = {
  guardar(login) { localStorage.setItem("access_sesion", JSON.stringify(login)); },
  obtener() { const raw = localStorage.getItem("access_sesion"); return raw ? JSON.parse(raw) : null; },
  actualizarTokens(accessToken, accessTokenExpiresAt, refreshToken) {
    const s = this.obtener();
    if (!s) return;
    s.accessToken = accessToken; s.accessTokenExpiresAt = accessTokenExpiresAt; s.refreshToken = refreshToken;
    this.guardar(s);
  },
  cerrar() { localStorage.removeItem("access_sesion"); }
};

let refrescando = null;

async function intentarRefresh() {
  const sesion = Sesion.obtener();
  if (!sesion?.refreshToken) return false;
  if (!refrescando) {
    refrescando = fetch(`${API_BASE_URL}/auth/refresh`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: sesion.refreshToken })
    }).then(async r => {
      if (!r.ok) return false;
      const data = await r.json();
      Sesion.actualizarTokens(data.accessToken, data.accessTokenExpiresAt, data.refreshToken);
      return true;
    }).catch(() => false).finally(() => { refrescando = null; });
  }
  return refrescando;
}

async function apiFetch(ruta, opciones = {}, reintento = true) {
  const sesion = Sesion.obtener();
  const headers = { "Content-Type": "application/json", ...(opciones.headers || {}) };
  if (sesion?.accessToken) headers["Authorization"] = `Bearer ${sesion.accessToken}`;

  const resp = await fetch(`${API_BASE_URL}${ruta}`, { ...opciones, headers });

  if (resp.status === 401 && reintento) {
    const ok = await intentarRefresh();
    if (ok) return apiFetch(ruta, opciones, false);
    Sesion.cerrar();
    mostrarLogin();
    throw new Error("Sesión expirada. Inicia sesión de nuevo.");
  }

  if (resp.status === 403) throw new Error("No tienes permiso para realizar esta acción.");

  if (!resp.ok) {
    let mensaje = `Error ${resp.status}`;
    try { const data = await resp.json(); mensaje = data.mensaje || data.title || JSON.stringify(data.errors || data); }
    catch { /* sin cuerpo JSON */ }
    throw new Error(mensaje);
  }

  if (resp.status === 204) return null;
  return resp.json();
}

const Api = {
  login: (email, password) => apiFetch("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),

  houses: {
    listar: () => apiFetch("/houses"),
    crear: (dto) => apiFetch("/houses", { method: "POST", body: JSON.stringify(dto) }),
    actualizar: (id, dto) => apiFetch(`/houses/${id}`, { method: "PUT", body: JSON.stringify(dto) })
  },
  residents: {
    listar: (houseId) => apiFetch(`/residents${houseId ? `?houseId=${houseId}` : ""}`),
    crear: (dto) => apiFetch("/residents", { method: "POST", body: JSON.stringify(dto) })
  },
  accessPasses: {
    listar: (houseId) => apiFetch(`/access-passes${houseId ? `?houseId=${houseId}` : ""}`),
    crear: (dto) => apiFetch("/access-passes", { method: "POST", body: JSON.stringify(dto) }),
    revocar: (id) => apiFetch(`/access-passes/${id}/revoke`, { method: "POST" })
  },
  accessPoints: {
    listar: () => apiFetch("/access-points"),
    crear: (dto) => apiFetch("/access-points", { method: "POST", body: JSON.stringify(dto) })
  },
  devices: {
    listar: () => apiFetch("/devices"),
    crear: (dto) => apiFetch("/devices", { method: "POST", body: JSON.stringify(dto) }),
    vincular: (deviceId, dto) => apiFetch(`/devices/${deviceId}/access-points`, { method: "POST", body: JSON.stringify(dto) })
  },
  accessEvents: {
    listar: (limit = 50) => apiFetch(`/access-events?limit=${limit}`)
  },
  access: {
    validate: (token) => apiFetch("/access/validate", { method: "POST", body: JSON.stringify({ token }) }),
    scan: (token, accessPointId) => apiFetch("/access/scan", { method: "POST", body: JSON.stringify({ token, accessPointId: accessPointId || null }) })
  },
  tenants: {
    listar: () => apiFetch("/tenants"),
    crear: (dto) => apiFetch("/tenants", { method: "POST", body: JSON.stringify(dto) })
  },
  inventory: {
    listar: (tenantId) => apiFetch(`/inventory${tenantId ? `?tenantId=${tenantId}` : ""}`),
    crear: (dto) => apiFetch("/inventory", { method: "POST", body: JSON.stringify(dto) }),
    actualizar: (id, dto) => apiFetch(`/inventory/${id}`, { method: "PUT", body: JSON.stringify(dto) }),
    eliminar: (id) => apiFetch(`/inventory/${id}`, { method: "DELETE" })
  }
};
