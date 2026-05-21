using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// WebApplicationFactory que arranca la API contra una BD SQLite temporal
/// con schema mínimo y datos de prueba. La BD se elimina al Dispose.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; }

    /// <summary>Hash SHA-256 legacy de "admin123" — el mismo que usa el sistema de escritorio.</summary>
    public static readonly string HashAdmin = ComputarHashLegacy("admin123");

    public ApiFactory()
    {
        DbPath = Path.Combine(Path.GetTempPath(), $"sg_test_{Guid.NewGuid():N}.db");
        CrearEsquemaYSemilla(DbPath);

        // Señal para Program.cs — se lee en el startup antes de Db.Inicializar()
        Environment.SetEnvironmentVariable("SG_TEST_BD", DbPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Arrancar en entorno "Test": deshabilita HSTS + HTTPS redirect
        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("SG_TEST_BD", null);
        try { if (File.Exists(DbPath)) File.Delete(DbPath); } catch { }
    }

    // ─────────────────────────────────────────────────────────────────────
    private static void CrearEsquemaYSemilla(string ruta)
    {
        using var conn = new SqliteConnection($"Data Source={ruta};");
        conn.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            PRAGMA journal_mode=WAL;

            -- Usuarios (subset de columnas requeridas por AuthEndpoints)
            CREATE TABLE IF NOT EXISTS Usuarios (
                Id                    INTEGER PRIMARY KEY AUTOINCREMENT,
                NombreUsuario         TEXT    NOT NULL UNIQUE,
                NombreCompleto        TEXT    NOT NULL DEFAULT '',
                PasswordHash          TEXT    NOT NULL,
                Rol                   TEXT    NOT NULL DEFAULT 'Admin',
                Activo                INTEGER NOT NULL DEFAULT 1,
                IntentosHoy           INTEGER NOT NULL DEFAULT 0,
                BloqueadoHasta        TEXT,
                UltimoAcceso          TEXT,
                RequiereCambioPassword INTEGER NOT NULL DEFAULT 0
            );

            -- Log de auditoría (escrito por AuthEndpoints)
            CREATE TABLE IF NOT EXISTS LogAuditoria (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora TEXT,
                Usuario   TEXT,
                Accion    TEXT,
                Tabla     TEXT,
                Detalle   TEXT
            );

            -- Sesiones JWT (creado también por Db.EnsureSesionesActivas())
            CREATE TABLE IF NOT EXISTS SesionesActivas (
                Jti           TEXT PRIMARY KEY,
                IdUsuario     INTEGER NOT NULL,
                NombreUsuario TEXT    NOT NULL,
                IpEmision     TEXT,
                FechaEmision  TEXT    NOT NULL,
                FechaExpira   TEXT    NOT NULL,
                FechaRevoca   TEXT
            );

            -- Inventario (requerido por /api/inventario y /api/dashboard)
            CREATE TABLE IF NOT EXISTS Inventario (
                Codigo         TEXT PRIMARY KEY,
                Descripcion    TEXT NOT NULL DEFAULT '',
                Departamento   TEXT DEFAULT '',
                Unidad         TEXT DEFAULT 'Unidad',
                Existencia     REAL DEFAULT 0,
                ExistenMin     REAL DEFAULT 0,
                ExistenMax     REAL DEFAULT 0,
                Precio         REAL DEFAULT 0,
                CostoActual    REAL DEFAULT 0,
                UltimaActualz  TEXT,
                Activo         INTEGER DEFAULT 1
            );

            -- Movimientos de inventario (requerido por /api/kardex/b, /api/kardex/a y /api/inventario/{codigo})
            CREATE TABLE IF NOT EXISTS MovimientosInventario (
                IdMovimiento      INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora         TEXT    NOT NULL,
                CodigoProducto    TEXT    NOT NULL,
                TipoMovimiento    TEXT    NOT NULL,
                Concepto          TEXT    DEFAULT '',
                Cantidad          REAL    DEFAULT 0,
                SaldoAnterior     REAL    DEFAULT 0,
                NuevoSaldo        REAL    DEFAULT 0,
                CostoUnitario     REAL    DEFAULT 0,
                Referencia        TEXT    DEFAULT '',
                Usuario           TEXT    DEFAULT '',
                TipoKardex        TEXT    DEFAULT 'PRODUCTO'
            );

            -- Clientes (requerido por /api/clientes)
            CREATE TABLE IF NOT EXISTS Clientes (
                Codigo      TEXT PRIMARY KEY,
                Descripcion TEXT NOT NULL DEFAULT '',
                CiRif       TEXT DEFAULT '',
                Telefono    TEXT DEFAULT '',
                Ciudad      TEXT DEFAULT ''
            );

            -- Facturas (requerido por /api/dashboard y /api/clientes/{codigo})
            CREATE TABLE IF NOT EXISTS Facturas (
                NumeroF   TEXT PRIMARY KEY,
                Fecha     TEXT NOT NULL,
                Cliente   TEXT DEFAULT '',
                TotalF    REAL DEFAULT 0,
                Estado    TEXT DEFAULT 'Activa'
            );

            -- Detalles de factura (requerido por /api/clientes/{codigo})
            CREATE TABLE IF NOT EXISTS DetallesFactura (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroF       TEXT,
                CodigoProducto TEXT,
                Descripcion   TEXT DEFAULT '',
                Cantidad      REAL DEFAULT 0,
                PrecioUnit    REAL DEFAULT 0,
                Total         REAL DEFAULT 0
            );

            -- Cuentas por cobrar (requerido por /api/dashboard y /api/clientes/{codigo})
            CREATE TABLE IF NOT EXISTS CuentasCobrar (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroF     TEXT,
                Cliente     TEXT DEFAULT '',
                Monto       REAL DEFAULT 0,
                Pagado      REAL DEFAULT 0,
                FechaVence  TEXT,
                Estado      TEXT DEFAULT 'Pendiente'
            );

            -- Tasa de cambio (requerido por /api/dashboard)
            CREATE TABLE IF NOT EXISTS TasaCambio (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Fecha        TEXT NOT NULL,
                TasaBCV      REAL DEFAULT 0,
                TasaParalelo REAL DEFAULT 0,
                Origen       TEXT DEFAULT 'MANUAL',
                Usuario      TEXT DEFAULT ''
            );

            -- Productos por proveedor (requerido por /api/dashboard alertas)
            CREATE TABLE IF NOT EXISTS ProductosProveedor (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo       TEXT DEFAULT '',
                Descripcion  TEXT DEFAULT '',
                StockMinimo  REAL DEFAULT 0,
                StockActual  REAL DEFAULT 0
            );
        ";
        cmd.ExecuteNonQuery();

        // ── Semilla ──────────────────────────────────────────────────────
        // Usuario admin con hash SHA-256 legacy de 'admin123'
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Usuarios
                (NombreUsuario, NombreCompleto, PasswordHash, Rol, Activo)
            VALUES ('admin', 'Administrador', $hash, 'Admin', 1);

            INSERT OR IGNORE INTO Inventario
                (Codigo, Descripcion, Unidad, Existencia, ExistenMin, ExistenMax, Precio, CostoActual)
            VALUES ('PROD-001', 'Producto de prueba', 'Kg', 100, 10, 500, 25.50, 18.00);

            INSERT OR IGNORE INTO Clientes
                (Codigo, Descripcion, CiRif, Telefono, Ciudad)
            VALUES ('CLI-001', 'Cliente de prueba', 'J-12345678-9', '0414-1234567', 'Caracas');

            INSERT OR IGNORE INTO TasaCambio
                (Fecha, TasaBCV, TasaParalelo, Origen, Usuario)
            VALUES (date('now'), 36.50, 40.00, 'MANUAL', 'admin');
        ";
        cmd.Parameters.AddWithValue("$hash", HashAdmin);
        cmd.ExecuteNonQuery();
    }

    private static string ComputarHashLegacy(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "SG_SALT_2024");
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLower();
    }
}
