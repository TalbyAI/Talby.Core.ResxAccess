# Talby.Core.ResxAccess

Solución base con una biblioteca C# y pruebas unitarias para .NET 10.

- `src/Talby.Core.ResxAccess`: biblioteca principal con `Metalama.Framework` 2026.1.28.
- `tests/Talby.Core.ResxAccess.Tests`: pruebas con xUnit 2.9.3 y `Metalama.Testing.UnitTesting` 2026.1.28; referencia la biblioteca principal.

## Requisitos

SDK .NET 10.0.401 o un parche posterior de la misma banda 10.0.4xx,
según `global.json`, y acceso a NuGet.org para restaurar los paquetes.

## Compilar y ejecutar las pruebas

Desde la raíz del repositorio:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

La prueba inicial crea una compilación con Metalama y consulta su modelo de código.
Comprueba la configuración de pruebas; la biblioteca aún no contiene funcionalidad.

Metalama está desactivado en el proyecto de pruebas. La biblioteca conserva el
código de compilación para permitir probar sus helpers, siguiendo la
[documentación de Metalama](https://doc.metalama.net/conceptual/aspects/testing/compile-time-testing).
