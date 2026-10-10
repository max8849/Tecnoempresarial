const ETIQUETAS_EVENTO: Record<string, string> = {
  QrScanned: 'QR escaneado',
  AccessAuthorized: 'Acceso autorizado',
  AccessDenied: 'Acceso denegado',
  OpenCommandSent: 'Apertura enviada',
  OpenCommandFailed: 'Apertura fallida',
  ManualOpen: 'Apertura manual',
  PassRevoked: 'QR revocado',
  PassExpired: 'QR vencido'
};

const ETIQUETAS_RESULTADO: Record<string, string> = {
  Authorized: 'Autorizado',
  CommandSent: 'Comando enviado',
  Opened: 'Abierta',
  Failed: 'Fallido',
  Denied: 'Denegado'
};

export function etiquetaEvento(valor: string): string {
  return ETIQUETAS_EVENTO[valor] ?? valor;
}

export function etiquetaResultado(valor: string): string {
  return ETIQUETAS_RESULTADO[valor] ?? valor;
}
