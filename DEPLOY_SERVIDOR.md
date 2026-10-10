# Guía de instalación — AccessControl SaaS en servidor

Este documento es para quien (persona o IA) vaya a instalar y levantar este proyecto
en el servidor de producción. Contexto actual: Cloudflare Tunnel ya está configurado
para enrutar `tecnoempresarial.gnatsoluciones.com` hacia `http://10.10.20.201:5460`
(la VM de Maximiliano). Falta: instalar dependencias, base de datos, publicar la app
y dejarla corriendo en el puerto **5460**.

## 1. Qué es este proyecto

- **Backend**: API ASP.NET Core 8 (`src/AccessControl.Api`), arquitectura en capas
  (Domain / Application / Infrastructure / Api). Usa Entity Framework Core sobre
  **PostgreSQL**, autenticación JWT, SignalR (`/hubs/agentes`) y Swagger (solo en
  Development).
- **Frontend**: Angular 18 (`frontend-angular/`). **Ya viene compilado** y copiado
  dentro de `src/AccessControl.Api/wwwroot/`. La propia API sirve ese build estático
  (`app.UseStaticFiles()` + `MapFallbackToFile("index.html")`) — front y back están
  en el mismo origen, por eso `environment.ts` usa `apiBaseUrl: '/api'` (ruta
  relativa, sin dominio ni puerto propio).
- Hay una carpeta `frontend/` (HTML/CSS/JS plano) que es un prototipo **legado**,
  reemplazado por el Angular. No hace falta desplegarla.
- Hay un proyecto `src/AccessControl.Agent`: es un **cliente** que corre en otra
  máquina (ej. la PC de un guardia, lector de accesos) y se conecta al backend vía
  HTTP/SignalR. **No se instala en este servidor.**

En resumen, en el servidor solo hace falta correr **una cosa**: la API de
`AccessControl.Api`, que ya sirve el frontend integrado.

## 2. Requisitos a instalar en el servidor

1. **.NET 8 Runtime (ASP.NET Core Runtime)** — no hace falta el SDK completo si vas
   a publicar en otra máquina y solo copiar el resultado, pero si vas a compilar
   directamente en el servidor instala el **SDK 8.0**.
   - Ubuntu/Debian:
     ```bash
     wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
     sudo dpkg -i packages-microsoft-prod.deb
     sudo apt-get update
     sudo apt-get install -y dotnet-sdk-8.0
     ```
2. **PostgreSQL** (13+; cualquier versión reciente sirve).
   ```bash
   sudo apt-get install -y postgresql postgresql-contrib
   sudo systemctl enable --now postgresql
   ```
3. **Node.js 18+ y npm** — solo si necesitas **recompilar** el frontend Angular (por
   ejemplo si hay cambios de código). Si el `wwwroot/` que llega con el proyecto ya
   está actualizado, puedes saltarte esto.
4. **git** (si vas a clonar el repo) o simplemente copiar la carpeta del proyecto al
   servidor.

## 3. Base de datos

Crear el usuario y la base que espera la app (ver `ConnectionStrings:Default` en
`src/AccessControl.Api/appsettings.json`):

```bash
sudo -u postgres psql <<'EOF'
CREATE USER postgres WITH PASSWORD 'postgres123';
CREATE DATABASE accesscontrol OWNER postgres;
EOF
```

> Si en el servidor ya existe un rol `postgres` con otra contraseña, es más simple
> ajustar la contraseña de ese rol o cambiar el `ConnectionStrings:Default` en el
> paso 5 para que coincida (usuario/clave/host/puerto/nombre de BD), en vez de pelear
> por igualar el valor por defecto.

**No hace falta correr migraciones a mano.** La app las aplica sola al arrancar:
`Program.cs` llama a `DbSeeder.SeedAsync`, que ejecuta `db.Database.MigrateAsync()`
y siembra usuarios iniciales (ver sección 6).

## 4. Copiar el proyecto al servidor

Copia toda la carpeta `AccessControlSaaS/` al servidor (por ejemplo a
`/opt/accesscontrol/src`), o clónala si está en un repositorio git accesible desde ahí.

## 5. Configuración antes de publicar (IMPORTANTE — seguridad)

Editar `src/AccessControl.Api/appsettings.json` (o mejor, sobreescribir por variables
de entorno en el servicio systemd, ver paso 8, para no dejar secretos en el repo):

