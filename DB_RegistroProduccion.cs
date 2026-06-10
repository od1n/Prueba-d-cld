// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_RegistroProduccion.cs  →  raíz del proyecto
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  MODELOS
    // ════════════════════════════════════════════════════════════════════════

    public class MoldeProductos
    {
        public string CodigoMolde  { get; set; }
        public string CodPrimera   { get; set; }  // producto virgen primera
        public string CodSegunda   { get; set; }  // producto virgen segunda (sufijo S)
        public string CodReciclado { get; set; }  // producto reciclado (sufijo R)

        // Descripciones para UI (JOIN Inventario)
        public string DescPrimera   { get; set; }
        public string DescSegunda   { get; set; }
        public string DescReciclado { get; set; }
    }

    public class RegistroProduccionDia
    {
        public int      Id            { get; set; }
        public DateTime Fecha         { get; set; }
        public string   TipoMaterial  { get; set; }  // "Virgen" | "Reciclado"
        public string   Observacion   { get; set; }

        public List<DetalleProduccionMolde> Moldes     { get; set; } = new();
        public List<MaterialProduccionDia>  Materiales { get; set; } = new();

        // Totales por producto (calculados)
        public Dictionary<string, decimal> TotalPorProducto =>
            Moldes.GroupBy(m => m.CodigoProducto)
                  .ToDictionary(g => g.Key, g => g.Sum(m => m.CantTotal));
    }

    public class DetalleProduccionMolde
    {
        public int     Id              { get; set; }
        public int     IdRegistro      { get; set; }
        public string  CodigoMolde     { get; set; }
        public string  DescMolde       { get; set; }
        public string  CodigoProducto  { get; set; }  // producto base (del molde)
        public string  DescProducto    { get; set; }
        public decimal Cantidad        { get; set; }   // total (compat. anterior)
        public decimal CantPrimera     { get; set; }
        public decimal CantSegunda     { get; set; }
        public decimal CantReciclado   { get; set; }
        /// <summary>Piezas defectuosas: se registran pero NO se suman al inventario bueno.</summary>
        public decimal CantDefectuosas { get; set; }

        /// <summary>Total de piezas buenas (excluye defectuosas).</summary>
        public decimal CantTotal => CantPrimera + CantSegunda + CantReciclado;
    }

    public class MaterialProduccionDia
    {
        public int     Id           { get; set; }
        public int     IdRegistro   { get; set; }
        public string  CodigoInsumo { get; set; }
        public string  DescInsumo   { get; set; }
        public decimal Cantidad     { get; set; }  // en kg
    }

    /// <summary>Resumen de merma calculada antes de confirmar.</summary>
    public class ResumenMermaProduccion
    {
        public DateTime Fecha               { get; set; }
        public string   TipoMaterial        { get; set; }
        public decimal  MaterialConsumido   { get; set; }  // kg ingresados
        public decimal  PesoTeorico         { get; set; }  // kg según peso×cantidad
        public decimal  MermaKg             => MaterialConsumido - PesoTeorico;
        public decimal  PctMerma            => MaterialConsumido > 0
            ? Math.Round(MermaKg / MaterialConsumido * 100, 2) : 0;
        public List<(string Producto, decimal Cantidad, decimal PesoUnit, decimal SubTotal)>
            DetalleProductos { get; set; } = new();
        public List<(string Insumo, decimal Kg)>
            DetalleInsumos { get; set; } = new();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CRUD
    // ════════════════════════════════════════════════════════════════════════
    public static partial class DatabaseService
    {
        // ── MoldeProductos ────────────────────────────────────────────────

        public static MoldeProductos ObtenerMoldeProductos(string codigoMolde)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT mp.CodigoMolde,
                       mp.CodPrimera,   COALESCE(i1.Descripcion,''),
                       mp.CodSegunda,   COALESCE(i2.Descripcion,''),
                       mp.CodReciclado, COALESCE(i3.Descripcion,'')
                FROM MoldeProductos mp
                LEFT JOIN Inventario i1 ON i1.Codigo = mp.CodPrimera
                LEFT JOIN Inventario i2 ON i2.Codigo = mp.CodSegunda
                LEFT JOIN Inventario i3 ON i3.Codigo = mp.CodReciclado
                WHERE mp.CodigoMolde = $cod";
            cmd.Parameters.AddWithValue("$cod", codigoMolde);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new MoldeProductos {
                CodigoMolde  = S(r,0),
                CodPrimera   = r.IsDBNull(1) ? null : S(r,1),
                DescPrimera  = S(r,2),
                CodSegunda   = r.IsDBNull(3) ? null : S(r,3),
                DescSegunda  = S(r,4),
                CodReciclado = r.IsDBNull(5) ? null : S(r,5),
                DescReciclado= S(r,6)
            };
        }

        public static List<MoldeProductos> ObtenerTodosMoldeProductos()
        {
            var list = new List<MoldeProductos>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT mp.CodigoMolde,
                       mp.CodPrimera,   COALESCE(i1.Descripcion,''),
                       mp.CodSegunda,   COALESCE(i2.Descripcion,''),
                       mp.CodReciclado, COALESCE(i3.Descripcion,'')
                FROM MoldeProductos mp
                LEFT JOIN Inventario i1 ON i1.Codigo = mp.CodPrimera
                LEFT JOIN Inventario i2 ON i2.Codigo = mp.CodSegunda
                LEFT JOIN Inventario i3 ON i3.Codigo = mp.CodReciclado
                ORDER BY mp.CodigoMolde";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MoldeProductos {
                    CodigoMolde  = S(r,0),
                    CodPrimera   = r.IsDBNull(1) ? null : S(r,1),
                    DescPrimera  = S(r,2),
                    CodSegunda   = r.IsDBNull(3) ? null : S(r,3),
                    DescSegunda  = S(r,4),
                    CodReciclado = r.IsDBNull(5) ? null : S(r,5),
                    DescReciclado= S(r,6)
                });
            return list;
        }

        public static void GuardarMoldeProductos(MoldeProductos mp)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MoldeProductos
                    (CodigoMolde, CodPrimera, CodSegunda, CodReciclado)
                VALUES ($cm, $cp, $cs, $cr)
                ON CONFLICT(CodigoMolde)
                DO UPDATE SET CodPrimera=$cp, CodSegunda=$cs, CodReciclado=$cr";
            cmd.Parameters.AddWithValue("$cm", mp.CodigoMolde);
            cmd.Parameters.AddWithValue("$cp",
                string.IsNullOrWhiteSpace(mp.CodPrimera)   ? (object)DBNull.Value : mp.CodPrimera);
            cmd.Parameters.AddWithValue("$cs",
                string.IsNullOrWhiteSpace(mp.CodSegunda)   ? (object)DBNull.Value : mp.CodSegunda);
            cmd.Parameters.AddWithValue("$cr",
                string.IsNullOrWhiteSpace(mp.CodReciclado) ? (object)DBNull.Value : mp.CodReciclado);
            cmd.ExecuteNonQuery();
        }

        // ── RegistroProduccionDia ─────────────────────────────────────────

        public static RegistroProduccionDia ObtenerRegistroProduccion(DateTime fecha)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Fecha, COALESCE(TipoMaterial,'Virgen'), COALESCE(Observacion,'')
                FROM RegistroProduccionDia WHERE Fecha = $f";
            cmd.Parameters.AddWithValue("$f", fecha.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            var reg = new RegistroProduccionDia {
                Id           = r.GetInt32(0),
                Fecha        = DateTime.Parse(r.GetString(1)),
                TipoMaterial = S(r,2),
                Observacion  = S(r,3)
            };
            reg.Moldes     = ObtenerDetalleMoldes(reg.Id);
            reg.Materiales = ObtenerDetalleMateriales(reg.Id);
            return reg;
        }

        public static List<RegistroProduccionDia> ObtenerRegistrosProduccion(
            DateTime desde, DateTime hasta)
        {
            var list = new List<RegistroProduccionDia>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Fecha, COALESCE(TipoMaterial,'Virgen'), COALESCE(Observacion,'')
                FROM RegistroProduccionDia
                WHERE Fecha >= $d AND Fecha <= $h ORDER BY Fecha DESC";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var reg = new RegistroProduccionDia {
                    Id           = r.GetInt32(0),
                    Fecha        = DateTime.Parse(r.GetString(1)),
                    TipoMaterial = S(r,2),
                    Observacion  = S(r,3)
                };
                reg.Moldes     = ObtenerDetalleMoldes(reg.Id);
                reg.Materiales = ObtenerDetalleMateriales(reg.Id);
                list.Add(reg);
            }
            return list;
        }

        /// <summary>
        /// Guarda el registro completo, mueve inventario y retorna resumen de merma
        /// para que el usuario confirme antes de registrarla.
        /// </summary>
        public static ResumenMermaProduccion GuardarRegistroProduccion(
            RegistroProduccionDia reg)
        {
            using var c = Conn();
            c.Open();
            using var tx = c.BeginTransaction();
            try
            {
                // 1. Cabecera
                var cmdH = c.CreateCommand(); cmdH.Transaction = tx;
                if (reg.Id == 0)
                {
                    cmdH.CommandText = @"
                        INSERT INTO RegistroProduccionDia (Fecha, TipoMaterial, Observacion)
                        VALUES ($f,$tm,$ob); SELECT last_insert_rowid();";
                    cmdH.Parameters.AddWithValue("$f",  reg.Fecha.ToString("yyyy-MM-dd"));
                    cmdH.Parameters.AddWithValue("$tm", reg.TipoMaterial ?? "Virgen");
                    cmdH.Parameters.AddWithValue("$ob", reg.Observacion ?? "");
                    reg.Id = (int)(long)cmdH.ExecuteScalar();
                }
                else
                {
                    cmdH.CommandText = @"
                        UPDATE RegistroProduccionDia
                        SET Fecha=$f, TipoMaterial=$tm, Observacion=$ob WHERE Id=$id";
                    cmdH.Parameters.AddWithValue("$id", reg.Id);
                    cmdH.Parameters.AddWithValue("$f",  reg.Fecha.ToString("yyyy-MM-dd"));
                    cmdH.Parameters.AddWithValue("$tm", reg.TipoMaterial ?? "Virgen");
                    cmdH.Parameters.AddWithValue("$ob", reg.Observacion ?? "");
                    cmdH.ExecuteNonQuery();
                }

                // 2. Borrar detalles anteriores
                var del = c.CreateCommand(); del.Transaction = tx;
                del.CommandText = "DELETE FROM DetalleProduccionMolde WHERE IdRegistro=$id";
                del.Parameters.AddWithValue("$id", reg.Id);
                del.ExecuteNonQuery();
                del.CommandText = "DELETE FROM MaterialProduccionDia WHERE IdRegistro=$id";
                del.ExecuteNonQuery();

                // 3. Insertar moldes
                foreach (var m in reg.Moldes)
                {
                    var ins = c.CreateCommand(); ins.Transaction = tx;
                    ins.CommandText = @"
                        INSERT INTO DetalleProduccionMolde
                            (IdRegistro,CodigoMolde,CodigoProducto,Cantidad,
                             CantPrimera,CantSegunda,CantReciclado,CantDefectuosas)
                        VALUES ($ir,$cm,$cp,$ca,$cp1,$cs,$cr,$cd)";
                    ins.Parameters.AddWithValue("$ir",  reg.Id);
                    ins.Parameters.AddWithValue("$cm",  m.CodigoMolde);
                    ins.Parameters.AddWithValue("$cp",  m.CodigoProducto ?? "");
                    ins.Parameters.AddWithValue("$ca",  (double)m.CantTotal);
                    ins.Parameters.AddWithValue("$cp1", (double)m.CantPrimera);
                    ins.Parameters.AddWithValue("$cs",  (double)m.CantSegunda);
                    ins.Parameters.AddWithValue("$cr",  (double)m.CantReciclado);
                    ins.Parameters.AddWithValue("$cd",  (double)m.CantDefectuosas);
                    ins.ExecuteNonQuery();
                }

                // 4. Insertar materiales
                foreach (var mat in reg.Materiales)
                {
                    var ins = c.CreateCommand(); ins.Transaction = tx;
                    ins.CommandText = @"
                        INSERT INTO MaterialProduccionDia (IdRegistro,CodigoInsumo,Cantidad)
                        VALUES ($ir,$ci,$ca)";
                    ins.Parameters.AddWithValue("$ir", reg.Id);
                    ins.Parameters.AddWithValue("$ci", mat.CodigoInsumo);
                    ins.Parameters.AddWithValue("$ca", (double)mat.Cantidad);
                    ins.ExecuteNonQuery();
                }

                // 5. Mover inventario
                // 5a. Descontar insumos
                foreach (var mat in reg.Materiales)
                {
                    var inv = c.CreateCommand(); inv.Transaction = tx;
                    inv.CommandText = @"
                        UPDATE Inventario SET Existencia = Existencia - $cant
                        WHERE Codigo = $cod";
                    inv.Parameters.AddWithValue("$cant", (double)mat.Cantidad);
                    inv.Parameters.AddWithValue("$cod",  mat.CodigoInsumo);
                    inv.ExecuteNonQuery();

                    // Kardex salida insumo
                    var mp = ObtenerMoldeProductos(reg.Moldes.FirstOrDefault()?.CodigoMolde ?? "");
                    RegistrarMovimientoTx(c, tx, mat.CodigoInsumo, "SALIDA", mat.Cantidad,
                        $"Produccion {reg.Fecha:dd/MM/yyyy} ({reg.TipoMaterial})",
                        reg.Fecha.ToString("yyyy-MM-dd"), Sesion.NombreUsuario ?? "Sistema");
                }

                // 5b. Sumar productos producidos
                // Obtener pesos para el resumen de merma
                var invDict = new Dictionary<string, (decimal Peso, decimal Existencia)>();
                foreach (var m in reg.Moldes)
                {
                    var mp = ObtenerMoldeProductos(m.CodigoMolde);
                    if (mp == null) continue;

                    void SumarProd(string cod, decimal cant)
                    {
                        if (string.IsNullOrWhiteSpace(cod) || cant <= 0) return;
                        var cmdU = c.CreateCommand(); cmdU.Transaction = tx;
                        cmdU.CommandText =
                            "UPDATE Inventario SET Existencia=Existencia+$c WHERE Codigo=$cod";
                        cmdU.Parameters.AddWithValue("$c",   (double)cant);
                        cmdU.Parameters.AddWithValue("$cod", cod);
                        cmdU.ExecuteNonQuery();
                        RegistrarMovimientoTx(c, tx, cod, "ENTRADA", cant,
                            $"Produccion {reg.Fecha:dd/MM/yyyy} ({reg.TipoMaterial})",
                            reg.Fecha.ToString("yyyy-MM-dd"), Sesion.NombreUsuario ?? "Sistema");

                        // Guardar peso para merma
                        if (!invDict.ContainsKey(cod))
                        {
                            var cmdP = c.CreateCommand(); cmdP.Transaction = tx;
                            cmdP.CommandText =
                                "SELECT COALESCE(PesoKg,0), Existencia FROM Inventario WHERE Codigo=$c";
                            cmdP.Parameters.AddWithValue("$c", cod);
                            using var rp = cmdP.ExecuteReader();
                            if (rp.Read())
                                invDict[cod] = (D(rp,0), D(rp,1));
                        }
                    }

                    if (reg.TipoMaterial == "Virgen")
                    {
                        SumarProd(mp.CodPrimera,   m.CantPrimera);
                        SumarProd(mp.CodSegunda,   m.CantSegunda);
                    }
                    else // Reciclado
                    {
                        SumarProd(mp.CodReciclado, m.CantReciclado);
                    }
                }

                tx.Commit();

                // 6. Calcular resumen de merma (post-commit, para confirmación del usuario)
                return CalcularResumenMerma(reg, invDict);
            }
            catch { tx.Rollback(); throw; }
        }

        private static ResumenMermaProduccion CalcularResumenMerma(
            RegistroProduccionDia reg,
            Dictionary<string, (decimal Peso, decimal Existencia)> pesos)
        {
            var resumen = new ResumenMermaProduccion {
                Fecha            = reg.Fecha,
                TipoMaterial     = reg.TipoMaterial,
                MaterialConsumido = reg.Materiales.Sum(m => m.Cantidad)
            };

            resumen.DetalleInsumos = reg.Materiales
                .Select(m => (m.DescInsumo ?? m.CodigoInsumo, m.Cantidad))
                .ToList();

            decimal pesoTeorico = 0;
            foreach (var m in reg.Moldes)
            {
                var mp = ObtenerMoldeProductos(m.CodigoMolde);
                if (mp == null) continue;

                void AgregarDetalle(string cod, decimal cant, string desc)
                {
                    if (string.IsNullOrWhiteSpace(cod) || cant <= 0) return;
                    decimal peso = pesos.ContainsKey(cod) ? pesos[cod].Peso : 0;
                    decimal sub  = cant * peso;
                    pesoTeorico += sub;
                    resumen.DetalleProductos.Add((desc ?? cod, cant, peso, sub));
                }

                if (reg.TipoMaterial == "Virgen")
                {
                    AgregarDetalle(mp.CodPrimera,   m.CantPrimera,   mp.DescPrimera);
                    AgregarDetalle(mp.CodSegunda,   m.CantSegunda,   mp.DescSegunda);
                }
                else
                {
                    AgregarDetalle(mp.CodReciclado, m.CantReciclado, mp.DescReciclado);
                }
            }

            resumen.PesoTeorico = Math.Round(pesoTeorico, 4);
            return resumen;
        }

        /// <summary>Registra la merma confirmada por el usuario en la tabla Mermas.</summary>
        public static void RegistrarMermaProduccion(ResumenMermaProduccion resumen)
        {
            if (resumen.MermaKg <= 0) return;
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Mermas
                    (Fecha, Tipo, Descripcion, CantidadKg, Porcentaje, Origen)
                VALUES ($f, 'Produccion', $desc, $kg, $pct, 'Produccion diaria')";
            cmd.Parameters.AddWithValue("$f",   resumen.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$desc",
                $"Merma produccion {resumen.TipoMaterial} {resumen.Fecha:dd/MM/yyyy}");
            cmd.Parameters.AddWithValue("$kg",  (double)resumen.MermaKg);
            cmd.Parameters.AddWithValue("$pct", (double)resumen.PctMerma);
            cmd.ExecuteNonQuery();
        }

        // ── Helpers privados ──────────────────────────────────────────────
        private static List<DetalleProduccionMolde> ObtenerDetalleMoldes(int idReg)
        {
            var list = new List<DetalleProduccionMolde>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT d.Id, d.IdRegistro, d.CodigoMolde,
                       COALESCE(m.Descripcion,d.CodigoMolde),
                       d.CodigoProducto,
                       COALESCE(i.Descripcion,d.CodigoProducto),
                       d.Cantidad,
                       COALESCE(d.CantPrimera,0),
                       COALESCE(d.CantSegunda,0),
                       COALESCE(d.CantReciclado,0),
                       COALESCE(d.CantDefectuosas,0)
                FROM DetalleProduccionMolde d
                LEFT JOIN TbMoldes  m ON m.CodigoMold = d.CodigoMolde
                LEFT JOIN Inventario i ON i.Codigo    = d.CodigoProducto
                WHERE d.IdRegistro = $id ORDER BY d.CodigoMolde";
            cmd.Parameters.AddWithValue("$id", idReg);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new DetalleProduccionMolde {
                    Id              = r.GetInt32(0),
                    IdRegistro      = r.GetInt32(1),
                    CodigoMolde     = S(r,2),
                    DescMolde       = S(r,3),
                    CodigoProducto  = S(r,4),
                    DescProducto    = S(r,5),
                    Cantidad        = D(r,6),
                    CantPrimera     = D(r,7),
                    CantSegunda     = D(r,8),
                    CantReciclado   = D(r,9),
                    CantDefectuosas = D(r,10)
                });
            return list;
        }

        private static List<MaterialProduccionDia> ObtenerDetalleMateriales(int idReg)
        {
            var list = new List<MaterialProduccionDia>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT mat.Id, mat.IdRegistro, mat.CodigoInsumo,
                       COALESCE(i.Descripcion,mat.CodigoInsumo), mat.Cantidad
                FROM MaterialProduccionDia mat
                LEFT JOIN Inventario i ON i.Codigo = mat.CodigoInsumo
                WHERE mat.IdRegistro = $id ORDER BY mat.CodigoInsumo";
            cmd.Parameters.AddWithValue("$id", idReg);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MaterialProduccionDia {
                    Id           = r.GetInt32(0),
                    IdRegistro   = r.GetInt32(1),
                    CodigoInsumo = S(r,2),
                    DescInsumo   = S(r,3),
                    Cantidad     = D(r,4)
                });
            return list;
        }

        private static void RegistrarMovimientoTx(
            SqliteConnection c, SqliteTransaction tx,
            string codigo, string tipo, decimal cantidad,
            string concepto, string refNum, string usuario)
        {
            try
            {
                var cmdS = c.CreateCommand(); cmdS.Transaction = tx;
                cmdS.CommandText =
                    "SELECT COALESCE(Existencia,0) FROM Inventario WHERE Codigo=$cod";
                cmdS.Parameters.AddWithValue("$cod", codigo);
                decimal saldoAnt = D(cmdS.ExecuteScalar());
                decimal nuevoSaldo = tipo == "ENTRADA"
                    ? saldoAnt + cantidad : saldoAnt - cantidad;

                var cmdM = c.CreateCommand(); cmdM.Transaction = tx;
                cmdM.CommandText = @"
                    INSERT INTO MovimientosInventario
                        (CodigoProducto,TipoMovimiento,Cantidad,Concepto,
                         Referencia,SaldoAnterior,NuevoSaldo,FechaHora,Usuario)
                    VALUES ($cod,$tipo,$cant,$conc,$ref,$sa,$ns,$fh,$usr)";
                cmdM.Parameters.AddWithValue("$cod",  codigo);
                cmdM.Parameters.AddWithValue("$tipo", tipo);
                cmdM.Parameters.AddWithValue("$cant", (double)cantidad);
                cmdM.Parameters.AddWithValue("$conc", concepto);
                cmdM.Parameters.AddWithValue("$ref",  refNum);
                cmdM.Parameters.AddWithValue("$sa",   (double)saldoAnt);
                cmdM.Parameters.AddWithValue("$ns",   (double)nuevoSaldo);
                cmdM.Parameters.AddWithValue("$fh",   DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdM.Parameters.AddWithValue("$usr",  usuario);
                cmdM.ExecuteNonQuery();
            }
            catch { /* No interrumpir si kardex falla */ }
        }

        // ── Resumen para reportes ─────────────────────────────────────────
        public static List<(string Codigo, string Desc, decimal Total)>
            ResumenProduccionRango(DateTime desde, DateTime hasta)
        {
            var list = new List<(string, string, decimal)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT d.CodigoProducto,
                       COALESCE(i.Descripcion,d.CodigoProducto),
                       SUM(d.Cantidad)
                FROM DetalleProduccionMolde d
                JOIN RegistroProduccionDia r ON r.Id = d.IdRegistro
                LEFT JOIN Inventario i ON i.Codigo = d.CodigoProducto
                WHERE r.Fecha >= $d AND r.Fecha <= $h
                GROUP BY d.CodigoProducto ORDER BY SUM(d.Cantidad) DESC";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add((S(r,0), S(r,1), D(r,2)));
            return list;
        }

        public static decimal CalcularBonoProduccion(
            int idEmpleado, DateTime desde, DateTime hasta)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(d.Cantidad * t.TarifaBs),0)
                FROM DetalleProduccionMolde d
                JOIN RegistroProduccionDia r ON r.Id = d.IdRegistro
                JOIN TarifasBonoProduccion t
                  ON t.CodigoProducto = d.CodigoProducto
                 AND t.IdEmpleado     = $id
                 AND r.Fecha >= t.FechaDesde
                 AND (t.FechaHasta IS NULL OR r.Fecha <= t.FechaHasta)
                WHERE r.Fecha >= $d AND r.Fecha <= $h";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$d",  desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h",  hasta.ToString("yyyy-MM-dd"));
            return D(cmd.ExecuteScalar());
        }

        public static decimal CalcularBonoVentas(
            int idEmpleado, DateTime desde, DateTime hasta)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(df.Cantidad * t.TarifaBs),0)
                FROM DetallesFactura df
                JOIN Facturas f ON f.rowid = df.IdFactura
                JOIN TarifasBonoVentas t
                  ON t.CodigoProducto = df.Codigo
                 AND t.IdEmpleado     = $id
                 AND f.Fecha >= t.FechaDesde
                 AND (t.FechaHasta IS NULL OR f.Fecha <= t.FechaHasta)
                WHERE f.Fecha >= $d AND f.Fecha <= $h";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$d",  desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h",  hasta.ToString("yyyy-MM-dd"));
            return D(cmd.ExecuteScalar());
        }

        // ── Migración v25: campo CantDefectuosas ──────────────────────────
        /// <summary>
        /// Agrega la columna CantDefectuosas a DetalleProduccionMolde.
        /// Las piezas defectuosas se registran pero NO se suman al inventario.
        /// Llamado desde MigrarEsquema() en DB.cs cuando versionActual &lt; 25.
        /// </summary>
        public static void MigrarV25(SqliteConnection conn, SqliteTransaction tx)
        {
            _ultimoSqlMigracion = "[v25] inicio MigrarV25 — CantDefectuosas";
            // AlterTableSafe ignora el error si la columna ya existe
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            try
            {
                cmd.CommandText =
                    "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantDefectuosas REAL DEFAULT 0";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("duplicate column") &&
                    !ex.Message.Contains("already has a column"))
                    throw;
            }
        }
    }
}
