let casasCache = [];
let accessPointsCache = [];
let qrsCache = [];
let inventarioCache = [];
let tenantsCache = [];

const TITULOS_SECCION = {
  "dashboard": "Dashboard",
  "mis-qrs": "Mis Visitantes / QRs",
  "escanear": "Escanear QR",
  "casas": "Casas",
  "residentes": "Residentes",
  "dispositivos": "Puntos de Acceso / Dispositivos",
  "inventario": "Equipo e Inventario",
  "eventos": "Historial de Accesos",
  "tenants": "Privadas"
};

document.addEventListener("DOMContentLoaded", () => {
  const sesion = Sesion.obtener();
  if (sesion) iniciarApp(); else mostrarLogin();

  document.getElementById("formLogin").addEventListener("submit", onLogin);
  document.getElementById("btnLogout").addEventListener("click", () => { detenerCamara(); Sesion.cerrar(); mostrarLogin(); });
  document.querySelectorAll("#navMenu [data-seccion]").forEach(el =>
    el.addEventListener("click", (e) => { e.preventDefault(); mostrarSeccion(el.dataset.seccion); cerrarSidebarMovil(); }));

  document.getElementById("btnMenuMovil").addEventListener("click", abrirSidebarMovil);
  document.getElementById("overlaySidebar").addEventListener("click", cerrarSidebarMovil);

  document.getElementById("formNuevoQr").addEventListener("submit", onGenerarQr);
  document.getElementById("formCasa").addEventListener("submit", onGuardarCasa);
  document.getElementById("formResidente").addEventListener("submit", onGuardarResidente);
  document.getElementById("formAccessPoint").addEventListener("submit", onGuardarAccessPoint);
  document.getElementById("formDevice").addEventListener("submit", onGuardarDevice);
  document.getElementById("formTenant").addEventListener("submit", onGuardarTenant);
  document.getElementById("formInventario").addEventListener("submit", onGuardarInventario);
  document.getElementById("modalInventario").addEventListener("show.bs.modal", (e) => {
    if (!e.relatedTarget) return; // se abrió programáticamente para editar, ya se llenó el form
    prepararModalInventarioNuevo();
  });
  document.getElementById("selectorTenantInventario").addEventListener("change", cargarInventario);

  document.getElementById("btnIniciarCamara").addEventListener("click", iniciarCamara);
  document.getElementById("btnDetenerCamara").addEventListener("click", detenerCamara);
  document.getElementById("btnEscanearManual").addEventListener("click", () => {
    const token = document.getElementById("tokenManual").value.trim();
    if (token) escanear(token);
  });
});

function mostrarLogin() {
  document.getElementById("loginBox").classList.remove("d-none");
  document.getElementById("app").classList.add("d-none");
}

function abrirSidebarMovil() {
  document.getElementById("sidebar").classList.add("abierta");
  document.getElementById("overlaySidebar").classList.add("activa");
}
function cerrarSidebarMovil() {
  document.getElementById("sidebar").classList.remove("abierta");
  document.getElementById("overlaySidebar").classList.remove("activa");
}

async function onLogin(e) {
  e.preventDefault();
  const email = document.getElementById("loginEmail").value;
  const password = document.getElementById("loginPassword").value;
  const errorBox = document.getElementById("loginError");
  errorBox.classList.add("d-none");
  try {
    const resp = await Api.login(email, password);
    Sesion.guardar(resp);
    iniciarApp();
  } catch (err) {
    errorBox.textContent = err.message;
    errorBox.classList.remove("d-none");
  }
}

