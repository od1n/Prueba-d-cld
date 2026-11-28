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
                Ciudad      TEXT DEFAULT '',
                Direccion   TEXT DEFAULT '',
                Email       TEXT DEFAULT '',
                Contacto    TEXT DEFAULT '',
                AFavor      REAL DEFAULT 0
            );

            -- Facturas (requerido por /api/dashboard, /api/facturas y /api/clientes/{codigo})
            CREATE TABLE IF NOT EXISTS Facturas (
                NumeroF           TEXT PRIMARY KEY,
                Fecha             TEXT NOT NULL,
                Codigo            TEXT DEFAULT '',
                Descuento         REAL DEFAULT 0,
                Transporte        TEXT DEFAULT '',
                Observaciones     TEXT DEFAULT '',
                FormaDePago       TEXT DEFAULT 'Contado',
                Abono             REAL DEFAULT 0,
                IvaF              REAL DEFAULT 0,
                DiasCreditoF      INTEGER DEFAULT 0,
                TotalF            REAL DEFAULT 0,
                TipoDocumento     TEXT DEFAULT 'FacturaConIVA',
                Anulada           INTEGER DEFAULT 0,
                IdTasaCambio      INTEGER DEFAULT 0,
                TasaBCVUsada      REAL DEFAULT 0,
                TasaParaleloUsada REAL DEFAULT 0
            );

            -- Detalles de factura
            CREATE TABLE IF NOT EXISTS DetallesFactura (
                IdFactura  INTEGER,
                Codigo     TEXT,
                Cantidad   REAL DEFAULT 0,
                PrecioUnit REAL DEFAULT 0,
                PRIMARY KEY(IdFactura, Codigo)
            );

            -- Abonos
            CREATE TABLE IF NOT EXISTS Abonos (
                NumAbono          INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroF           TEXT,
                Fecha             TEXT NOT NULL,
                Monto             REAL DEFAULT 0,
                Cliente           TEXT DEFAULT '',
                MetodoPago        TEXT DEFAULT 'Efectivo',
                Referencia        TEXT DEFAULT '',
                NombreRemitente   TEXT DEFAULT '',
                RutaComprobante   TEXT DEFAULT '',
                Observaciones     TEXT DEFAULT '',
                IdCuentaReceptora INTEGER DEFAULT 0,
                Estado            TEXT DEFAULT 'Confirmado',
                FOREIGN KEY(NumeroF) REFERENCES Facturas(NumeroF)
            );

            -- ComprobantesFactura
            CREATE TABLE IF NOT EXISTS ComprobantesFactura (
                Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroF           TEXT NOT NULL,
                MetodoPago        TEXT NOT NULL,
                Monto             REAL DEFAULT 0,
                Referencia        TEXT DEFAULT '',
                RutaImagen        TEXT DEFAULT '',
                IdCuentaReceptora INTEGER DEFAULT 0,
                Fecha             TEXT NOT NULL,
                Observaciones     TEXT DEFAULT ''
            );

            -- CuentasReceptoras
            CREATE TABLE IF NOT EXISTS CuentasReceptoras (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre    TEXT NOT NULL,
                Banco     TEXT NOT NULL,
                Tipo      TEXT NOT NULL,
                Titular   TEXT NOT NULL,
                Documento TEXT NOT NULL,
                Activa    INTEGER DEFAULT 1
            );

            -- Cuentas por cobrar (requerido por /api/dashboard, /api/cxc y /api/clientes/{codigo})
            CREATE TABLE IF NOT EXISTS CuentasCobrar (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Cliente      TEXT NOT NULL DEFAULT '',
                Concepto     TEXT NOT NULL DEFAULT '',
                Monto        REAL NOT NULL DEFAULT 0,
                Pagado       REAL DEFAULT 0,
                FechaEmision TEXT NOT NULL DEFAULT '',
                FechaVence   TEXT NOT NULL DEFAULT '',
                Estado       TEXT NOT NULL DEFAULT 'Vigente'
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

            INSERT OR IGNORE INTO Inventario
                (Codigo, Descripcion, Unidad, Existencia, ExistenMin, ExistenMax, Precio, CostoActual)
            VALUES ('PROD-CXC-001', 'Producto prueba CxC', 'Und', 500, 10, 1000, 50.00, 30.00);

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

[Xunit.CollectionDefinition("ApiIntegration")]
public class ApiIntegrationCollection : Xunit.ICollectionFixture<ApiFactory> { }
