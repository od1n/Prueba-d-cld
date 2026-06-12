import sqlite3, sys, os

db = r"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db"
if not os.path.exists(db):
    print("ERROR: BD no encontrada"); sys.exit(1)

c = sqlite3.connect(db)
c.execute("DROP TABLE IF EXISTS ProductosProveedor")
c.execute("""CREATE TABLE ProductosProveedor (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    CodigoInterno    TEXT    NOT NULL UNIQUE,
    Descripcion      TEXT    NOT NULL,
    CodigoProveedor  TEXT,
    IdProveedorPrinc INTEGER,
    TipoInsumo       TEXT    NOT NULL DEFAULT 'Materia prima',
    Unidad           TEXT    NOT NULL DEFAULT 'kg',
    PrecioUltCompra  REAL    DEFAULT 0,
    PrecioPromedio   REAL    DEFAULT 0,
    StockActual      REAL    DEFAULT 0,
    StockMinimo      REAL    DEFAULT 0,
    Activo           INTEGER DEFAULT 1,
    Observaciones    TEXT
)""")
c.commit()
cols = [r[1] for r in c.execute("PRAGMA table_info(ProductosProveedor)")]
print(f"OK - tabla recreada con {len(cols)} columnas: {cols}")
c.close()
