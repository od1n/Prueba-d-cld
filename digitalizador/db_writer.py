# db_writer.py — sistema_gestion.db  (%AppData%\SistemaGestion\)
import os, sqlite3
from datetime import datetime
from pathlib import Path
from typing import Optional

def _resolver_db() -> Path:
    # SG_DB_PATH es la variable canónica (Machine scope en Windows)
    ov = os.getenv("SG_DB_PATH", "") or os.getenv("DB_PATH", "")
    if ov:
        return Path(ov)
    appdata = os.getenv("APPDATA") or ""
    if appdata:
        p = Path(appdata) / "SistemaGestion" / "sistema_gestion.db"
        if p.exists():
            return p
    return Path(__file__).parent.parent / "sistema_gestion.db"

DB_PATH = _resolver_db()
USUARIO = "Digitalizador"

def _conn() -> sqlite3.Connection:
    c = sqlite3.connect(str(DB_PATH), timeout=10)
    c.execute("PRAGMA journal_mode=WAL")
    c.row_factory = sqlite3.Row
    return c

def _get_existencia(cur, codigo: str) -> float:
    cur.execute("SELECT COALESCE(Existencia,0) FROM Inventario WHERE Codigo=?", (codigo,))
    row = cur.fetchone()
    return float(row[0]) if row else 0.0

def _registrar_movimiento(cur, codigo: str, tipo: str, cantidad: float,
                          concepto: str, fecha_str: str):
    saldo_ant = _get_existencia(cur, codigo)
    nuevo = saldo_ant + cantidad if tipo == "ENTRADA" else saldo_ant - cantidad
    cur.execute(
        "INSERT INTO MovimientosInventario"
        "    (CodigoProducto, TipoMovimiento, Cantidad, Concepto,"
        "     Referencia, SaldoAnterior, NuevoSaldo, FechaHora, Usuario)"
        " VALUES (?,?,?,?,?,?,?,?,?)",
        (codigo, tipo, cantidad, concepto, fecha_str, saldo_ant, nuevo,
         datetime.now().strftime("%Y-%m-%d %H:%M:%S"), USUARIO)
    )

def _act_inv(cur, codigo: str, delta: float, concepto: str, fecha_str: str):
    if abs(delta) < 0.001:
        return
    tipo = "ENTRADA" if delta > 0 else "SALIDA"
    cur.execute(
        "UPDATE Inventario SET Existencia = Existencia + ? WHERE Codigo = ?",
        (delta, codigo)
    )
    _registrar_movimiento(cur, codigo, tipo, abs(delta), concepto, fecha_str)

def obtener_molde_productos(codigo_molde: str) -> Optional[dict]:
    with _conn() as c:
        row = c.execute(
            "SELECT mp.CodPrimera, mp.CodSegunda, mp.CodReciclado,"
            "       COALESCE(i1.Descripcion,''), COALESCE(i2.Descripcion,''),"
            "       COALESCE(i3.Descripcion,'')"
            " FROM MoldeProductos mp"
            " LEFT JOIN Inventario i1 ON i1.Codigo = mp.CodPrimera"
            " LEFT JOIN Inventario i2 ON i2.Codigo = mp.CodSegunda"
            " LEFT JOIN Inventario i3 ON i3.Codigo = mp.CodReciclado"
            " WHERE mp.CodigoMolde = ?",
            (codigo_molde,)
        ).fetchone()
        if not row:
            return None
        return {
            "cod_primera":   row[0], "desc_primera":   row[3],
            "cod_segunda":   row[1], "desc_segunda":   row[4],
            "cod_reciclado": row[2], "desc_reciclado": row[5],
        }

def obtener_todos_moldes() -> list:
    with _conn() as c:
        for tabla in ("TbMoldes", "TbMolde"):
            try:
                rows = c.execute(
                    f"SELECT CodigoMold, Descripcion FROM {tabla} ORDER BY CodigoMold"
                ).fetchall()
                return [{"codigo": r[0], "descripcion": r[1]} for r in rows]
            except Exception:
                continue
        return []

def obtener_insumos() -> list:
    with _conn() as c:
        try:
            rows = c.execute(
                "SELECT CodigoInterno, Descripcion"
                " FROM ProductosProveedor WHERE Activo=1 ORDER BY Descripcion"
            ).fetchall()
        except Exception:
            rows = []
        return [{"codigo": r[0], "descripcion": r[1]} for r in rows]