function iniciarApp() {
  const sesion = Sesion.obtener();
  document.getElementById("loginBox").classList.add("d-none");
  document.getElementById("app").classList.remove("d-none");
  document.getElementById("usuarioNombre").textContent = sesion.fullName;
  document.getElementById("usuarioRol").textContent = etiquetaRol(sesion.role);

  document.querySelectorAll("#navMenu [data-rol]").forEach(el => {
    const rolesPermitidos = el.dataset.rol.split(",");
    el.style.display = rolesPermitidos.includes(sesion.role) ? "" : "none";
  });

  document.getElementById("campoInventarioTenant").style.display = sesion.role === "SUPER_ADMIN" ? "" : "none";
  document.getElementById("filtroTenantInventarioWrap").style.display = sesion.role === "SUPER_ADMIN" ? "" : "none";

  const primeraSeccionVisible = Array.from(document.querySelectorAll("#navMenu [data-seccion]"))
    .find(a => a.parentElement.style.display !== "none");
  mostrarSeccion(primeraSeccionVisible ? primeraSeccionVisible.dataset.seccion : "eventos");
}

function etiquetaRol(rol) {
  return { SUPER_ADMIN: "Super Administrador", ADMIN_PRIVADA: "Administrador de Privada",
    RESIDENTE: "Residente", GUARDIA: "Guardia" }[rol] || rol;
}

function mostrarSeccion(nombre) {
  document.querySelectorAll(".seccion").forEach(s => s.classList.remove("activa"));
  document.getElementById(`seccion-${nombre}`)?.classList.add("activa");
  document.querySelectorAll("#navMenu .nav-link").forEach(a => a.classList.toggle("activo", a.dataset.seccion === nombre));
  document.getElementById("tituloSeccion").textContent = TITULOS_SECCION[nombre] || "";

  if (nombre === "dashboard") cargarDashboard();
  if (nombre === "mis-qrs") cargarMisQrs();
  if (nombre === "casas") cargarCasas();
  if (nombre === "residentes") cargarResidentes();
  if (nombre === "dispositivos") cargarDispositivos();
  if (nombre === "inventario") cargarInventario();
  if (nombre === "eventos") cargarEventos();
  if (nombre === "tenants") cargarTenants();
}

