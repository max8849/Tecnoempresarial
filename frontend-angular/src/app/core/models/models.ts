// Roles tal cual los emite el backend (AppRoles en AccessControl.Infrastructure.Identity).
export type Rol = 'SUPER_ADMIN' | 'ADMIN_PRIVADA' | 'RESIDENTE' | 'GUARDIA' | 'OPERADOR';

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  email: string;
  fullName: string;
  role: Rol;
  tenantId: string | null;
}

export interface Sesion extends LoginResponse {}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  totalHouses: number;
}

export interface House {
  id: string;
  number: string;
  notes: string | null;
  status: 'Active' | 'Inactive';
  totalResidents: number;
  tenantId: string;
  tenantName: string;
}

export interface Resident {
  id: string;
  houseId: string;
  houseNumber: string;
  firstName: string;
  lastName: string;
  phone: string | null;
  email: string | null;
  status: 'Active' | 'Inactive';
  startDate: string;
  endDate: string | null;
  tenantId: string;
  tenantName: string;
}

export interface UsuarioCuenta {
  id: string;
  email: string;
  fullName: string;
  role: Rol;
  tenantId: string | null;
  tenantName: string;
  casa: string | null;
  residentId: string | null;
  activo: boolean;
}

export interface ResidenteSinCuenta {
  id: string;
  nombreCompleto: string;
  casa: string;
  tenantId: string;
  tenantName: string;
}

export interface Vehicle {
  id: string;
  houseId: string;
  plate: string;
  brand: string | null;
  model: string | null;
  color: string | null;
}

export type AccessPassStatus = 'Active' | 'Used' | 'Expired' | 'Revoked' | 'Disabled';

export interface AccessPass {
  id: string;
  houseId: string;
  houseNumber: string;
  visitorName: string;
  vehiclePlate: string | null;
  validFrom: string;
  validUntil: string;
  maxUses: number;
  currentUses: number;
  status: AccessPassStatus;
  createdAt: string;
  revokedAt: string | null;
  // Solo viene poblado si el pase sigue activo y dentro de su ventana de vigencia.
  qrImageBase64: string | null;
}

export interface AccessPassCreated {
  id: string;
  visitorName: string;
  validFrom: string;
  validUntil: string;
  maxUses: number;
  qrContent: string;
  qrImageBase64: string;
}

export interface AccessPoint {
  id: string;
  name: string;
  type: string;
  direction: string;
  isActive: boolean;
  tenantId: string;
  tenantName: string;
}

export interface DeviceAccessPointLink {
  accessPointId: string;
  accessPointName: string;
  channelNumber: number;
}

export interface Device {
  id: string;
  name: string;
  manufacturer: string | null;
  model: string | null;
  ipAddress: string | null;
  status: string;
  driverType: string;
  tieneCredenciales: boolean;
  accessPoints: DeviceAccessPointLink[];
  tenantId: string;
  tenantName: string;
}

export type InventoryStatus = 'Active' | 'InRepair' | 'InStorage' | 'Decommissioned';

export interface InventoryItem {
  id: string;
  tenantId: string;
  tenantName: string;
  category: string;
  name: string;
  brand: string | null;
  model: string | null;
  serialNumber: string | null;
  status: InventoryStatus;
  location: string | null;
  installationDate: string | null;
  purchaseDate: string | null;
  warrantyUntil: string | null;
  provider: string | null;
  cost: number | null;
  notes: string | null;
  linkedDeviceId: string | null;
  linkedDeviceName: string | null;
}

export interface AccessEvent {
  id: string;
  accessPointName: string | null;
  deviceName: string | null;
  visitorName: string | null;
  eventType: string;
  result: string;
  timestamp: string;
  metadata: string | null;
}

export interface ValidateTokenResponse {
  isValid: boolean;
  denialReason: string | null;
  visitorName: string | null;
  houseNumber: string | null;
  vehiclePlate: string | null;
}

export interface ScanResponse {
  isAuthorized: boolean;
  denialReason: string | null;
  openCommandSent: boolean;
  visitorName: string | null;
  houseNumber: string | null;
  vehiclePlate: string | null;
}
