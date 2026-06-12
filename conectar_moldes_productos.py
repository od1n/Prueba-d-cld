import sqlite3, sys, os

# ── Detectar BD (solo OLIMPO) ──
if os.path.exists(r"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db"):
    db = r"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db"
else:
    print("ERROR: No se encontro la base de datos"); sys.exit(1)

print(f"BD: {db}")
c = sqlite3.connect(db)
cur = c.cursor()

# ── Mapa molde → código primera (de ModeloQfabrica) ──
molde_primera = {
    "Beb 01 01": "551100",
    "beb 01 02": "551100",
    "Bs S01 01": "771000",
    "Bs Sl 01":  "771000",
    "Co C md 01":"552002",
    "Co c t H01":"552001",
    "Co c t M01":"552003",
    "Com 01 01": "551000",
    "Post 01 01":"661001",
    "Post 01 02":"661001",
    "Estr 3p 01":"441000",
    "Estr 4p 01":"441100",
    "Gl 01 01":  "101700",
    "Mc 10 01":  "101001",
    "Mc 11 01":  "101102",
    "Mc 12Bo 01":"101202",
    "Mc 12Hu 01":"101202",
    "Mc 12T 01": "101202",
    "Mc 12TN 01":"101202",
    "Mc 14 01":  "101224",
    "Ms gr 01":  "101302",
    "Mx gr 01":  "101400",
    "Mx Chr Pl": "101421",
    "Mx Chr Al":  "101422",
    "Pi gr 01":  "101300",
    "Sal 01 01": "552000",
    "Slt in1 01":"101800",
    "Tr 01 01":  "101600",
    "Tx peq 01": "102100",
    "Tx West 01":"102101",
    "Tx Ws H 01":"102102",
    "Tb Co01 01":"800001",
    "Sac Bt 01": "772000",
}

try:
    # ── 1. Obtener descripción y precio de cada producto primera ──
    codigos_unicos = sorted(set(molde_primera.values()))
    productos = {}
    for cod in codigos_unicos:
        cur.execute("SELECT Descripcion, CostoActual, Precio FROM Inventario WHERE Codigo=?", (cod,))
        row = cur.fetchone()
        if row:
            productos[cod] = {"desc": row[0], "costo": row[1], "precio": row[2]}
        else:
            print(f"  ADVERTENCIA: producto primera {cod} no existe en Inventario")

    # ── 2. Crear productos Segunda (sufijo S) y Reciclado (sufijo R) ──
    n_creados = 0
    for cod in codigos_unicos:
        if cod not in productos:
            continue
        info = productos[cod]
        cod_s = cod + "S"
        cod_r = cod + "R"
        desc_s = info["desc"] + " (Segunda)"
        desc_r = info["desc"] + " (Reciclado)"
        # Precio segunda = 70% del primera, reciclado = 50%
        precio_s = round(info["precio"] * 0.7, 2)
        precio_r = round(info["precio"] * 0.5, 2)
        costo_s = round(info["costo"] * 0.7, 2)
        costo_r = round(info["costo"] * 0.5, 2)

        # Crear solo si no existe
        cur.execute("SELECT 1 FROM Inventario WHERE Codigo=?", (cod_s,))
        if not cur.fetchone():
            cur.execute(
                "INSERT INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,0)",
                (cod_s, desc_s, costo_s, precio_s))
            n_creados += 1
            print(f"  + {cod_s} = {desc_s} (${precio_s})")

        cur.execute("SELECT 1 FROM Inventario WHERE Codigo=?", (cod_r,))
        if not cur.fetchone():
            cur.execute(
                "INSERT INTO Inventario (Codigo,Descripcion,CostoActual,Precio,Existencia) VALUES(?,?,?,?,0)",
                (cod_r, desc_r, costo_r, precio_r))
            n_creados += 1
            print(f"  + {cod_r} = {desc_r} (${precio_r})")

    print(f"\n{n_creados} productos nuevos creados (segunda + reciclado).")

    # ── 3. Conectar MoldeProductos con los 3 códigos ──
    n_moldes = 0
    for molde, cod_1a in molde_primera.items():
        cod_s = cod_1a + "S"
        cod_r = cod_1a + "R"
        cur.execute("""
            INSERT OR REPLACE INTO MoldeProductos
                (CodigoMolde, CodPrimera, CodSegunda, CodReciclado)
            VALUES (?, ?, ?, ?)
        """, (molde, cod_1a, cod_s, cod_r))
        n_moldes += 1

    c.commit()
    print(f"{n_moldes} moldes conectados a productos (1a + 2a + R).")
    print("Listo.")

except Exception as e:
    c.rollback()
    print(f"ERROR: {e}")
finally:
    c.close()