function notificar(mensaje, esError = false) {
  const contenedor = document.getElementById("toastContainer");
  const div = document.createElement("div");
  div.className = `toast align-items-center text-white ${esError ? "bg-danger" : "bg-success"} border-0`;
  div.innerHTML = `<div class="d-flex"><div class="toast-body">${mensaje}</div>
    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
  contenedor.appendChild(div);
  new bootstrap.Toast(div, { delay: 4000 }).show();
}

// ---------- Dashboard ----------

async function cargarDashboard() {
  const [casas, qrs, eventos] = await Promise.all([
    Api.houses.listar(), Api.accessPasses.listar(), Api.accessEvents.listar(10)
  ]);
  let inventario = [];
  try { inventario = await Api.inventory.listar(); } catch { /* sin permiso, se ignora */ }

  document.getElementById("kpiCasas").textContent = casas.length;
  document.getElementById("kpiResidentes").textContent = casas.reduce((acc, c) => acc + c.totalResidents, 0);
  document.getElementById("kpiQrsActivos").textContent = qrs.filter(p => p.status === "Active").length;
  document.getElementById("kpiInventario").textContent = inventario.length;

  document.getElementById("tablaDashboardEventos").innerHTML = eventos.map(ev => `
    <tr><td>${new Date(ev.timestamp).toLocaleString()}</td><td>${ev.eventType}</td><td>${ev.result}</td><td>${ev.visitorName || "-"}</td></tr>
  `).join("") || `<tr><td colspan="4" class="text-center text-muted py-4">Sin eventos todavía.</td></tr>`;
}

// ---------- Mis QRs (RESIDENTE) ----------

const ETIQUETAS_PASS = { Active: "Activo", Used: "Usado", Expired: "Expirado", Revoked: "Revocado", Disabled: "Deshabilitado" };

async function cargarMisQrs() {
  qrsCache = await Api.accessPasses.listar();
  document.getElementById("tablaMisQrs").innerHTML = qrsCache.map(p => `
    <tr>
      <td>${p.visitorName}</td>
      <td>${p.vehiclePlate || "-"}</td>
      <td>${new Date(p.validFrom).toLocaleString()} — ${new Date(p.validUntil).toLocaleString()}</td>
      <td>${p.currentUses}/${p.maxUses}</td>
      <td><span class="badge badge-pass-${p.status}">${ETIQUETAS_PASS[p.status]}</span></td>
      <td class="text-end">
        ${p.status === "Active" ? `<button class="btn btn-sm btn-outline-danger" onclick="revocarQr('${p.id}')">Revocar</button>` : ""}
      </td>
    </tr>`).join("") || `<tr><td colspan="6" class="text-center text-muted py-4">Aún no has generado ningún QR.</td></tr>`;
}

async function onGenerarQr(e) {
  e.preventDefault();
  try {
    const casas = await Api.houses.listar();
    if (!casas.length) { notificar("No se encontró tu casa asociada.", true); return; }

    const dto = {
      houseId: casas[0].id,
      visitorName: document.getElementById("qrVisitante").value,
      vehiclePlate: document.getElementById("qrPlaca").value || null,
      notes: document.getElementById("qrNotas").value || null,
      validFrom: new Date(document.getElementById("qrDesde").value).toISOString(),
      validUntil: new Date(document.getElementById("qrHasta").value).toISOString(),
      maxUses: Number(document.getElementById("qrUsos").value)
    };
    const creado = await Api.accessPasses.crear(dto);
    bootstrap.Modal.getInstance(document.getElementById("modalNuevoQr")).hide();
    document.getElementById("formNuevoQr").reset();

    document.getElementById("imagenQrGenerado").src = `data:image/png;base64,${creado.qrImageBase64}`;
    document.getElementById("qrGeneradoUsos").textContent = creado.maxUses;
    new bootstrap.Modal(document.getElementById("modalVerQr")).show();

    cargarMisQrs();
  } catch (err) {
    notificar(err.message, true);
  }
}

async function revocarQr(id) {
  if (!confirm("¿Revocar este QR? El visitante ya no podrá usarlo.")) return;
  try {
    await Api.accessPasses.revocar(id);
    notificar("QR revocado.");
    cargarMisQrs();
  } catch (err) { notificar(err.message, true); }
}

// ---------- Escanear (GUARDIA) ----------

let lectorQr = null;

function iniciarCamara() {
  lectorQr = new Html5Qrcode("lectorQr");
  lectorQr.start(
    { facingMode: "environment" },
    { fps: 10, qrbox: 250 },
    (decodedText) => { escanear(decodedText); },
    () => {}
  ).then(() => {
    document.getElementById("btnIniciarCamara").classList.add("d-none");
    document.getElementById("btnDetenerCamara").classList.remove("d-none");
  }).catch(err => notificar("No se pudo acceder a la cámara: " + err, true));
}

function detenerCamara() {
  if (lectorQr) {
    lectorQr.stop().then(() => lectorQr.clear()).catch(() => {});
    lectorQr = null;
  }
  document.getElementById("btnIniciarCamara").classList.remove("d-none");
  document.getElementById("btnDetenerCamara").classList.add("d-none");
}

async function escanear(token) {
  try {
    const resultado = await Api.access.scan(token);
    const box = document.getElementById("resultadoScan");
    const body = document.getElementById("resultadoScanBody");
    box.classList.remove("d-none", "resultado-scan-ok", "resultado-scan-error");

    if (resultado.isAuthorized) {
      box.classList.add("resultado-scan-ok");
      body.innerHTML = `<h4>✅ Acceso autorizado</h4>
        <p><strong>${resultado.visitorName}</strong> — Casa ${resultado.houseNumber}</p>
        <p>${resultado.openCommandSent ? "Comando de apertura enviado correctamente." : "⚠️ No se pudo enviar el comando de apertura (revisa dispositivos)."}</p>`;
    } else {
      box.classList.add("resultado-scan-error");
      body.innerHTML = `<h4>❌ Acceso denegado</h4><p>${resultado.denialReason || "Motivo no especificado."}</p>`;
    }
    document.getElementById("tokenManual").value = "";
  } catch (err) {
    notificar(err.message, true);
  }
}

// ---------- Casas ----------

async function cargarCasas() {
  casasCache = await Api.houses.listar();
  document.getElementById("tablaCasas").innerHTML = casasCache.map(c => `
    <tr><td>${c.number}</td><td>${c.notes || "-"}</td>
      <td><span class="badge badge-estado-${c.status}">${c.status === "Active" ? "Activa" : "Inactiva"}</span></td>
      <td>${c.totalResidents}</td></tr>`).join("");
}

async function onGuardarCasa(e) {
  e.preventDefault();
  try {
    await Api.houses.crear({ number: document.getElementById("casaNumero").value, notes: document.getElementById("casaNotas").value || null });
    bootstrap.Modal.getInstance(document.getElementById("modalCasa")).hide();
    document.getElementById("formCasa").reset();
    notificar("Casa creada.");
    cargarCasas();
  } catch (err) { notificar(err.message, true); }
}

// ---------- Residentes ----------

async function cargarResidentes() {
  const [residentes, casas] = await Promise.all([Api.residents.listar(), Api.houses.listar()]);
  casasCache = casas;
  document.getElementById("residenteCasaId").innerHTML = casas.map(c => `<option value="${c.id}">${c.number}</option>`).join("");

  document.getElementById("tablaResidentes").innerHTML = residentes.map(r => `
    <tr><td>${r.firstName} ${r.lastName}</td><td>${r.houseNumber}</td><td>${r.phone || "-"}</td>
      <td>${r.email || "-"}</td>
      <td><span class="badge badge-estado-${r.status}">${r.status === "Active" ? "Activo" : "Inactivo"}</span></td></tr>`).join("");
}

async function onGuardarResidente(e) {
  e.preventDefault();
  try {
    const dto = {
      houseId: document.getElementById("residenteCasaId").value,
      firstName: document.getElementById("residenteNombre").value,
      lastName: document.getElementById("residenteApellido").value,
      phone: document.getElementById("residenteTelefono").value || null,
      email: document.getElementById("residenteEmail").value || null
    };
    await Api.residents.crear(dto);
    bootstrap.Modal.getInstance(document.getElementById("modalResidente")).hide();
    document.getElementById("formResidente").reset();
    notificar("Residente creado.");
    cargarResidentes();
  } catch (err) { notificar(err.message, true); }
}

// ---------- Puntos de acceso / Dispositivos ----------

async function cargarDispositivos() {
  const [puntos, dispositivos] = await Promise.all([Api.accessPoints.listar(), Api.devices.listar()]);
  accessPointsCache = puntos;

  document.getElementById("tablaAccessPoints").innerHTML = puntos.map(p => `
    <tr><td>${p.name}</td><td>${p.type}</td><td>${p.direction}</td><td>${p.isActive ? "Sí" : "No"}</td></tr>`).join("");

  document.getElementById("devAccessPointId").innerHTML = puntos.map(p => `<option value="${p.id}">${p.name}</option>`).join("");

  document.getElementById("tablaDevices").innerHTML = dispositivos.map(d => `
    <tr><td>${d.name}</td><td>${d.driverType}</td>
      <td>${d.accessPoints.map(ap => `${ap.accessPointName} (canal ${ap.channelNumber})`).join(", ") || "-"}</td></tr>`).join("");
}

async function onGuardarAccessPoint(e) {
  e.preventDefault();
  try {
    await Api.accessPoints.crear({
      name: document.getElementById("apNombre").value,
      type: document.getElementById("apTipo").value,
      direction: document.getElementById("apDireccion").value
    });
    bootstrap.Modal.getInstance(document.getElementById("modalAccessPoint")).hide();
    document.getElementById("formAccessPoint").reset();
    notificar("Punto de acceso creado.");
    cargarDispositivos();
  } catch (err) { notificar(err.message, true); }
}

async function onGuardarDevice(e) {
  e.preventDefault();
  try {
    const device = await Api.devices.crear({
      name: document.getElementById("devNombre").value,
      manufacturer: document.getElementById("devFabricante").value || null,
      model: null,
      ipAddress: document.getElementById("devIp").value || null,
      driverType: document.getElementById("devDriver").value
    });

    const accessPointId = document.getElementById("devAccessPointId").value;
    if (accessPointId) {
      await Api.devices.vincular(device.id, { accessPointId, channelNumber: Number(document.getElementById("devCanal").value) });
    }

    bootstrap.Modal.getInstance(document.getElementById("modalDevice")).hide();
    document.getElementById("formDevice").reset();
    notificar("Dispositivo creado y vinculado.");
    cargarDispositivos();
  } catch (err) { notificar(err.message, true); }
}

// ---------- Inventario ----------

const ETIQUETAS_CATEGORIA = {
  GateMotor: "Motor de portón", Barrier: "Pluma / Barrera", HikvisionPanel: "Panel de control",
  Camera: "Cámara", Reader: "Lector", Relay: "Relay", NetworkEquipment: "Equipo de red",
  Ups: "UPS", Cabling: "Cableado", Other: "Otro"
};
const ETIQUETAS_INV_ESTADO = { Active: "Activo", InRepair: "En reparación", InStorage: "En almacén", Decommissioned: "Dado de baja" };

async function cargarInventario() {
  const sesion = Sesion.obtener();
  const esSuperAdmin = sesion.role === "SUPER_ADMIN";

  if (esSuperAdmin && tenantsCache.length === 0) {
    tenantsCache = await Api.tenants.listar();
    document.getElementById("selectorTenantInventario").innerHTML =
      `<option value="">Todas las privadas</option>` + tenantsCache.map(t => `<option value="${t.id}">${t.name}</option>`).join("");
    document.getElementById("inventarioTenantId").innerHTML = tenantsCache.map(t => `<option value="${t.id}">${t.name}</option>`).join("");
  }

  const tenantIdFiltro = esSuperAdmin ? document.getElementById("selectorTenantInventario").value : null;
  inventarioCache = await Api.inventory.listar(tenantIdFiltro || null);

  document.querySelectorAll('[data-rol-col="SUPER_ADMIN"]').forEach(th => th.style.display = esSuperAdmin ? "" : "none");

  document.getElementById("tablaInventario").innerHTML = inventarioCache.map(i => `
    <tr>
      ${esSuperAdmin ? `<td>${i.tenantName}</td>` : ""}
      <td>${i.name}</td>
      <td>${ETIQUETAS_CATEGORIA[i.category]}</td>
      <td>${[i.brand, i.model].filter(Boolean).join(" / ") || "-"}</td>
      <td>${i.serialNumber || "-"}</td>
      <td><span class="badge badge-inv-${i.status}">${ETIQUETAS_INV_ESTADO[i.status]}</span></td>
      <td>${i.location || "-"}</td>
      <td class="text-end">
        <button class="btn btn-sm btn-outline-primary" onclick="abrirModalInventarioEditar('${i.id}')">Editar</button>
        <button class="btn btn-sm btn-outline-danger" onclick="eliminarInventario('${i.id}')">Eliminar</button>
      </td>
    </tr>`).join("") || `<tr><td colspan="8" class="text-center text-muted py-4">Sin equipo registrado todavía.</td></tr>`;
}

function prepararModalInventarioNuevo() {
  document.getElementById("formInventario").reset();
  document.getElementById("inventarioId").value = "";
  document.getElementById("tituloModalInventario").textContent = "Nuevo ítem de inventario";
  document.getElementById("campoInventarioEstado").style.display = "none";
}

function abrirModalInventarioEditar(id) {
  const item = inventarioCache.find(i => i.id === id);
  if (!item) return;

  document.getElementById("tituloModalInventario").textContent = "Editar ítem de inventario";
  document.getElementById("inventarioId").value = item.id;
  document.getElementById("inventarioCategoria").value = item.category;
  document.getElementById("inventarioNombre").value = item.name;
  document.getElementById("inventarioMarca").value = item.brand || "";
  document.getElementById("inventarioModelo").value = item.model || "";
  document.getElementById("inventarioSerie").value = item.serialNumber || "";
  document.getElementById("inventarioUbicacion").value = item.location || "";
  document.getElementById("inventarioFechaInstalacion").value = item.installationDate ? item.installationDate.substring(0, 10) : "";
  document.getElementById("inventarioFechaCompra").value = item.purchaseDate ? item.purchaseDate.substring(0, 10) : "";
  document.getElementById("inventarioGarantia").value = item.warrantyUntil ? item.warrantyUntil.substring(0, 10) : "";
  document.getElementById("inventarioProveedor").value = item.provider || "";
  document.getElementById("inventarioCosto").value = item.cost || "";
  document.getElementById("inventarioNotas").value = item.notes || "";
  document.getElementById("inventarioEstado").value = item.status;
  document.getElementById("campoInventarioEstado").style.display = "";
  if (document.getElementById("inventarioTenantId").options.length) document.getElementById("inventarioTenantId").value = item.tenantId;

  new bootstrap.Modal(document.getElementById("modalInventario")).show();
}

async function onGuardarInventario(e) {
  e.preventDefault();
  const id = document.getElementById("inventarioId").value;
  const base = {
    category: document.getElementById("inventarioCategoria").value,
    name: document.getElementById("inventarioNombre").value,
    brand: document.getElementById("inventarioMarca").value || null,
    model: document.getElementById("inventarioModelo").value || null,
    serialNumber: document.getElementById("inventarioSerie").value || null,
    location: document.getElementById("inventarioUbicacion").value || null,
    installationDate: document.getElementById("inventarioFechaInstalacion").value || null,
    purchaseDate: document.getElementById("inventarioFechaCompra").value || null,
    warrantyUntil: document.getElementById("inventarioGarantia").value || null,
    provider: document.getElementById("inventarioProveedor").value || null,
    cost: document.getElementById("inventarioCosto").value ? Number(document.getElementById("inventarioCosto").value) : null,
    notes: document.getElementById("inventarioNotas").value || null,
    linkedDeviceId: null
  };

  try {
    if (id) {
      await Api.inventory.actualizar(id, { ...base, status: document.getElementById("inventarioEstado").value });
    } else {
      const tenantSelect = document.getElementById("inventarioTenantId");
      await Api.inventory.crear({ ...base, tenantId: tenantSelect.options.length ? tenantSelect.value : null });
    }
    bootstrap.Modal.getInstance(document.getElementById("modalInventario")).hide();
    notificar("Inventario guardado correctamente.");
    cargarInventario();
  } catch (err) { notificar(err.message, true); }
}

async function eliminarInventario(id) {
  if (!confirm("¿Eliminar este ítem del inventario?")) return;
  try {
    await Api.inventory.eliminar(id);
    notificar("Ítem eliminado.");
    cargarInventario();
  } catch (err) { notificar(err.message, true); }
}

// ---------- Eventos ----------

async function cargarEventos() {
  const eventos = await Api.accessEvents.listar(100);
  document.getElementById("tablaEventos").innerHTML = eventos.map(ev => `
    <tr>
      <td>${new Date(ev.timestamp).toLocaleString()}</td>
      <td>${ev.eventType}</td>
      <td>${ev.result}</td>
      <td>${ev.visitorName || "-"}</td>
      <td>${ev.accessPointName || "-"}</td>
      <td>${ev.deviceName || "-"}</td>
      <td class="small text-muted">${ev.metadata || "-"}</td>
    </tr>`).join("") || `<tr><td colspan="7" class="text-center text-muted py-4">Sin eventos todavía.</td></tr>`;
}

// ---------- Tenants (SUPER_ADMIN) ----------

async function cargarTenants() {
  tenantsCache = await Api.tenants.listar();
  document.getElementById("tablaTenants").innerHTML = tenantsCache.map(t => `
    <tr><td>${t.name}</td><td>${t.slug}</td><td>${t.isActive ? "Sí" : "No"}</td><td>${t.totalHouses}</td></tr>`).join("");
}

async function onGuardarTenant(e) {
  e.preventDefault();
  try {
    await Api.tenants.crear({ name: document.getElementById("tenantNombre").value, slug: document.getElementById("tenantSlug").value });
    bootstrap.Modal.getInstance(document.getElementById("modalTenant")).hide();
    document.getElementById("formTenant").reset();
    notificar("Privada creada.");
    cargarTenants();
  } catch (err) { notificar(err.message, true); }
}
