# AppaRently

Plataforma de rentas cortas construida en ASP.NET Core sobre .NET 10, con tres frontends MVC y una capa de infraestructura compartida para persistencia, seeding y reglas de negocio.

## Requisitos previos

- Docker y Docker Compose
- .NET 10 SDK, solo si quieres ejecutar fuera de contenedores
- PostgreSQL, solo si quieres levantarlo manualmente fuera de Docker

## Levantar el entorno

```bash
docker compose up --build
```

Ese comando levanta:

- Base de datos PostgreSQL
- Portal cliente en `http://localhost:5001`
- Portal owner en `http://localhost:5002`
- Portal superadmin en `http://localhost:5003`

## Cuentas seed

El arranque ejecuta migraciones y seed automáticamente. Si no existen datos, crea:

- Super admin: `superadmin@apparently.local`
- Owner 1: `lukreroll1@gmail.com`
- Owner 2: `lukreroll2@gmail.com`
- Password por defecto: `AppaRently123!`

Además, cada owner recibe 3 apartments de ejemplo.

## Arquitectura

- `AppaRently.Domain`: entidades del dominio y reglas de soft delete
- `AppaRently.App`: contratos, DTOs e interfaces de servicios
- `AppaRently.Infrastructure`: EF Core, migraciones, repositorios/servicios, seeders y notificaciones
- `AppaRently.Web`: portal cliente
- `AppaRently.Web.Owner`: portal de propietarios
- `AppaRently.Web.SuperAdmin`: portal administrativo

La app usa un solo esquema de datos compartido y tres UI separadas, con inicialización común desde `SeedAppaRentlyAsync()`.

## Decisiones técnicas relevantes

- Disponibilidad estricta: las reservas se validan contra solapamientos y los filtros de búsqueda por rango excluyen apartments ocupados.
- Horarios estándar: toda reserva se normaliza a `02:00 PM` para check-in y `12:00 PM` para check-out.
- Soft delete con cascada: al eliminar client, apartment u owner se archivan sus dependencias para mantener consistencia histórica.
- Notificaciones dentro de la app: cada portal muestra inbox, conteo de no leídas y acciones para marcar leído.
- Métricas de negocio: owner y superadmin exponen revenue, revenue potencial, occupancy, profitability, average reservation value, average stay y tenants únicos, con periodo seleccionable.
- Docker: todo el stack se arranca con un solo comando y usa PostgreSQL como servicio dedicado.

## Rutas útiles

- Cliente: catálogo, favoritos, reservas y notificaciones
- Owner: dashboard, inventario, exportación Excel y notificaciones
- SuperAdmin: usuarios, apartments, métricas y notificaciones

Hecho por:
Andrés Hidrobo