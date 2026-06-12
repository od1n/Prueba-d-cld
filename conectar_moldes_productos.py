import sqlite3, sys, os

if os.path.exists(r"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db"):
    db = r"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db"
else:
    print("ERROR: No se encontro la base de datos"); sys.exit(1)

print(f"BD: {db}")
c = sqlite3.connect(db)
cur = c.cursor()
n = 0
try:
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Beb 01 01", "551100")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("beb 01 02", "551100")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Bs S01 01", "771000")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Com 01 01", "551000")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Post 01 01", "661001")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Post 01 02", "661001")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Estr 4p 01", "441100")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Gl 01 01", "101700")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 10 01", "101001")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 11 01", "101102")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 12Bo 01", "101202")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 12Hu 01", "101202")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 12T 01", "101202")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 12TN 01", "101202")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mc 14 01", "101224")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Ms gr 01", "101302")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mx gr 01", "101400")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Pi gr 01", "101300")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Sal 01 01", "552000")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Slt in1 01", "101800")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Tr 01 01", "101600")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Tx peq 01", "102100")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Tx West 01", "102101")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Tx Ws H 01", "102102")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mx Chr Pl", "101421")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Mx Chr Al", "101422")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Co C md 01", "552002")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Co c t H01", "552001")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Co c t M01", "552003")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Tb Co01 01", "800001")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Estr 3p 01", "441000")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Bs Sl 01", "771000")); n+=1
    cur.execute("INSERT OR REPLACE INTO MoldeProductos (CodigoMolde, CodPrimera, CodSegunda, CodReciclado) VALUES(?,?,NULL,NULL)", ("Sac Bt 01", "772000")); n+=1
    c.commit()
    print(f"{n} moldes conectados a productos.")
except Exception as e:
    c.rollback()
    print(f"ERROR: {e}")
finally:
    c.close()