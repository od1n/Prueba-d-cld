import sqlite3
import sys
import os

# Detect machine by path
## Ruta canónica: C:\SistemaGestion\sistema_gestion.db (OLIMPO)
## Fallback: SG_DB_PATH si está definida
db = os.environ.get("SG_DB_PATH") or r"C:\SistemaGestion\sistema_gestion.db"
if not os.path.exists(db):
    alt = os.path.join(os.environ.get("APPDATA", ""), "SistemaGestion", "sistema_gestion.db")
    if os.path.exists(alt):
        db = alt
    else:
        print("ERROR: No se encontro la base de datos")
        sys.exit(1)

print(f"BD: {db}")
c = sqlite3.connect(db)
cur = c.cursor()

try:
    # ── 1. Productos del inventario (sin existencia) ──
    n_prod = 0
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101001", "Mc Clellan 10\"", 8.5, 17.0, "101001")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101102", "Mc.Clellan 11\"", 8.5, 17.0, "101102")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101201", "Mc.Clellan 12\" Reciclado", 9.0, 19.0, "101201")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101202", "Mc.Clellan 12\" de trabajo", 10.0, 21.0, "101202")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101214", "Mc.Clellan 14\"", 11.0, 24.0, "101214")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101224", "Mc,Clellan 14\" Reciclado", 10.0, 21.0, "101224")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101300", "Tipo Pico", 12.0, 24.0, "101300")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101302", "Pico Mesacé", 12.0, 24.0, "101302")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101400", "Mexicano", 12.0, 24.0, "101400")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101600", "Traqueo", 5.0, 10.0, "101600")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101700", "Galápago", 5.0, 15.0, "101700")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("101800", "Salto Ingles", 6.0, 15.0, "101800")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("102100", "Texano", 11.0, 24.0, "102100")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("102110", "Texano De Encierro", 12.0, 24.0, "102110")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("441000", "Estribo #1", 2.0, 5.0, "441000")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("441100", "Estribo #2", 2.0, 5.0, "441100")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("551000", "Comedero", 16.0, 30.0, "551000")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("551100", "Bebedero", 16.0, 30.0, "551100")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("552000", "Salero", 10.0, 25.0, "552000")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("661001", "Estantillo #1", 25.0, 25.0, "661001")); n_prod+=1
    cur.execute("INSERT OR REPLACE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,COALESCE((SELECT Existencia FROM Inventario WHERE Codigo=?),0))", ("771000", "Base Slalon", 9.0, 15.0, "771000")); n_prod+=1

    # ── 2. Productos faltantes (referenciados por moldes) ──
    n_falt = 0
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("101421", "Mexicano Tipo Charro pico plastico")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("101422", "Mexicano Tipo Charro pico Aluminio")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("102101", "Texano Westtern 15,5\"")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("102102", "Texano Western Hombros anchos 15,5\"")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("552001", "Comedeto Canoa Tapa hembra")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("552002", "Comedero Canoa medio")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("552003", "Comedero Canoa Tapa Macho")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("772000", "Saca Botas")); n_falt += cur.rowcount
    cur.execute("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,0,0,0)", ("800001", "Tabla Cocina 01")); n_falt += cur.rowcount

    # ── 3. Moldes (FechaLastMant como NULL si no existe) ──
    n_mold = 0
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Beb 01 01", "Bebedero modelo 1 molde 01", "1990-01-01", None, "", "551100", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("beb 01 02", "Bebedero modelo 1 molde 02", "1990-01-01", None, "", "551100", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Bs S01 01", "Base para Slalon modelo 1 molde 01", "2002-01-01", None, "", "771000", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Com 01 01", "Comedero modelo 1 molde 01", "1990-01-01", None, "", "551000", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Post 01 01", "Estantillo modelo 1 molde 01", "2001-01-01", None, "", "661001", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Post 01 02", "Estantillo modelo 1 molde 02", "2001-01-01", None, "", "661001", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Estr 4p 01", "Estribo 4\" molde 01", "2002-01-01", None, "", "441100", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Gl 01 01", "Galapago modelo1 molde 01", "1992-01-01", None, "", "101700", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 10 01", "Mc Clelland 10\" molde 1", "1990-01-01", None, "", "101001", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 11 01", "Mc clellan 11\" molde 01", "1990-01-01", None, "", "101102", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 12Bo 01", "Mc Clellan 12 solo bocado Grande 01", "1997-01-01", None, "", "101202", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 12Hu 01", "Mc Clellan 12\" solo hueco molde 01", "1990-01-01", None, "", "101202", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 12T 01", "Mc Clellan con Bocado Grande y hueco molde 01", "1990-01-01", None, "", "101202", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 12TN 01", "Mc Clelland Bocado Pequeño  y hueco molde 01", "1994-01-01", None, "", "101202", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mc 14 01", "Mc Clellan 14\"  molde 01", "1994-01-01", None, "", "101224", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Ms gr 01", "Mesace grande molde 01", "1994-01-01", None, "", "101302", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mx gr 01", "Mexicano grande molde 01", "1990-01-01", None, "", "101400", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Pi gr 01", "Pico colombiano grande molde 01", "1992-01-01", None, "", "101300", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Sal 01 01", "Salero modelo 1 molde 01", "1992-01-01", None, "", "552000", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Slt in1 01", "Salto Ingles modeli 1 molde 01", "1993-01-01", None, "", "101800", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Tr 01 01", "Traqueo modelo 1 molde 01", "1992-01-01", None, "", "101600", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Tx peq 01", "Texano peq 13\" molde 01", "1995-01-01", None, "", "102100", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Tx West 01", "Texano Westtern 15,5\" molde 01", "2001-10-01", None, "", "102101", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Tx Ws H 01", "Texano Western Hombros anchos 15,5\" molde 01", "2026-05-01", None, "", "102102", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mx Chr Pl", "Mexicano Tipo Charro pico plastico molde 01", "2017-04-01", None, "", "101421", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Mx Chr Al", "Mexicano Tipo Charro pico Aluminio molde 01", "2017-04-01", None, "", "101422", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Co C md 01", "Comedero Canoa medio molde 01", "2026-01-01", None, "", "552002", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Co c t H01", "Comedeto Canoa Tapa hembra molde 01", "2026-01-01", None, "", "552001", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Co c t M01", "Comedero Canoa Tapa Macho molde 01", "2026-01-01", None, "", "552003", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Tb Co01 01", "Tabla Cocina 01 Molde 01", "2026-02-01", None, "", "800001", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Estr 3p 01", "Estribo 3\" molde 01", "2001-01-01", None, "", "441000", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Bs Sl 01", "Base Slalon molde 01", "1999-01-01", None, "", "771000", "", 0)); n_mold+=1
    cur.execute("INSERT OR REPLACE INTO TbMoldes (CodigoMold,Descripcion,Fecha,FechaLastMant,DescMant,ModeloQfabrica,Observaciones,CantMatXunid) VALUES(?,?,?,?,?,?,?,?)", ("Sac Bt 01", "Saca Botas molde 01", "1999-01-01", None, "", "772000", "", 0)); n_mold+=1

    # ── 4. Poblar MoldeProductos (relación molde→producto para la UI) ──
    cur.execute("""
        INSERT OR IGNORE INTO MoldeProductos (CodigoMolde, CodPrimera)
        SELECT CodigoMold, ModeloQfabrica
        FROM TbMoldes
        WHERE ModeloQfabrica IS NOT NULL AND ModeloQfabrica != ''
    """)
    n_mp = cur.rowcount

    c.commit()
    print(f"Productos importados: {n_prod}")
    print(f"Productos nuevos creados (faltaban): {n_falt}")
    print(f"Moldes importados: {n_mold}")
    print(f"MoldeProductos vinculados: {n_mp}")
    print("OK - Importacion completada.")
except Exception as e:
    c.rollback()
    print(f"ERROR: {e}")
finally:
    c.close()