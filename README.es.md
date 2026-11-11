# Employee CRUD API

[English](README.md) | [Español](README.es.md)

API REST para gestionar empleados, desarrollada con .NET 10, ASP.NET Core Controllers, Entity Framework Core y SQLite.

## Funcionalidades

- Crear, consultar, actualizar y eliminar empleados.
- Campos del empleado: ID, nombre, email y departamento.
- Validación de entrada y direcciones de email únicas.
- Listado de empleados con paginación.
- Respuestas de error estructuradas mediante Problem Details.
- Swagger UI y documentación OpenAPI en Development.
- Pruebas de integración que cubren operaciones CRUD, validación, emails duplicados, registros inexistentes, paginación y documentación de la API.

## Estructura del proyecto

- `src/EmployeeCrud.Api`: configuración de la API, endpoints de empleados, DTOs de entrada y salida y contexto de base de datos.
- `tests/EmployeeCrud.Tests`: pruebas de integración HTTP con una base de datos SQLite aislada para cada prueba.

## Primeros pasos

Requiere el SDK de .NET 10. Ejecuta estos comandos desde la raíz del repositorio:

```powershell
dotnet restore
dotnet test
dotnet run --project src/EmployeeCrud.Api
```

El perfil de inicio HTTP predeterminado ejecuta la API en `http://localhost:5050`, en el entorno Development. Los IDE que admiten el perfil de inicio abren Swagger automáticamente. Si utilizas `dotnet run`, abre [Swagger UI](http://localhost:5050/swagger) manualmente.

El [documento OpenAPI](http://localhost:5050/openapi/v1.json) y Swagger UI solo están disponibles en Development. También hay solicitudes de ejemplo en `src/EmployeeCrud.Api/EmployeeCrud.Api.http`.

## Base de datos

SQLite guarda los datos en `src/EmployeeCrud.Api/employees.db` de forma predeterminada. La base de datos se crea automáticamente al iniciar la aplicación y los datos se conservan entre reinicios. No requiere un servidor de base de datos independiente ni instalar Docker.

Para cambiar la cadena de conexión, configura `ConnectionStrings:Employees` o utiliza la variable de entorno `ConnectionStrings__Employees`.

La inicialización del esquema utiliza `EnsureCreatedAsync`; no aplica cambios de esquema a una base de datos existente. Para borrar los datos locales, detén la API y elimina el archivo de base de datos y sus posibles archivos asociados `-wal` y `-shm`.

## Endpoints

| Operación | Endpoint | Respuestas |
| --- | --- | --- |
| Crear | POST `/api/employees` | 201 con cuerpo de respuesta y cabecera Location; 400 o 409 |
| Listar | GET `/api/employees?page=1&pageSize=20` | 200 con items, page, pageSize y total; 400 |
| Consultar | GET `/api/employees/{id}` | 200 o 404 |
| Reemplazar campos editables | PUT `/api/employees/{id}` | 204, 400, 404 o 409 |
| Eliminar | DELETE `/api/employees/{id}` | 204 o 404 |

El nombre y el departamento son obligatorios y están limitados a 100 caracteres. El email es obligatorio, se valida, está limitado a 254 caracteres, se guarda en minúsculas y se eliminan sus espacios iniciales y finales. Un índice único en la base de datos evita emails duplicados. La base de datos genera los identificadores de los empleados. El tamaño de página admite entre 1 y 100 elementos y su valor predeterminado es 20.

### Ejemplo

```powershell
Invoke-RestMethod http://localhost:5050/api/employees -Method Post -ContentType 'application/json' -Body '{"name":"Ana Perez","email":"ana@example.com","department":"HR"}'
Invoke-RestMethod http://localhost:5050/api/employees
```