- `ConnectionStrings:Default` → ajustar host/usuario/clave/base si es distinto al
  valor por defecto.
- `Jwt:Key` → **cambiar** el valor de ejemplo por uno propio, largo y aleatorio, antes
  de exponer esto a internet.
- `Security:TokenPepper` → igual, cambiar por un valor propio.
- `Seed:*` (emails/contraseñas de superadmin, admin, residente, guardia de ejemplo)
  → estas son las cuentas que se crean automáticamente la primera vez que arranca
  contra una base vacía. Cambia las contraseñas o bórralas de la config si no las
  quieres en producción.
- `Cors:OrigenesPermitidos` → agregar el dominio real:
  ```json
  "Cors": {
    "OrigenesPermitidos": [ "https://tecnoempresarial.gnatsoluciones.com" ]
  }
  ```
  (los `localhost` de ahí son solo para desarrollo local; no molestan si los dejas,
  pero agrega el dominio de producción o el login desde el navegador fallará por CORS).

## 6. Si necesitas recompilar el frontend

Solo si hiciste cambios en `frontend-angular/` o el `wwwroot/` no está actualizado:

```bash
cd frontend-angular
npm install
npm run build
# el build queda en dist/frontend-angular/browser
rm -rf ../src/AccessControl.Api/wwwroot/*
cp -r dist/frontend-angular/browser/* ../src/AccessControl.Api/wwwroot/
```

Si el `wwwroot/` que ya trae el proyecto está al día, sáltate este paso.

## 7. Publicar y correr la API

```bash
cd src/AccessControl.Api
dotnet publish -c Release -o /opt/accesscontrol/publish
```

Esto genera el binario listo para correr en `/opt/accesscontrol/publish`
(incluye el `wwwroot/` compilado).

Probar en caliente, en el puerto que espera Cloudflare (**5460**):

```bash
cd /opt/accesscontrol/publish
ASPNETCORE_URLS="http://0.0.0.0:5460" ASPNETCORE_ENVIRONMENT=Production dotnet AccessControl.Api.dll
```

Si todo va bien deberías poder entrar por `http://10.10.20.201:5460` y ver el
frontend, y `http://10.10.20.201:5460/swagger` **no** debería estar disponible
(Swagger solo se activa en Development, correcto para producción).

## 8. Dejarlo corriendo siempre (systemd)

Crear `/etc/systemd/system/accesscontrol.service`:

```ini
[Unit]
Description=AccessControl SaaS API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/accesscontrol/publish
ExecStart=/usr/bin/dotnet /opt/accesscontrol/publish/AccessControl.Api.dll
Restart=always
RestartSec=5
User=www-data
Environment=ASPNETCORE_URLS=http://0.0.0.0:5460
Environment=ASPNETCORE_ENVIRONMENT=Production
# Opcional: sobreescribir secretos sin tocar appsettings.json, ej.:
# Environment=ConnectionStrings__Default=Host=localhost;Port=5432;Database=accesscontrol;Username=postgres;Password=CAMBIAR
# Environment=Jwt__Key=CAMBIAR_POR_ALGO_LARGO_Y_ALEATORIO
# Environment=Security__TokenPepper=CAMBIAR

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now accesscontrol
sudo systemctl status accesscontrol
```

## 9. Verificación final

- `curl -I http://10.10.20.201:5460` → debe responder 200 y servir el `index.html`
  del Angular.
- Abrir `https://tecnoempresarial.gnatsoluciones.com` desde afuera (el túnel de
  Cloudflare ya está apuntando bien, según infra) y probar login con alguna de las
  cuentas sembradas (ver `Seed` en `appsettings.json`, ej. `superadmin@accesscontrol.local`
  con la contraseña configurada ahí).
- Revisar logs si algo falla: `journalctl -u accesscontrol -f`.

## 10. Notas / pendientes conocidos

- Las contraseñas y la JWT key en `appsettings.json` son valores de ejemplo del
  desarrollo — **cambiarlas antes de dejar esto expuesto en internet** de forma
  permanente.
- `AccessControl.Agent` (cliente de lector de accesos) no requiere nada en este
  servidor; solo necesita saber la URL pública (`https://tecnoempresarial.gnatsoluciones.com`)
  y un `AgentApiKey` válido cuando se instale en la máquina del guardia/lector.
- La carpeta `frontend/` (HTML/CSS/JS suelto) es un prototipo viejo, no se despliega.
