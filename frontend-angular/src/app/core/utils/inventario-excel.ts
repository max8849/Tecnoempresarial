import { InventoryItem } from '../models/models';

const ETIQUETAS_CATEGORIA: Record<string, string> = {
  GateMotor: 'Motor de portón', Barrier: 'Pluma / Barrera', HikvisionPanel: 'Panel de control',
  Camera: 'Cámara', Reader: 'Lector', Relay: 'Relay', NetworkEquipment: 'Equipo de red',
  Ups: 'UPS', Cabling: 'Cableado', Other: 'Otro'
};
const ETIQUETAS_ESTADO: Record<string, string> = {
  Active: 'Activo', InRepair: 'En reparación', InStorage: 'En almacén', Decommissioned: 'Dado de baja'
};

const COLOR_ENCABEZADO = 'FF4F46E5';

function fecha(valor: string | null): Date | null {
  if (!valor) return null;
  const [y, m, d] = valor.substring(0, 10).split('-').map(Number);
  return new Date(y, m - 1, d);
}

function nombreHoja(valor: string): string {
  return valor.replace(/[\[\]:*?\/\\]/g, ' ').trim().substring(0, 31) || 'Hoja';
}

function filas(items: InventoryItem[], incluirPrivada: boolean) {
  return items.map(i => ({
    ...(incluirPrivada ? { privada: i.tenantName } : {}),
    nombre: i.name,
    categoria: ETIQUETAS_CATEGORIA[i.category] ?? i.category,
    marca: i.brand ?? '',
    modelo: i.model ?? '',
    serie: i.serialNumber ?? '',
    estado: ETIQUETAS_ESTADO[i.status] ?? i.status,
    ubicacion: i.location ?? '',
    proveedor: i.provider ?? '',
    costo: i.cost,
    instalacion: fecha(i.installationDate),
    compra: fecha(i.purchaseDate),
    garantia: fecha(i.warrantyUntil),
    dispositivo: i.linkedDeviceName ?? '',
    notas: i.notes ?? ''
  }));
}

function columnas(incluirPrivada: boolean) {
  const base = [
    { header: 'Nombre', key: 'nombre', width: 30 },
    { header: 'Categoría', key: 'categoria', width: 22 },
    { header: 'Marca', key: 'marca', width: 16 },
    { header: 'Modelo', key: 'modelo', width: 16 },
    { header: 'N° de serie', key: 'serie', width: 20 },
    { header: 'Estado', key: 'estado', width: 16 },
    { header: 'Ubicación', key: 'ubicacion', width: 24 },
    { header: 'Proveedor', key: 'proveedor', width: 22 },
    { header: 'Costo', key: 'costo', width: 14, style: { numFmt: '#,##0.00' } },
    { header: 'Fecha de instalación', key: 'instalacion', width: 20, style: { numFmt: 'dd/mm/yyyy' } },
    { header: 'Fecha de compra', key: 'compra', width: 18, style: { numFmt: 'dd/mm/yyyy' } },
    { header: 'Garantía hasta', key: 'garantia', width: 18, style: { numFmt: 'dd/mm/yyyy' } },
    { header: 'Dispositivo vinculado', key: 'dispositivo', width: 24 },
    { header: 'Notas', key: 'notas', width: 36 }
  ];
  return incluirPrivada ? [{ header: 'Privada', key: 'privada', width: 26 }, ...base] : base;
}

function hoja(libro: any, nombre: string, items: InventoryItem[], incluirPrivada: boolean) {
  const ws = libro.addWorksheet(nombreHoja(nombre));
  const cols = columnas(incluirPrivada);
  ws.columns = cols.map(c => ({ header: c.header, key: c.key, width: c.width, style: c.style ?? {} }));

  const encabezado = ws.getRow(1);
  encabezado.font = { bold: true, color: { argb: 'FFFFFFFF' } };
  encabezado.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: COLOR_ENCABEZADO } };
  encabezado.alignment = { vertical: 'middle' };
  encabezado.height = 22;

  ws.addRows(filas(items, incluirPrivada));
  ws.views = [{ state: 'frozen', ySplit: 1 }];
  ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: cols.length } };
  return ws;
}

export async function exportarInventarioExcel(items: InventoryItem[], mostrarPorPrivada: boolean): Promise<void> {
  const { default: ExcelJS } = await import('exceljs');
  const libro = new ExcelJS.Workbook();
  libro.creator = 'Portal Tecnoempresarial';
  libro.created = new Date();

  const ordenados = [...items].sort((a, b) =>
    a.tenantName.localeCompare(b.tenantName, 'es') ||
    (ETIQUETAS_CATEGORIA[a.category] ?? '').localeCompare(ETIQUETAS_CATEGORIA[b.category] ?? '', 'es') ||
    a.name.localeCompare(b.name, 'es'));

  hoja(libro, 'Inventario general', ordenados, true);

  if (mostrarPorPrivada) {
    const privadas = [...new Set(ordenados.map(i => i.tenantName))];
    for (const privada of privadas) {
      hoja(libro, privada, ordenados.filter(i => i.tenantName === privada), false);
    }
  }

  const buffer = await libro.xlsx.writeBuffer();
  const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  const url = URL.createObjectURL(blob);
  const hoy = new Date().toISOString().substring(0, 10);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = `inventario-${hoy}.xlsx`;
  enlace.click();
  URL.revokeObjectURL(url);
}
