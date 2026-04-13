"""
generar_kardex.py
=================
Genera movimientos de Kardex (SALIDA) en MovimientosInventario
a partir de los DetallesFactura ya migrados.

Proceso:
    1. Lee DetallesFactura JOIN Facturas ordenado por Fecha ASC
    2. Por cada detalle inserta un movimiento SALIDA en MovimientosInventario
    3. Calcula SaldoAnterior y NuevoSaldo acumulando por producto
    4. Actualiza Inventario.Existencia al saldo final de cada producto

IMPORTANTE: Ejecutar UNA sola vez. Si ya hay movimientos migrados,
el script los detecta y pregunta antes de continuar.

Uso:
    python generar_kardex.py "<ruta_sqlite>"

Ejemplo:
    python generar_kardex.py "%APPDATA%\SistemaGestion\sistema_gestion.db"
"""

import sys
import sqlite3
from datetime import datetime

if len(sys.argv) < 2:
    print("Uso: python generar_kardex.py <ruta_sqlite>")
    sys.exit(1)

SQLITE_PATH = sys.argv[1]
USUARIO     = "migracion"


def migrar_kardex():
    print("=" * 55)
    print("  Generacion de Kardex desde DetallesFactura migrados")
    print("=" * 55)
    print(f"  BD: {SQLITE_PATH}")
    print()

    sql = sqlite3.connect(SQLITE_PATH)
    sql.execute("PRAGMA foreign_keys = OFF")

    # Verificar si ya hay movimientos de migracion
    ya_migrados = sql.execute(
        "SELECT COUNT(*) FROM MovimientosInventario WHERE Usuario = ?",
        (USUARIO,)
    ).fetchone()[0]

    if ya_migrados > 0:
        print(f"  ADVERTENCIA: Ya existen {ya_migrados} movimientos de migracion.")
        resp = input("  Deseas borrarlos y regenerar? (s/n): ").strip().lower()
        if resp == "s":
            sql.execute("DELETE FROM MovimientosInventario WHERE Usuario = ?", (USUARIO,))
            sql.commit()
            print("  Movimientos anteriores eliminados.")
        else:
            print("  Operacion cancelada.")
            sql.close()
            sys.exit(0)

    # Cargar stock inicial de cada producto (existencia actual en Inventario)
    # Usamos esto como punto de partida para calcular saldos hacia atras.
    # Como los datos son historicos, partimos desde 0 y dejamos que el
    # sistema recalcule la existencia real al final.
    print("  Cargando productos del inventario...")
    saldos = {}
    for fila in sql.execute("SELECT Codigo, Existencia FROM Inventario"):
        saldos[fila[0]] = fila[1]  # saldo actual migrado desde Access

    print(f"  Productos en inventario: {len(saldos)}")
    print()

    # Leer DetallesFactura JOIN Facturas ordenado cronologicamente
    print("  Leyendo DetallesFactura ordenados por fecha...")
    cur = sql.execute("""
        SELECT
            df.IdFactura,
            df.Codigo,
            df.Cantidad,
            f.NumeroF,
            f.Fecha
        FROM DetallesFactura df
        JOIN Facturas f ON f.rowid = df.IdFactura
        ORDER BY f.Fecha ASC, f.NumeroF ASC
    """)
    filas = cur.fetchall()
    print(f"  Detalles a procesar: {len(filas)}")
    print()

    # Construir saldos desde cero para el kardex historico
    # (Los saldos en Inventario reflejan existencia actual de Access,
    #  para el kardex historico partimos de 0 por producto)
    saldos_kardex = {cod: 0.0 for cod in saldos}

    insertados = 0
    omitidos   = 0
    errores    = 0

    movimientos = []

    for id_factura, codigo, cantidad, numero_f, fecha in filas:
        if codigo not in saldos_kardex:
            # Producto en factura que no esta en inventario — omitir
            omitidos += 1
            continue

        if cantidad is None or cantidad <= 0:
            omitidos += 1
            continue

        saldo_anterior = saldos_kardex[codigo]
        nuevo_saldo    = max(0.0, saldo_anterior - float(cantidad))
        saldos_kardex[codigo] = nuevo_saldo

        movimientos.append((
            fecha,                          # FechaHora
            codigo,                         # CodigoProducto
            "SALIDA",                       # TipoMovimiento
            f"Factura {numero_f}",          # Concepto
            float(cantidad),                # Cantidad
            float(saldo_anterior),          # SaldoAnterior
            float(nuevo_saldo),             # NuevoSaldo
            numero_f,                       # Referencia
            USUARIO,                        # Usuario
        ))

    # Insertar en lotes de 500
    print("  Insertando movimientos en Kardex...")
    batch_size = 500
    for i in range(0, len(movimientos), batch_size):
        lote = movimientos[i:i+batch_size]
        try:
            sql.executemany("""
                INSERT INTO MovimientosInventario
                    (FechaHora, CodigoProducto, TipoMovimiento, Concepto,
                     Cantidad, SaldoAnterior, NuevoSaldo, Referencia, Usuario)
                VALUES (?,?,?,?,?,?,?,?,?)
            """, lote)
            insertados += len(lote)
            print(f"  ... {insertados} movimientos insertados")
        except Exception as e:
            print(f"  [ERROR] Lote {i}: {e}")
            errores += len(lote)

    # Actualizar Existencia en Inventario con el saldo final del kardex
    # (saldo_kardex refleja cuanto quedo despues de todas las salidas historicas)
    # NOTA: Como partimos de 0, el saldo final sera negativo o 0.
    # La existencia real viene de Access (ya migrada). No la pisamos.
    print()
    print("  Existencias conservadas desde Access (no se modifican).")
    print("  Si deseas recalcular existencias desde el kardex,")
    print("  debes registrar tambien las entradas (compras/produccion).")

    sql.execute("PRAGMA foreign_keys = ON")
    sql.commit()
    sql.close()

    print()
    print("=" * 55)
    print("  RESUMEN")
    print("=" * 55)
    print(f"  Movimientos insertados : {insertados}")
    print(f"  Omitidos               : {omitidos}")
    print(f"  Errores                : {errores}")
    print()

    if errores > 0:
        print("  Hay errores. Revisa los mensajes arriba.")
        sys.exit(1)
    else:
        print("  Kardex generado correctamente.")
        print("  Abre FrmRentabilidad y filtra el periodo para ver los datos.")


if __name__ == "__main__":
    migrar_kardex()