def _validar_datos_produccion(datos: dict) -> None:
    """Valida el JSON devuelto por el LLM antes de persistirlo.
    Lanza ValueError si algo está fuera de rangos razonables.
    Mitiga prompt-injection que intente inyectar datos falsos o extremos.
    """
    try:
        datetime.strptime(datos["fecha"], "%d/%m/%Y")
    except (KeyError, ValueError, TypeError):
        raise ValueError("Fecha inválida o ausente en los datos de producción")

    material = datos.get("material")
    if material not in ("Virgen", "Reciclado", None):
        raise ValueError(f"Material desconocido: {material!r}")

    for m in datos.get("moldes", []):
        for campo in ("primera", "segunda", "reciclado", "defectuosas"):
            val = m.get(campo)
            if val is None:
                continue
            try:
                fval = float(val)
            except (TypeError, ValueError):
                raise ValueError(f"Cantidad no numérica en molde: {campo}={val!r}")
            if fval < 0 or fval > 10_000:
                raise ValueError(f"Cantidad fuera de rango en molde: {campo}={fval}")

    for mat in datos.get("materiales", []):
        kg = mat.get("kg")
        if kg is None:
            continue
        try:
            fkg = float(kg)
        except (TypeError, ValueError):
            raise ValueError(f"Kg no numérico: {kg!r}")
        if fkg < 0 or fkg > 50_000:
            raise ValueError(f"Kg fuera de rango: {fkg}")


def guardar_produccion(datos: dict) -> dict:
    try:
        _validar_datos_produccion(datos)  # Validar schema antes de tocar la BD
        fecha_dt  = datetime.strptime(datos["fecha"], "%d/%m/%Y")
        fecha_str = fecha_dt.strftime("%Y-%m-%d")
        tipo_mat  = datos.get("material", "Virgen")
        obs       = datos.get("observaciones") or ""
        concepto  = f"Produccion {fecha_dt:%d/%m/%Y} ({tipo_mat}) Digitalizador"

        with _conn() as c:
            cur = c.cursor()
            c.execute("BEGIN")
            try:
                row = cur.execute(
                    "SELECT Id FROM RegistroProduccionDia WHERE Fecha=?",
                    (fecha_str,)
                ).fetchone()

                if row:
                    reg_id = row[0]
                    cur.execute(
                        "UPDATE RegistroProduccionDia"
                        " SET TipoMaterial=?, Observacion=? WHERE Id=?",
                        (tipo_mat, obs, reg_id)
                    )
                    cur.execute(
                        "DELETE FROM DetalleProduccionMolde WHERE IdRegistro=?",
                        (reg_id,)
                    )
                    cur.execute(
                        "DELETE FROM MaterialProduccionDia WHERE IdRegistro=?",
                        (reg_id,)
                    )
                else:
                    cur.execute(
                        "INSERT INTO RegistroProduccionDia (Fecha, TipoMaterial, Observacion)"
                        " VALUES (?,?,?)",
                        (fecha_str, tipo_mat, obs)
                    )
                    reg_id = cur.lastrowid

                for m in datos.get("moldes", []):
                    cod   = m.get("codigo_molde", "")
                    p1    = float(m.get("primera",     0) or 0)
                    p2    = float(m.get("segunda",     0) or 0)
                    rec   = float(m.get("reciclado",   0) or 0)
                    defe  = float(m.get("defectuosas", 0) or 0)
                    total = p1 + p2 + rec
                    cur.execute(
                        "INSERT INTO DetalleProduccionMolde"
                        "    (IdRegistro, CodigoMolde, CodigoProducto, Cantidad,"
                        "     CantPrimera, CantSegunda, CantReciclado, CantDefectuosas)"
                        " VALUES (?,?,?,?,?,?,?,?)",
                        (reg_id, cod, "", total, p1, p2, rec, defe)
                    )
                    mp = obtener_molde_productos(cod)
                    if mp:
                        if tipo_mat == "Virgen":
                            if mp["cod_primera"] and p1 > 0:
                                _act_inv(cur, mp["cod_primera"], p1, concepto, fecha_str)
                            if mp["cod_segunda"] and p2 > 0:
                                _act_inv(cur, mp["cod_segunda"], p2, concepto, fecha_str)
                        else:
                            if mp["cod_reciclado"] and rec > 0:
                                _act_inv(cur, mp["cod_reciclado"], rec, concepto, fecha_str)

                for mat in datos.get("materiales", []):
                    cod_ins = mat.get("codigo_insumo", "")
                    kg      = float(mat.get("kg", 0) or 0)
                    if not cod_ins or kg <= 0:
                        continue
                    cur.execute(
                        "INSERT INTO MaterialProduccionDia (IdRegistro, CodigoInsumo, Cantidad)"
                        " VALUES (?,?,?)",
                        (reg_id, cod_ins, kg)
                    )
                    _act_inv(cur, cod_ins, -kg, concepto, fecha_str)

                c.execute("COMMIT")
                return {"ok": True, "id": reg_id}

            except Exception:
                c.execute("ROLLBACK")
                raise

    except Exception as ex:
        return {"ok": False, "error": str(ex)}
