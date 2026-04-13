"""
migrar_ElHato2K.py  (v6 - IdFactura usa rowid de Facturas)
===========================================================
Orden de migracion respetando llaves foraneas:
    1. Clientes        (maestra)
    2. Inventario      (ya migrado, INSERT OR IGNORE no duplica)
    3. Facturas        (depende de Clientes)
    4. DetallesFactura (IdFactura = rowid de Facturas)
    5. Produccion      (depende de Inventario)

Uso desde el .bat:
    python migrar_ElHato2K.py <mdb> <mdw> <usuario> <password> <sqlite>
"""

import sys
import sqlite3
from datetime import datetime

try:
    import pyodbc
except ImportError:
    print("[ERROR] pyodbc no instalado. Ejecuta: pip install pyodbc")
    sys.exit(1)

if len(sys.argv) < 6:
    print("Uso: python migrar_ElHato2K.py <mdb> <mdw> <usuario> <password> <sqlite>")
    sys.exit(1)

MDB_PATH    = sys.argv[1]
MDW_PATH    = sys.argv[2]
DB_USER     = sys.argv[3]
DB_PASS     = sys.argv[4]
SQLITE_PATH = sys.argv[5]

HOY = datetime.today().strftime("%Y-%m-%d")


def conectar_access():
    conn_str = (
        f"DRIVER={{Microsoft Access Driver (*.mdb, *.accdb)}};"
        f"DBQ={MDB_PATH};"
        f"SYSTEMDB={MDW_PATH};"
        f"UID={DB_USER};"
        f"PWD={DB_PASS};"
    )
    try:
        return pyodbc.connect(conn_str)
    except Exception as e:
        print(f"[ERROR] No se pudo conectar al .mdb:\n        {e}")
        sys.exit(1)


def fecha_str(val):
    if val is None:
        return HOY
    if hasattr(val, 'strftime'):
        return val.strftime("%Y-%m-%d")
    s = str(val).strip()
    for fmt in ("%Y-%m-%d", "%m/%d/%Y", "%d/%m/%Y", "%m-%d-%Y"):
        try:
            return datetime.strptime(s[:10], fmt).strftime("%Y-%m-%d")
        except:
            continue
    return s[:10]


def flt(val, default=0.0):
    try:
        return float(val) if val is not None else default
    except:
        return default


def ent(val, default=0):
    try:
        return int(val) if val is not None else default
    except:
        return default


def txt(val, default=""):
    if val is None:
        return default
    return str(val).strip()


def migrar():
    print(f"  MDB    : {MDB_PATH}")
    print(f"  MDW    : {MDW_PATH}")
    print(f"  SQLite : {SQLITE_PATH}")
    print()

    acc = conectar_access()
    sql = sqlite3.connect(SQLITE_PATH)
    sql.execute("PRAGMA foreign_keys = OFF")

    totales = {}

    # =========================================================================
    # 1. CLIENTES
    # =========================================================================
    print("=" * 50)
    print("[1/5] Migrando Clientes...")
    print("=" * 50)

    cur = acc.execute("SELECT * FROM [Clientes]")
    cols = [d[0] for d in cur.description]
    insertados = omitidos = errores = 0

    for row in cur:
        r = dict(zip(cols, row))
        codigo = txt(r.get("Codigo"))
        if not codigo:
            omitidos += 1
            continue
        try:
            sql.execute("""
                INSERT OR IGNORE INTO Clientes
                    (Codigo, Descripcion, Direccion, Ciudad, Estado,
                     CiRif, Nit, Fax, Telefono, Telefono2,
                     Credito, TipoCliente, Fecha, Observaciones,
                     Descuento, AFavor)
                VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
            """, (
                codigo,
                txt(r.get("Descripción") or r.get("Descripcion")),
                txt(r.get("Dirección")   or r.get("Direccion")),
                txt(r.get("Ciudad")),
                txt(r.get("Estado")),
                txt(r.get("CI/Rif")      or r.get("CiRif")),
                txt(r.get("NIT")         or r.get("Nit")),
                txt(r.get("Fax")),
                txt(r.get("Telefono")),
                txt(r.get("Telefono2")),
                flt(r.get("Credito")),
                txt(r.get("Tipo de Cliente") or r.get("TipoCliente"), "Regular"),
                fecha_str(r.get("Fecha")),
                txt(r.get("Observaciones")),
                flt(r.get("Descuento")),
                flt(r.get("AFavor")),
            ))
            insertados += 1
            print(f"  + {codigo:<12} {txt(r.get('Descripción') or r.get('Descripcion'))}")
        except Exception as e:
            print(f"  [ERROR] Cliente '{codigo}': {e}")
            errores += 1

    totales["Clientes"] = (insertados, errores)
    print(f"\n  Insertados: {insertados}  Errores: {errores}\n")

    # =========================================================================
    # 2. INVENTARIO (ya migrado)
    # =========================================================================
    print("=" * 50)
    print("[2/5] Inventario ya migrado anteriormente (se omite).")
    print("=" * 50)
    print()

    # =========================================================================
    # 3. FACTURAS
    # =========================================================================
    print("=" * 50)
    print("[3/5] Migrando Facturas (9258 registros)...")
    print("=" * 50)

    cur = acc.execute("SELECT * FROM [Factura]")
    cols = [d[0] for d in cur.description]
    insertados = omitidos = errores = 0

    for row in cur:
        r = dict(zip(cols, row))
        numero = txt(r.get("Numero_F"))
        if not numero:
            omitidos += 1
            continue
        try:
            sql.execute("""
                INSERT OR IGNORE INTO Facturas
                    (NumeroF, Fecha, Codigo, Descuento, Transporte,
                     Observaciones, FormaDePago, Abono,
                     IvaF, DiasCreditoF, TotalF)
                VALUES (?,?,?,?,?,?,?,?,?,?,?)
            """, (
                numero,
                fecha_str(r.get("Fecha")),
                txt(r.get("Codigo")),
                flt(r.get("Descuento")),
                txt(r.get("Transporte")),
                txt(r.get("Observaciones")),
                txt(r.get("Forma_de_Pago") or r.get("FormaDePago"), "Contado"),
                flt(r.get("Abono")),
                flt(r.get("IvaF")),
                ent(r.get("DiasCreditoF")),
                flt(r.get("TotalF")),
            ))
            insertados += 1
            if insertados % 500 == 0:
                print(f"  ... {insertados} facturas insertadas")
        except Exception as e:
            print(f"  [ERROR] Factura '{numero}': {e}")
            errores += 1

    totales["Facturas"] = (insertados, errores)
    print(f"\n  Insertados: {insertados}  Errores: {errores}\n")

    # =========================================================================
    # 4. DETALLES FACTURA
    # DetallesFactura.IdFactura es INTEGER = rowid de la fila en Facturas
    # =========================================================================
    print("=" * 50)
    print("[4/5] Migrando DetallesFactura (14108 registros)...")
    print("=" * 50)

    print("  Construyendo mapa NumeroF -> rowid...")
    mapa_rowid = {}
    for fila in sql.execute("SELECT rowid, NumeroF FROM Facturas"):
        mapa_rowid[fila[1]] = fila[0]
    print(f"  Facturas en mapa: {len(mapa_rowid)}")

    cur = acc.execute("SELECT * FROM [Detalles]")
    cols = [d[0] for d in cur.description]
    insertados = omitidos = errores = 0

    for row in cur:
        r = dict(zip(cols, row))
        numero_f = txt(r.get("IdFactura"))
        codigo   = txt(r.get("Codigo"))

        if not numero_f or not codigo:
            omitidos += 1
            continue

        rowid = mapa_rowid.get(numero_f)
        if rowid is None:
            omitidos += 1
            continue

        try:
            sql.execute("""
                INSERT OR IGNORE INTO DetallesFactura (IdFactura, Codigo, Cantidad)
                VALUES (?,?,?)
            """, (
                rowid,
                codigo,
                flt(r.get("Cantidad")),
            ))
            insertados += 1
            if insertados % 1000 == 0:
                print(f"  ... {insertados} detalles insertados")
        except Exception as e:
            print(f"  [ERROR] Detalle '{numero_f}': {e}")
            errores += 1

    totales["DetallesFactura"] = (insertados, errores)
    print(f"\n  Insertados: {insertados}  Omitidos: {omitidos}  Errores: {errores}\n")

    # =========================================================================
    # 5. PRODUCCION
    # =========================================================================
    print("=" * 50)
    print("[5/5] Migrando Produccion (6090 registros)...")
    print("=" * 50)

    cur = acc.execute("SELECT * FROM [Produccion]")
    cols = [d[0] for d in cur.description]
    insertados = omitidos = errores = 0

    for row in cur:
        r = dict(zip(cols, row))
        codigo = txt(r.get("Codigo"))
        if not codigo:
            omitidos += 1
            continue
        try:
            sql.execute("""
                INSERT INTO Produccion (Codigo, Fecha, Cantidad)
                VALUES (?,?,?)
            """, (
                codigo,
                fecha_str(r.get("Fecha")),
                flt(r.get("Cantidad")),
            ))
            insertados += 1
            if insertados % 500 == 0:
                print(f"  ... {insertados} registros insertados")
        except Exception as e:
            print(f"  [ERROR] Produccion '{codigo}': {e}")
            errores += 1

    totales["Produccion"] = (insertados, errores)
    print(f"\n  Insertados: {insertados}  Omitidos: {omitidos}  Errores: {errores}\n")

    # =========================================================================
    # RESUMEN FINAL
    # =========================================================================
    sql.execute("PRAGMA foreign_keys = ON")
    sql.commit()
    acc.close()
    sql.close()

    print("=" * 50)
    print("  RESUMEN FINAL")
    print("=" * 50)
    total_errores = 0
    for tabla, (ins, err) in totales.items():
        print(f"  {tabla:<20} Insertados: {ins:<6} Errores: {err}")
        total_errores += err
    print()

    if total_errores > 0:
        print("  Hay errores. Revisa los mensajes arriba.")
        sys.exit(1)
    else:
        print("  Migracion completada sin errores.")


if __name__ == "__main__":
    migrar()
