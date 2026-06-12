using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  MODELOS
    // ════════════════════════════════════════════════════════════════════════

    public class Inventario
    {
        public string   Codigo        { get; set; }
        public string   Descripcion   { get; set; }
        public string   Departamento  { get; set; }
        public string   Unidad        { get; set; }
        public string   Enser         { get; set; }
        public decimal  CostoActual   { get; set; }
        public decimal  CostoAnt      { get; set; }
        public decimal  CostoPro      { get; set; }
        public decimal  Precio        { get; set; }
        public decimal  Existencia    { get; set; }
        public decimal  ExistenMin    { get; set; }
        public decimal  ExistenMax    { get; set; }
        public string   Proveedores   { get; set; }
        public DateTime UltimaActualz { get; set; }
        public string   CodProveedor  { get; set; }
        public decimal  PesoKg        { get; set; }  // peso en kg de una unidad
    }

    public class DetalleFactura
    {
        public int     IdFactura  { get; set; }
        public string  Codigo     { get; set; }
        public decimal Cantidad   { get; set; }
    }

    public class DetalleCompra
    {
        public int     IdCompra   { get; set; }
        public string  CodigoI    { get; set; }
        public decimal Cantidad   { get; set; }
        public decimal PrecioC    { get; set; }
    }

    public class Produccion
    {
        public string   Codigo    { get; set; }
        public DateTime Fecha     { get; set; }
        public decimal  Cantidad  { get; set; }
    }

    public class Factura
    {
        public string   NumeroF       { get; set; }
        public DateTime Fecha         { get; set; }
        public string   Codigo        { get; set; }   // cliente
        public decimal  Descuento     { get; set; }
        public string   Transporte    { get; set; }
        public string   Observaciones { get; set; }
        public string   FormaDePago   { get; set; }
        public decimal  Abono         { get; set; }
        public decimal  IvaF          { get; set; }
        public int      DiasCreditoF  { get; set; }
        public decimal  TotalF        { get; set; }
        // ── Trazabilidad de tasa (v19) ─────────────────────────────────────
        // Snapshot inmutable de la tasa que estaba vigente cuando se emitió
        // la factura. Permite que reportes históricos no cambien si la tasa
        // del día se modifica después.
        public int      IdTasaCambio       { get; set; }
        public decimal  TasaBCVUsada       { get; set; }
        public decimal  TasaParaleloUsada  { get; set; }
        // ── Anulación (v21) ────────────────────────────────────────────────
        // No se eliminan facturas (rompe correlativo SENIAT). Se marcan como
        // anuladas y todos los reportes/CxC las excluyen. La fila queda en
        // BD para auditoría con motivo, fecha y usuario que la anuló.
        public bool     Anulada            { get; set; }
        public string   MotivoAnulacion    { get; set; }
        public DateTime?FechaAnulacion     { get; set; }
        public string   UsuarioAnulacion   { get; set; }
        // ── Tipo de documento (v22) ────────────────────────────────────────
        // Se deduce automáticamente al guardar:
        //   IVA > 0                       → FacturaConIVA
        //   IVA = 0 y EsExportacion = 1   → FacturaExportacion (legal)
        //   IVA = 0 y EsExportacion = 0   → NotaEntrega (sin valor fiscal)
        // Las NotaEntrega NO se cuentan en reportes/dashboard/CxC a menos
        // que un Admin active explícitamente Sesion.MostrarNotasEntrega.
        public string   TipoDocumento      { get; set; } = "FacturaConIVA";
        public bool     EsExportacion      { get; set; }
        public string   DestinoExportacion { get; set; }

        // Helpers de conveniencia
        public bool EsNotaEntrega        => TipoDocumento == "NotaEntrega";
        public bool EsFacturaConIVA      => TipoDocumento == "FacturaConIVA";
        public bool EsFacturaExportacion => TipoDocumento == "FacturaExportacion";
    }

    public class Compra
    {
        public string   CompraN       { get; set; }
        public DateTime FechaC        { get; set; }
        public string   CodigoP       { get; set; }   // proveedor
        public decimal  DescuentoC    { get; set; }
        public string   FormaDePago   { get; set; }
        public decimal  AbonoC        { get; set; }
        public decimal  IvaC          { get; set; }
        public int      DiasDeCred    { get; set; }
        public decimal  TotalC        { get; set; }
        public string   Transporte    { get; set; }
        public string   Observaciones { get; set; }
    }

    public class Cliente
    {
        public string   Codigo        { get; set; }
        public string   Descripcion   { get; set; }
        public string   Direccion     { get; set; }
        public string   Ciudad        { get; set; }
        public string   Estado        { get; set; }
        public string   CiRif         { get; set; }
        public string   Nit           { get; set; }
        public string   Fax           { get; set; }
        public string   Telefono      { get; set; }
        public string   Telefono2     { get; set; }
        public decimal  Credito       { get; set; }
        public string   TipoCliente   { get; set; }
        public DateTime Fecha         { get; set; }
        public string   Observaciones { get; set; }
        public decimal  Descuento     { get; set; }
        public decimal  AFavor        { get; set; }
    }

    public class Proveedor
    {
        public string   Codigo        { get; set; }
        public string   Descripcion   { get; set; }
        public string   Representante { get; set; }
        public string   Direccion     { get; set; }
        public string   Telefono1     { get; set; }
        public string   Telefono2     { get; set; }
        public string   Fax1          { get; set; }
        public string   Fax2          { get; set; }
        public int      DiasCredito   { get; set; }
        public decimal  PctRetencion  { get; set; }
        public DateTime FechaInicio   { get; set; }
        public string   CiRif         { get; set; }
        public string   Nit           { get; set; }
        public string   Tipo          { get; set; }
        public string   Observaciones { get; set; }
    }

    public class Abono
    {
        public int      NumAbono   { get; set; }
        public string   NumeroF    { get; set; }
        public DateTime Fecha      { get; set; }
        public decimal  Monto      { get; set; }
        public string   Cliente    { get; set; }
    }

    public class TbMolde
    {
        public string   CodigoMold     { get; set; }
        public string   Descripcion    { get; set; }
        public DateTime Fecha          { get; set; }
        public DateTime FechaLastMant  { get; set; }
        public string   DescMant       { get; set; }
        public string   ModeloQfabrica { get; set; }
        public string   Observaciones  { get; set; }
        public decimal  CantMatXunid   { get; set; }
        public string   CodigoProducto { get; set; }
    }

    public class LineaProducDia
    {
        public DateTime FechaLinea { get; set; }
        public string   Molde      { get; set; }
        public string   Puesto     { get; set; }
    }

    public class ProduccionDelDiaEnLinea
    {
        public DateTime FechaLinea        { get; set; }
        public decimal  CantidadProducida { get; set; }
        public decimal  SacosConsumidos   { get; set; }
        public decimal  FustesMalos       { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DATABASE SERVICE
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    //  LINEA DE FACTURA  (modelo independiente)
    // ════════════════════════════════════════════════════════════════════════
    public class LineaFactura
    {
        public int     IdFactura   { get; set; }
        public string  CodigoItem  { get; set; }
        public string  Descripcion { get; set; }
        public decimal Cantidad    { get; set; }
        public decimal PrecioUnit  { get; set; }
        public decimal Descuento   { get; set; }
        public decimal Subtotal    => Cantidad * PrecioUnit * (1 - Descuento / 100);
    }

    



    // ════════════════════════════════════════════════════════════════════════
    //  MANTENIMIENTO DE MOLDES
    // ════════════════════════════════════════════════════════════════════════
    public class RegistroMantenimiento
    {
        public int      Id             { get; set; }
        public string   CodigoMolde    { get; set; }
        public DateTime FechaRealizada { get; set; }
        public string   TipoMant       { get; set; }  // Preventivo / Correctivo / Predictivo
        public string   Tecnico        { get; set; }
        public string   Descripcion    { get; set; }
        public decimal  CostoMant      { get; set; }
        public int      DuracionHoras  { get; set; }
        public string   Resultado      { get; set; }  // Completado / Pendiente / Cancelado
        public string   Observaciones  { get; set; }
        public DateTime ProximoMant    { get; set; }
    }

    public class ProgramaMant
    {
        public int      Id             { get; set; }
        public string   CodigoMolde    { get; set; }
        public string   DescMolde      { get; set; }
        public string   TipoMant       { get; set; }
        public int      FrecuenciaDias { get; set; }   // cada N dias
        public DateTime UltimoMant     { get; set; }
        public DateTime ProximoMant    => UltimoMant.AddDays(FrecuenciaDias);
        public int      DiasRestantes  => (ProximoMant - DateTime.Today).Days;
        public string   Estado         => DiasRestantes < 0  ? "VENCIDO"
                                        : DiasRestantes <= 7 ? "URGENTE"
                                        : DiasRestantes <= 30? "PROXIMO"
                                        : "OK";
        public bool     Activo         { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CONTROL DE CALIDAD
    // ════════════════════════════════════════════════════════════════════════
    public class InspeccionCalidad
    {
        public int      Id               { get; set; }
        public int      IdIngreso        { get; set; }  // FK IngresoProduccion
        public string   Lote             { get; set; }
        public string   ProductoCod      { get; set; }
        public DateTime FechaInspeccion  { get; set; }
        public string   Inspector        { get; set; }
        // Cantidades
        public decimal  CantidadTotal    { get; set; }
        public decimal  CantidadAprobada { get; set; }
        public decimal  CantidadRechazada{ get; set; }
        public decimal  CantidadRetrabajo{ get; set; }
        // Criterios (puntuacion 1-5 cada uno)
        public int      PuntajeDimensiones  { get; set; }
        public int      PuntajeApariencia   { get; set; }
        public int      PuntajeFuncional    { get; set; }
        public int      PuntajeEtiquetado   { get; set; }
        // Resultado
        public string   Resultado        { get; set; }  // Aprobado / Rechazado / Retrabajo
        public string   Observaciones    { get; set; }
        public string   AccionCorrectiva { get; set; }
        // Calculados
        public decimal  PctAprobado      => CantidadTotal > 0
            ? Math.Round(CantidadAprobada / CantidadTotal * 100, 1) : 0;
        public decimal  PctRechazado     => CantidadTotal > 0
            ? Math.Round(CantidadRechazada / CantidadTotal * 100, 1) : 0;
        public decimal  PuntajePromedio  =>
            Math.Round((PuntajeDimensiones + PuntajeApariencia +
                        PuntajeFuncional   + PuntajeEtiquetado) / 4.0m, 1);
    }

    public class DefectoCalidad
    {
        public int    IdInspeccion { get; set; }
        public string TipoDefecto  { get; set; }
        public int    Cantidad     { get; set; }
        public string Descripcion  { get; set; }
        public string Severidad    { get; set; }  // Critico / Mayor / Menor
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ORDENES DE PRODUCCION
    // ════════════════════════════════════════════════════════════════════════
    public class OrdenProduccion
    {
        public int      Id            { get; set; }
        public string   NumOrden      { get; set; }   // OP-0001
        public string   ProductoCod   { get; set; }   // producto a fabricar
        public string   ProductoDesc  { get; set; }
        public decimal  CantidadPlan  { get; set; }   // unidades planificadas
        public decimal  CantidadReal  { get; set; }   // unidades producidas al cerrar
        public DateTime FechaInicio   { get; set; }
        public DateTime FechaFin      { get; set; }
        public string   Estado        { get; set; }   // Borrador/En proceso/Completada/Cancelada
        public string   Responsable   { get; set; }
        public string   Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class LineaOrden   // materia prima requerida
    {
        public int     IdOrden      { get; set; }
        public string  MateriaCod   { get; set; }
        public string  MateriaDesc  { get; set; }
        public decimal CantidadReq  { get; set; }   // requerida por unidad producida
        public decimal CantidadTot  { get; set; }   // CantidadReq * CantidadPlan
        public decimal StockActual  { get; set; }   // snapshot al momento de verificar
        public bool    Disponible   => StockActual >= CantidadTot;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MODELO DE RENTABILIDAD POR PRODUCTO
    // ════════════════════════════════════════════════════════════════════════
    public class RentabilidadProducto
    {
        public string  Codigo           { get; set; }
        public string  Descripcion      { get; set; }
        public string  Departamento     { get; set; }
        public string  Unidad           { get; set; }
        public decimal CostoUnitario    { get; set; }
        public decimal PrecioVenta      { get; set; }
        public decimal UnidadesVendidas { get; set; }
        public decimal UnidadesCompradas{ get; set; }
        public decimal IngresoTotal     { get; set; }
        public decimal CostoTotal       { get; set; }
        public decimal UtilidadBruta    => IngresoTotal - CostoTotal;
        public decimal MargenPct        => IngresoTotal > 0
            ? Math.Round(UtilidadBruta / IngresoTotal * 100, 2) : 0;
        public decimal StockActual      { get; set; }
        public decimal ValorInventario  => StockActual * CostoUnitario;
        public decimal RotacionMeses    { get; set; }  // meses promedio para vender el stock
    }


    // ════════════════════════════════════════════════════════════════════════
    //  PREFERENCIAS DE USUARIO
    // ════════════════════════════════════════════════════════════════════════
    public class PreferenciasUsuario
    {
        public string NombreUsuario   { get; set; }
        public string Tema            { get; set; } = "Claro";  // Claro / Oscuro
        public string ColorAcento     { get; set; } = "Azul";
        public bool   MostrarKpis     { get; set; } = true;
        public bool   SonidoAlertas   { get; set; } = false;
        public int    FilasPorPagina  { get; set; } = 50;
        public bool   ConfirmarEliminar{ get; set; } = true;
        public bool   AbrirUltimoModulo{ get; set; } = false;
        public string NombreCompleto  { get; set; } = "";
        public string Email           { get; set; } = "";
        public string Telefono        { get; set; } = "";
    }


    // ════════════════════════════════════════════════════════════════════════
    //  NOMINA

    // ════════════════════════════════════════════════════════════════════════
    //  APORTES PATRONALES POR PERIODO
    // ════════════════════════════════════════════════════════════════════════
    public class AportePatronal
    {
        public int      Id                { get; set; }
        public int      IdPeriodo         { get; set; }
        public int      IdEmpleado        { get; set; }
        public string   NombreEmpleado    { get; set; }
        // Base de calculo
        public decimal  SalarioNormalBs   { get; set; }  // TotalDevengado * TasaBCV
        public decimal  TasaBCV           { get; set; }
        // IVSS patron (9% riesgo minimo por defecto, configurable 9-11%)
        public decimal  PorcentajeIVSS    { get; set; }  // default 9.00
        public decimal  IVSSPatronBs      { get; set; }  // SalarioNormalBs * PorcentajeIVSS/100
        // RPVH / FAOV patron 2%
        public decimal  RPVHPatronBs      { get; set; }  // SalarioNormalBs * 0.02
        // INCES 2% sobre nomina total
        public decimal  INCESBs           { get; set; }  // SalarioNormalBs * 0.02
        // Bono Alimentacion CESTA (0.5 UT por dia laboral)
        public decimal  DiasLaborales     { get; set; }
        public decimal  ValorUTBs         { get; set; }  // valor UT vigente en Bs
        public decimal  CestaBs           { get; set; }  // DiasLaborales * 0.5 * ValorUTBs
        // Total costo patronal adicional
        public decimal  TotalAportesBs    => IVSSPatronBs + RPVHPatronBs + INCESBs + CestaBs;
        public decimal  TotalAportesUSD   => TasaBCV > 0 ? TotalAportesBs / TasaBCV : 0;
    }

    /// <summary>Resumen de aportes patronales de todo un periodo.</summary>
    public class ResumenAportesPatronales
    {
        public int      IdPeriodo         { get; set; }
        public string   DescPeriodo       { get; set; }
        public decimal  NominaTotal       { get; set; }  // suma devengados USD
        public decimal  NominaTotalBs     { get; set; }
        public decimal  IVSSPatronBs      { get; set; }
        public decimal  RPVHPatronBs      { get; set; }
        public decimal  INCESBs           { get; set; }
        public decimal  CestaBs           { get; set; }
        public decimal  TotalAportesBs    => IVSSPatronBs + RPVHPatronBs + INCESBs + CestaBs;
        public decimal  CostoTotalBs      { get; set; }  // NominaTotalBs + TotalAportesBs
        public int      NumEmpleados      { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    public class Empleado
    {
        public int      Id               { get; set; }
        public string   Cedula           { get; set; }
        public string   Nombre           { get; set; }
        public string   Apellido         { get; set; }
        public string   NombreCompleto   => Nombre + " " + Apellido;
        public string   Cargo            { get; set; }
        public string   TipoEmpleado     { get; set; }  // Produccion / Oficina / Fijo
        public DateTime FechaIngreso     { get; set; }
        public bool     Activo           { get; set; } = true;
        public string   Telefono         { get; set; }
        public string   Email            { get; set; }
        // Parametros salariales
        public decimal  SueldoDiarioUSD  { get; set; }  // base diario en USD
        public decimal  TarifaPiezaUSD   { get; set; }  // solo Produccion
        public decimal  TarifaVentaUSD   { get; set; }  // solo Oficina ($ fijo/venta)
        // Datos para beneficios legales
        public decimal  SalarioNormalUSD { get; set; }  // salario normal mensual para LOTTT
        public int      AniosServicio    => Math.Max(0, (int)((DateTime.Today - FechaIngreso).TotalDays / 365.25));
        public bool     TipEmpleadoEs(string tipo) => TipoEmpleado?.Equals(tipo, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public class PeriodoNomina
    {
        public int      Id               { get; set; }
        public DateTime FechaDesde       { get; set; }
        public DateTime FechaHasta       { get; set; }
        public decimal  TasaBCV          { get; set; }  // Bs por USD vigente esa semana
        public string   Estado           { get; set; }  // Abierto / Cerrado
        public string   Observaciones    { get; set; }
        public string   DescPeriodo      => FechaDesde.ToString("dd/MM") + " al " + FechaHasta.ToString("dd/MM/yyyy");
    }

    public class DetalleNomina
    {
        public int      Id               { get; set; }
        public int      IdPeriodo        { get; set; }
        public int      IdEmpleado       { get; set; }
        public string   NombreEmpleado   { get; set; }
        public string   TipoEmpleado     { get; set; }
        // Dias y produccion
        public decimal  DiasTrabajados   { get; set; }
        public decimal  DiasInasistencia { get; set; }
        public decimal  PiezasProducidas { get; set; }  // solo Produccion
        public decimal  VentasCerradas   { get; set; }  // solo Oficina (# facturas)
        // Devengado
        public decimal  SueldoDiarioUSD  { get; set; }
        public decimal  DevengadoSueldo  { get; set; }  // SueldoDiario * DiasTrabajados
        public decimal  DevengadoPiezas  { get; set; }  // piezas * tarifa
        public decimal  DevengadoVentas  { get; set; }  // ventas * tarifa
        public decimal  OtrosIngresos    { get; set; }  // bonos manuales
        public decimal  TotalDevengado   => DevengadoSueldo + DevengadoPiezas + DevengadoVentas + OtrosIngresos;
        // Deducciones
        public decimal  DeduccionIVSS    { get; set; }  // 4% sobre equiv. Bs
        public decimal  DeduccionRPVH    { get; set; }  // 1% sobre equiv. Bs
        public decimal  DescuentoFaltas  { get; set; }
        public decimal  CuotaPrestamo    { get; set; }
        public decimal  OtrasDeduccion   { get; set; }
        public decimal  TotalDeducciones => DeduccionIVSS + DeduccionRPVH + DescuentoFaltas + CuotaPrestamo + OtrasDeduccion;
        // Neto
        public decimal  NetoUSD          => TotalDevengado - TotalDeducciones;
        public decimal  TasaBCV          { get; set; }
        public decimal  NetoBs           => NetoUSD * TasaBCV;
        public string   Observaciones    { get; set; }
    }

    public class PrestamoEmpleado
    {
        public int      Id               { get; set; }
        public int      IdEmpleado       { get; set; }
        public string   NombreEmpleado   { get; set; }
        public DateTime FechaOtorgado    { get; set; }
        public decimal  MontoTotalUSD    { get; set; }
        public decimal  CuotaSemanalUSD  { get; set; }
        public decimal  SaldoPendiente   { get; set; }
        public string   Estado           { get; set; }  // Activo / Saldado / Cancelado
        public string   Descripcion      { get; set; }
    }

    public class BeneficioAnual
    {
        public int      Id               { get; set; }
        public int      IdEmpleado       { get; set; }
        public string   NombreEmpleado   { get; set; }
        public int      Anio             { get; set; }
        public string   TipoBeneficio    { get; set; }  // Utilidades / Vacaciones / Prestaciones
        // Calculo
        public decimal  DiasCorresponden { get; set; }
        public decimal  SalarioDiarioUSD { get; set; }
        public decimal  MontoUSD         { get; set; }
        public decimal  TasaBCV          { get; set; }
        public decimal  MontoBs          => MontoUSD * TasaBCV;
        public DateTime FechaPago        { get; set; }
        public string   Estado           { get; set; }  // Pendiente / Pagado
        public string   Observaciones    { get; set; }
    }

    /// <summary>
    /// DTO plano para reporte de ventas: una fila por línea de detalle, con
    /// la info de la factura y la tasa cambiaria del día ya inlineadas.
    /// Permite hacer todo el reporte con una sola query SQL (JOIN).
    /// </summary>
    public class LineaVentaPlana
    {
        public string   NumeroF      { get; set; }
        public DateTime Fecha        { get; set; }
        public string   Codigo       { get; set; }
        public string   Descripcion  { get; set; }
        public decimal  Cantidad     { get; set; }
        public decimal  PrecioUnit   { get; set; }
        public decimal  Subtotal     { get; set; }
        public decimal  TasaBCV      { get; set; }
        public decimal  TasaParalelo { get; set; }
    }

    public static partial class DatabaseService
    {
        private static string ConnStr => SecurityService.CadenaConexion;

        private static SqliteConnection Conn()
        {
            var c = new SqliteConnection(ConnStr);
            c.Open();
            // WAL mode: permite lecturas concurrentes mientras hay una escritura activa.
            // Reduce drásticamente "database is locked" con múltiples conexiones.
            using var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=3000;";
            cmd.ExecuteNonQuery();
            return c;
        }

        // ── INICIALIZAR TODAS LAS TABLAS ─────────────────────────────────────
        public static void Inicializar()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Inventario (
                Codigo        TEXT PRIMARY KEY,
                Descripcion   TEXT,
                Departamento  TEXT,
                Unidad        TEXT,
                Enser         TEXT,
                CostoActual   REAL DEFAULT 0,
                CostoAnt      REAL DEFAULT 0,
                CostoPro      REAL DEFAULT 0,
                Precio        REAL DEFAULT 0,
                Existencia    REAL DEFAULT 0,
                ExistenMin    REAL DEFAULT 0,
                ExistenMax    REAL DEFAULT 0,
                Proveedores   TEXT,
                UltimaActualz TEXT,
                CodProveedor  TEXT
            );

            CREATE TABLE IF NOT EXISTS Clientes (
                Codigo        TEXT PRIMARY KEY,
                Descripcion   TEXT,
                Direccion     TEXT,
                Ciudad        TEXT,
                Estado        TEXT,
                CiRif         TEXT,
                Nit           TEXT,
                Fax           TEXT,
                Telefono      TEXT,
                Telefono2     TEXT,
                Credito       REAL DEFAULT 0,
                TipoCliente   TEXT,
                Fecha         TEXT,
                Observaciones TEXT,
                Descuento     REAL DEFAULT 0,
                AFavor        REAL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Proveedores (
                Codigo        TEXT PRIMARY KEY,
                Descripcion   TEXT,
                Representante TEXT,
                Direccion     TEXT,
                Telefono1     TEXT,
                Telefono2     TEXT,
                Fax1          TEXT,
                Fax2          TEXT,
                DiasCredito   INTEGER DEFAULT 0,
                PctRetencion  REAL DEFAULT 0,
                FechaInicio   TEXT,
                CiRif         TEXT,
                Nit           TEXT,
                Tipo          TEXT,
                Observaciones TEXT
            );

            CREATE TABLE IF NOT EXISTS Facturas (
                NumeroF       TEXT PRIMARY KEY,
                Fecha         TEXT NOT NULL,
                Codigo        TEXT,
                Descuento     REAL DEFAULT 0,
                Transporte    TEXT,
                Observaciones TEXT,
                FormaDePago   TEXT,
                Abono         REAL DEFAULT 0,
                IvaF          REAL DEFAULT 0,
                DiasCreditoF  INTEGER DEFAULT 0,
                TotalF        REAL DEFAULT 0,
                FOREIGN KEY(Codigo) REFERENCES Clientes(Codigo)
            );

            CREATE TABLE IF NOT EXISTS DetallesFactura (
                IdFactura  INTEGER,
                Codigo     TEXT,
                Cantidad   REAL DEFAULT 0,
                PRIMARY KEY(IdFactura, Codigo),
                FOREIGN KEY(Codigo) REFERENCES Inventario(Codigo)
            );

            CREATE TABLE IF NOT EXISTS Compras (
                CompraN       TEXT PRIMARY KEY,
                FechaC        TEXT NOT NULL,
                CodigoP       TEXT,
                DescuentoC    REAL DEFAULT 0,
                FormaDePago   TEXT,
                AbonoC        REAL DEFAULT 0,
                IvaC          REAL DEFAULT 0,
                DiasDeCred    INTEGER DEFAULT 0,
                TotalC        REAL DEFAULT 0,
                Transporte    TEXT,
                Observaciones TEXT,
                FOREIGN KEY(CodigoP) REFERENCES Proveedores(Codigo)
            );

            CREATE TABLE IF NOT EXISTS DetallesCompra (
                IdCompra  INTEGER,
                CodigoI   TEXT,
                Cantidad  REAL DEFAULT 0,
                PrecioC   REAL DEFAULT 0,
                PRIMARY KEY(IdCompra, CodigoI),
                FOREIGN KEY(CodigoI) REFERENCES Inventario(Codigo)
            );

            CREATE TABLE IF NOT EXISTS Produccion (
                Codigo   TEXT,
                Fecha    TEXT,
                Cantidad REAL DEFAULT 0,
                PRIMARY KEY(Codigo, Fecha),
                FOREIGN KEY(Codigo) REFERENCES Inventario(Codigo)
            );

            CREATE TABLE IF NOT EXISTS Abonos (
                NumAbono  INTEGER PRIMARY KEY AUTOINCREMENT,
                NumeroF   TEXT,
                Fecha     TEXT NOT NULL,
                Monto     REAL DEFAULT 0,
                Cliente   TEXT,
                FOREIGN KEY(NumeroF) REFERENCES Facturas(NumeroF)
            );

            CREATE TABLE IF NOT EXISTS TbMoldes (
                CodigoMold     TEXT PRIMARY KEY,
                Descripcion    TEXT,
                Fecha          TEXT,
                FechaLastMant  TEXT,
                DescMant       TEXT,
                ModeloQfabrica TEXT,
                Observaciones  TEXT,
                CantMatXunid   REAL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS LineaProducDia (
                FechaLinea TEXT,
                Molde      TEXT,
                Puesto     TEXT,
                PRIMARY KEY(FechaLinea, Molde, Puesto),
                FOREIGN KEY(Molde) REFERENCES TbMoldes(CodigoMold)
            );

            CREATE TABLE IF NOT EXISTS ProduccionDelDiaEnLinea (
                FechaLinea        TEXT,
                CantidadProducida REAL DEFAULT 0,
                SacosConsumidos   REAL DEFAULT 0,
                FustesMalos       REAL DEFAULT 0,
                PRIMARY KEY(FechaLinea)
            );

            CREATE TABLE IF NOT EXISTS MovimientosInventario (
                IdMovimiento   INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora      TEXT    NOT NULL,
                CodigoProducto TEXT    NOT NULL,
                TipoMovimiento TEXT    NOT NULL,
                Concepto       TEXT,
                Cantidad       REAL    NOT NULL DEFAULT 0,
                SaldoAnterior  REAL    NOT NULL DEFAULT 0,
                NuevoSaldo     REAL    NOT NULL DEFAULT 0,
                Referencia     TEXT,
                Usuario        TEXT,
                FOREIGN KEY(CodigoProducto) REFERENCES Inventario(Codigo)
            );

                        CREATE TABLE IF NOT EXISTS IngresoProduccion (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Producto    TEXT NOT NULL,
                Lote        TEXT,
                Cantidad    INTEGER DEFAULT 0,
                Unidad      TEXT,
                Fecha       TEXT NOT NULL,
                Responsable TEXT,
                Observacion TEXT,
                Estado      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CuentasCobrar (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Cliente      TEXT NOT NULL,
                Concepto     TEXT NOT NULL,
                Monto        REAL NOT NULL,
                Pagado       REAL DEFAULT 0,
                FechaEmision TEXT NOT NULL,
                FechaVence   TEXT NOT NULL,
                Estado       TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProductosPrecios (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo              TEXT NOT NULL,
                Nombre              TEXT NOT NULL,
                Categoria           TEXT,
                CostoActual         REAL DEFAULT 0,
                PrecioActual        REAL DEFAULT 0,
                CostoNuevo          REAL DEFAULT 0,
                PrecioNuevo         REAL DEFAULT 0,
                UltimaActualizacion TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Usuarios (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                NombreUsuario  TEXT NOT NULL UNIQUE,
                NombreCompleto TEXT NOT NULL,
                PasswordHash   TEXT NOT NULL,
                Rol            TEXT NOT NULL DEFAULT 'Ventas',
                Activo         INTEGER NOT NULL DEFAULT 1,
                FechaCreacion  TEXT NOT NULL,
                UltimoAcceso   TEXT
            );

            CREATE TABLE IF NOT EXISTS Empleados (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                Cedula           TEXT    NOT NULL UNIQUE,
                Nombre           TEXT    NOT NULL,
                Apellido         TEXT    NOT NULL,
                Cargo            TEXT,
                TipoEmpleado     TEXT    NOT NULL DEFAULT 'Fijo',
                FechaIngreso     TEXT    NOT NULL,
                Activo           INTEGER NOT NULL DEFAULT 1,
                Telefono         TEXT,
                Email            TEXT,
                SueldoDiarioUSD  REAL    NOT NULL DEFAULT 0,
                TarifaPiezaUSD   REAL    NOT NULL DEFAULT 0,
                TarifaVentaUSD   REAL    NOT NULL DEFAULT 0,
                SalarioNormalUSD REAL    NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS PeriodosNomina (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaDesde       TEXT    NOT NULL,
                FechaHasta       TEXT    NOT NULL,
                TasaBCV          REAL    NOT NULL DEFAULT 1,
                Estado           TEXT    NOT NULL DEFAULT 'Abierto',
                Observaciones    TEXT
            );

            CREATE TABLE IF NOT EXISTS DetallesNomina (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                IdPeriodo        INTEGER NOT NULL,
                IdEmpleado       INTEGER NOT NULL,
                NombreEmpleado   TEXT,
                TipoEmpleado     TEXT,
                DiasTrabajados   REAL    DEFAULT 0,
                DiasInasistencia REAL    DEFAULT 0,
                PiezasProducidas REAL    DEFAULT 0,
                VentasCerradas   REAL    DEFAULT 0,
                SueldoDiarioUSD  REAL    DEFAULT 0,
                DevengadoSueldo  REAL    DEFAULT 0,
                DevengadoPiezas  REAL    DEFAULT 0,
                DevengadoVentas  REAL    DEFAULT 0,
                OtrosIngresos    REAL    DEFAULT 0,
                DeduccionIVSS    REAL    DEFAULT 0,
                DeduccionRPVH    REAL    DEFAULT 0,
                DescuentoFaltas  REAL    DEFAULT 0,
                CuotaPrestamo    REAL    DEFAULT 0,
                OtrasDeduccion   REAL    DEFAULT 0,
                TasaBCV          REAL    DEFAULT 1,
                Observaciones    TEXT,
                FOREIGN KEY(IdPeriodo)  REFERENCES PeriodosNomina(Id),
                FOREIGN KEY(IdEmpleado) REFERENCES Empleados(Id)
            );

            CREATE TABLE IF NOT EXISTS PrestamosEmpleado (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                IdEmpleado       INTEGER NOT NULL,
                NombreEmpleado   TEXT,
                FechaOtorgado    TEXT    NOT NULL,
                MontoTotalUSD    REAL    NOT NULL DEFAULT 0,
                CuotaSemanalUSD  REAL    NOT NULL DEFAULT 0,
                SaldoPendiente   REAL    NOT NULL DEFAULT 0,
                Estado           TEXT    NOT NULL DEFAULT 'Activo',
                Descripcion      TEXT,
                FOREIGN KEY(IdEmpleado) REFERENCES Empleados(Id)
            );

            CREATE TABLE IF NOT EXISTS BeneficiosAnuales (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                IdEmpleado       INTEGER NOT NULL,
                NombreEmpleado   TEXT,
                Anio             INTEGER NOT NULL,
                TipoBeneficio    TEXT    NOT NULL,
                DiasCorresponden REAL    DEFAULT 0,
                SalarioDiarioUSD REAL    DEFAULT 0,
                MontoUSD         REAL    DEFAULT 0,
                TasaBCV          REAL    DEFAULT 1,
                FechaPago        TEXT,
                Estado           TEXT    NOT NULL DEFAULT 'Pendiente',
                Observaciones    TEXT,
                FOREIGN KEY(IdEmpleado) REFERENCES Empleados(Id)
            );

            CREATE TABLE IF NOT EXISTS AportesPatronales (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                IdPeriodo       INTEGER NOT NULL,
                IdEmpleado      INTEGER NOT NULL,
                NombreEmpleado  TEXT,
                SalarioNormalBs REAL    DEFAULT 0,
                TasaBCV         REAL    DEFAULT 1,
                PorcentajeIVSS  REAL    DEFAULT 9.0,
                IVSSPatronBs    REAL    DEFAULT 0,
                RPVHPatronBs    REAL    DEFAULT 0,
                INCESBs         REAL    DEFAULT 0,
                DiasLaborales   REAL    DEFAULT 0,
                ValorUTBs       REAL    DEFAULT 0,
                CestaBs         REAL    DEFAULT 0,
                FOREIGN KEY(IdPeriodo)  REFERENCES PeriodosNomina(Id),
                FOREIGN KEY(IdEmpleado) REFERENCES Empleados(Id)
            );

            CREATE TABLE IF NOT EXISTS PreferenciasUsuario (
                NombreUsuario    TEXT PRIMARY KEY,
                Tema             TEXT DEFAULT 'Claro',
                ColorAcento      TEXT DEFAULT 'Azul',
                MostrarKpis      INTEGER DEFAULT 1,
                SonidoAlertas    INTEGER DEFAULT 0,
                FilasPorPagina   INTEGER DEFAULT 50,
                ConfirmarEliminar INTEGER DEFAULT 1,
                AbrirUltimoModulo INTEGER DEFAULT 0,
                NombreCompleto   TEXT DEFAULT '',
                Email            TEXT DEFAULT '',
                Telefono         TEXT DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS LogAuditoria (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                FechaHora TEXT    NOT NULL,
                Usuario   TEXT    NOT NULL,
                Accion    TEXT    NOT NULL,
                Tabla     TEXT,
                Detalle   TEXT
            );

            CREATE TABLE IF NOT EXISTS RegistrosMantenimiento (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                CodigoMolde    TEXT    NOT NULL,
                FechaRealizada TEXT    NOT NULL,
                TipoMant       TEXT    NOT NULL DEFAULT 'Preventivo',
                Tecnico        TEXT,
                Descripcion    TEXT,
                CostoMant      REAL    DEFAULT 0,
                DuracionHoras  INTEGER DEFAULT 0,
                Resultado      TEXT    DEFAULT 'Completado',
                Observaciones  TEXT,
                ProximoMant    TEXT,
                FOREIGN KEY(CodigoMolde) REFERENCES TbMoldes(CodigoMold)
            );

            CREATE TABLE IF NOT EXISTS ProgramasMantenimiento (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                CodigoMolde    TEXT    NOT NULL UNIQUE,
                DescMolde      TEXT,
                TipoMant       TEXT    NOT NULL DEFAULT 'Preventivo',
                FrecuenciaDias INTEGER NOT NULL DEFAULT 90,
                UltimoMant     TEXT    NOT NULL,
                Activo         INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY(CodigoMolde) REFERENCES TbMoldes(CodigoMold)
            );

            CREATE TABLE IF NOT EXISTS InspeccionesCalidad (
                Id                INTEGER PRIMARY KEY AUTOINCREMENT,
                IdIngreso         INTEGER NOT NULL,
                Lote              TEXT,
                ProductoCod       TEXT,
                FechaInspeccion   TEXT    NOT NULL,
                Inspector         TEXT,
                CantidadTotal     REAL    DEFAULT 0,
                CantidadAprobada  REAL    DEFAULT 0,
                CantidadRechazada REAL    DEFAULT 0,
                CantidadRetrabajo REAL    DEFAULT 0,
                PuntajeDimensiones INTEGER DEFAULT 3,
                PuntajeApariencia  INTEGER DEFAULT 3,
                PuntajeFuncional   INTEGER DEFAULT 3,
                PuntajeEtiquetado  INTEGER DEFAULT 3,
                Resultado         TEXT    DEFAULT 'Aprobado',
                Observaciones     TEXT,
                AccionCorrectiva  TEXT,
                FOREIGN KEY(IdIngreso) REFERENCES IngresoProduccion(Id)
            );

            CREATE TABLE IF NOT EXISTS DefectosCalidad (
                IdInspeccion INTEGER NOT NULL,
                TipoDefecto  TEXT    NOT NULL,
                Cantidad     INTEGER DEFAULT 0,
                Descripcion  TEXT,
                Severidad    TEXT    DEFAULT 'Menor',
                PRIMARY KEY(IdInspeccion, TipoDefecto),
                FOREIGN KEY(IdInspeccion) REFERENCES InspeccionesCalidad(Id)
            );

            CREATE TABLE IF NOT EXISTS OrdenesProduccion (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                NumOrden      TEXT    NOT NULL UNIQUE,
                ProductoCod   TEXT    NOT NULL,
                ProductoDesc  TEXT,
                CantidadPlan  REAL    NOT NULL DEFAULT 0,
                CantidadReal  REAL    NOT NULL DEFAULT 0,
                FechaInicio   TEXT    NOT NULL,
                FechaFin      TEXT    NOT NULL,
                Estado        TEXT    NOT NULL DEFAULT 'Borrador',
                Responsable   TEXT,
                Observaciones TEXT,
                FechaCreacion TEXT    NOT NULL,
                FOREIGN KEY(ProductoCod) REFERENCES Inventario(Codigo)
            );

            CREATE TABLE IF NOT EXISTS LineasOrden (
                IdOrden      INTEGER NOT NULL,
                MateriaCod   TEXT    NOT NULL,
                MateriaDesc  TEXT,
                CantidadReq  REAL    NOT NULL DEFAULT 0,
                CantidadTot  REAL    NOT NULL DEFAULT 0,
                PRIMARY KEY(IdOrden, MateriaCod),
                FOREIGN KEY(IdOrden)    REFERENCES OrdenesProduccion(Id),
                FOREIGN KEY(MateriaCod) REFERENCES Inventario(Codigo)
            );

            CREATE TABLE IF NOT EXISTS Gastos (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Categoria   TEXT NOT NULL,
                Descripcion TEXT NOT NULL,
                Monto       REAL NOT NULL,
                Fecha       TEXT NOT NULL,
                Proveedor   TEXT,
                Estado      TEXT NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();

            // ── Fix defensivo: CategoriasGasto puede existir con esquema
            //    incompleto en BDs antiguas (pre-v20). Agregar columnas
            //    faltantes AQUÍ, fuera de la transacción de MigrarEsquema,
            //    para que las migraciones v10→v25 nunca fallen por
            //    "no such column: Orden/Color".
            //    Cada ALTER corre en autocommit; try/catch individual ignora
            //    "duplicate column" si ya existe. 100% idempotente.
            {
                using var chk = c.CreateCommand();
                chk.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='CategoriasGasto'";
                if (chk.ExecuteScalar() != null)
                {
                    string[] colsFix = {
                        "ALTER TABLE CategoriasGasto ADD COLUMN Color TEXT DEFAULT '#1A4A8A'",
                        "ALTER TABLE CategoriasGasto ADD COLUMN Activa INTEGER DEFAULT 1",
                        "ALTER TABLE CategoriasGasto ADD COLUMN Orden INTEGER DEFAULT 0",
                        "ALTER TABLE CategoriasGasto ADD COLUMN EsMateriaPrima INTEGER DEFAULT 0"
                    };
                    foreach (var sql in colsFix)
                    {
                        try
                        {
                            using var fix = c.CreateCommand();
                            fix.CommandText = sql;
                            fix.ExecuteNonQuery();
                        }
                        catch (Exception exFix)
                        {
                            // "duplicate column name" es esperado si ya existe
                            if (!exFix.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
                                Console.WriteLine($"[DB] WARN fix CategoriasGasto: {exFix.Message}");
                        }
                    }
                }
            }

            // ── Fix defensivo: Inventario.Activo (v17 en DB_Deploy nunca se invocó) ──
            {
                using var chk = c.CreateCommand();
                chk.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Inventario'";
                if (chk.ExecuteScalar() != null)
                {
                    try
                    {
                        using var fix = c.CreateCommand();
                        fix.CommandText = "ALTER TABLE Inventario ADD COLUMN Activo INTEGER NOT NULL DEFAULT 1";
                        fix.ExecuteNonQuery();
                        Console.WriteLine("[DB] Fix: columna Activo agregada a Inventario");
                    }
                    catch (Exception exFix)
                    {
                        if (!exFix.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
                            Console.WriteLine($"[DB] WARN fix Inventario.Activo: {exFix.Message}");
                    }
                }
            }

            // Aplicar migraciones incrementales al esquema existente
            MigrarEsquema(c);

            // ── Tablas opcionales (defensivo) ───────────────────────────────
            //   TasaCambio puede no existir en BDs migradas desde versiones
            //   antiguas. La creamos aqui para que el JOIN del reporte de
            //   ventas no falle con "no such table" cuando el usuario no
            //   ha registrado ninguna tasa todavia.
            using var cmdTabs = c.CreateCommand();
            cmdTabs.CommandText = @"
                CREATE TABLE IF NOT EXISTS TasaCambio (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha        TEXT    NOT NULL UNIQUE,
                    TasaBCV      REAL    NOT NULL DEFAULT 0,
                    TasaParalelo REAL    NOT NULL DEFAULT 0,
                    Usuario      TEXT
                );";
            try { cmdTabs.ExecuteNonQuery(); }
            catch (Exception ex) { Console.WriteLine("[DB] WARN crear TasaCambio: " + ex.Message); }

            // ── Indices para reportes (idempotente). ────────────────────────
            //   Sin esto, la query JOIN del reporte de ventas hace full-scan
            //   sobre Facturas (potencialmente 28 anos de historico) y se
            //   degrada de O(log n) a O(n*m). El primer arranque tras este
            //   parche puede tardar 1-3s creando los indices, despues es
            //   instantaneo y los reportes corren ~100x mas rapido.
            using var cmdIdx = c.CreateCommand();
            cmdIdx.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Facturas_Fecha
                    ON Facturas(Fecha);
                CREATE INDEX IF NOT EXISTS IX_DetallesFactura_IdFactura
                    ON DetallesFactura(IdFactura);
                CREATE INDEX IF NOT EXISTS IX_DetallesFactura_Codigo
                    ON DetallesFactura(Codigo);
                CREATE INDEX IF NOT EXISTS IX_TasaCambio_Fecha
                    ON TasaCambio(Fecha);
                CREATE INDEX IF NOT EXISTS IX_MovimientosInventario_FechaHora
                    ON MovimientosInventario(FechaHora);
                CREATE INDEX IF NOT EXISTS IX_MovimientosInventario_Codigo
                    ON MovimientosInventario(CodigoProducto);
                CREATE INDEX IF NOT EXISTS IX_CuentasCobrar_FechaVence
                    ON CuentasCobrar(FechaVence);
                CREATE INDEX IF NOT EXISTS IX_CuentasPagar_FechaVence
                    ON CuentasPagar(FechaVence);";
            try { cmdIdx.ExecuteNonQuery(); }
            catch (Exception ex) { Console.WriteLine("[DB] WARN crear indices: " + ex.Message); }
        }

        // ════════ FLUJO DE CAJA ══════════════════════════════════════════════
        /// <summary>
        /// Construye el flujo de caja proyectado a 90 dias cruzando:
        ///  - Ingresos: facturas con saldo pendiente (fecha vencimiento = emision + dias credito)
        ///  - Ingresos: cuentas por cobrar pendientes (su FechaVence)
        ///  - Egresos:  compras con saldo pendiente (fecha vencimiento = fechaC + diasCredito)
        ///  - Egresos:  nomina semanal proyectada (neto USD del ultimo periodo procesado)
        ///  - Egresos:  gastos pendientes
        ///  - Manual:   movimientos adicionales que el usuario puede agregar
        /// saldoInicial: efectivo disponible hoy (ingresado manualmente)
        /// </summary>
        public static (List<MovimientoCaja> Movimientos, ResumenFlujoCaja Resumen)
            ObtenerFlujoCaja(decimal saldoInicial = 0)
        {
            var hoy  = DateTime.Today;
            var mov  = new List<MovimientoCaja>();

            // ── 1. Facturas con saldo pendiente ───────────────────────────
            //   Excluye anuladas (no cobrables) y filas con fecha corrupta.
            foreach (var f in ObtenerFacturas(incluirAnuladas: false)
                                .Where(f => f.Abono >= 0 && (f.TotalF - f.Abono) > 0))
            {
                DateTime vence = f.DiasCreditoF > 0
                    ? f.Fecha.AddDays(f.DiasCreditoF)
                    : f.Fecha;
                if (vence < hoy) vence = hoy.AddDays(1);  // vencidas: cobrar "manana"
                if ((vence - hoy).TotalDays > 90) continue;

                mov.Add(new MovimientoCaja
                {
                    Fecha      = vence,
                    Tipo       = "Ingreso",
                    Categoria  = "CxC Factura",
                    Concepto   = "Factura " + f.NumeroF + " — " + f.Codigo,
                    MontoUSD   = f.TotalF - f.Abono,
                    Referencia = f.NumeroF,
                    EsReal     = false
                });
            }

            // ── 2. Cuentas por cobrar ─────────────────────────────────────
            foreach (var c in ObtenerCuentas().Where(c => c.Estado != "Pagado" && c.Saldo > 0))
            {
                if ((c.FechaVence - hoy).TotalDays > 90) continue;
                DateTime vence = c.FechaVence < hoy ? hoy.AddDays(1) : c.FechaVence;
                mov.Add(new MovimientoCaja
                {
                    Fecha      = vence,
                    Tipo       = "Ingreso",
                    Categoria  = "CxC",
                    Concepto   = c.Cliente + " — " + c.Concepto,
                    MontoUSD   = c.Saldo,
                    Referencia = c.Id.ToString(),
                    EsReal     = false
                });
            }

            // ── 3. Compras con saldo pendiente ────────────────────────────
            foreach (var cp in ObtenerCompras().Where(cp => cp.AbonoC >= 0 && (cp.TotalC - cp.AbonoC) > 0))
            {
                DateTime vence = cp.DiasDeCred > 0
                    ? cp.FechaC.AddDays(cp.DiasDeCred)
                    : cp.FechaC;
                if (vence < hoy) vence = hoy.AddDays(1);
                if ((vence - hoy).TotalDays > 90) continue;
                mov.Add(new MovimientoCaja
                {
                    Fecha      = vence,
                    Tipo       = "Egreso",
                    Categoria  = "Compra",
                    Concepto   = "Compra " + cp.CompraN + " — " + cp.CodigoP,
                    MontoUSD   = cp.TotalC - cp.AbonoC,
                    Referencia = cp.CompraN,
                    EsReal     = false
                });
            }

            // ── 4. Nomina semanal proyectada ──────────────────────────────
            // Toma el neto total del ultimo periodo cerrado y lo repite cada 7 dias
            var periodos = ObtenerPeriodos();
            var ultimoPeriodo = periodos.FirstOrDefault();
            if (ultimoPeriodo != null)
            {
                var detalles  = ObtenerDetallesNomina(ultimoPeriodo.Id);
                decimal neto  = detalles.Sum(d => d.NetoUSD);
                if (neto > 0)
                {
                    for (int semana = 1; semana <= 13; semana++)  // 13 semanas = 91 dias
                    {
                        DateTime fechaNom = hoy.AddDays(semana * 7);
                        if ((fechaNom - hoy).TotalDays > 90) break;
                        mov.Add(new MovimientoCaja
                        {
                            Fecha      = fechaNom,
                            Tipo       = "Egreso",
                            Categoria  = "Nomina",
                            Concepto   = "Nomina semanal (proyeccion)",
                            MontoUSD   = neto,
                            Referencia = "SEM-" + semana,
                            EsReal     = false
                        });
                    }
                }
            }

            // ── 5. Gastos pendientes ──────────────────────────────────────
            foreach (var g in ObtenerGastos().Where(g => g.Estado == "Pendiente"))
            {
                DateTime fechaGasto = g.Fecha < hoy ? hoy.AddDays(1) : g.Fecha;
                if ((fechaGasto - hoy).TotalDays > 90) continue;
                mov.Add(new MovimientoCaja
                {
                    Fecha      = fechaGasto,
                    Tipo       = "Egreso",
                    Categoria  = "Gasto",
                    Concepto   = g.Categoria + " — " + g.Descripcion,
                    MontoUSD   = g.Monto,
                    Referencia = g.Id.ToString(),
                    EsReal     = false
                });
            }

            // Ordenar por fecha
            mov.Sort((a, b) => a.Fecha.CompareTo(b.Fecha));

            // Calcular resumen por tramos
            var r = new ResumenFlujoCaja { SaldoInicial = saldoInicial };
            var lim30 = hoy.AddDays(30);
            var lim60 = hoy.AddDays(60);
            var lim90 = hoy.AddDays(90);

            foreach (var m in mov)
            {
                bool en30 = m.Fecha <= lim30;
                bool en60 = m.Fecha > lim30 && m.Fecha <= lim60;
                bool en90 = m.Fecha > lim60 && m.Fecha <= lim90;

                if (m.Tipo == "Ingreso")
                {
                    if (en30) r.IngreSos30 += m.MontoUSD;
                    else if (en60) r.IngreSos60 += m.MontoUSD;
                    else if (en90) r.IngreSos90 += m.MontoUSD;
                }
                else
                {
                    if (en30) r.Egresos30 += m.MontoUSD;
                    else if (en60) r.Egresos60 += m.MontoUSD;
                    else if (en90) r.Egresos90 += m.MontoUSD;
                }
            }

            return (mov, r);
        }

                // ════════ MIGRACION DE ESQUEMA ══════════════════════════════════════
        // Cada migracion tiene un numero de version unico y creciente.
        // PRAGMA user_version se guarda en el header del archivo .db de SQLite.
        // Al arrancar se ejecutan solo las migraciones cuyo numero > version actual.
        // Nunca se modifica una migracion existente: si hay un error se agrega
        // una nueva migracion que lo corrija.
        // Version actual del esquema en codigo: 7
        public  const int SCHEMA_VERSION = 25;  // v25: CantDefectuosas en DetalleProduccionMolde

        private static void MigrarEsquema(SqliteConnection conn)
        {
            // Leer version actual de la BD
            int versionActual = GetSchemaVersion(conn);

            if (versionActual >= SCHEMA_VERSION) return;  // ya esta al dia

            RegistrarAuditoria("MIGRACION_INICIO", "Sistema",
                "BD version " + versionActual + " -> " + SCHEMA_VERSION);

            using var tx = conn.BeginTransaction();
            try
            {
                // ── v1: columna UltimoAcceso en Usuarios
                // Usa AlterTableSafe porque la columna puede ya existir en BDs
                // que tenian esta columna antes de que existiera el sistema de migraciones
                if (versionActual < 1)
                {
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Usuarios ADD COLUMN UltimoAcceso TEXT");
                    SetSchemaVersion(conn, tx, 1);
                }

                // ── v2: tablas de control de calidad
                if (versionActual < 2)
                    EjecutarMigracion(conn, tx, 2, @"
                        CREATE TABLE IF NOT EXISTS InspeccionesCalidad (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdIngreso INTEGER NOT NULL,
                            Lote TEXT, ProductoCod TEXT,
                            FechaInspeccion TEXT NOT NULL,
                            Inspector TEXT,
                            CantidadTotal REAL DEFAULT 0, CantidadAprobada REAL DEFAULT 0,
                            CantidadRechazada REAL DEFAULT 0, CantidadRetrabajo REAL DEFAULT 0,
                            PuntajeDimensiones INTEGER DEFAULT 3, PuntajeApariencia INTEGER DEFAULT 3,
                            PuntajeFuncional INTEGER DEFAULT 3, PuntajeEtiquetado INTEGER DEFAULT 3,
                            Resultado TEXT DEFAULT 'Aprobado',
                            Observaciones TEXT, AccionCorrectiva TEXT
                        );
                        CREATE TABLE IF NOT EXISTS DefectosCalidad (
                            IdInspeccion INTEGER NOT NULL, TipoDefecto TEXT NOT NULL,
                            Cantidad INTEGER DEFAULT 0, Descripcion TEXT,
                            Severidad TEXT DEFAULT 'Menor',
                            PRIMARY KEY(IdInspeccion, TipoDefecto)
                        )");

                // ── v3: tablas de mantenimiento de moldes
                if (versionActual < 3)
                    EjecutarMigracion(conn, tx, 3, @"
                        CREATE TABLE IF NOT EXISTS RegistrosMantenimiento (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CodigoMolde TEXT NOT NULL,
                            FechaRealizada TEXT NOT NULL,
                            TipoMant TEXT NOT NULL DEFAULT 'Preventivo',
                            Tecnico TEXT, Descripcion TEXT,
                            CostoMant REAL DEFAULT 0, DuracionHoras INTEGER DEFAULT 0,
                            Resultado TEXT DEFAULT 'Completado',
                            Observaciones TEXT, ProximoMant TEXT
                        );
                        CREATE TABLE IF NOT EXISTS ProgramasMantenimiento (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CodigoMolde TEXT NOT NULL UNIQUE,
                            DescMolde TEXT, TipoMant TEXT NOT NULL DEFAULT 'Preventivo',
                            FrecuenciaDias INTEGER NOT NULL DEFAULT 90,
                            UltimoMant TEXT NOT NULL, Activo INTEGER NOT NULL DEFAULT 1
                        )");

                // ── v4: tablas de nomina
                if (versionActual < 4)
                    EjecutarMigracion(conn, tx, 4, @"
                        CREATE TABLE IF NOT EXISTS Empleados (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Cedula TEXT NOT NULL UNIQUE, Nombre TEXT NOT NULL,
                            Apellido TEXT NOT NULL, Cargo TEXT,
                            TipoEmpleado TEXT NOT NULL DEFAULT 'Fijo',
                            FechaIngreso TEXT NOT NULL, Activo INTEGER NOT NULL DEFAULT 1,
                            Telefono TEXT, Email TEXT,
                            SueldoDiarioUSD REAL NOT NULL DEFAULT 0,
                            TarifaPiezaUSD REAL NOT NULL DEFAULT 0,
                            TarifaVentaUSD REAL NOT NULL DEFAULT 0,
                            SalarioNormalUSD REAL NOT NULL DEFAULT 0
                        );
                        CREATE TABLE IF NOT EXISTS PeriodosNomina (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FechaDesde TEXT NOT NULL, FechaHasta TEXT NOT NULL,
                            TasaBCV REAL NOT NULL DEFAULT 1,
                            Estado TEXT NOT NULL DEFAULT 'Abierto', Observaciones TEXT
                        );
                        CREATE TABLE IF NOT EXISTS DetallesNomina (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdPeriodo INTEGER NOT NULL, IdEmpleado INTEGER NOT NULL,
                            NombreEmpleado TEXT, TipoEmpleado TEXT,
                            DiasTrabajados REAL DEFAULT 0, DiasInasistencia REAL DEFAULT 0,
                            PiezasProducidas REAL DEFAULT 0, VentasCerradas REAL DEFAULT 0,
                            SueldoDiarioUSD REAL DEFAULT 0,
                            DevengadoSueldo REAL DEFAULT 0, DevengadoPiezas REAL DEFAULT 0,
                            DevengadoVentas REAL DEFAULT 0, OtrosIngresos REAL DEFAULT 0,
                            DeduccionIVSS REAL DEFAULT 0, DeduccionRPVH REAL DEFAULT 0,
                            DescuentoFaltas REAL DEFAULT 0, CuotaPrestamo REAL DEFAULT 0,
                            OtrasDeduccion REAL DEFAULT 0,
                            TasaBCV REAL DEFAULT 1, Observaciones TEXT
                        );
                        CREATE TABLE IF NOT EXISTS PrestamosEmpleado (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdEmpleado INTEGER NOT NULL, NombreEmpleado TEXT,
                            FechaOtorgado TEXT NOT NULL,
                            MontoTotalUSD REAL NOT NULL DEFAULT 0,
                            CuotaSemanalUSD REAL NOT NULL DEFAULT 0,
                            SaldoPendiente REAL NOT NULL DEFAULT 0,
                            Estado TEXT NOT NULL DEFAULT 'Activo', Descripcion TEXT
                        );
                        CREATE TABLE IF NOT EXISTS BeneficiosAnuales (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdEmpleado INTEGER NOT NULL, NombreEmpleado TEXT,
                            Anio INTEGER NOT NULL, TipoBeneficio TEXT NOT NULL,
                            DiasCorresponden REAL DEFAULT 0, SalarioDiarioUSD REAL DEFAULT 0,
                            MontoUSD REAL DEFAULT 0, TasaBCV REAL DEFAULT 1,
                            FechaPago TEXT, Estado TEXT NOT NULL DEFAULT 'Pendiente',
                            Observaciones TEXT
                        )");

                // ── v5: tablas de preferencias y aportes patronales
                if (versionActual < 5)
                    EjecutarMigracion(conn, tx, 5, @"
                        CREATE TABLE IF NOT EXISTS PreferenciasUsuario (
                            NombreUsuario TEXT PRIMARY KEY,
                            Tema TEXT DEFAULT 'Claro', ColorAcento TEXT DEFAULT 'Azul',
                            MostrarKpis INTEGER DEFAULT 1, SonidoAlertas INTEGER DEFAULT 0,
                            FilasPorPagina INTEGER DEFAULT 50,
                            ConfirmarEliminar INTEGER DEFAULT 1,
                            AbrirUltimoModulo INTEGER DEFAULT 0,
                            NombreCompleto TEXT DEFAULT '',
                            Email TEXT DEFAULT '', Telefono TEXT DEFAULT ''
                        );
                        CREATE TABLE IF NOT EXISTS AportesPatronales (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdPeriodo INTEGER NOT NULL, IdEmpleado INTEGER NOT NULL,
                            NombreEmpleado TEXT,
                            SalarioNormalBs REAL DEFAULT 0, TasaBCV REAL DEFAULT 1,
                            PorcentajeIVSS REAL DEFAULT 9.0,
                            IVSSPatronBs REAL DEFAULT 0, RPVHPatronBs REAL DEFAULT 0,
                            INCESBs REAL DEFAULT 0, DiasLaborales REAL DEFAULT 0,
                            ValorUTBs REAL DEFAULT 0, CestaBs REAL DEFAULT 0
                        )");

                // ── v6: indice de rendimiento en tablas de alto uso
                if (versionActual < 6)
                    EjecutarMigracion(conn, tx, 6, @"
                        CREATE INDEX IF NOT EXISTS idx_mov_codigo
                            ON MovimientosInventario(CodigoProducto);
                        CREATE INDEX IF NOT EXISTS idx_mov_fecha
                            ON MovimientosInventario(FechaHora);
                        CREATE INDEX IF NOT EXISTS idx_fact_cliente
                            ON Facturas(Codigo);
                        CREATE INDEX IF NOT EXISTS idx_fact_fecha
                            ON Facturas(Fecha);
                        CREATE INDEX IF NOT EXISTS idx_det_nomina_periodo
                            ON DetallesNomina(IdPeriodo);
                        CREATE INDEX IF NOT EXISTS idx_log_fecha
                            ON LogAuditoria(FechaHora);
                        CREATE INDEX IF NOT EXISTS idx_log_usuario
                            ON LogAuditoria(Usuario)");

                // ── v7: columna SalarioNormalUSD en Empleados si se agrego despues
                //        (ALTER TABLE solo agrega columnas nuevas, falla si ya existe,
                //         por eso usamos el helper seguro que ignora el error)
                if (versionActual < 7)
                {
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Empleados ADD COLUMN SalarioNormalUSD REAL NOT NULL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE DetallesNomina ADD COLUMN OtrasDeduccion REAL DEFAULT 0");
                    SetSchemaVersion(conn, tx, 7);
                }

                if (versionActual < 8)
                    EjecutarMigracion(conn, tx, 8, @"
                        CREATE TABLE IF NOT EXISTS ProduccionDiaria (
                            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                            Codigo        TEXT    NOT NULL,
                            Fecha         TEXT    NOT NULL,
                            Cantidad      REAL    NOT NULL DEFAULT 0,
                            CostoUnitario REAL    NOT NULL DEFAULT 0,
                            Observacion   TEXT,
                            IdEmpleado    INTEGER,
                            FOREIGN KEY(Codigo) REFERENCES Inventario(Codigo)
                        );
                        CREATE INDEX IF NOT EXISTS idx_proddiaria_fecha ON ProduccionDiaria(Fecha);
                        CREATE INDEX IF NOT EXISTS idx_proddiaria_codigo ON ProduccionDiaria(Codigo);
                    ");

                if (versionActual < 9)
                {
                    EjecutarMigracion(conn, tx, 9, @"
                        CREATE TABLE IF NOT EXISTS TarifasBonoProduccion (
                            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdEmpleado      INTEGER NOT NULL,
                            CodigoProducto  TEXT    NOT NULL,
                            TarifaBs        REAL    NOT NULL DEFAULT 0,
                            FechaDesde      TEXT    NOT NULL,
                            FechaHasta      TEXT,
                            FOREIGN KEY(IdEmpleado)     REFERENCES Empleados(Id),
                            FOREIGN KEY(CodigoProducto) REFERENCES Inventario(Codigo)
                        );
                        CREATE INDEX IF NOT EXISTS idx_tarifa_emp ON TarifasBonoProduccion(IdEmpleado);
                        CREATE INDEX IF NOT EXISTS idx_tarifa_prod ON TarifasBonoProduccion(CodigoProducto);
                    ");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProduccionDiaria ADD COLUMN IdEmpleado INTEGER");
                }

                if (versionActual < 10)
                {
                    EjecutarMigracion(conn, tx, 10, @"
                        CREATE TABLE IF NOT EXISTS RegistroProduccionDia (
                            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                            Fecha       TEXT    NOT NULL UNIQUE,
                            Observacion TEXT
                        );
                        CREATE TABLE IF NOT EXISTS DetalleProduccionMolde (
                            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdRegistro      INTEGER NOT NULL,
                            CodigoMolde     TEXT    NOT NULL,
                            CodigoProducto  TEXT    NOT NULL,
                            Cantidad        REAL    NOT NULL DEFAULT 0,
                            FOREIGN KEY(IdRegistro)     REFERENCES RegistroProduccionDia(Id),
                            FOREIGN KEY(CodigoProducto) REFERENCES Inventario(Codigo)
                        );
                        CREATE TABLE IF NOT EXISTS MaterialProduccionDia (
                            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdRegistro   INTEGER NOT NULL,
                            CodigoInsumo TEXT    NOT NULL,
                            Cantidad     REAL    NOT NULL DEFAULT 0,
                            FOREIGN KEY(IdRegistro) REFERENCES RegistroProduccionDia(Id)
                        );
                        CREATE TABLE IF NOT EXISTS TarifasBonoVentas (
                            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                            IdEmpleado      INTEGER NOT NULL,
                            CodigoProducto  TEXT    NOT NULL,
                            TarifaBs        REAL    NOT NULL DEFAULT 0,
                            FechaDesde      TEXT    NOT NULL,
                            FechaHasta      TEXT,
                            FOREIGN KEY(IdEmpleado)     REFERENCES Empleados(Id),
                            FOREIGN KEY(CodigoProducto) REFERENCES Inventario(Codigo)
                        );
                        CREATE INDEX IF NOT EXISTS idx_regprod_fecha ON RegistroProduccionDia(Fecha);
                        CREATE INDEX IF NOT EXISTS idx_detmolde_registro ON DetalleProduccionMolde(IdRegistro);
                        CREATE INDEX IF NOT EXISTS idx_detmolde_producto ON DetalleProduccionMolde(CodigoProducto);
                        CREATE INDEX IF NOT EXISTS idx_matprod_registro ON MaterialProduccionDia(IdRegistro);
                        CREATE INDEX IF NOT EXISTS idx_bonov_emp ON TarifasBonoVentas(IdEmpleado);
                    ");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE TbMoldes ADD COLUMN CodigoProducto TEXT");
                }

                if (versionActual < 11)
                    EjecutarMigracion(conn, tx, 11, @"
                        CREATE TABLE IF NOT EXISTS PreferenciasVentana (
                            NombreUsuario  TEXT NOT NULL,
                            NombreVentana  TEXT NOT NULL,
                            Datos          TEXT NOT NULL,
                            PRIMARY KEY(NombreUsuario, NombreVentana)
                        );
                    ");

                if (versionActual < 13)
                {
                    // Paso 1: crear tablas (idempotente)
                    EjecutarSql(conn, tx, @"
                        CREATE TABLE IF NOT EXISTS ProductosProveedor (
                            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                            CodigoInterno    TEXT    NOT NULL UNIQUE,
                            CodigoProveedor  TEXT,
                            IdProveedorPrinc INTEGER,
                            Descripcion      TEXT    NOT NULL,
                            TipoInsumo       TEXT    NOT NULL DEFAULT 'Materia prima',
                            Unidad           TEXT    NOT NULL DEFAULT 'kg',
                            PrecioUltCompra  REAL    DEFAULT 0,
                            PrecioPromedio   REAL    DEFAULT 0,
                            StockActual      REAL    DEFAULT 0,
                            StockMinimo      REAL    DEFAULT 0,
                            Activo           INTEGER DEFAULT 1,
                            Observaciones    TEXT,
                            FOREIGN KEY(IdProveedorPrinc) REFERENCES Proveedores(Codigo)
                        );
                        CREATE TABLE IF NOT EXISTS ProductoProveedorAlt (
                            IdProducto   INTEGER NOT NULL,
                            IdProveedor  TEXT    NOT NULL,
                            PrecioRef    REAL    DEFAULT 0,
                            PRIMARY KEY(IdProducto, IdProveedor),
                            FOREIGN KEY(IdProducto)  REFERENCES ProductosProveedor(Id),
                            FOREIGN KEY(IdProveedor) REFERENCES Proveedores(Codigo)
                        );");
                    // Paso 2: columnas que pueden faltar en DBs con tabla pre-existente
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN CodigoProveedor TEXT");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN IdProveedorPrinc INTEGER");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN TipoInsumo TEXT NOT NULL DEFAULT 'Materia prima'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Unidad TEXT NOT NULL DEFAULT 'kg'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN PrecioUltCompra REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN PrecioPromedio REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN StockActual REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN StockMinimo REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Activo INTEGER DEFAULT 1");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Observaciones TEXT");
                    // Paso 3: índices (idempotente — columnas garantizadas arriba)
                    EjecutarSql(conn, tx,
                        "CREATE INDEX IF NOT EXISTS idx_pp_tipo ON ProductosProveedor(TipoInsumo);");
                    EjecutarSql(conn, tx,
                        "CREATE INDEX IF NOT EXISTS idx_pp_prov ON ProductosProveedor(IdProveedorPrinc);");
                    SetSchemaVersion(conn, tx, 13);
                }

                                if (versionActual < 12)
                {
                    EjecutarMigracion(conn, tx, 12, @"
                        CREATE TABLE IF NOT EXISTS MoldeProductos (
                            CodigoMolde  TEXT NOT NULL PRIMARY KEY,
                            CodPrimera   TEXT,
                            CodSegunda   TEXT,
                            CodReciclado TEXT,
                            FOREIGN KEY(CodigoMolde) REFERENCES TbMoldes(CodigoMold)
                        );
                    ");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Inventario ADD COLUMN PesoKg REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE RegistroProduccionDia ADD COLUMN TipoMaterial TEXT DEFAULT 'Virgen'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantPrimera REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantSegunda REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantReciclado REAL DEFAULT 0");
                }

                // ── MIGRACIÓN v18: Seguridad de autenticación ─────────────────────
                // Agrega columnas para PBKDF2, RequiereCambioPassword y rate limiting
                if (versionActual < 18)
                {
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Usuarios ADD COLUMN RequiereCambioPassword INTEGER NOT NULL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Usuarios ADD COLUMN IntentosHoy INTEGER NOT NULL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Usuarios ADD COLUMN BloqueadoHasta TEXT");
                    // El PasswordHash existente (SHA-256 legacy) se migra a PBKDF2
                    // automáticamente en el siguiente login exitoso de cada usuario.
                    // Forzar cambio de contraseña al admin para iniciar el ciclo de migración.
                    EjecutarMigracion(conn, tx, 18, @"
                        UPDATE Usuarios SET RequiereCambioPassword=1 WHERE Rol='Admin';
                        CREATE INDEX IF NOT EXISTS idx_usuarios_bloqueado
                            ON Usuarios(BloqueadoHasta) WHERE BloqueadoHasta IS NOT NULL;
                    ");
                }

                // ── MIGRACIÓN v19: Trazabilidad de TasaCambio ──────────────────
                //   1. TasaCambio.Origen ('AUTO_API'/'MANUAL'/'EDITADO'/'IMPORTADO')
                //   2. TasaCambio.FechaRegistro (cuándo se ingresó, distinto de
                //      Fecha — ej: registro tasa de ayer hoy a las 9 a.m.)
                //   3. Facturas.IdTasaCambio + snapshot TasaBCVUsada/TasaParaleloUsada
                //      ⇒ las facturas conservan QUÉ tasa usaron incluso si esa
                //         tasa se modifica después. Sin esto, los reportes
                //         retroactivos cambian al editar una tasa vieja.
                if (versionActual < 19)
                {
                    // Crear TasaCambio si no existe (BDs muy antiguas sin esta tabla)
                    EjecutarSql(conn, tx, @"
                        CREATE TABLE IF NOT EXISTS TasaCambio (
                            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                            Fecha        TEXT    NOT NULL UNIQUE,
                            TasaBCV      REAL    NOT NULL DEFAULT 0,
                            TasaParalelo REAL    NOT NULL DEFAULT 0,
                            Usuario      TEXT
                        );");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE TasaCambio ADD COLUMN Origen TEXT NOT NULL DEFAULT 'MANUAL'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE TasaCambio ADD COLUMN FechaRegistro TEXT");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Facturas ADD COLUMN IdTasaCambio INTEGER");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Facturas ADD COLUMN TasaBCVUsada REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE Facturas ADD COLUMN TasaParaleloUsada REAL DEFAULT 0");
                    EjecutarMigracion(conn, tx, 19, @"
                        CREATE INDEX IF NOT EXISTS idx_facturas_idtasa
                            ON Facturas(IdTasaCambio);
                        CREATE INDEX IF NOT EXISTS idx_tasacambio_origen
                            ON TasaCambio(Origen);
                        UPDATE TasaCambio
                            SET FechaRegistro = COALESCE(FechaRegistro, Fecha)
                            WHERE FechaRegistro IS NULL;
                    ");
                }

                // ── MIGRACIÓN v20: Módulo Egresos ──────────────────────────────
                //   Tabla unificada Gastos para TODOS los egresos del negocio.
                //   Multi-moneda + CxP + comprobante + snapshot tasa.
                //
                //   IMPORTANTE: si ya existe una tabla "Gastos" legacy (schema
                //   viejo con columna 'Categoria' TEXT en lugar de IdCategoria
                //   FK), la renombramos a Gastos_legacy_v9 para conservar los
                //   datos y crear la nueva con el schema correcto.
                // ── Retroactive rebuild: CategoriasGasto schema completo ──────────────
                _ultimoSqlMigracion = "[pre-v20] Retroactive rebuild CategoriasGasto — check";
                // BDs pre-v20 tienen CategoriasGasto sin Color/Orden/EsMateriaPrima.
                // ALTER TABLE ADD COLUMN puede fallar en algunos estados de la BD;
                // usamos el enfoque recomendado por SQLite: crear tabla temporal con
                // schema correcto, copiar datos y renombrar (idempotente).
                if (TablaExiste(conn, tx, "CategoriasGasto") &&
                    (!ColumnaExiste(conn, tx, "CategoriasGasto", "Color") ||
                     !ColumnaExiste(conn, tx, "CategoriasGasto", "Orden") ||
                     !ColumnaExiste(conn, tx, "CategoriasGasto", "EsMateriaPrima") ||
                     !ColumnaExiste(conn, tx, "CategoriasGasto", "Activa")))
                {
                    // Solo copiar columnas que existen actualmente; las faltantes toman DEFAULT
                    bool cgHasColor  = ColumnaExiste(conn, tx, "CategoriasGasto", "Color");
                    bool cgHasActiva = ColumnaExiste(conn, tx, "CategoriasGasto", "Activa");
                    bool cgHasOrden  = ColumnaExiste(conn, tx, "CategoriasGasto", "Orden");
                    bool cgHasEsMat  = ColumnaExiste(conn, tx, "CategoriasGasto", "EsMateriaPrima");
                    var cgCols = new System.Collections.Generic.List<string> { "Id", "Nombre" };
                    if (cgHasColor)  cgCols.Add("Color");
                    if (cgHasActiva) cgCols.Add("Activa");
                    if (cgHasOrden)  cgCols.Add("Orden");
                    if (cgHasEsMat)  cgCols.Add("EsMateriaPrima");
                    string cgColList = string.Join(", ", cgCols);
                    EjecutarSql(conn, tx, "DROP TABLE IF EXISTS CategoriasGasto_rebuild");
                    EjecutarSql(conn, tx, @"CREATE TABLE CategoriasGasto_rebuild (
                            Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                            Nombre         TEXT    NOT NULL UNIQUE,
                            Color          TEXT    DEFAULT '#1A4A8A',
                            Activa         INTEGER DEFAULT 1,
                            Orden          INTEGER DEFAULT 0,
                            EsMateriaPrima INTEGER DEFAULT 0
                        )");
                    EjecutarSql(conn, tx,
                        $"INSERT INTO CategoriasGasto_rebuild ({cgColList}) SELECT {cgColList} FROM CategoriasGasto");
                    EjecutarSql(conn, tx, "DROP TABLE CategoriasGasto");
                    EjecutarSql(conn, tx, "ALTER TABLE CategoriasGasto_rebuild RENAME TO CategoriasGasto");
                }

                if (versionActual < 20)
                {
                    EjecutarMigracionV20(conn, tx);
                    SetSchemaVersion(conn, tx, 20);
                }

                // ── MIGRACIÓN v21: Anulación de facturas ──────────────────────
                //   Agrega columnas para marcar facturas como anuladas (no se
                //   eliminan: rompe correlativo SENIAT). Detecta facturas con
                //   fechas inválidas (año < 2000 o > año_actual+1) y las marca
                //   como ANULADA con motivo automático.
                if (versionActual < 21)
                {
                    EjecutarMigracionV21(conn, tx);
                    SetSchemaVersion(conn, tx, 21);
                }

                // ── MIGRACIÓN v22: Tipo de documento (Factura/NE/Export) ──────
                //   Distingue entre facturas legales con IVA, facturas de
                //   exportación (IVA 0% legal) y notas de entrega (sin valor
                //   fiscal). Las NE se ocultan por default de reportes.
                if (versionActual < 22)
                {
                    EjecutarMigracionV22(conn, tx);
                    SetSchemaVersion(conn, tx, 22);
                }

                // ── MIGRACIÓN v23: Cámaras de vigilancia (EZVIZ) ──────────────
                //   Tablas Camaras (catálogo con clave RTSP cifrada DPAPI) y
                //   PermisosCamara (qué cámaras ve cada usuario). Admin ve
                //   todas; otros usuarios solo las que tengan asignadas.
                if (versionActual < 23)
                {
                    EjecutarMigracionV23(conn, tx);
                    SetSchemaVersion(conn, tx, 23);
                }

                if (versionActual < 24)
                {
                    MigrarV24(conn, tx);
                    SetSchemaVersion(conn, tx, 24);
                }

                // ── v25: CantDefectuosas en DetalleProduccionMolde ─────────────
                if (versionActual < 25)
                {
                    MigrarV25(conn, tx);
                    SetSchemaVersion(conn, tx, 25);
                }

                // ── Retroactive fix: columnas faltantes en ProductosProveedor ──
                // BDs antiguas pueden tener esta tabla con un esquema incompleto
                // (sin TipoInsumo, sin IdProveedorPrinc, etc.).
                // AlterTableSafe ignora "duplicate column" — completamente idempotente.
                if (TablaExiste(conn, tx, "ProductosProveedor"))
                {
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN CodigoProveedor TEXT");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN IdProveedorPrinc INTEGER");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN TipoInsumo TEXT NOT NULL DEFAULT 'Materia prima'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Unidad TEXT NOT NULL DEFAULT 'kg'");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN PrecioUltCompra REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN PrecioPromedio REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN StockActual REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN StockMinimo REAL DEFAULT 0");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Activo INTEGER DEFAULT 1");
                    AlterTableSafe(conn, tx,
                        "ALTER TABLE ProductosProveedor ADD COLUMN Observaciones TEXT");
                    EjecutarSql(conn, tx,
                        "CREATE INDEX IF NOT EXISTS idx_pp_tipo ON ProductosProveedor(TipoInsumo);");
                    EjecutarSql(conn, tx,
                        "CREATE INDEX IF NOT EXISTS idx_pp_prov ON ProductosProveedor(IdProveedorPrinc);");
                }


                tx.Commit();
                RegistrarAuditoria("MIGRACION_OK", "Sistema",
                    "Esquema actualizado a v" + SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                RegistrarAuditoria("MIGRACION_ERROR", "Sistema", ex.Message);
                string sqlCtx = _ultimoSqlMigracion ?? "(desconocido)";
                if (sqlCtx.Length > 300) sqlCtx = sqlCtx[..300] + "...";
                throw new Exception(
                    "Error al migrar la base de datos a v" + SCHEMA_VERSION + ":\n" +
                    ex.Message + "\n\nÚltimo SQL:\n" + sqlCtx +
                    "\n\nContacta al administrador del sistema.", ex);
            }
        }

        /// <summary>Ejecuta un bloque SQL de migracion y actualiza la version.</summary>
        private static void EjecutarMigracion(
            SqliteConnection conn, SqliteTransaction tx, int version, string sql)
        {
            _ultimoSqlMigracion = $"[v{version}] {sql}";
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText  = sql;
            cmd.ExecuteNonQuery();
            SetSchemaVersion(conn, tx, version);
        }

        /// <summary>
        /// Migración v20 paso a paso. Detecta tabla Gastos legacy (sin
        /// IdCategoria) y la renombra a Gastos_legacy_v9 antes de crear
        /// la nueva. Idempotente.
        /// </summary>
        private static void EjecutarMigracionV20(SqliteConnection conn, SqliteTransaction tx)
        {
            _ultimoSqlMigracion = "[v20] inicio EjecutarMigracionV20";
            // 1. Verificar si la tabla Gastos existe y si tiene IdCategoria
            bool gastosExiste = TablaExiste(conn, tx, "Gastos");
            bool tieneIdCategoria = false;
            if (gastosExiste)
                tieneIdCategoria = ColumnaExiste(conn, tx, "Gastos", "IdCategoria");

            // 2. Si existe pero es schema viejo, renombrar para conservar datos
            if (gastosExiste && !tieneIdCategoria)
            {
                // Si por alguna razon ya existe un legacy renombrado, no pisarlo
                if (!TablaExiste(conn, tx, "Gastos_legacy_v9"))
                {
                    EjecutarSql(conn, tx, "ALTER TABLE Gastos RENAME TO Gastos_legacy_v9");
                    Console.WriteLine("[Migr v20] Tabla Gastos legacy preservada como Gastos_legacy_v9.");
                }
                else
                {
                    // Caso raro: Gastos viejo + Gastos_legacy_v9 ya existen
                    // → simplemente droppear el viejo (quedan en el legacy)
                    EjecutarSql(conn, tx, "DROP TABLE Gastos");
                }
            }

            // 3. Crear tabla CategoriasGasto (idempotente)
            _ultimoSqlMigracion = "[v20-paso3] CREATE TABLE IF NOT EXISTS CategoriasGasto";
            EjecutarSql(conn, tx, @"
                CREATE TABLE IF NOT EXISTS CategoriasGasto (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre      TEXT    NOT NULL UNIQUE,
                    Color       TEXT    DEFAULT '#1A4A8A',
                    Activa      INTEGER NOT NULL DEFAULT 1,
                    Orden       INTEGER NOT NULL DEFAULT 0,
                    EsMateriaPrima INTEGER NOT NULL DEFAULT 0
                );");
            // Columnas que pueden faltar si CategoriasGasto ya existía
            // Usa ColumnaExiste (check explícito, sin captura de excepciones)
            // Sin NOT NULL en ALTER para máxima compatibilidad SQLite.
            if (!ColumnaExiste(conn, tx, "CategoriasGasto", "Color"))
                EjecutarSql(conn, tx, "ALTER TABLE CategoriasGasto ADD COLUMN Color TEXT DEFAULT '#1A4A8A'");
            if (!ColumnaExiste(conn, tx, "CategoriasGasto", "Activa"))
                EjecutarSql(conn, tx, "ALTER TABLE CategoriasGasto ADD COLUMN Activa INTEGER DEFAULT 1");
            if (!ColumnaExiste(conn, tx, "CategoriasGasto", "Orden"))
                EjecutarSql(conn, tx, "ALTER TABLE CategoriasGasto ADD COLUMN Orden INTEGER DEFAULT 0");
            if (!ColumnaExiste(conn, tx, "CategoriasGasto", "EsMateriaPrima"))
                EjecutarSql(conn, tx, "ALTER TABLE CategoriasGasto ADD COLUMN EsMateriaPrima INTEGER DEFAULT 0");

            // 4. Crear tabla Gastos nueva (solo si no existe ya con schema correcto)
            _ultimoSqlMigracion = "[v20-paso4] CREATE TABLE IF NOT EXISTS Gastos (nueva)";
            EjecutarSql(conn, tx, @"
                CREATE TABLE IF NOT EXISTS Gastos (
                    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha           TEXT    NOT NULL,
                    IdCategoria     INTEGER NOT NULL,
                    Concepto        TEXT    NOT NULL,
                    Proveedor       TEXT,
                    IdInsumo        INTEGER,
                    Cantidad        REAL    DEFAULT 0,
                    MonedaOriginal  TEXT    NOT NULL DEFAULT 'USD',
                    TipoTasa        TEXT    NOT NULL DEFAULT 'USD_DIRECTO',
                    TasaUsada       REAL    NOT NULL DEFAULT 0,
                    IdTasaCambio    INTEGER,
                    MontoOriginal   REAL    NOT NULL DEFAULT 0,
                    MontoUSD        REAL    NOT NULL DEFAULT 0,
                    MontoBs         REAL    NOT NULL DEFAULT 0,
                    Estado          TEXT    NOT NULL DEFAULT 'Pagado',
                    FechaVencimiento TEXT,
                    FechaPago       TEXT,
                    RutaComprobante TEXT,
                    Usuario         TEXT    NOT NULL DEFAULT 'Sistema',
                    FechaRegistro   TEXT    NOT NULL,
                    Observaciones   TEXT,
                    FOREIGN KEY (IdCategoria) REFERENCES CategoriasGasto(Id)
                );");
            // Columnas que pueden faltar si Gastos nueva ya existía parcialmente
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN IdCategoria INTEGER");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Concepto TEXT");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Proveedor TEXT");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN IdInsumo INTEGER");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Cantidad REAL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN MonedaOriginal TEXT NOT NULL DEFAULT 'USD'");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN TipoTasa TEXT NOT NULL DEFAULT 'USD_DIRECTO'");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN TasaUsada REAL NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN IdTasaCambio INTEGER");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN MontoOriginal REAL NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN MontoUSD REAL NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN MontoBs REAL NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Estado TEXT NOT NULL DEFAULT 'Pagado'");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN FechaVencimiento TEXT");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN FechaPago TEXT");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN RutaComprobante TEXT");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Usuario TEXT NOT NULL DEFAULT 'Sistema'");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN FechaRegistro TEXT NOT NULL DEFAULT ''");
            AlterTableSafe(conn, tx, "ALTER TABLE Gastos ADD COLUMN Observaciones TEXT");

            // 5. Índices (idempotente)
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_gastos_fecha ON Gastos(Fecha);");
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_gastos_categoria ON Gastos(IdCategoria);");
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_gastos_estado ON Gastos(Estado);");
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_gastos_vencimiento ON Gastos(FechaVencimiento);");

            // 6. Categorías predefinidas — solo si la tabla está vacía.
            //    Si ya tiene filas: es BD antigua con categorías propias; no sobrescribir.
            //    Si está vacía: se acaba de crear con CREATE TABLE y tiene TODAS las columnas.
            {
                var cntCmd = conn.CreateCommand();
                cntCmd.Transaction = tx;
                cntCmd.CommandText = "SELECT COUNT(*) FROM CategoriasGasto";
                long rowCount = (long)cntCmd.ExecuteScalar();
                if (rowCount == 0)
                {
                    EjecutarSql(conn, tx, @"
                        INSERT OR IGNORE INTO CategoriasGasto (Nombre, Color, Orden, EsMateriaPrima) VALUES
                            ('Materia Prima',            '#1A4A8A', 1, 1),
                            ('Servicios públicos',       '#2D6CC0', 2, 0),
                            ('Mantenimiento',            '#0F6E56', 3, 0),
                            ('Oficina y papelería',      '#BA7517', 4, 0),
                            ('Limpieza',                 '#534AB7', 5, 0),
                            ('Ferretería y herramientas','#A32D2D', 6, 0),
                            ('Honorarios',               '#A35A1A', 7, 0),
                            ('Combustible y transporte',  '#A8B1BB', 8, 0),
                            ('Otros',                    '#888780', 99, 0);");
                }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        //  MIGRACIÓN v21: Anulación de facturas + reparación de fechas
        // ────────────────────────────────────────────────────────────────────
        //  Agrega 4 columnas a Facturas para soportar anulación reversible
        //  con auditoría (no eliminamos nunca: rompe el correlativo SENIAT).
        //  Adicionalmente detecta facturas con fechas inválidas (año < 2000)
        //  y las marca como ANULADA con motivo automático para que dejen de
        //  romper la apertura del módulo y los reportes.
        private static void EjecutarMigracionV21(SqliteConnection conn, SqliteTransaction tx)
        {
            _ultimoSqlMigracion = "[v21] inicio EjecutarMigracionV21";
            // 1. Agregar columnas (idempotente — comprueba antes)
            if (!ColumnaExiste(conn, tx, "Facturas", "Anulada"))
                EjecutarSql(conn, tx, "ALTER TABLE Facturas ADD COLUMN Anulada INTEGER NOT NULL DEFAULT 0;");
            if (!ColumnaExiste(conn, tx, "Facturas", "MotivoAnulacion"))
                EjecutarSql(conn, tx, "ALTER TABLE Facturas ADD COLUMN MotivoAnulacion TEXT;");
            if (!ColumnaExiste(conn, tx, "Facturas", "FechaAnulacion"))
                EjecutarSql(conn, tx, "ALTER TABLE Facturas ADD COLUMN FechaAnulacion TEXT;");
            if (!ColumnaExiste(conn, tx, "Facturas", "UsuarioAnulacion"))
                EjecutarSql(conn, tx, "ALTER TABLE Facturas ADD COLUMN UsuarioAnulacion TEXT;");

            // 2. Índice para excluir rápido las anuladas en CxC y reportes
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_facturas_anulada ON Facturas(Anulada);");

            // 3. Reparación de filas con fecha inválida.
            //    Criterio: la fecha está guardada como "yyyy-MM-dd". Si el
            //    año (primeros 4 chars) es < 2000 o > año_actual+1, la fila
            //    es inutilizable (rompe DateTime.Parse al abrir el form).
            //    No tratamos de "adivinar" el año correcto — lo dejamos al
            //    usuario que la edite después si quiere recuperarla. Solo la
            //    marcamos como ANULADA para que el sistema vuelva a abrir.
            int anioMax = DateTime.Today.Year + 1;
            var cmdRep = conn.CreateCommand();
            cmdRep.Transaction = tx;
            cmdRep.CommandText = @"
                UPDATE Facturas
                SET    Anulada          = 1,
                       MotivoAnulacion  = 'Fecha inválida (año fuera de rango razonable). ' ||
                                          'Anulación automática en migración v21. ' ||
                                          'Fecha original guardada: ' || Fecha,
                       FechaAnulacion   = $hoy,
                       UsuarioAnulacion = 'Sistema (migración v21)'
                WHERE  Anulada = 0
                  AND  (CAST(substr(Fecha, 1, 4) AS INTEGER) < 2000
                        OR CAST(substr(Fecha, 1, 4) AS INTEGER) > $anioMax
                        OR length(Fecha) < 8);";
            cmdRep.Parameters.AddWithValue("$hoy",     DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmdRep.Parameters.AddWithValue("$anioMax", anioMax);
            int reparadas = cmdRep.ExecuteNonQuery();

            if (reparadas > 0)
            {
                Console.WriteLine($"[Migr v21] {reparadas} factura(s) con fecha inválida anuladas automáticamente.");
                // Dejar también constancia en el log de auditoría
                var cmdLog = conn.CreateCommand();
                cmdLog.Transaction = tx;
                cmdLog.CommandText = @"
                    INSERT INTO LogAuditoria (FechaHora, Usuario, Accion, Tabla, Detalle)
                    VALUES ($f, 'Sistema', 'MIGRACION_v21', 'Facturas', $det);";
                cmdLog.Parameters.AddWithValue("$f",   DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdLog.Parameters.AddWithValue("$det",
                    $"{reparadas} factura(s) con fecha inválida (año<2000 o >{anioMax}) " +
                    "anuladas automáticamente para permitir apertura del módulo.");
                try { cmdLog.ExecuteNonQuery(); } catch { /* tabla puede no existir aún */ }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        //  MIGRACIÓN v22: Tipo de documento (Factura / NotaEntrega / Export)
        // ────────────────────────────────────────────────────────────────────
        //  Permite distinguir documentos con valor fiscal (FacturaConIVA,
        //  FacturaExportacion) de documentos sin valor fiscal (NotaEntrega).
        //  Las NE se ocultan por default de TODOS los reportes y agregados;
        //  sólo aparecen cuando un Admin activa Sesion.MostrarNotasEntrega.
        //
        //  Datos existentes: se reclasifican automáticamente:
        //    IvaF > 0  → FacturaConIVA  (default, no toca)
        //    IvaF = 0  → FacturaConIVA  (asume retroactivo: "ya existían así
        //                                antes de tener este flag")
        //  El usuario puede luego corregir individualmente las que considere.
        private static void EjecutarMigracionV22(SqliteConnection conn, SqliteTransaction tx)
        {
            _ultimoSqlMigracion = "[v22] inicio EjecutarMigracionV22";
            // 1. Agregar columnas (idempotente)
            if (!ColumnaExiste(conn, tx, "Facturas", "TipoDocumento"))
                EjecutarSql(conn, tx,
                    "ALTER TABLE Facturas ADD COLUMN TipoDocumento TEXT NOT NULL DEFAULT 'FacturaConIVA';");
            if (!ColumnaExiste(conn, tx, "Facturas", "EsExportacion"))
                EjecutarSql(conn, tx,
                    "ALTER TABLE Facturas ADD COLUMN EsExportacion INTEGER NOT NULL DEFAULT 0;");
            if (!ColumnaExiste(conn, tx, "Facturas", "DestinoExportacion"))
                EjecutarSql(conn, tx,
                    "ALTER TABLE Facturas ADD COLUMN DestinoExportacion TEXT;");

            // 2. Índice — la mayoría de queries filtra por TipoDocumento != 'NotaEntrega'
            EjecutarSql(conn, tx,
                "CREATE INDEX IF NOT EXISTS idx_facturas_tipodoc ON Facturas(TipoDocumento);");

            // 3. Tabla de configuración del sistema (si no existe ya).
            //    Aquí guardamos cosas como "última vez que un admin activó la
            //    visibilidad de NEs" para auditoría y telemetría interna.
            EjecutarSql(conn, tx, @"
                CREATE TABLE IF NOT EXISTS ConfigSistema (
                    Clave   TEXT PRIMARY KEY,
                    Valor   TEXT,
                    Fecha   TEXT,
                    Usuario TEXT
                );");

            // 4. Campo en Clientes: AceptaFacturaLegal (default 1 = sí)
            //    Permite registrar como POLÍTICA por cliente si acepta o no
            //    factura con IVA. Así la decisión de emitir NE queda atada
            //    al cliente, no al criterio del operador en cada venta.
            if (!ColumnaExiste(conn, tx, "Clientes", "AceptaFacturaLegal"))
                EjecutarSql(conn, tx,
                    "ALTER TABLE Clientes ADD COLUMN AceptaFacturaLegal INTEGER NOT NULL DEFAULT 1;");
        }

        // ── Helpers de introspección de schema ──────────────────────────────
        private static bool TablaExiste(SqliteConnection conn, SqliteTransaction tx, string nombre)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", nombre);
            return cmd.ExecuteScalar() != null;
        }

        private static bool ColumnaExiste(SqliteConnection conn, SqliteTransaction tx,
            string tabla, string columna)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            // PRAGMA no acepta parámetros bound, pero el nombre de la tabla
            // viene de código nuestro (no input usuario), así que es seguro.
            cmd.CommandText = "PRAGMA table_info(\"" + tabla.Replace("\"", "\"\"") + "\")";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                if (r.GetString(1).Equals(columna, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // Último SQL ejecutado por EjecutarSql — para diagnóstico en catch de MigrarEsquema
        [ThreadStatic] private static string? _ultimoSqlMigracion;

        private static void EjecutarSql(SqliteConnection conn, SqliteTransaction tx, string sql)
        {
            _ultimoSqlMigracion = sql;
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Ejecuta un ALTER TABLE ignorando el error si la columna ya existe.
        /// SQLite no soporta IF NOT EXISTS en ALTER TABLE.
        /// </summary>
        private static void AlterTableSafe(
            SqliteConnection conn, SqliteTransaction tx, string sql)
        {
            try
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText  = sql;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // "duplicate column name" o "table ... already has a column named ..."
                // son errores esperados cuando la columna ya existe
                if (!ex.Message.Contains("duplicate column") &&
                    !ex.Message.Contains("already has a column"))
                    throw;
            }
        }

        private static int GetSchemaVersion(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version";
            return (int)(long)cmd.ExecuteScalar();
        }

        private static void SetSchemaVersion(
            SqliteConnection conn, SqliteTransaction tx, int version)
        {
            // PRAGMA user_version no acepta parámetros en SQLite.
            // Validamos explícitamente que version sea un entero positivo seguro
            // para eliminar cualquier riesgo de inyección.
            if (version < 0 || version > 9999)
                throw new ArgumentOutOfRangeException(nameof(version),
                    "Número de versión de esquema fuera de rango válido (0-9999).");

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText  = $"PRAGMA user_version = {version}";
            cmd.ExecuteNonQuery();
        }

                // ════════ INVENTARIO ════════════════════════════════════════════════
        public static List<Inventario> ObtenerInventario()
        {
            var list = new List<Inventario>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Inventario ORDER BY Descripcion";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Inventario {
                    Codigo = r.GetString(0), Descripcion = S(r,1), Departamento = S(r,2),
                    Unidad = S(r,3), Enser = S(r,4), CostoActual = D(r,5), CostoAnt = D(r,6),
                    CostoPro = D(r,7), Precio = D(r,8), Existencia = D(r,9),
                    ExistenMin = D(r,10), ExistenMax = D(r,11), Proveedores = S(r,12),
                    UltimaActualz = r.IsDBNull(13) ? DateTime.Today : DateTime.Parse(r.GetString(13)),
                    CodProveedor = S(r,14), PesoKg = r.FieldCount > 15 ? D(r,15) : 0 });
            return list;
        }

        public static void InsertarInventario(Inventario i)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO Inventario VALUES
                ($co,$de,$dp,$un,$en,$ca,$cant,$cp,$pr,$ex,$emi,$ema,$pv,$ua,$cpv,$pk)";
            BindInventario(cmd, i); cmd.ExecuteNonQuery();
        }

        public static void ActualizarInventario(Inventario i)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Inventario SET Descripcion=$de,Departamento=$dp,Unidad=$un,
                Enser=$en,CostoActual=$ca,CostoAnt=$cant,CostoPro=$cp,Precio=$pr,Existencia=$ex,
                ExistenMin=$emi,ExistenMax=$ema,Proveedores=$pv,UltimaActualz=$ua,CodProveedor=$cpv,PesoKg=$pk
                WHERE Codigo=$co";
            BindInventario(cmd, i); cmd.ExecuteNonQuery();
        }

        public static void EliminarInventario(string codigo)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Inventario WHERE Codigo=$id", ("$id", codigo));
        }

        private static void BindInventario(SqliteCommand cmd, Inventario i)
        {
            cmd.Parameters.AddWithValue("$co",  i.Codigo);
            cmd.Parameters.AddWithValue("$de",  i.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$dp",  i.Departamento ?? "");
            cmd.Parameters.AddWithValue("$un",  i.Unidad ?? "");
            cmd.Parameters.AddWithValue("$en",  i.Enser ?? "");
            cmd.Parameters.AddWithValue("$ca",  (double)i.CostoActual);
            cmd.Parameters.AddWithValue("$cant",(double)i.CostoAnt);
            cmd.Parameters.AddWithValue("$cp",  (double)i.CostoPro);
            cmd.Parameters.AddWithValue("$pr",  (double)i.Precio);
            cmd.Parameters.AddWithValue("$ex",  (double)i.Existencia);
            cmd.Parameters.AddWithValue("$emi", (double)i.ExistenMin);
            cmd.Parameters.AddWithValue("$ema", (double)i.ExistenMax);
            cmd.Parameters.AddWithValue("$pv",  i.Proveedores ?? "");
            cmd.Parameters.AddWithValue("$ua",  i.UltimaActualz.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cpv", i.CodProveedor ?? "");
            cmd.Parameters.AddWithValue("$pk",  (double)i.PesoKg);
        }

        // ════════ CLIENTES ═══════════════════════════════════════════════════
        public static List<Cliente> ObtenerClientes()
        {
            var list = new List<Cliente>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Clientes ORDER BY Descripcion";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Cliente {
                    Codigo = r.GetString(0), Descripcion = S(r,1), Direccion = S(r,2),
                    Ciudad = S(r,3), Estado = S(r,4), CiRif = S(r,5), Nit = S(r,6),
                    Fax = S(r,7), Telefono = S(r,8), Telefono2 = S(r,9),
                    Credito = D(r,10), TipoCliente = S(r,11),
                    Fecha = r.IsDBNull(12) ? DateTime.Today : DateTime.Parse(r.GetString(12)),
                    Observaciones = S(r,13), Descuento = D(r,14), AFavor = D(r,15) });
            return list;
        }

        public static void InsertarCliente(Cliente cl)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Clientes VALUES ($co,$de,$di,$ci,$es,$cr,$ni,$fa,$t1,$t2,$cred,$ti,$fe,$ob,$dc,$af)";
            BindCliente(cmd, cl); cmd.ExecuteNonQuery();
        }

        public static void ActualizarCliente(Cliente cl)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Clientes SET Descripcion=$de,Direccion=$di,Ciudad=$ci,
                Estado=$es,CiRif=$cr,Nit=$ni,Fax=$fa,Telefono=$t1,Telefono2=$t2,Credito=$cred,
                TipoCliente=$ti,Fecha=$fe,Observaciones=$ob,Descuento=$dc,AFavor=$af WHERE Codigo=$co";
            BindCliente(cmd, cl); cmd.ExecuteNonQuery();
        }

        public static void EliminarCliente(string codigo)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Clientes WHERE Codigo=$id", ("$id", codigo));
        }

        private static void BindCliente(SqliteCommand cmd, Cliente cl)
        {
            cmd.Parameters.AddWithValue("$co",   cl.Codigo);
            cmd.Parameters.AddWithValue("$de",   cl.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$di",   cl.Direccion ?? "");
            cmd.Parameters.AddWithValue("$ci",   cl.Ciudad ?? "");
            cmd.Parameters.AddWithValue("$es",   cl.Estado ?? "");
            cmd.Parameters.AddWithValue("$cr",   cl.CiRif ?? "");
            cmd.Parameters.AddWithValue("$ni",   cl.Nit ?? "");
            cmd.Parameters.AddWithValue("$fa",   cl.Fax ?? "");
            cmd.Parameters.AddWithValue("$t1",   cl.Telefono ?? "");
            cmd.Parameters.AddWithValue("$t2",   cl.Telefono2 ?? "");
            cmd.Parameters.AddWithValue("$cred", (double)cl.Credito);
            cmd.Parameters.AddWithValue("$ti",   cl.TipoCliente ?? "");
            cmd.Parameters.AddWithValue("$fe",   cl.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$ob",   cl.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$dc",   (double)cl.Descuento);
            cmd.Parameters.AddWithValue("$af",   (double)cl.AFavor);
        }

        // ════════ PROVEEDORES ════════════════════════════════════════════════
        public static List<Proveedor> ObtenerProveedores()
        {
            var list = new List<Proveedor>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Proveedores ORDER BY Descripcion";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Proveedor {
                    Codigo = r.GetString(0), Descripcion = S(r,1), Representante = S(r,2),
                    Direccion = S(r,3), Telefono1 = S(r,4), Telefono2 = S(r,5),
                    Fax1 = S(r,6), Fax2 = S(r,7), DiasCredito = r.IsDBNull(8) ? 0 : r.GetInt32(8),
                    PctRetencion = D(r,9),
                    FechaInicio = r.IsDBNull(10) ? DateTime.Today : DateTime.Parse(r.GetString(10)),
                    CiRif = S(r,11), Nit = S(r,12), Tipo = S(r,13), Observaciones = S(r,14) });
            return list;
        }

        public static void InsertarProveedor(Proveedor p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Proveedores VALUES ($co,$de,$re,$di,$t1,$t2,$f1,$f2,$dc,$pr,$fi,$cr,$ni,$ti,$ob)";
            BindProveedor(cmd, p); cmd.ExecuteNonQuery();
        }

        public static void ActualizarProveedor(Proveedor p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Proveedores SET Descripcion=$de,Representante=$re,Direccion=$di,
                Telefono1=$t1,Telefono2=$t2,Fax1=$f1,Fax2=$f2,DiasCredito=$dc,PctRetencion=$pr,
                FechaInicio=$fi,CiRif=$cr,Nit=$ni,Tipo=$ti,Observaciones=$ob WHERE Codigo=$co";
            BindProveedor(cmd, p); cmd.ExecuteNonQuery();
        }

        public static void EliminarProveedor(string codigo)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Proveedores WHERE Codigo=$id", ("$id", codigo));
        }

        private static void BindProveedor(SqliteCommand cmd, Proveedor p)
        {
            cmd.Parameters.AddWithValue("$co", p.Codigo);
            cmd.Parameters.AddWithValue("$de", p.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$re", p.Representante ?? "");
            cmd.Parameters.AddWithValue("$di", p.Direccion ?? "");
            cmd.Parameters.AddWithValue("$t1", p.Telefono1 ?? "");
            cmd.Parameters.AddWithValue("$t2", p.Telefono2 ?? "");
            cmd.Parameters.AddWithValue("$f1", p.Fax1 ?? "");
            cmd.Parameters.AddWithValue("$f2", p.Fax2 ?? "");
            cmd.Parameters.AddWithValue("$dc", p.DiasCredito);
            cmd.Parameters.AddWithValue("$pr", (double)p.PctRetencion);
            cmd.Parameters.AddWithValue("$fi", p.FechaInicio.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cr", p.CiRif ?? "");
            cmd.Parameters.AddWithValue("$ni", p.Nit ?? "");
            cmd.Parameters.AddWithValue("$ti", p.Tipo ?? "");
            cmd.Parameters.AddWithValue("$ob", p.Observaciones ?? "");
        }

        // ════════ FACTURAS ═══════════════════════════════════════════════════
        public static List<Factura> ObtenerFacturas(
            bool incluirAnuladas = true, bool? incluirNotasEntrega = null)
        {
            // incluirNotasEntrega: null → respeta Sesion.MostrarNotasEntrega
            //                      true → incluye siempre (para FrmFacturas listing)
            //                      false → nunca (para reportes/totales/CxC)
            bool incluirNE = incluirNotasEntrega ?? Sesion.MostrarNotasEntrega;

            var list = new List<Factura>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            // Columnas explícitas (más robusto que SELECT * con schema cambiante).
            cmd.CommandText = @"
                SELECT NumeroF, Fecha, Codigo, Descuento, Transporte, Observaciones,
                       FormaDePago, Abono, IvaF, DiasCreditoF, TotalF,
                       COALESCE(Anulada,0) AS Anulada,
                       MotivoAnulacion, FechaAnulacion, UsuarioAnulacion,
                       COALESCE(TipoDocumento,'FacturaConIVA') AS TipoDocumento,
                       COALESCE(EsExportacion,0)               AS EsExportacion,
                       DestinoExportacion
                FROM   Facturas
                WHERE  1=1
                " + (incluirAnuladas ? "" : "AND COALESCE(Anulada,0) = 0 ")
                  + (incluirNE       ? "" : "AND COALESCE(TipoDocumento,'FacturaConIVA') != 'NotaEntrega' ") + @"
                ORDER BY Fecha DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(LeerFactura(r));
            return list;
        }

        // Helper centralizado para parsear filas de Facturas. Tolera fechas
        // inválidas (las trata como anuladas con DateTime.MinValue) para que
        // la lectura nunca tire excepción aunque queden filas corruptas.
        private static Factura LeerFactura(SqliteDataReader r)
        {
            var f = new Factura {
                NumeroF       = r.GetString(0),
                Codigo        = S(r,2), Descuento = D(r,3), Transporte = S(r,4),
                Observaciones = S(r,5), FormaDePago = S(r,6),
                Abono         = D(r,7), IvaF = D(r,8),
                DiasCreditoF  = r.IsDBNull(9) ? 0 : r.GetInt32(9),
                TotalF        = D(r,10),
                // COALESCE devuelve INTEGER → puede ser long en SQLite
                Anulada       = !r.IsDBNull(11) && Convert.ToInt64(r.GetValue(11)) != 0,
                MotivoAnulacion    = S(r,12),
                UsuarioAnulacion   = S(r,14),
                // v22: tipo de documento
                TipoDocumento      = S(r,15),
                EsExportacion      = !r.IsDBNull(16) && Convert.ToInt64(r.GetValue(16)) != 0,
                DestinoExportacion = S(r,17)
            };
            if (string.IsNullOrEmpty(f.TipoDocumento)) f.TipoDocumento = "FacturaConIVA";
            // Fecha tolerante: si está corrupta, no se rompe la lectura
            string sFecha = r.IsDBNull(1) ? "" : r.GetString(1);
            f.Fecha = DateTime.TryParse(sFecha, out var dt) ? dt : DateTime.MinValue;
            // FechaAnulacion (nullable)
            string sFa = S(r, 13);
            if (!string.IsNullOrEmpty(sFa) && DateTime.TryParse(sFa, out var dtA))
                f.FechaAnulacion = dtA;
            return f;
        }

        public static void InsertarFactura(Factura f)
        {
            // ── Auto-clasificación de TipoDocumento ──────────────────────
            // Regla: el tipo se DEDUCE de IVA + EsExportacion, no se confía
            // en lo que llegue del caller (defensa contra manipulación).
            //   IVA > 0                 → FacturaConIVA
            //   IVA = 0 + EsExportacion → FacturaExportacion (legal)
            //   IVA = 0 + NO export     → NotaEntrega (sin valor fiscal)
            f.TipoDocumento = ClasificarTipoDocumento(f.IvaF, f.EsExportacion);

            // Si es Nota de Entrega y el caller no asignó número, generar
            // uno con prefijo NE-YYYY-NNNN (correlativo independiente).
            if (f.TipoDocumento == "NotaEntrega" &&
                (string.IsNullOrWhiteSpace(f.NumeroF) ||
                 !f.NumeroF.StartsWith("NE-", StringComparison.OrdinalIgnoreCase)))
            {
                f.NumeroF = ProximoNumeroNE();
            }

            // Capturar SNAPSHOT de la tasa vigente en el momento de emisión.
            if (f.IdTasaCambio == 0)
            {
                try
                {
                    var tasa = ObtenerTasaPorFecha(f.Fecha);
                    if (tasa != null)
                    {
                        f.IdTasaCambio      = tasa.Id;
                        f.TasaBCVUsada      = tasa.TasaBCV;
                        f.TasaParaleloUsada = tasa.TasaParalelo;
                    }
                }
                catch { /* sin tasa: factura se inserta con 0 — auditable luego */ }
            }

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Facturas
                    (NumeroF, Fecha, Codigo, Descuento, Transporte, Observaciones,
                     FormaDePago, Abono, IvaF, DiasCreditoF, TotalF,
                     IdTasaCambio, TasaBCVUsada, TasaParaleloUsada,
                     TipoDocumento, EsExportacion, DestinoExportacion)
                VALUES
                    ($nf, $fe, $co, $dc, $tr, $ob,
                     $fp, $ab, $iv, $df, $tf,
                     $idt, $tbcv, $tpar,
                     $tipo, $exp, $dest)";
            BindFactura(cmd, f);
            cmd.Parameters.AddWithValue("$idt",  f.IdTasaCambio == 0 ? (object)DBNull.Value : f.IdTasaCambio);
            cmd.Parameters.AddWithValue("$tbcv", (double)f.TasaBCVUsada);
            cmd.Parameters.AddWithValue("$tpar", (double)f.TasaParaleloUsada);
            cmd.Parameters.AddWithValue("$tipo", f.TipoDocumento ?? "FacturaConIVA");
            cmd.Parameters.AddWithValue("$exp",  f.EsExportacion ? 1 : 0);
            cmd.Parameters.AddWithValue("$dest", (object)(f.DestinoExportacion ?? "") ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            // Auditar la creación, especialmente si es NotaEntrega
            if (f.TipoDocumento == "NotaEntrega")
                RegistrarAuditoria("NE_CREADA", "Facturas",
                    $"Nota de Entrega {f.NumeroF} emitida — Cliente {f.Codigo} — Total {f.TotalF:C2}");
        }

        public static void ActualizarFactura(Factura f)
        {
            // Reclasificar al actualizar — por si cambió el IVA o exportación
            f.TipoDocumento = ClasificarTipoDocumento(f.IvaF, f.EsExportacion);

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Facturas SET Fecha=$fe,Codigo=$co,Descuento=$dc,Transporte=$tr,
                Observaciones=$ob,FormaDePago=$fp,Abono=$ab,IvaF=$iv,DiasCreditoF=$df,TotalF=$tf,
                TipoDocumento=$tipo, EsExportacion=$exp, DestinoExportacion=$dest
                WHERE NumeroF=$nf";
            BindFactura(cmd, f);
            cmd.Parameters.AddWithValue("$tipo", f.TipoDocumento ?? "FacturaConIVA");
            cmd.Parameters.AddWithValue("$exp",  f.EsExportacion ? 1 : 0);
            cmd.Parameters.AddWithValue("$dest", (object)(f.DestinoExportacion ?? "") ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // Reglas de clasificación CENTRALIZADAS — no confiar en el caller.
        public static string ClasificarTipoDocumento(decimal iva, bool esExportacion)
        {
            if (iva > 0) return "FacturaConIVA";
            return esExportacion ? "FacturaExportacion" : "NotaEntrega";
        }

        // Siguiente número NE-YYYY-NNNN del año actual, en serie independiente
        // del correlativo de facturas legales. Empieza en 0001 cada año.
        public static string ProximoNumeroNE()
        {
            int year = DateTime.Today.Year;
            string prefix = $"NE-{year}-";
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT NumeroF FROM Facturas
                                WHERE NumeroF LIKE $p
                                ORDER BY NumeroF DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$p", prefix + "%");
            var last = cmd.ExecuteScalar()?.ToString();
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length)
            {
                var sufijo = last.Substring(prefix.Length);
                if (int.TryParse(sufijo, out int n)) next = n + 1;
            }
            return $"{prefix}{next:D4}";
        }

        public static void EliminarFactura(string numero)
        {
            // ATENCIÓN: el flujo recomendado es AnularFactura — no eliminar.
            // Eliminar rompe el correlativo SENIAT y deja huecos en la
            // numeración, lo cual es problemático ante una auditoría fiscal.
            // Este método se conserva sólo para limpieza administrativa.
            using var c = Conn();
            Exec(c, "DELETE FROM Facturas WHERE NumeroF=$id", ("$id", numero));
        }

        // ── Anulación reversible (preferido sobre eliminar) ─────────────────
        // Marca la factura como anulada, conservando la fila para trazabilidad
        // fiscal. Reportes, CxC y dashboard la excluyen automáticamente.
        public static void AnularFactura(string numero, string motivo, string usuario)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("Número de factura requerido.");
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
                throw new ArgumentException("Motivo de anulación requerido (mínimo 5 caracteres).");

            using var c = Conn();
            Exec(c, @"UPDATE Facturas
                      SET Anulada = 1,
                          MotivoAnulacion  = $m,
                          FechaAnulacion   = $f,
                          UsuarioAnulacion = $u
                      WHERE NumeroF = $n
                        AND COALESCE(Anulada,0) = 0",
                ("$m", motivo.Trim()),
                ("$f", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                ("$u", usuario ?? "desconocido"),
                ("$n", numero));
            RegistrarAuditoria("FACTURA_ANULADA", "Facturas",
                $"Factura {numero} anulada por {usuario}. Motivo: {motivo.Trim()}");
        }

        // Reactivar una factura anulada (sólo Admin debe llamarla — la UI
        // controla el permiso). Sirve para revertir un anular hecho por error.
        public static void ReactivarFactura(string numero, string usuario)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("Número de factura requerido.");
            using var c = Conn();
            Exec(c, @"UPDATE Facturas
                      SET Anulada = 0,
                          MotivoAnulacion  = NULL,
                          FechaAnulacion   = NULL,
                          UsuarioAnulacion = NULL
                      WHERE NumeroF = $n", ("$n", numero));
            RegistrarAuditoria("FACTURA_REACTIVADA", "Facturas",
                $"Factura {numero} reactivada por {usuario}");
        }

        // Corrige la fecha de una factura cuando el operador detecta un error
        // (típicamente para "des-anular" una factura que fue anulada por la
        // migración v21 al detectar fecha inválida). Requiere permiso de
        // FacturasAnular y deja registro en auditoría.
        public static void CorregirFechaFactura(string numero, DateTime nuevaFecha, string usuario)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("Número de factura requerido.");
            int anioMax = DateTime.Today.Year + 1;
            if (nuevaFecha.Year < 2000 || nuevaFecha.Year > anioMax)
                throw new ArgumentException(
                    $"La fecha debe estar entre 2000 y {anioMax}.");

            using var c = Conn();
            // Capturar fecha anterior para el log
            string fechaAnt = "";
            using (var rd = c.CreateCommand())
            {
                rd.CommandText = "SELECT Fecha FROM Facturas WHERE NumeroF=$n";
                rd.Parameters.AddWithValue("$n", numero);
                fechaAnt = rd.ExecuteScalar()?.ToString() ?? "";
            }
            Exec(c, "UPDATE Facturas SET Fecha=$f WHERE NumeroF=$n",
                ("$f", nuevaFecha.ToString("yyyy-MM-dd")), ("$n", numero));
            RegistrarAuditoria("FACTURA_FECHA_CORREGIDA", "Facturas",
                $"Factura {numero}: fecha {fechaAnt} → {nuevaFecha:yyyy-MM-dd} por {usuario}");
        }

        private static void BindFactura(SqliteCommand cmd, Factura f)
        {
            cmd.Parameters.AddWithValue("$nf", f.NumeroF);
            cmd.Parameters.AddWithValue("$fe", f.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$co", f.Codigo ?? "");
            cmd.Parameters.AddWithValue("$dc", (double)f.Descuento);
            cmd.Parameters.AddWithValue("$tr", f.Transporte ?? "");
            cmd.Parameters.AddWithValue("$ob", f.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$fp", f.FormaDePago ?? "");
            cmd.Parameters.AddWithValue("$ab", (double)f.Abono);
            cmd.Parameters.AddWithValue("$iv", (double)f.IvaF);
            cmd.Parameters.AddWithValue("$df", f.DiasCreditoF);
            cmd.Parameters.AddWithValue("$tf", (double)f.TotalF);
        }

        // ════════ COMPRAS ════════════════════════════════════════════════════
        public static List<Compra> ObtenerCompras()
        {
            var list = new List<Compra>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Compras ORDER BY FechaC DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Compra {
                    CompraN = r.GetString(0),
                    FechaC = DateTime.Parse(r.GetString(1)),
                    CodigoP = S(r,2), DescuentoC = D(r,3), FormaDePago = S(r,4),
                    AbonoC = D(r,5), IvaC = D(r,6), DiasDeCred = r.IsDBNull(7) ? 0 : r.GetInt32(7),
                    TotalC = D(r,8), Transporte = S(r,9), Observaciones = S(r,10) });
            return list;
        }

        public static void InsertarCompra(Compra cp)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Compras VALUES ($cn,$fc,$cp,$dc,$fp,$ab,$iv,$dd,$tc,$tr,$ob)";
            BindCompra(cmd, cp); cmd.ExecuteNonQuery();
        }

        public static void ActualizarCompra(Compra cp)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Compras SET FechaC=$fc,CodigoP=$cp,DescuentoC=$dc,FormaDePago=$fp,
                AbonoC=$ab,IvaC=$iv,DiasDeCred=$dd,TotalC=$tc,Transporte=$tr,Observaciones=$ob WHERE CompraN=$cn";
            BindCompra(cmd, cp); cmd.ExecuteNonQuery();
        }

        public static void EliminarCompra(string numero)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Compras WHERE CompraN=$id", ("$id", numero));
        }

        private static void BindCompra(SqliteCommand cmd, Compra cp)
        {
            cmd.Parameters.AddWithValue("$cn", cp.CompraN);
            cmd.Parameters.AddWithValue("$fc", cp.FechaC.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cp", cp.CodigoP ?? "");
            cmd.Parameters.AddWithValue("$dc", (double)cp.DescuentoC);
            cmd.Parameters.AddWithValue("$fp", cp.FormaDePago ?? "");
            cmd.Parameters.AddWithValue("$ab", (double)cp.AbonoC);
            cmd.Parameters.AddWithValue("$iv", (double)cp.IvaC);
            cmd.Parameters.AddWithValue("$dd", cp.DiasDeCred);
            cmd.Parameters.AddWithValue("$tc", (double)cp.TotalC);
            cmd.Parameters.AddWithValue("$tr", cp.Transporte ?? "");
            cmd.Parameters.AddWithValue("$ob", cp.Observaciones ?? "");
        }

        // ════════ ABONOS ══════════════════════════════════════════════════════
        public static List<Abono> ObtenerAbonos(string numeroF = null)
        {
            var list = new List<Abono>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = numeroF == null
                ? "SELECT * FROM Abonos ORDER BY Fecha DESC"
                : "SELECT * FROM Abonos WHERE NumeroF=$nf ORDER BY Fecha DESC";
            if (numeroF != null) cmd.Parameters.AddWithValue("$nf", numeroF);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Abono { NumAbono = r.GetInt32(0), NumeroF = S(r,1),
                    Fecha = DateTime.Parse(r.GetString(2)), Monto = D(r,3), Cliente = S(r,4) });
            return list;
        }

        public static void InsertarAbono(Abono a)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Abonos (NumeroF,Fecha,Monto,Cliente) VALUES ($nf,$fe,$mo,$cl)";
            cmd.Parameters.AddWithValue("$nf", a.NumeroF ?? "");
            cmd.Parameters.AddWithValue("$fe", a.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$mo", (double)a.Monto);
            cmd.Parameters.AddWithValue("$cl", a.Cliente ?? "");
            cmd.ExecuteNonQuery();
        }

        // ════════ MOLDES ═════════════════════════════════════════════════════
        public static List<TbMolde> ObtenerMoldes()
        {
            var list = new List<TbMolde>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM TbMoldes ORDER BY Descripcion";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TbMolde { CodigoMold = r.GetString(0), Descripcion = S(r,1),
                    Fecha = r.IsDBNull(2) || string.IsNullOrWhiteSpace(r.GetString(2)) ? DateTime.Today : DateTime.Parse(r.GetString(2)),
                    FechaLastMant = r.IsDBNull(3) || string.IsNullOrWhiteSpace(r.GetString(3)) ? DateTime.Today : DateTime.Parse(r.GetString(3)),
                    DescMant = S(r,4), ModeloQfabrica = S(r,5),
                    Observaciones = S(r,6), CantMatXunid = D(r,7),
                    CodigoProducto = r.FieldCount > 8 ? S(r,8) : "" });
            return list;
        }

        public static void InsertarMolde(TbMolde m)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO TbMoldes VALUES ($co,$de,$fe,$fl,$dm,$mq,$ob,$cm,$cp)";
            BindMolde(cmd, m); cmd.ExecuteNonQuery();
        }

        public static void ActualizarMolde(TbMolde m)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE TbMoldes SET Descripcion=$de,Fecha=$fe,FechaLastMant=$fl,
                DescMant=$dm,ModeloQfabrica=$mq,Observaciones=$ob,CantMatXunid=$cm,CodigoProducto=$cp WHERE CodigoMold=$co";
            BindMolde(cmd, m); cmd.ExecuteNonQuery();
        }

        public static void EliminarMolde(string codigo)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM TbMoldes WHERE CodigoMold=$id", ("$id", codigo));
        }

        private static void BindMolde(SqliteCommand cmd, TbMolde m)
        {
            cmd.Parameters.AddWithValue("$co", m.CodigoMold);
            cmd.Parameters.AddWithValue("$de", m.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$fe", m.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fl", m.FechaLastMant.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$dm", m.DescMant ?? "");
            cmd.Parameters.AddWithValue("$mq", m.ModeloQfabrica ?? "");
            cmd.Parameters.AddWithValue("$ob", m.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$cm", (double)m.CantMatXunid);
            cmd.Parameters.AddWithValue("$cp", m.CodigoProducto ?? "");
        }

        // ════════ PRODUCCION DEL DIA ══════════════════════════════════════════
        public static List<ProduccionDelDiaEnLinea> ObtenerProduccionDia()
        {
            var list = new List<ProduccionDelDiaEnLinea>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM ProduccionDelDiaEnLinea ORDER BY FechaLinea DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ProduccionDelDiaEnLinea {
                    FechaLinea = DateTime.Parse(r.GetString(0)),
                    CantidadProducida = D(r,1), SacosConsumidos = D(r,2), FustesMalos = D(r,3) });
            return list;
        }

        public static void InsertarProduccionDia(ProduccionDelDiaEnLinea p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO ProduccionDelDiaEnLinea VALUES ($fl,$cp,$sc,$fm)";
            cmd.Parameters.AddWithValue("$fl", p.FechaLinea.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cp", (double)p.CantidadProducida);
            cmd.Parameters.AddWithValue("$sc", (double)p.SacosConsumidos);
            cmd.Parameters.AddWithValue("$fm", (double)p.FustesMalos);
            cmd.ExecuteNonQuery();
        }

        // ════════ GASTOS ══════════════════════════════════════════════════════
        public static List<Gasto> ObtenerGastos()
        {
            var list = new List<Gasto>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Gastos ORDER BY Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Gasto { Id = r.GetInt32(0), Categoria = S(r,1), Descripcion = S(r,2),
                    Monto = D(r,3), Fecha = DateTime.Parse(r.GetString(4)),
                    Proveedor = S(r,5), Estado = S(r,6) });
            return list;
        }

        public static void InsertarGasto(Gasto g)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Gastos (Categoria,Descripcion,Monto,Fecha,Proveedor,Estado) VALUES ($ca,$de,$mo,$fe,$pr,$es)";
            cmd.Parameters.AddWithValue("$ca", g.Categoria); cmd.Parameters.AddWithValue("$de", g.Descripcion);
            cmd.Parameters.AddWithValue("$mo", (double)g.Monto); cmd.Parameters.AddWithValue("$fe", g.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$pr", g.Proveedor ?? ""); cmd.Parameters.AddWithValue("$es", g.Estado);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()"; g.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarGasto(Gasto g)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Gastos SET Categoria=$ca,Descripcion=$de,Monto=$mo,Fecha=$fe,Proveedor=$pr,Estado=$es WHERE Id=$id";
            cmd.Parameters.AddWithValue("$ca", g.Categoria); cmd.Parameters.AddWithValue("$de", g.Descripcion);
            cmd.Parameters.AddWithValue("$mo", (double)g.Monto); cmd.Parameters.AddWithValue("$fe", g.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$pr", g.Proveedor ?? ""); cmd.Parameters.AddWithValue("$es", g.Estado);
            cmd.Parameters.AddWithValue("$id", g.Id); cmd.ExecuteNonQuery();
        }

        public static void EliminarGasto(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Gastos WHERE Id=$id", ("$id", id));
        }

        // ════════ INGRESO PRODUCCION ════════════════════════════════════════
        public static List<IngresoProduccion> ObtenerIngresos()
        {
            var list = new List<IngresoProduccion>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM IngresoProduccion ORDER BY Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new IngresoProduccion {
                    Id = r.GetInt32(0), Producto = S(r,1), Lote = S(r,2),
                    Cantidad = r.IsDBNull(3) ? 0 : r.GetInt32(3), Unidad = S(r,4),
                    Fecha = r.IsDBNull(5) ? DateTime.Today : DateTime.Parse(r.GetString(5)),
                    Responsable = S(r,6), Observacion = S(r,7), Estado = S(r,8) });
            return list;
        }

        public static void InsertarIngreso(IngresoProduccion ip)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO IngresoProduccion (Producto,Lote,Cantidad,Unidad,Fecha,Responsable,Observacion,Estado)
                VALUES ($p,$l,$c,$u,$f,$r,$o,$e)";
            cmd.Parameters.AddWithValue("$p", ip.Producto ?? ""); cmd.Parameters.AddWithValue("$l", ip.Lote ?? "");
            cmd.Parameters.AddWithValue("$c", ip.Cantidad);       cmd.Parameters.AddWithValue("$u", ip.Unidad ?? "");
            cmd.Parameters.AddWithValue("$f", ip.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$r", ip.Responsable ?? ""); cmd.Parameters.AddWithValue("$o", ip.Observacion ?? "");
            cmd.Parameters.AddWithValue("$e", ip.Estado ?? "");
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()"; ip.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarIngreso(IngresoProduccion ip)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE IngresoProduccion SET Producto=$p,Lote=$l,Cantidad=$c,Unidad=$u,
                Fecha=$f,Responsable=$r,Observacion=$o,Estado=$e WHERE Id=$id";
            cmd.Parameters.AddWithValue("$p",  ip.Producto ?? ""); cmd.Parameters.AddWithValue("$l", ip.Lote ?? "");
            cmd.Parameters.AddWithValue("$c",  ip.Cantidad);       cmd.Parameters.AddWithValue("$u", ip.Unidad ?? "");
            cmd.Parameters.AddWithValue("$f",  ip.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$r",  ip.Responsable ?? ""); cmd.Parameters.AddWithValue("$o", ip.Observacion ?? "");
            cmd.Parameters.AddWithValue("$e",  ip.Estado ?? "");      cmd.Parameters.AddWithValue("$id", ip.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarIngreso(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM IngresoProduccion WHERE Id=$id", ("$id", id));
        }

        // ════════ CUENTAS POR COBRAR ═════════════════════════════════════════
        public static List<CuentaCobrar> ObtenerCuentas()
        {
            var list = new List<CuentaCobrar>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM CuentasCobrar ORDER BY FechaVence ASC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CuentaCobrar {
                    Id = r.GetInt32(0), Cliente = S(r,1), Concepto = S(r,2),
                    Monto = D(r,3), Pagado = D(r,4),
                    FechaEmision = r.IsDBNull(5) ? DateTime.Today : DateTime.Parse(r.GetString(5)),
                    FechaVence   = r.IsDBNull(6) ? DateTime.Today : DateTime.Parse(r.GetString(6)),
                    Estado = S(r,7) });
            return list;
        }

        public static void InsertarCuenta(CuentaCobrar cc)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO CuentasCobrar (Cliente,Concepto,Monto,Pagado,FechaEmision,FechaVence,Estado)
                VALUES ($cl,$co,$m,$p,$fe,$fv,$e)";
            cmd.Parameters.AddWithValue("$cl", cc.Cliente ?? ""); cmd.Parameters.AddWithValue("$co", cc.Concepto ?? "");
            cmd.Parameters.AddWithValue("$m",  (double)cc.Monto); cmd.Parameters.AddWithValue("$p", (double)cc.Pagado);
            cmd.Parameters.AddWithValue("$fe", cc.FechaEmision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fv", cc.FechaVence.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$e",  cc.Estado ?? "");
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()"; cc.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarCuenta(CuentaCobrar cc)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE CuentasCobrar SET Cliente=$cl,Concepto=$co,Monto=$m,
                Pagado=$p,FechaEmision=$fe,FechaVence=$fv,Estado=$e WHERE Id=$id";
            cmd.Parameters.AddWithValue("$cl", cc.Cliente ?? ""); cmd.Parameters.AddWithValue("$co", cc.Concepto ?? "");
            cmd.Parameters.AddWithValue("$m",  (double)cc.Monto); cmd.Parameters.AddWithValue("$p", (double)cc.Pagado);
            cmd.Parameters.AddWithValue("$fe", cc.FechaEmision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fv", cc.FechaVence.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$e",  cc.Estado ?? ""); cmd.Parameters.AddWithValue("$id", cc.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarCuenta(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM CuentasCobrar WHERE Id=$id", ("$id", id));
        }

        // ════════ PRODUCTOS / PRECIOS ═════════════════════════════════════════
        public static List<ProductoPrecio> ObtenerProductos()
        {
            var list = new List<ProductoPrecio>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM ProductosPrecios ORDER BY Nombre ASC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ProductoPrecio {
                    Id = r.GetInt32(0), Codigo = S(r,1), Nombre = S(r,2), Categoria = S(r,3),
                    CostoActual = D(r,4), PrecioActual = D(r,5), CostoNuevo = D(r,6), PrecioNuevo = D(r,7),
                    UltimaActualizacion = r.IsDBNull(8) ? DateTime.Today : DateTime.Parse(r.GetString(8)) });
            return list;
        }

        public static void InsertarProducto(ProductoPrecio p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO ProductosPrecios (Codigo,Nombre,Categoria,CostoActual,PrecioActual,CostoNuevo,PrecioNuevo,UltimaActualizacion)
                VALUES ($co,$n,$ca,$ca2,$pa,$cn,$pn,$ua)";
            cmd.Parameters.AddWithValue("$co", p.Codigo ?? ""); cmd.Parameters.AddWithValue("$n", p.Nombre ?? "");
            cmd.Parameters.AddWithValue("$ca", p.Categoria ?? ""); cmd.Parameters.AddWithValue("$ca2", (double)p.CostoActual);
            cmd.Parameters.AddWithValue("$pa", (double)p.PrecioActual); cmd.Parameters.AddWithValue("$cn", (double)p.CostoNuevo);
            cmd.Parameters.AddWithValue("$pn", (double)p.PrecioNuevo); cmd.Parameters.AddWithValue("$ua", p.UltimaActualizacion.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()"; p.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarProducto(ProductoPrecio p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE ProductosPrecios SET Codigo=$co,Nombre=$n,Categoria=$ca,
                CostoActual=$ca2,PrecioActual=$pa,CostoNuevo=$cn,PrecioNuevo=$pn,UltimaActualizacion=$ua WHERE Id=$id";
            cmd.Parameters.AddWithValue("$co", p.Codigo ?? ""); cmd.Parameters.AddWithValue("$n", p.Nombre ?? "");
            cmd.Parameters.AddWithValue("$ca", p.Categoria ?? ""); cmd.Parameters.AddWithValue("$ca2", (double)p.CostoActual);
            cmd.Parameters.AddWithValue("$pa", (double)p.PrecioActual); cmd.Parameters.AddWithValue("$cn", (double)p.CostoNuevo);
            cmd.Parameters.AddWithValue("$pn", (double)p.PrecioNuevo); cmd.Parameters.AddWithValue("$ua", p.UltimaActualizacion.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$id", p.Id); cmd.ExecuteNonQuery();
        }

        public static void EliminarProducto(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM ProductosPrecios WHERE Id=$id", ("$id", id));
        }

                // ════════ KARDEX - MOVIMIENTOS DE INVENTARIO ════════════════════════════
        /// <summary>
        /// Registra un movimiento en el Kardex y actualiza el stock del producto.
        /// Toda la operación ocurre en una sola transacción SQLite.
        /// </summary>
        public static MovimientoInventario RegistrarMovimiento(
            string codigoProducto,
            string tipoMovimiento,   // "ENTRADA" o "SALIDA"
            decimal cantidad,
            string concepto,
            string referencia = "",
            string usuario = "Sistema",
            decimal costoUnitarioEntrada = 0)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor que cero.");

            using var conn = new SqliteConnection(ConnStr);
            conn.Open();

            // ALTA-5 FIX: BEGIN IMMEDIATE bloquea escrituras concurrentes mientras se verifica
            // y descuenta el stock — elimina la race condition donde dos procesos leen el
            // saldo antes de que alguno haga commit (double-spend de inventario).
            using var tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                // 1. Leer saldo actual del producto
                var cmdSaldo = conn.CreateCommand();
                cmdSaldo.Transaction = tx;
                cmdSaldo.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo = $cod";
                cmdSaldo.Parameters.AddWithValue("$cod", codigoProducto);
                var obj = cmdSaldo.ExecuteScalar();
                if (obj == null)
                    throw new Exception($"Producto '{codigoProducto}' no existe en Inventario.");

                decimal saldoAnterior = (decimal)(double)obj;

                // 2. Calcular nuevo saldo
                decimal nuevoSaldo = tipoMovimiento == "ENTRADA"
                    ? saldoAnterior + cantidad
                    : saldoAnterior - cantidad;

                if (nuevoSaldo < 0)
                    throw new Exception(
                        $"Stock insuficiente. Existencia actual: {saldoAnterior}, " +
                        $"cantidad solicitada: {cantidad}.");

                // 3. Insertar fila en Kardex
                var cmdIns = conn.CreateCommand();
                cmdIns.Transaction = tx;
                cmdIns.CommandText = @"
                    INSERT INTO MovimientosInventario
                        (FechaHora, CodigoProducto, TipoMovimiento, Concepto,
                         Cantidad, SaldoAnterior, NuevoSaldo, Referencia, Usuario,
                         TipoKardex, CostoUnitario)
                    VALUES ($fh,$cp,$tm,$co,$ca,$sa,$ns,$re,$us,'PRODUCTO',$cu)";
                cmdIns.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdIns.Parameters.AddWithValue("$cp", codigoProducto);
                cmdIns.Parameters.AddWithValue("$tm", tipoMovimiento);
                cmdIns.Parameters.AddWithValue("$co", concepto ?? "");
                cmdIns.Parameters.AddWithValue("$ca", (double)cantidad);
                cmdIns.Parameters.AddWithValue("$sa", (double)saldoAnterior);
                cmdIns.Parameters.AddWithValue("$ns", (double)nuevoSaldo);
                cmdIns.Parameters.AddWithValue("$re", referencia ?? "");
                cmdIns.Parameters.AddWithValue("$us", usuario ?? "Sistema");
                cmdIns.Parameters.AddWithValue("$cu", (double)costoUnitarioEntrada);
                cmdIns.ExecuteNonQuery();

                // Recuperar el ID generado
                var cmdId = conn.CreateCommand();
                cmdId.Transaction = tx;
                cmdId.CommandText = "SELECT last_insert_rowid()";
                int idMov = (int)(long)cmdId.ExecuteScalar();

                // 4. Actualizar stock en tabla maestra Inventario
                var cmdUpd = conn.CreateCommand();
                cmdUpd.Transaction = tx;
                cmdUpd.CommandText = @"
                    UPDATE Inventario
                    SET Existencia = $ns, UltimaActualz = $fecha
                    WHERE Codigo = $cod";
                cmdUpd.Parameters.AddWithValue("$ns",    (double)nuevoSaldo);
                cmdUpd.Parameters.AddWithValue("$fecha", DateTime.Today.ToString("yyyy-MM-dd"));
                cmdUpd.Parameters.AddWithValue("$cod",   codigoProducto);
                cmdUpd.ExecuteNonQuery();

                tx.Commit();

                return new MovimientoInventario
                {
                    IdMovimiento   = idMov,
                    FechaHora      = DateTime.Now,
                    CodigoProducto = codigoProducto,
                    TipoMovimiento = tipoMovimiento,
                    Concepto       = concepto ?? "",
                    Cantidad       = cantidad,
                    SaldoAnterior  = saldoAnterior,
                    NuevoSaldo     = nuevoSaldo,
                    Referencia     = referencia ?? "",
                    Usuario        = usuario ?? "Sistema",
                    TipoKardex     = "PRODUCTO",
                    CostoUnitario  = costoUnitarioEntrada
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Obtiene el historial de movimientos de un producto (o todos si codigo es null).
        /// </summary>
        public static List<MovimientoInventario> ObtenerKardex(
            string codigoProducto = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var list = new List<MovimientoInventario>();
            using var c = Conn();
            var cmd = c.CreateCommand();

            var where = new System.Collections.Generic.List<string>();
            if (codigoProducto != null) where.Add("CodigoProducto = $cod");
            if (desde != null)          where.Add("FechaHora >= $desde");
            if (hasta != null)          where.Add("FechaHora <= $hasta");

            cmd.CommandText = "SELECT * FROM MovimientosInventario" +
                (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") +
                " ORDER BY IdMovimiento DESC";

            if (codigoProducto != null) cmd.Parameters.AddWithValue("$cod",   codigoProducto);
            if (desde != null)          cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd"));
            if (hasta != null)          cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd") + " 23:59:59");

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MovimientoInventario
                {
                    IdMovimiento   = r.GetInt32(0),
                    FechaHora      = DateTime.Parse(r.GetString(1)),
                    CodigoProducto = S(r, 2),
                    TipoMovimiento = S(r, 3),
                    Concepto       = S(r, 4),
                    Cantidad       = D(r, 5),
                    SaldoAnterior  = D(r, 6),
                    NuevoSaldo     = D(r, 7),
                    Referencia     = S(r, 8),
                    Usuario        = S(r, 9),
                    TipoKardex     = r.FieldCount > 10 ? S(r, 10) : "PRODUCTO",
                    CostoUnitario  = r.FieldCount > 11 ? D(r, 11) : 0m
                });
            return list;
        }

        /// <summary>
        /// Registra una corrección en el Kardex sin borrar ningún movimiento anterior.
        /// </summary>
        public static void RegistrarCorreccion(
            string codigoProducto,
            decimal nuevoStockReal,
            string motivo,
            string usuario = "Admin")
        {
            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            var cmdSaldo = conn.CreateCommand();
            cmdSaldo.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo = $cod";
            cmdSaldo.Parameters.AddWithValue("$cod", codigoProducto);
            var obj = cmdSaldo.ExecuteScalar();
            if (obj == null) throw new Exception($"Producto '{codigoProducto}' no encontrado.");

            decimal saldoActual = (decimal)(double)obj;
            decimal diferencia  = nuevoStockReal - saldoActual;
            string tipo = diferencia >= 0 ? "ENTRADA" : "SALIDA";

            RegistrarMovimiento(
                codigoProducto,
                tipo,
                Math.Abs(diferencia),
                $"AJUSTE/CORRECCIÓN: {motivo}",
                "AJUSTE",
                usuario);
        }

                // ════════ USUARIOS ═══════════════════════════════════════════════════════
        // ── PBKDF2 con sal aleatoria por usuario (RFC 2898, 600k iteraciones SHA-256) ──
        // Formato almacenado: "v2:SALT_HEX:HASH_HEX"
        // El prefijo "v2:" permite migrar el algoritmo en el futuro sin romper logins.
        private const string HASH_PREFIX    = "v2";
        private const int    PBKDF2_ITERS   = 600_000;
        private const int    SALT_BYTES     = 16;   // 128 bits

        /// <summary>
        /// Genera un hash PBKDF2-SHA256 con sal aleatoria única por usuario.
        /// Reemplaza el SHA-256 con sal global que era vulnerable a ataques de diccionario.
        /// </summary>
        public static string HashPassword(string password)
        {
            var salt = new byte[SALT_BYTES];
            System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
            return ComputePbkdf2(password, salt);
        }

        /// <summary>
        /// Verifica una contraseña contra un hash almacenado.
        /// Soporta el formato legacy SHA-256 (v1) para migración gradual.
        /// </summary>
        public static bool VerificarPassword(string password, string hashAlmacenado)
        {
            if (string.IsNullOrEmpty(hashAlmacenado)) return false;

            // Formato nuevo PBKDF2
            if (hashAlmacenado.StartsWith(HASH_PREFIX + ":"))
            {
                var partes = hashAlmacenado.Split(':');
                if (partes.Length != 3) return false;
                var salt = Convert.FromHexString(partes[1]);
                var hashEsperado = ComputePbkdf2(password, salt);
                // Comparación en tiempo constante para evitar timing attacks
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(partes[2]),
                    Convert.FromHexString(hashEsperado.Split(':')[2]));
            }

            // Formato legacy SHA-256 (v1): hash hexadecimal puro de 64 chars
            // Permitimos login pero el hash se migra a PBKDF2 al iniciar sesión
            if (hashAlmacenado.Length == 64 && !hashAlmacenado.Contains(':'))
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes(password + "SG_SALT_2024");
                var legacyHash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLower();
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(legacyHash),
                    System.Text.Encoding.UTF8.GetBytes(hashAlmacenado));
            }

            return false;
        }

        private static string ComputePbkdf2(string password, byte[] salt)
        {
            using var kdf = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password,
                salt,
                PBKDF2_ITERS,
                System.Security.Cryptography.HashAlgorithmName.SHA256);
            var hash = kdf.GetBytes(32);   // 256 bits
            return $"{HASH_PREFIX}:{Convert.ToHexString(salt).ToLower()}:{Convert.ToHexString(hash).ToLower()}";
        }

        // Mantener para compatibilidad con código que solo necesita generar un hash nuevo
        // (InsertarUsuario, SeedAdmin). NO usar para verificación — usar VerificarPassword().
        [System.Obsolete("Usar HashPassword() para generar y VerificarPassword() para verificar.")]
        private static string HashPasswordLegacy(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "SG_SALT_2024");
            return Convert.ToHexString(sha.ComputeHash(bytes)).ToLower();
        }

        public static Usuario ValidarLogin(string nombreUsuario, string password)
        {
            // Paso 1: obtener el hash almacenado por nombre de usuario (sin exponer el hash en el WHERE)
            // Esto evita timing differences por búsqueda de hash en índice.
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id,NombreUsuario,NombreCompleto,PasswordHash,Rol,
                                       Activo,FechaCreacion,UltimoAcceso,RequiereCambioPassword,
                                       IntentosHoy,BloqueadoHasta
                                FROM Usuarios
                                WHERE NombreUsuario=$u AND Activo=1";
            cmd.Parameters.AddWithValue("$u", nombreUsuario);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            // Paso 2: Verificar bloqueo temporal por intentos fallidos
            var bloqueadoHastaStr = r.IsDBNull(10) ? null : r.GetString(10);
            if (bloqueadoHastaStr != null)
            {
                var bloqueadoHasta = DateTime.Parse(bloqueadoHastaStr);
                if (DateTime.Now < bloqueadoHasta) return null;  // sigue bloqueado
            }

            // Paso 3: verificar contraseña con comparación en tiempo constante
            var hashAlmacenado = r.GetString(3);
            if (!VerificarPassword(password, hashAlmacenado))
            {
                // Incrementar contador de intentos fallidos (persistente)
                var intentosHoy = r.IsDBNull(9) ? 0 : r.GetInt32(9);
                intentosHoy++;
                int userId = r.GetInt32(0);
                var cmdFail = c.CreateCommand();
                if (intentosHoy >= 5)
                {
                    // Bloquear 30 minutos
                    var hasta = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss");
                    cmdFail.CommandText = @"UPDATE Usuarios
                        SET IntentosHoy=$it, BloqueadoHasta=$bh WHERE Id=$id";
                    cmdFail.Parameters.AddWithValue("$it", intentosHoy);
                    cmdFail.Parameters.AddWithValue("$bh", hasta);
                    cmdFail.Parameters.AddWithValue("$id", userId);
                }
                else
                {
                    cmdFail.CommandText = @"UPDATE Usuarios
                        SET IntentosHoy=$it WHERE Id=$id";
                    cmdFail.Parameters.AddWithValue("$it", intentosHoy);
                    cmdFail.Parameters.AddWithValue("$id", userId);
                }
                cmdFail.ExecuteNonQuery();
                return null;
            }

            var user = new Usuario
            {
                Id                    = r.GetInt32(0),
                NombreUsuario         = r.GetString(1),
                NombreCompleto        = r.GetString(2),
                PasswordHash          = hashAlmacenado,
                Rol                   = r.GetString(4),
                Activo                = r.GetInt32(5) == 1,
                FechaCreacion         = DateTime.Parse(r.GetString(6)),
                UltimoAcceso          = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7)),
                RequiereCambioPassword= r.IsDBNull(8) ? false : r.GetInt32(8) == 1,
            };

            // Paso 4: Migrar hash legacy SHA-256 a PBKDF2 automáticamente en el login
            var cmdUpd = c.CreateCommand();
            if (!hashAlmacenado.StartsWith(HASH_PREFIX + ":"))
            {
                var nuevoHash = HashPassword(password);
                cmdUpd.CommandText = @"UPDATE Usuarios
                    SET PasswordHash=$ph, UltimoAcceso=$ua, IntentosHoy=0, BloqueadoHasta=NULL
                    WHERE Id=$id";
                cmdUpd.Parameters.AddWithValue("$ph", nuevoHash);
                // FIX DB-LOCKED: usar la misma conn 'c' para auditoría
                // en lugar de RegistrarAuditoria() que abriría una segunda conexión
                try {
                    var _audCmd = c.CreateCommand();
                    _audCmd.CommandText = @"INSERT INTO LogAuditoria
                        (FechaHora,Usuario,Accion,Tabla,Detalle)
                        VALUES ($fh,$us,$ac,$ta,$de)";
                    _audCmd.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    _audCmd.Parameters.AddWithValue("$us", user.NombreUsuario);
                    _audCmd.Parameters.AddWithValue("$ac", "HASH_MIGRADO");
                    _audCmd.Parameters.AddWithValue("$ta", "Usuarios");
                    _audCmd.Parameters.AddWithValue("$de", "Hash SHA-256 migrado a PBKDF2 para: " + user.NombreUsuario);
                    _audCmd.ExecuteNonQuery();
                } catch { }
            }
            else
            {
                cmdUpd.CommandText = @"UPDATE Usuarios
                    SET UltimoAcceso=$ua, IntentosHoy=0, BloqueadoHasta=NULL
                    WHERE Id=$id";
            }
            cmdUpd.Parameters.AddWithValue("$ua", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmdUpd.Parameters.AddWithValue("$id", user.Id);
            cmdUpd.ExecuteNonQuery();

            return user;
        }

        public static System.Collections.Generic.List<Usuario> ObtenerUsuarios()
        {
            var list = new System.Collections.Generic.List<Usuario>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            // Columnas explícitas para no depender del orden de SELECT *
            cmd.CommandText = @"SELECT Id,NombreUsuario,NombreCompleto,PasswordHash,Rol,
                                        Activo,FechaCreacion,UltimoAcceso,
                                        RequiereCambioPassword,IntentosHoy,BloqueadoHasta
                                 FROM Usuarios ORDER BY NombreCompleto";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Usuario
                {
                    Id                    = r.GetInt32(0),
                    NombreUsuario         = r.GetString(1),
                    NombreCompleto        = r.GetString(2),
                    PasswordHash          = r.GetString(3),
                    Rol                   = r.GetString(4),
                    Activo                = r.GetInt32(5) == 1,
                    FechaCreacion         = DateTime.Parse(r.GetString(6)),
                    UltimoAcceso          = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7)),
                    RequiereCambioPassword= r.IsDBNull(8) ? false : r.GetInt32(8) == 1,
                    IntentosHoy           = r.IsDBNull(9) ? 0 : r.GetInt32(9),
                    BloqueadoHasta        = r.IsDBNull(10) ? null : DateTime.Parse(r.GetString(10)),
                });
            return list;
        }

        public static void InsertarUsuario(Usuario u)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Usuarios
                (NombreUsuario,NombreCompleto,PasswordHash,Rol,Activo,FechaCreacion,
                 RequiereCambioPassword,IntentosHoy,BloqueadoHasta)
                VALUES ($u,$nc,$ph,$r,$a,$fc,$rcp,0,NULL)";
            cmd.Parameters.AddWithValue("$u",   u.NombreUsuario);
            cmd.Parameters.AddWithValue("$nc",  u.NombreCompleto);
            cmd.Parameters.AddWithValue("$ph",  u.PasswordHash);
            cmd.Parameters.AddWithValue("$r",   u.Rol);
            cmd.Parameters.AddWithValue("$a",   u.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$fc",  u.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$rcp", u.RequiereCambioPassword ? 1 : 0);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            u.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarUsuario(Usuario u)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Usuarios
                SET NombreCompleto=$nc,PasswordHash=$ph,Rol=$r,Activo=$a,
                    RequiereCambioPassword=$rcp
                WHERE Id=$id";
            cmd.Parameters.AddWithValue("$nc",  u.NombreCompleto);
            cmd.Parameters.AddWithValue("$ph",  u.PasswordHash);
            cmd.Parameters.AddWithValue("$r",   u.Rol);
            cmd.Parameters.AddWithValue("$a",   u.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$rcp", u.RequiereCambioPassword ? 1 : 0);
            cmd.Parameters.AddWithValue("$id",  u.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarUsuario(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM Usuarios WHERE Id=$id", ("$id", id));
        }

        /// <summary>Crea el admin por defecto si no existe ningún usuario.</summary>
        public static void SeedAdminSiNoExiste()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Usuarios";
            var count = (long)cmd.ExecuteScalar();
            if (count == 0)
            {
                // CRÍTICO-2: La contraseña inicial usa PBKDF2.
                // RequiereCambioPassword=1 fuerza cambio en el primer login.
                InsertarUsuario(new Usuario
                {
                    NombreUsuario         = "admin",
                    NombreCompleto        = "Administrador",
                    PasswordHash          = HashPassword("admin123"),   // PBKDF2 con sal aleatoria
                    Rol                   = "Admin",
                    Activo                = true,
                    FechaCreacion         = DateTime.Now,
                    RequiereCambioPassword= true   // fuerza cambio al primer login
                });
                RegistrarAuditoria("SEED_ADMIN", "Usuarios",
                    "Usuario admin creado — requiere cambio de contraseña.");
            }
        }

        // ════════ FACTURA — Métodos auxiliares centralizados ════════════════════
        /// <summary>
        /// Devuelve el rowid interno de SQLite para una factura.
        /// ALTA-3 FIX: Centralizado en DatabaseService — los formularios no abren SqliteConnection.
        /// </summary>
        public static int ObtenerIdFactura(string numeroF)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT rowid FROM Facturas WHERE NumeroF=$n";
            cmd.Parameters.AddWithValue("$n", numeroF);
            var obj = cmd.ExecuteScalar();
            return obj != null ? (int)(long)obj : 0;
        }

        /// <summary>
        /// Devuelve la versión actual del esquema de la BD (PRAGMA user_version).
        /// ALTA-3 FIX: Centralizado para evitar conexiones directas en la UI.
        /// </summary>
        public static int ObtenerVersionEsquema()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA user_version";
            return (int)(long)cmd.ExecuteScalar();
        }

        // ════════ SEGURIDAD — Métodos centralizados (ALTA-3/4 fix) ════════════════
        /// <summary>
        /// Elimina un beneficio anual. ALTA-4 FIX: Solo accesible desde DatabaseService;
        /// el formulario debe verificar Sesion.EsAdmin antes de llamar.
        /// </summary>
        public static void EliminarBeneficio(int id)
        {
            if (!Sesion.EsAdmin)
                throw new UnauthorizedAccessException("Solo el administrador puede eliminar beneficios.");

            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM BeneficiosAnuales WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("ELIMINAR_BENEFICIO", "BeneficiosAnuales", $"Id={id}");
        }

        /// <summary>
        /// Cancela un préstamo. ALTA-3 FIX: Centralizado en DatabaseService con auditoría.
        /// </summary>
        public static void CancelarPrestamo(int id)
        {
            if (!Sesion.EsAdmin)
                throw new UnauthorizedAccessException("Solo el administrador puede cancelar préstamos.");

            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE PrestamosEmpleado SET Estado='Cancelado' WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("CANCELAR_PRESTAMO", "PrestamosEmpleado", $"Id={id}");
        }

        /// <summary>
        /// Actualiza el estado de un usuario en la BD (bloqueo de cuenta).
        /// ALTA-3 FIX: Centralizado para garantizar auditoría en todos los cambios de estado.
        /// </summary>
        public static void DesbloquearUsuario(int id)
        {
            if (!Sesion.EsAdmin)
                throw new UnauthorizedAccessException("Solo el administrador puede desbloquear usuarios.");

            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Usuarios
                SET IntentosHoy=0, BloqueadoHasta=NULL WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("DESBLOQUEAR_USUARIO", "Usuarios", $"Id={id}");
        }

                // ════════ LOG DE AUDITORÍA ════════════════════════════════════════════
        public static void RegistrarAuditoria(string accion, string tabla = "", string detalle = "")
        {
            try
            {
                using var c = Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO LogAuditoria
                    (FechaHora,Usuario,Accion,Tabla,Detalle)
                    VALUES ($fh,$us,$ac,$ta,$de)";
                cmd.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$us", Sesion.NombreUsuario ?? "Sistema");
                cmd.Parameters.AddWithValue("$ac", accion);
                cmd.Parameters.AddWithValue("$ta", tabla);
                cmd.Parameters.AddWithValue("$de", detalle);
                cmd.ExecuteNonQuery();
            }
            catch (Exception exAud)
            {
                // ALTA-6 FIX: Log de auditoría falla silenciosamente para no
                // interrumpir el flujo, pero registramos en Debug para diagnóstico.
                // En producción, un fallo persistente aquí indica problema grave de BD.
                System.Diagnostics.Debug.WriteLine($"[ERROR] RegistrarAuditoria falló: {exAud.Message}");
            }
        }

        public static System.Collections.Generic.List<(DateTime FechaHora, string Usuario,
            string Accion, string Tabla, string Detalle)> ObtenerAuditoria(int ultimas = 500)
        {
            var list = new System.Collections.Generic.List<(DateTime, string, string, string, string)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            // CRÍTICO-5: Limitar con hardcap — nunca interpolar valor de usuario directamente
            int _limiteSeguro1 = Math.Max(1, Math.Min(ultimas, 2000));
            cmd.CommandText = $"SELECT FechaHora,Usuario,Accion,Tabla,Detalle FROM LogAuditoria ORDER BY Id DESC LIMIT {_limiteSeguro1}";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((DateTime.Parse(r.GetString(0)), S(r,1), S(r,2), S(r,3), S(r,4)));
            return list;
        }


        public static List<(int Id, DateTime FechaHora, string Usuario,
            string Accion, string Tabla, string Detalle)> ObtenerAuditoriaFiltrada(
            string usuario = null,
            string accion  = null,
            string tabla   = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            int limite = 1000)
        {
            var list = new List<(int, DateTime, string, string, string, string)>();
            using var c = Conn();
            var cmd = c.CreateCommand();

            var where = new List<string>();
            if (!string.IsNullOrEmpty(usuario)) where.Add("lower(Usuario) LIKE $us");
            if (!string.IsNullOrEmpty(accion))  where.Add("lower(Accion)  LIKE $ac");
            if (!string.IsNullOrEmpty(tabla))    where.Add("lower(Tabla)   LIKE $ta");
            if (desde.HasValue)                  where.Add("FechaHora >= $de");
            if (hasta.HasValue)                  where.Add("FechaHora <= $ha");

            cmd.CommandText = "SELECT Id,FechaHora,Usuario,Accion,Tabla,Detalle FROM LogAuditoria"
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY Id DESC LIMIT " + Math.Max(1, Math.Min(limite, 2000));

            if (!string.IsNullOrEmpty(usuario)) cmd.Parameters.AddWithValue("$us", "%" + usuario.ToLower() + "%");
            if (!string.IsNullOrEmpty(accion))  cmd.Parameters.AddWithValue("$ac", "%" + accion.ToLower()  + "%");
            if (!string.IsNullOrEmpty(tabla))    cmd.Parameters.AddWithValue("$ta", "%" + tabla.ToLower()   + "%");
            if (desde.HasValue) cmd.Parameters.AddWithValue("$de", desde.Value.ToString("yyyy-MM-dd"));
            if (hasta.HasValue) cmd.Parameters.AddWithValue("$ha", hasta.Value.ToString("yyyy-MM-dd") + " 23:59:59");

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), DateTime.Parse(r.GetString(1)),
                    S(r,2), S(r,3), S(r,4), S(r,5)));
            return list;
        }

        public static List<string> ObtenerUsuariosAuditoria()
        {
            var list = new List<string> { "Todos" };
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Usuario FROM LogAuditoria ORDER BY Usuario";
            using var r = cmd.ExecuteReader();
            while (r.Read()) if (!r.IsDBNull(0)) list.Add(r.GetString(0));
            return list;
        }

        public static List<string> ObtenerAccionesAuditoria()
        {
            var list = new List<string> { "Todas" };
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Accion FROM LogAuditoria ORDER BY Accion";
            using var r = cmd.ExecuteReader();
            while (r.Read()) if (!r.IsDBNull(0)) list.Add(r.GetString(0));
            return list;
        }

        /// <summary>
        /// OPTIMIZACIÓN: trae todas las líneas de venta del periodo en UNA
        /// sola query SQL con JOIN, en lugar de N+1 queries (una por factura).
        /// Sustituye al patrón anterior que hacía ~5.000 queries para un mes
        /// de ventas. El filtro por código de producto se aplica en SQL.
        /// </summary>
        public static List<LineaVentaPlana> ObtenerLineasVentaPorPeriodo(
            DateTime desde, DateTime hasta, string codProducto = null)
        {
            var list = new List<LineaVentaPlana>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT f.NumeroF, f.Fecha,
                       df.Codigo,
                       COALESCE(i.Descripcion, df.Codigo) AS Descripcion,
                       df.Cantidad,
                       COALESCE(i.Precio, 0) AS PrecioUnit,
                       (df.Cantidad * COALESCE(i.Precio, 0)) AS Subtotal,
                       COALESCE(t.TasaBCV, 0)      AS TasaBCV,
                       COALESCE(t.TasaParalelo, 0) AS TasaParalelo
                FROM Facturas f
                INNER JOIN DetallesFactura df ON df.IdFactura = f.rowid
                LEFT  JOIN Inventario     i  ON i.Codigo     = df.Codigo
                LEFT  JOIN TasaCambio     t  ON t.Fecha      = f.Fecha
                WHERE f.Fecha >= $desde AND f.Fecha <= $hasta
                  AND ($cod IS NULL OR df.Codigo = $cod)
                ORDER BY f.Fecha DESC, f.NumeroF";
            cmd.Parameters.AddWithValue("$desde", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$hasta", hasta.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cod",   (object)codProducto ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new LineaVentaPlana
                {
                    NumeroF      = S(r, 0),
                    Fecha        = DateTime.TryParse(S(r, 1), out var f) ? f : DateTime.MinValue,
                    Codigo       = S(r, 2),
                    Descripcion  = S(r, 3),
                    Cantidad     = D(r, 4),
                    PrecioUnit   = D(r, 5),
                    Subtotal     = D(r, 6),
                    TasaBCV      = D(r, 7),
                    TasaParalelo = D(r, 8)
                });
            return list;
        }

        // ── REPORTES OPTIMIZADOS ─────────────────────────────────────────
        // Filtran por fecha en SQL (índices ya creados en Inicializar).
        // Reemplazan el patrón "ObtenerXxx() + LINQ.Where(fecha)" que traía
        // todo el histórico para descartar el 99% en memoria.

        public static List<Factura> ObtenerFacturasPorPeriodo(
            DateTime desde, DateTime hasta, string codCliente = null,
            bool incluirAnuladas = false, bool? incluirNotasEntrega = null)
        {
            // Reportes y CxC normalmente NO deben incluir anuladas ni NEs.
            // Para NEs el default es respetar Sesion.MostrarNotasEntrega
            // (que es false a menos que un Admin lo active para esta sesión).
            bool incluirNE = incluirNotasEntrega ?? Sesion.MostrarNotasEntrega;

            var list = new List<Factura>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT NumeroF, Fecha, Codigo, Descuento, Transporte, Observaciones,
                       FormaDePago, Abono, IvaF, DiasCreditoF, TotalF,
                       COALESCE(Anulada,0) AS Anulada,
                       MotivoAnulacion, FechaAnulacion, UsuarioAnulacion,
                       COALESCE(TipoDocumento,'FacturaConIVA') AS TipoDocumento,
                       COALESCE(EsExportacion,0)               AS EsExportacion,
                       DestinoExportacion
                FROM   Facturas
                WHERE  Fecha >= $desde AND Fecha <= $hasta
                  AND  ($cod IS NULL OR Codigo = $cod)
                  " + (incluirAnuladas ? "" : "AND COALESCE(Anulada,0) = 0 ")
                    + (incluirNE       ? "" : "AND COALESCE(TipoDocumento,'FacturaConIVA') != 'NotaEntrega' ") + @"
                ORDER BY Fecha DESC";
            cmd.Parameters.AddWithValue("$desde", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$hasta", hasta.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cod",   (object)codCliente ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(LeerFactura(r));
            return list;
        }

        public static List<Abono> ObtenerAbonosPorPeriodo(
            DateTime desde, DateTime hasta, string codCliente = null)
        {
            var list = new List<Abono>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT NumAbono, NumeroF, Fecha, Monto, Cliente
                FROM Abonos
                WHERE Fecha >= $desde AND Fecha <= $hasta
                  AND ($cod IS NULL OR Cliente = $cod)
                ORDER BY Fecha DESC";
            cmd.Parameters.AddWithValue("$desde", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$hasta", hasta.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cod",   (object)codCliente ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Abono {
                    NumAbono = r.GetInt32(0),
                    NumeroF  = S(r, 1),
                    Fecha    = DateTime.TryParse(S(r, 2), out var f) ? f : DateTime.MinValue,
                    Monto    = D(r, 3),
                    Cliente  = S(r, 4)
                });
            return list;
        }

        public static List<CuentaCobrar> ObtenerCuentasFiltrado(
            string codCliente = null, bool soloPendientes = false)
        {
            var list = new List<CuentaCobrar>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Cliente, Concepto, Monto, Pagado, FechaEmision, FechaVence, Estado
                FROM CuentasCobrar
                WHERE ($cod IS NULL OR Cliente = $cod)
                  AND ($pend = 0 OR (Monto - COALESCE(Pagado,0)) > 0)
                ORDER BY FechaVence";
            cmd.Parameters.AddWithValue("$cod",  (object)codCliente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pend", soloPendientes ? 1 : 0);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CuentaCobrar {
                    Id           = r.GetInt32(0),
                    Cliente      = S(r, 1),
                    Concepto     = S(r, 2),
                    Monto        = D(r, 3),
                    Pagado       = D(r, 4),
                    FechaEmision = DateTime.TryParse(S(r, 5), out var fe) ? fe : DateTime.MinValue,
                    FechaVence   = DateTime.TryParse(S(r, 6), out var fv) ? fv : DateTime.MinValue,
                    Estado       = S(r, 7)
                });
            return list;
        }

        /// <summary>
        /// Para el reporte de patrones de venta: agrega total facturado por
        /// (año, mes) directamente en SQL en lugar de traer todas las
        /// facturas y agrupar en LINQ.
        /// </summary>
        public static List<(int Ano, int Mes, decimal Total)> ObtenerVentasMensualesPorAnos(int[] anos)
        {
            var list = new List<(int, int, decimal)>();
            if (anos == null || anos.Length == 0) return list;
            using var c = Conn();
            var cmd = c.CreateCommand();
            // Construir IN (...) inline (anos es int controlado, no SQLi).
            var inAnos = string.Join(",", anos);
            cmd.CommandText = $@"
                SELECT CAST(strftime('%Y', Fecha) AS INTEGER) AS Ano,
                       CAST(strftime('%m', Fecha) AS INTEGER) AS Mes,
                       SUM(TotalF)
                FROM Facturas
                WHERE CAST(strftime('%Y', Fecha) AS INTEGER) IN ({inAnos})
                GROUP BY Ano, Mes
                ORDER BY Ano, Mes";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetInt32(1), D(r, 2)));
            return list;
        }

        /// <summary>
        /// Resumen ejecutivo en UNA sola query. Reemplaza 6 ObtenerXxx()
        /// fullscan que se ejecutaban al cargar el tab Resumen.
        /// </summary>
        public static (int Facturas, decimal TotalFact, int Compras, decimal TotalComp,
                       int Clientes, int Proveedores, int Productos, int BajoMin,
                       int Moldes, int MantVencido)
            ObtenerResumenGlobal()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    (SELECT COUNT(*) FROM Facturas),
                    (SELECT COALESCE(SUM(TotalF),0) FROM Facturas),
                    (SELECT COUNT(*) FROM Compras),
                    (SELECT COALESCE(SUM(TotalC),0) FROM Compras),
                    (SELECT COUNT(*) FROM Clientes),
                    (SELECT COUNT(*) FROM Proveedores),
                    (SELECT COUNT(*) FROM Inventario),
                    (SELECT COUNT(*) FROM Inventario WHERE Existencia < ExistenMin),
                    (SELECT COUNT(*) FROM TbMoldes),
                    (SELECT COUNT(*) FROM TbMoldes
                       WHERE FechaLastMant IS NOT NULL
                         AND CAST(julianday('now') - julianday(FechaLastMant) AS INTEGER) > 180)";
            using var r = cmd.ExecuteReader();
            r.Read();
            return (
                r.GetInt32(0), D(r,1),
                r.GetInt32(2), D(r,3),
                r.GetInt32(4), r.GetInt32(5),
                r.GetInt32(6), r.GetInt32(7),
                r.GetInt32(8), r.GetInt32(9));
        }

        public static List<LineaFactura> ObtenerDetallesFactura(int idFactura)
        {
            var list = new List<LineaFactura>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT df.IdFactura, df.Codigo, COALESCE(i.Descripcion,''), df.Cantidad,
                       COALESCE(i.Precio,0), 0
                FROM DetallesFactura df
                LEFT JOIN Inventario i ON i.Codigo = df.Codigo
                WHERE df.IdFactura = $id";
            cmd.Parameters.AddWithValue("$id", idFactura);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new LineaFactura
                {
                    IdFactura   = r.GetInt32(0),
                    CodigoItem  = S(r, 1),
                    Descripcion = S(r, 2),
                    Cantidad    = D(r, 3),
                    PrecioUnit  = D(r, 4),
                    Descuento   = D(r, 5)
                });
            return list;
        }

        public static void EliminarDetallesFactura(int idFactura)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM DetallesFactura WHERE IdFactura=$id", ("$id", idFactura));
        }

        /// <summary>
        /// Guarda los detalles de una factura Y registra cada linea como
        /// SALIDA en el Kardex dentro de una unica transaccion.
        /// Si cualquier linea falla (stock insuficiente) hace ROLLBACK completo.
        /// </summary>
        public static void GuardarDetallesYKardex(
            int idFactura,
            string numeroF,
            List<LineaFactura> lineas,
            bool esNueva)
        {
            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            // ALTA-5 FIX: Serializable para proteger stock en emisión de factura
            using var tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                // 1. Borrar detalles anteriores (para edicion)
                var cmdDel = conn.CreateCommand();
                cmdDel.Transaction = tx;
                cmdDel.CommandText = "DELETE FROM DetallesFactura WHERE IdFactura=$id";
                cmdDel.Parameters.AddWithValue("$id", idFactura);
                cmdDel.ExecuteNonQuery();

                foreach (var linea in lineas)
                {
                    if (linea.Cantidad <= 0) continue;

                    // 2. Insertar linea de detalle
                    var cmdIns = conn.CreateCommand();
                    cmdIns.Transaction = tx;
                    cmdIns.CommandText = @"
                        INSERT INTO DetallesFactura (IdFactura, Codigo, Cantidad)
                        VALUES ($id, $cod, $qty)";
                    cmdIns.Parameters.AddWithValue("$id",  idFactura);
                    cmdIns.Parameters.AddWithValue("$cod", linea.CodigoItem);
                    cmdIns.Parameters.AddWithValue("$qty", (double)linea.Cantidad);
                    cmdIns.ExecuteNonQuery();

                    // 3. Leer stock actual del producto
                    var cmdStock = conn.CreateCommand();
                    cmdStock.Transaction = tx;
                    cmdStock.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo=$cod";
                    cmdStock.Parameters.AddWithValue("$cod", linea.CodigoItem);
                    var stockObj = cmdStock.ExecuteScalar();
                    if (stockObj == null) continue;   // producto no en inventario: omitir

                    decimal saldoAnt  = (decimal)(double)stockObj;
                    decimal nuevoSaldo = saldoAnt - linea.Cantidad;

                    if (nuevoSaldo < 0)
                        throw new Exception(
                            "Stock insuficiente para " + linea.CodigoItem +
                            ". Existencia: " + saldoAnt +
                            ", solicitado: " + linea.Cantidad);

                    // 4. Insertar movimiento en Kardex
                    var cmdKardex = conn.CreateCommand();
                    cmdKardex.Transaction = tx;
                    cmdKardex.CommandText = @"
                        INSERT INTO MovimientosInventario
                            (FechaHora, CodigoProducto, TipoMovimiento, Concepto,
                             Cantidad, SaldoAnterior, NuevoSaldo, Referencia, Usuario)
                        VALUES ($fh,$cp,'SALIDA',$co,$ca,$sa,$ns,$ref,$us)";
                    cmdKardex.Parameters.AddWithValue("$fh",  DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmdKardex.Parameters.AddWithValue("$cp",  linea.CodigoItem);
                    cmdKardex.Parameters.AddWithValue("$co",  "Venta Factura " + numeroF);
                    cmdKardex.Parameters.AddWithValue("$ca",  (double)linea.Cantidad);
                    cmdKardex.Parameters.AddWithValue("$sa",  (double)saldoAnt);
                    cmdKardex.Parameters.AddWithValue("$ns",  (double)nuevoSaldo);
                    cmdKardex.Parameters.AddWithValue("$ref", numeroF);
                    cmdKardex.Parameters.AddWithValue("$us",  Sesion.NombreUsuario ?? "Sistema");
                    cmdKardex.ExecuteNonQuery();

                    // 5. Actualizar stock en tabla maestra
                    var cmdUpd = conn.CreateCommand();
                    cmdUpd.Transaction = tx;
                    cmdUpd.CommandText = @"
                        UPDATE Inventario
                        SET Existencia=$ns, UltimaActualz=$fecha
                        WHERE Codigo=$cod";
                    cmdUpd.Parameters.AddWithValue("$ns",    (double)nuevoSaldo);
                    cmdUpd.Parameters.AddWithValue("$fecha", DateTime.Today.ToString("yyyy-MM-dd"));
                    cmdUpd.Parameters.AddWithValue("$cod",   linea.CodigoItem);
                    cmdUpd.ExecuteNonQuery();
                }

                tx.Commit();

                RegistrarAuditoria(
                    esNueva ? "FACTURA_NUEVA" : "FACTURA_EDIT",
                    "Facturas",
                    "Factura " + numeroF + " con " + lineas.Count + " lineas");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ════════ RENTABILIDAD POR PRODUCTO ══════════════════════════════════
        /// <summary>
        /// Calcula la rentabilidad de cada producto cruzando:
        ///   - Inventario (costo, precio, stock)
        ///   - MovimientosInventario (unidades vendidas y compradas por periodo)
        /// </summary>
        // ════════════════════════════════════════════════════════════════════════
        // REEMPLAZAR el metodo ObtenerRentabilidad en DB.cs con este
        // Calcula IngresoTotal usando el precio REAL de cada factura (PrecioF)
        // en lugar del precio actual del inventario
        // ════════════════════════════════════════════════════════════════════════

        public static List<RentabilidadProducto> ObtenerRentabilidad(
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var inventario = ObtenerInventario();

            // ── Ingresos reales desde DetallesFactura JOIN Facturas ──────────────
            // Usa PrecioF (precio al momento de la venta) * Cantidad
            var ingresosPorProducto = new Dictionary<string, decimal>();
            var unidadesPorProducto = new Dictionary<string, decimal>();

            using (var c = Conn())
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
            SELECT df.Codigo,
                   SUM(df.Cantidad)           AS Unidades,
                   SUM(df.PrecioF * df.Cantidad) AS Ingreso
            FROM DetallesFactura df
            JOIN Facturas f ON f.rowid = df.IdFactura
            WHERE 1=1 " +
                    (desde.HasValue ? " AND f.Fecha >= $desde" : "") +
                    (hasta.HasValue ? " AND f.Fecha <= $hasta" : "") +
                    @" GROUP BY df.Codigo";

                if (desde.HasValue)
                    cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd"));
                if (hasta.HasValue)
                    cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd"));

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string cod = S(r, 0);
                    ingresosPorProducto[cod] = D(r, 2);
                    unidadesPorProducto[cod] = D(r, 1);
                }
            }

            // ── Entradas desde Kardex (compras/produccion) ───────────────────────
            var kardex = ObtenerKardex(null, desde, hasta);
            var entradas = kardex.Where(k => k.TipoMovimiento == "ENTRADA")
                                 .GroupBy(k => k.CodigoProducto)
                                 .ToDictionary(g => g.Key, g => g.Sum(k => k.Cantidad));

            var resultado = new List<RentabilidadProducto>();

            foreach (var inv in inventario)
            {
                decimal vendidas = unidadesPorProducto.ContainsKey(inv.Codigo)
                                    ? unidadesPorProducto[inv.Codigo] : 0;
                decimal compradas = entradas.ContainsKey(inv.Codigo)
                                    ? entradas[inv.Codigo] : 0;
                decimal ingreso = ingresosPorProducto.ContainsKey(inv.Codigo)
                                    ? ingresosPorProducto[inv.Codigo] : 0;
                decimal costo = vendidas * inv.CostoActual;

                // Precio promedio ponderado de venta en el periodo
                decimal precioPromedio = vendidas > 0
                    ? Math.Round(ingreso / vendidas, 2)
                    : inv.Precio;

                decimal rotacion = 0;
                if (vendidas > 0 && inv.Existencia > 0)
                {
                    int mesesPeriodo = desde.HasValue && hasta.HasValue
                        ? Math.Max(1, (int)Math.Ceiling(
                            (hasta.Value - desde.Value).TotalDays / 30.0))
                        : 1;
                    decimal ventasPorMes = vendidas / mesesPeriodo;
                    rotacion = ventasPorMes > 0
                        ? Math.Round(inv.Existencia / ventasPorMes, 1)
                        : 0;
                }

                resultado.Add(new RentabilidadProducto
                {
                    Codigo = inv.Codigo,
                    Descripcion = inv.Descripcion ?? "",
                    Departamento = inv.Departamento ?? "",
                    Unidad = inv.Unidad ?? "",
                    CostoUnitario = inv.CostoActual,
                    PrecioVenta = precioPromedio,   // precio promedio real del periodo
                    UnidadesVendidas = vendidas,
                    UnidadesCompradas = compradas,
                    IngresoTotal = ingreso,           // ingreso real de facturas
                    CostoTotal = costo,
                    StockActual = inv.Existencia,
                    RotacionMeses = rotacion
                });
            }

            resultado.Sort((a, b) => b.UtilidadBruta.CompareTo(a.UtilidadBruta));
            return resultado;
        }


        // ════════ MANTENIMIENTO DE MOLDES ════════════════════════════════════
        public static List<RegistroMantenimiento> ObtenerRegistrosMant(string codigoMolde = null)
        {
            var list = new List<RegistroMantenimiento>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM RegistrosMantenimiento"
                + (codigoMolde != null ? " WHERE CodigoMolde=$cod" : "")
                + " ORDER BY FechaRealizada DESC";
            if (codigoMolde != null) cmd.Parameters.AddWithValue("$cod", codigoMolde);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new RegistroMantenimiento
                {
                    Id             = r.GetInt32(0),  CodigoMolde = S(r,1),
                    FechaRealizada = DateTime.Parse(r.GetString(2)),
                    TipoMant       = S(r,3),  Tecnico     = S(r,4),
                    Descripcion    = S(r,5),  CostoMant   = D(r,6),
                    DuracionHoras  = r.IsDBNull(7) ? 0 : r.GetInt32(7),
                    Resultado      = S(r,8),  Observaciones = S(r,9),
                    ProximoMant    = r.IsDBNull(10) ? DateTime.Today.AddDays(90)
                                    : DateTime.Parse(r.GetString(10))
                });
            return list;
        }

        public static void InsertarRegistroMant(RegistroMantenimiento rm)
        {
            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // 1. Insertar registro
                var cmd = conn.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO RegistrosMantenimiento
                    (CodigoMolde,FechaRealizada,TipoMant,Tecnico,Descripcion,
                     CostoMant,DuracionHoras,Resultado,Observaciones,ProximoMant)
                    VALUES($cm,$fr,$tm,$te,$de,$co,$dh,$re,$ob,$pm)";
                BindRM(cmd, rm); cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT last_insert_rowid()";
                rm.Id = (int)(long)cmd.ExecuteScalar();

                // 2. Actualizar FechaLastMant en TbMoldes
                var cmdM = conn.CreateCommand(); cmdM.Transaction = tx;
                cmdM.CommandText = @"UPDATE TbMoldes
                    SET FechaLastMant=$fl, DescMant=$dm WHERE CodigoMold=$cod";
                cmdM.Parameters.AddWithValue("$fl",  rm.FechaRealizada.ToString("yyyy-MM-dd"));
                cmdM.Parameters.AddWithValue("$dm",  rm.Descripcion ?? "");
                cmdM.Parameters.AddWithValue("$cod", rm.CodigoMolde);
                cmdM.ExecuteNonQuery();

                // 3. Actualizar programa si existe
                var cmdP = conn.CreateCommand(); cmdP.Transaction = tx;
                cmdP.CommandText = @"UPDATE ProgramasMantenimiento
                    SET UltimoMant=$ul WHERE CodigoMolde=$cod";
                cmdP.Parameters.AddWithValue("$ul",  rm.FechaRealizada.ToString("yyyy-MM-dd"));
                cmdP.Parameters.AddWithValue("$cod", rm.CodigoMolde);
                cmdP.ExecuteNonQuery();

                tx.Commit();
                RegistrarAuditoria("MANT_REGISTRADO", "TbMoldes",
                    rm.CodigoMolde + " — " + rm.TipoMant);
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ActualizarRegistroMant(RegistroMantenimiento rm)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE RegistrosMantenimiento SET
                FechaRealizada=$fr,TipoMant=$tm,Tecnico=$te,Descripcion=$de,
                CostoMant=$co,DuracionHoras=$dh,Resultado=$re,
                Observaciones=$ob,ProximoMant=$pm WHERE Id=$id";
            BindRM(cmd, rm);
            cmd.Parameters.AddWithValue("$id", rm.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarRegistroMant(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM RegistrosMantenimiento WHERE Id=$id", ("$id", id));
        }

        private static void BindRM(SqliteCommand cmd, RegistroMantenimiento rm)
        {
            cmd.Parameters.AddWithValue("$cm", rm.CodigoMolde);
            cmd.Parameters.AddWithValue("$fr", rm.FechaRealizada.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$tm", rm.TipoMant ?? "Preventivo");
            cmd.Parameters.AddWithValue("$te", rm.Tecnico ?? "");
            cmd.Parameters.AddWithValue("$de", rm.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$co", (double)rm.CostoMant);
            cmd.Parameters.AddWithValue("$dh", rm.DuracionHoras);
            cmd.Parameters.AddWithValue("$re", rm.Resultado ?? "Completado");
            cmd.Parameters.AddWithValue("$ob", rm.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$pm", rm.ProximoMant.ToString("yyyy-MM-dd"));
        }

        // ── Programas de mantenimiento ────────────────────────────────────
        public static List<ProgramaMant> ObtenerProgramas(bool soloActivos = false)
        {
            var list = new List<ProgramaMant>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT p.*, m.Descripcion FROM ProgramasMantenimiento p"
                + " LEFT JOIN TbMoldes m ON m.CodigoMold = p.CodigoMolde"
                + (soloActivos ? " WHERE p.Activo=1" : "")
                + " ORDER BY p.UltimoMant ASC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ProgramaMant
                {
                    Id             = r.GetInt32(0),  CodigoMolde    = S(r,1),
                    DescMolde      = S(r,7),         TipoMant       = S(r,3),
                    FrecuenciaDias = r.IsDBNull(4) ? 90 : r.GetInt32(4),
                    UltimoMant     = DateTime.Parse(r.GetString(5)),
                    Activo         = r.GetInt32(6) == 1
                });
            return list;
        }

        public static void GuardarPrograma(ProgramaMant p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO ProgramasMantenimiento
                (CodigoMolde,DescMolde,TipoMant,FrecuenciaDias,UltimoMant,Activo)
                VALUES($cm,$dm,$tm,$fd,$ul,$ac)";
            cmd.Parameters.AddWithValue("$cm", p.CodigoMolde);
            cmd.Parameters.AddWithValue("$dm", p.DescMolde ?? "");
            cmd.Parameters.AddWithValue("$tm", p.TipoMant ?? "Preventivo");
            cmd.Parameters.AddWithValue("$fd", p.FrecuenciaDias);
            cmd.Parameters.AddWithValue("$ul", p.UltimoMant.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$ac", p.Activo ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarPrograma(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM ProgramasMantenimiento WHERE Id=$id", ("$id", id));
        }

                // ════════ CONTROL DE CALIDAD ═════════════════════════════════════════
        public static List<InspeccionCalidad> ObtenerInspecciones(int? idIngreso = null)
        {
            var list = new List<InspeccionCalidad>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM InspeccionesCalidad"
                + (idIngreso.HasValue ? " WHERE IdIngreso=$id" : "")
                + " ORDER BY Id DESC";
            if (idIngreso.HasValue) cmd.Parameters.AddWithValue("$id", idIngreso.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InspeccionCalidad
                {
                    Id = r.GetInt32(0), IdIngreso = r.GetInt32(1),
                    Lote = S(r,2), ProductoCod = S(r,3),
                    FechaInspeccion = DateTime.Parse(r.GetString(4)),
                    Inspector = S(r,5),
                    CantidadTotal    = D(r,6),  CantidadAprobada  = D(r,7),
                    CantidadRechazada= D(r,8),  CantidadRetrabajo = D(r,9),
                    PuntajeDimensiones = r.IsDBNull(10) ? 3 : r.GetInt32(10),
                    PuntajeApariencia  = r.IsDBNull(11) ? 3 : r.GetInt32(11),
                    PuntajeFuncional   = r.IsDBNull(12) ? 3 : r.GetInt32(12),
                    PuntajeEtiquetado  = r.IsDBNull(13) ? 3 : r.GetInt32(13),
                    Resultado = S(r,14), Observaciones = S(r,15),
                    AccionCorrectiva = S(r,16)
                });
            return list;
        }

        public static void InsertarInspeccion(InspeccionCalidad ins)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO InspeccionesCalidad
                (IdIngreso,Lote,ProductoCod,FechaInspeccion,Inspector,
                 CantidadTotal,CantidadAprobada,CantidadRechazada,CantidadRetrabajo,
                 PuntajeDimensiones,PuntajeApariencia,PuntajeFuncional,PuntajeEtiquetado,
                 Resultado,Observaciones,AccionCorrectiva)
                VALUES($ii,$lo,$pc,$fi,$in,$ct,$ca,$cr,$cre,$pd,$pa,$pf,$pe,$re,$ob,$ac)";
            BindIns(cmd, ins);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            ins.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarInspeccion(InspeccionCalidad ins)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE InspeccionesCalidad SET
                Lote=$lo,ProductoCod=$pc,FechaInspeccion=$fi,Inspector=$in,
                CantidadTotal=$ct,CantidadAprobada=$ca,CantidadRechazada=$cr,
                CantidadRetrabajo=$cre,PuntajeDimensiones=$pd,PuntajeApariencia=$pa,
                PuntajeFuncional=$pf,PuntajeEtiquetado=$pe,Resultado=$re,
                Observaciones=$ob,AccionCorrectiva=$ac WHERE Id=$id";
            BindIns(cmd, ins);
            cmd.Parameters.AddWithValue("$id", ins.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarInspeccion(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM DefectosCalidad WHERE IdInspeccion=$id", ("$id", id));
            Exec(c, "DELETE FROM InspeccionesCalidad WHERE Id=$id", ("$id", id));
        }

        private static void BindIns(SqliteCommand cmd, InspeccionCalidad i)
        {
            cmd.Parameters.AddWithValue("$ii",  i.IdIngreso);
            cmd.Parameters.AddWithValue("$lo",  i.Lote ?? "");
            cmd.Parameters.AddWithValue("$pc",  i.ProductoCod ?? "");
            cmd.Parameters.AddWithValue("$fi",  i.FechaInspeccion.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$in",  i.Inspector ?? "");
            cmd.Parameters.AddWithValue("$ct",  (double)i.CantidadTotal);
            cmd.Parameters.AddWithValue("$ca",  (double)i.CantidadAprobada);
            cmd.Parameters.AddWithValue("$cr",  (double)i.CantidadRechazada);
            cmd.Parameters.AddWithValue("$cre", (double)i.CantidadRetrabajo);
            cmd.Parameters.AddWithValue("$pd",  i.PuntajeDimensiones);
            cmd.Parameters.AddWithValue("$pa",  i.PuntajeApariencia);
            cmd.Parameters.AddWithValue("$pf",  i.PuntajeFuncional);
            cmd.Parameters.AddWithValue("$pe",  i.PuntajeEtiquetado);
            cmd.Parameters.AddWithValue("$re",  i.Resultado ?? "Aprobado");
            cmd.Parameters.AddWithValue("$ob",  i.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$ac",  i.AccionCorrectiva ?? "");
        }

        // ── Defectos ──────────────────────────────────────────────────────
        public static List<DefectoCalidad> ObtenerDefectos(int idInspeccion)
        {
            var list = new List<DefectoCalidad>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM DefectosCalidad WHERE IdInspeccion=$id";
            cmd.Parameters.AddWithValue("$id", idInspeccion);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new DefectoCalidad
                {
                    IdInspeccion = r.GetInt32(0), TipoDefecto = S(r,1),
                    Cantidad = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                    Descripcion = S(r,3), Severidad = S(r,4)
                });
            return list;
        }

        public static void GuardarDefectos(int idInspeccion, List<DefectoCalidad> defectos)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM DefectosCalidad WHERE IdInspeccion=$id", ("$id", idInspeccion));
            foreach (var d in defectos)
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO DefectosCalidad
                    (IdInspeccion,TipoDefecto,Cantidad,Descripcion,Severidad)
                    VALUES($id,$td,$ca,$de,$se)";
                cmd.Parameters.AddWithValue("$id", idInspeccion);
                cmd.Parameters.AddWithValue("$td", d.TipoDefecto ?? "");
                cmd.Parameters.AddWithValue("$ca", d.Cantidad);
                cmd.Parameters.AddWithValue("$de", d.Descripcion ?? "");
                cmd.Parameters.AddWithValue("$se", d.Severidad ?? "Menor");
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Estadisticas de calidad por producto en un periodo.</summary>
        public static (int TotalInsp, decimal PctAprobado, decimal PctRechazado,
                       int DefectosCriticos, string DefectoMasFrecuente)
            EstadisticasCalidad(string productoCod = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var inspecciones = ObtenerInspecciones()
                .Where(i => (productoCod == null || i.ProductoCod == productoCod)
                         && (desde == null || i.FechaInspeccion >= desde)
                         && (hasta == null || i.FechaInspeccion <= hasta))
                .ToList();

            if (inspecciones.Count == 0)
                return (0, 0, 0, 0, "—");

            decimal totalU    = inspecciones.Sum(i => i.CantidadTotal);
            decimal aprobadas = inspecciones.Sum(i => i.CantidadAprobada);
            decimal rechazadas= inspecciones.Sum(i => i.CantidadRechazada);
            decimal pctAp     = totalU > 0 ? Math.Round(aprobadas  / totalU * 100, 1) : 0;
            decimal pctRe     = totalU > 0 ? Math.Round(rechazadas / totalU * 100, 1) : 0;

            var todosDefectos = new List<DefectoCalidad>();
            foreach (var ins in inspecciones)
                todosDefectos.AddRange(ObtenerDefectos(ins.Id));

            int criticos = todosDefectos.Where(d => d.Severidad == "Critico").Sum(d => d.Cantidad);
            string topDefecto = todosDefectos.Count > 0
                ? todosDefectos.GroupBy(d => d.TipoDefecto)
                               .OrderByDescending(g => g.Sum(d => d.Cantidad))
                               .First().Key
                : "—";

            return (inspecciones.Count, pctAp, pctRe, criticos, topDefecto);
        }

                // ════════ ORDENES DE PRODUCCION ════════════════════════════════════════
        public static List<OrdenProduccion> ObtenerOrdenes(string estado = null)
        {
            var list = new List<OrdenProduccion>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM OrdenesProduccion"
                + (estado != null ? " WHERE Estado=$e" : "")
                + " ORDER BY Id DESC";
            if (estado != null) cmd.Parameters.AddWithValue("$e", estado);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new OrdenProduccion
                {
                    Id            = r.GetInt32(0),
                    NumOrden      = S(r,1), ProductoCod  = S(r,2),
                    ProductoDesc  = S(r,3), CantidadPlan = D(r,4),
                    CantidadReal  = D(r,5),
                    FechaInicio   = DateTime.Parse(r.GetString(6)),
                    FechaFin      = DateTime.Parse(r.GetString(7)),
                    Estado        = S(r,8), Responsable   = S(r,9),
                    Observaciones = S(r,10),
                    FechaCreacion = DateTime.Parse(r.GetString(11))
                });
            return list;
        }

        public static void InsertarOrden(OrdenProduccion op)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO OrdenesProduccion
                (NumOrden,ProductoCod,ProductoDesc,CantidadPlan,CantidadReal,
                 FechaInicio,FechaFin,Estado,Responsable,Observaciones,FechaCreacion)
                VALUES($no,$pc,$pd,$cp,$cr,$fi,$ff,$es,$re,$ob,$fc)";
            BindOrden(cmd, op);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            op.Id = (int)(long)cmd.ExecuteScalar();
        }

        public static void ActualizarOrden(OrdenProduccion op)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE OrdenesProduccion SET
                NumOrden=$no,ProductoCod=$pc,ProductoDesc=$pd,CantidadPlan=$cp,
                CantidadReal=$cr,FechaInicio=$fi,FechaFin=$ff,Estado=$es,
                Responsable=$re,Observaciones=$ob WHERE Id=$id";
            BindOrden(cmd, op);
            cmd.Parameters.AddWithValue("$id", op.Id);
            cmd.ExecuteNonQuery();
        }

        public static void EliminarOrden(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM LineasOrden WHERE IdOrden=$id", ("$id", id));
            Exec(c, "DELETE FROM OrdenesProduccion WHERE Id=$id", ("$id", id));
        }

        private static void BindOrden(SqliteCommand cmd, OrdenProduccion op)
        {
            cmd.Parameters.AddWithValue("$no", op.NumOrden);
            cmd.Parameters.AddWithValue("$pc", op.ProductoCod);
            cmd.Parameters.AddWithValue("$pd", op.ProductoDesc ?? "");
            cmd.Parameters.AddWithValue("$cp", (double)op.CantidadPlan);
            cmd.Parameters.AddWithValue("$cr", (double)op.CantidadReal);
            cmd.Parameters.AddWithValue("$fi", op.FechaInicio.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$ff", op.FechaFin.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$es", op.Estado);
            cmd.Parameters.AddWithValue("$re", op.Responsable ?? "");
            cmd.Parameters.AddWithValue("$ob", op.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$fc", op.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // ── Lineas (materias primas) ──────────────────────────────────────
        public static List<LineaOrden> ObtenerLineasOrden(int idOrden)
        {
            var list = new List<LineaOrden>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT lo.IdOrden, lo.MateriaCod,
                       COALESCE(i.Descripcion, lo.MateriaDesc,''),
                       lo.CantidadReq, lo.CantidadTot,
                       COALESCE(i.Existencia, 0)
                FROM LineasOrden lo
                LEFT JOIN Inventario i ON i.Codigo = lo.MateriaCod
                WHERE lo.IdOrden = $id";
            cmd.Parameters.AddWithValue("$id", idOrden);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new LineaOrden
                {
                    IdOrden     = r.GetInt32(0), MateriaCod  = S(r,1),
                    MateriaDesc = S(r,2),        CantidadReq = D(r,3),
                    CantidadTot = D(r,4),        StockActual = D(r,5)
                });
            return list;
        }

        public static void GuardarLineasOrden(int idOrden, List<LineaOrden> lineas)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM LineasOrden WHERE IdOrden=$id", ("$id", idOrden));
            foreach (var l in lineas)
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO LineasOrden
                    (IdOrden,MateriaCod,MateriaDesc,CantidadReq,CantidadTot)
                    VALUES($id,$mc,$md,$cr,$ct)";
                cmd.Parameters.AddWithValue("$id", idOrden);
                cmd.Parameters.AddWithValue("$mc", l.MateriaCod);
                cmd.Parameters.AddWithValue("$md", l.MateriaDesc ?? "");
                cmd.Parameters.AddWithValue("$cr", (double)l.CantidadReq);
                cmd.Parameters.AddWithValue("$ct", (double)l.CantidadTot);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Cierra una orden: descuenta materias primas del inventario via Kardex
        /// y agrega el producto terminado como ENTRADA. Todo en una transaccion.
        /// </summary>
        public static void CerrarOrden(OrdenProduccion op, decimal cantidadReal)
        {
            var lineas = ObtenerLineasOrden(op.Id);

            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // 1. Descontar cada materia prima (proporcional a cantidad real)
                decimal factor = op.CantidadPlan > 0 ? cantidadReal / op.CantidadPlan : 1;

                foreach (var l in lineas)
                {
                    decimal consumo = Math.Round(l.CantidadTot * factor, 4);
                    if (consumo <= 0) continue;

                    // Leer stock actual
                    var cmdStock = conn.CreateCommand();
                    cmdStock.Transaction = tx;
                    cmdStock.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo=$cod";
                    cmdStock.Parameters.AddWithValue("$cod", l.MateriaCod);
                    var obj = cmdStock.ExecuteScalar();
                    if (obj == null) continue;

                    decimal saldoAnt  = (decimal)(double)obj;
                    decimal nuevoSaldo = saldoAnt - consumo;
                    if (nuevoSaldo < 0)
                        throw new Exception(
                            "Stock insuficiente: " + l.MateriaCod +
                            " — Existencia: " + saldoAnt +
                            ", requerido: " + consumo);

                    // Kardex SALIDA materia prima
                    InsBitacoraKardex(conn, tx, l.MateriaCod, "SALIDA",
                        "Consumo OP " + op.NumOrden, consumo, saldoAnt, nuevoSaldo, op.NumOrden);

                    // Actualizar stock
                    UpdStock(conn, tx, l.MateriaCod, nuevoSaldo);
                }

                // 2. Agregar producto terminado (ENTRADA)
                var cmdProd = conn.CreateCommand();
                cmdProd.Transaction = tx;
                cmdProd.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo=$cod";
                cmdProd.Parameters.AddWithValue("$cod", op.ProductoCod);
                var objProd = cmdProd.ExecuteScalar();

                if (objProd != null)
                {
                    decimal stockProd  = (decimal)(double)objProd;
                    decimal nuevoStock = stockProd + cantidadReal;
                    InsBitacoraKardex(conn, tx, op.ProductoCod, "ENTRADA",
                        "Produccion OP " + op.NumOrden, cantidadReal, stockProd, nuevoStock, op.NumOrden);
                    UpdStock(conn, tx, op.ProductoCod, nuevoStock);
                }

                // 3. Actualizar estado de la orden
                var cmdOp = conn.CreateCommand();
                cmdOp.Transaction = tx;
                cmdOp.CommandText = @"UPDATE OrdenesProduccion
                    SET Estado='Completada', CantidadReal=$cr WHERE Id=$id";
                cmdOp.Parameters.AddWithValue("$cr", (double)cantidadReal);
                cmdOp.Parameters.AddWithValue("$id", op.Id);
                cmdOp.ExecuteNonQuery();

                tx.Commit();

                RegistrarAuditoria("ORDEN_CERRADA", "OrdenesProduccion",
                    op.NumOrden + " — " + cantidadReal + " unidades producidas");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void InsBitacoraKardex(
            SqliteConnection conn, SqliteTransaction tx,
            string codigo, string tipo, string concepto,
            decimal cantidad, decimal saldoAnt, decimal nuevoSaldo, string ref_)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO MovimientosInventario
                (FechaHora,CodigoProducto,TipoMovimiento,Concepto,
                 Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario)
                VALUES($fh,$cp,$tm,$co,$ca,$sa,$ns,$re,$us)";
            cmd.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$cp", codigo);
            cmd.Parameters.AddWithValue("$tm", tipo);
            cmd.Parameters.AddWithValue("$co", concepto);
            cmd.Parameters.AddWithValue("$ca", (double)cantidad);
            cmd.Parameters.AddWithValue("$sa", (double)saldoAnt);
            cmd.Parameters.AddWithValue("$ns", (double)nuevoSaldo);
            cmd.Parameters.AddWithValue("$re", ref_);
            cmd.Parameters.AddWithValue("$us", Sesion.NombreUsuario ?? "Sistema");
            cmd.ExecuteNonQuery();
        }

        private static void UpdStock(
            SqliteConnection conn, SqliteTransaction tx, string codigo, decimal nuevoSaldo)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE Inventario
                SET Existencia=$ns, UltimaActualz=$f WHERE Codigo=$cod";
            cmd.Parameters.AddWithValue("$ns",  (double)nuevoSaldo);
            cmd.Parameters.AddWithValue("$f",   DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cod", codigo);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Genera el siguiente numero de orden: OP-0001, OP-0002...</summary>
        public static string GenerarNumOrden()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM OrdenesProduccion";
            int n = (int)(long)cmd.ExecuteScalar();
            return "OP-" + (n + 1).ToString("D4");
        }

                // ════════ NOMINA ═════════════════════════════════════════════════════
        // ── Empleados ─────────────────────────────────────────────────────
        public static List<Empleado> ObtenerEmpleados(bool soloActivos = false)
        {
            var list = new List<Empleado>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empleados"
                + (soloActivos ? " WHERE Activo=1" : "")
                + " ORDER BY Apellido,Nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(BindEmp(r));
            return list;
        }

        public static Empleado ObtenerEmpleado(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM Empleados WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? BindEmp(r) : null;
        }

        public static void InsertarEmpleado(Empleado e)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO Empleados
                (Cedula,Nombre,Apellido,Cargo,TipoEmpleado,FechaIngreso,Activo,
                 Telefono,Email,SueldoDiarioUSD,TarifaPiezaUSD,TarifaVentaUSD,SalarioNormalUSD)
                VALUES($ci,$no,$ap,$ca,$te,$fi,$ac,$tf,$em,$sd,$tp,$tv,$sn)";
            BindEmpParams(cmd, e);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            e.Id = (int)(long)cmd.ExecuteScalar();
            RegistrarAuditoria("EMPLEADO_NUEVO", "Empleados", e.NombreCompleto);
        }

        public static void ActualizarEmpleado(Empleado e)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Empleados SET
                Cedula=$ci,Nombre=$no,Apellido=$ap,Cargo=$ca,TipoEmpleado=$te,
                FechaIngreso=$fi,Activo=$ac,Telefono=$tf,Email=$em,
                SueldoDiarioUSD=$sd,TarifaPiezaUSD=$tp,TarifaVentaUSD=$tv,
                SalarioNormalUSD=$sn WHERE Id=$id";
            BindEmpParams(cmd, e);
            cmd.Parameters.AddWithValue("$id", e.Id);
            cmd.ExecuteNonQuery();
        }

        private static Empleado BindEmp(SqliteDataReader r) => new Empleado
        {
            Id = r.GetInt32(0), Cedula = S(r,1), Nombre = S(r,2), Apellido = S(r,3),
            Cargo = S(r,4), TipoEmpleado = S(r,5),
            FechaIngreso = DateTime.Parse(r.GetString(6)),
            Activo = r.GetInt32(7) == 1, Telefono = S(r,8), Email = S(r,9),
            SueldoDiarioUSD  = D(r,10), TarifaPiezaUSD = D(r,11),
            TarifaVentaUSD   = D(r,12), SalarioNormalUSD = D(r,13)
        };

        private static void BindEmpParams(SqliteCommand cmd, Empleado e)
        {
            cmd.Parameters.AddWithValue("$ci", e.Cedula ?? "");
            cmd.Parameters.AddWithValue("$no", e.Nombre ?? "");
            cmd.Parameters.AddWithValue("$ap", e.Apellido ?? "");
            cmd.Parameters.AddWithValue("$ca", e.Cargo ?? "");
            cmd.Parameters.AddWithValue("$te", e.TipoEmpleado ?? "Fijo");
            cmd.Parameters.AddWithValue("$fi", e.FechaIngreso.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$ac", e.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$tf", e.Telefono ?? "");
            cmd.Parameters.AddWithValue("$em", e.Email ?? "");
            cmd.Parameters.AddWithValue("$sd", (double)e.SueldoDiarioUSD);
            cmd.Parameters.AddWithValue("$tp", (double)e.TarifaPiezaUSD);
            cmd.Parameters.AddWithValue("$tv", (double)e.TarifaVentaUSD);
            cmd.Parameters.AddWithValue("$sn", (double)e.SalarioNormalUSD);
        }

        // ── Periodos de nomina ─────────────────────────────────────────────
        public static List<PeriodoNomina> ObtenerPeriodos()
        {
            var list = new List<PeriodoNomina>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM PeriodosNomina ORDER BY Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PeriodoNomina
                {
                    Id = r.GetInt32(0),
                    FechaDesde = DateTime.Parse(r.GetString(1)),
                    FechaHasta = DateTime.Parse(r.GetString(2)),
                    TasaBCV    = D(r,3), Estado = S(r,4), Observaciones = S(r,5)
                });
            return list;
        }

        public static int InsertarPeriodo(PeriodoNomina p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO PeriodosNomina
                (FechaDesde,FechaHasta,TasaBCV,Estado,Observaciones)
                VALUES($fd,$fh,$tb,$es,$ob)";
            cmd.Parameters.AddWithValue("$fd", p.FechaDesde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fh", p.FechaHasta.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$tb", (double)p.TasaBCV);
            cmd.Parameters.AddWithValue("$es", p.Estado ?? "Abierto");
            cmd.Parameters.AddWithValue("$ob", p.Observaciones ?? "");
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            int id = (int)(long)cmd.ExecuteScalar();
            RegistrarAuditoria("PERIODO_NOMINA", "PeriodosNomina",
                p.FechaDesde.ToString("dd/MM") + " al " + p.FechaHasta.ToString("dd/MM/yyyy"));
            return id;
        }

        public static void CerrarPeriodo(int idPeriodo)
        {
            using var c = Conn();
            Exec(c, "UPDATE PeriodosNomina SET Estado='Cerrado' WHERE Id=$id", ("$id", idPeriodo));
            RegistrarAuditoria("PERIODO_CERRADO", "PeriodosNomina", "Id: " + idPeriodo);
        }

        // ── Detalles de nomina ─────────────────────────────────────────────
        public static List<DetalleNomina> ObtenerDetallesNomina(int idPeriodo)
        {
            var list = new List<DetalleNomina>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM DetallesNomina WHERE IdPeriodo=$id ORDER BY NombreEmpleado";
            cmd.Parameters.AddWithValue("$id", idPeriodo);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new DetalleNomina
                {
                    Id = r.GetInt32(0), IdPeriodo = r.GetInt32(1), IdEmpleado = r.GetInt32(2),
                    NombreEmpleado = S(r,3), TipoEmpleado = S(r,4),
                    DiasTrabajados   = D(r,5),  DiasInasistencia = D(r,6),
                    PiezasProducidas = D(r,7),  VentasCerradas   = D(r,8),
                    SueldoDiarioUSD  = D(r,9),  DevengadoSueldo  = D(r,10),
                    DevengadoPiezas  = D(r,11), DevengadoVentas  = D(r,12),
                    OtrosIngresos    = D(r,13), DeduccionIVSS    = D(r,14),
                    DeduccionRPVH    = D(r,15), DescuentoFaltas  = D(r,16),
                    CuotaPrestamo    = D(r,17), OtrasDeduccion   = D(r,18),
                    TasaBCV          = D(r,19), Observaciones    = S(r,20)
                });
            return list;
        }

        public static void GuardarDetalleNomina(DetalleNomina d)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            // Upsert por IdPeriodo+IdEmpleado
            cmd.CommandText = @"INSERT OR REPLACE INTO DetallesNomina
                (IdPeriodo,IdEmpleado,NombreEmpleado,TipoEmpleado,
                 DiasTrabajados,DiasInasistencia,PiezasProducidas,VentasCerradas,
                 SueldoDiarioUSD,DevengadoSueldo,DevengadoPiezas,DevengadoVentas,
                 OtrosIngresos,DeduccionIVSS,DeduccionRPVH,DescuentoFaltas,
                 CuotaPrestamo,OtrasDeduccion,TasaBCV,Observaciones)
                VALUES($ip,$ie,$ne,$te,$dt,$di,$pp,$vc,
                       $sd,$ds,$dp,$dv,$oi,$vi,$rv,$df,$cp,$od,$tb,$ob)";
            cmd.Parameters.AddWithValue("$ip", d.IdPeriodo);
            cmd.Parameters.AddWithValue("$ie", d.IdEmpleado);
            cmd.Parameters.AddWithValue("$ne", d.NombreEmpleado ?? "");
            cmd.Parameters.AddWithValue("$te", d.TipoEmpleado ?? "");
            cmd.Parameters.AddWithValue("$dt", (double)d.DiasTrabajados);
            cmd.Parameters.AddWithValue("$di", (double)d.DiasInasistencia);
            cmd.Parameters.AddWithValue("$pp", (double)d.PiezasProducidas);
            cmd.Parameters.AddWithValue("$vc", (double)d.VentasCerradas);
            cmd.Parameters.AddWithValue("$sd", (double)d.SueldoDiarioUSD);
            cmd.Parameters.AddWithValue("$ds", (double)d.DevengadoSueldo);
            cmd.Parameters.AddWithValue("$dp", (double)d.DevengadoPiezas);
            cmd.Parameters.AddWithValue("$dv", (double)d.DevengadoVentas);
            cmd.Parameters.AddWithValue("$oi", (double)d.OtrosIngresos);
            cmd.Parameters.AddWithValue("$vi", (double)d.DeduccionIVSS);
            cmd.Parameters.AddWithValue("$rv", (double)d.DeduccionRPVH);
            cmd.Parameters.AddWithValue("$df", (double)d.DescuentoFaltas);
            cmd.Parameters.AddWithValue("$cp", (double)d.CuotaPrestamo);
            cmd.Parameters.AddWithValue("$od", (double)d.OtrasDeduccion);
            cmd.Parameters.AddWithValue("$tb", (double)d.TasaBCV);
            cmd.Parameters.AddWithValue("$ob", d.Observaciones ?? "");
            cmd.ExecuteNonQuery();
        }

        public static void EliminarDetalleNomina(int idPeriodo, int idEmpleado)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM DetallesNomina WHERE IdPeriodo=$ip AND IdEmpleado=$ie";
            cmd.Parameters.AddWithValue("$ip", idPeriodo);
            cmd.Parameters.AddWithValue("$ie", idEmpleado);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Calcula automaticamente el detalle de nomina para un empleado
        /// dado el periodo y sus datos de produccion/ventas.
        /// Aplica: IVSS 4%, RPVH 1% sobre el equivalente en bolivares.
        /// </summary>
        public static DetalleNomina CalcularDetalle(
            Empleado emp,
            PeriodoNomina periodo,
            decimal diasTrabajados,
            decimal diasInasistencia,
            decimal piezasProducidas,
            decimal ventasCerradas,
            decimal cuotaPrestamo,
            decimal otrosIngresos = 0,
            decimal otrasDeduccion = 0)
        {
            decimal tasa = periodo.TasaBCV > 0 ? periodo.TasaBCV : 1;

            // Devengado
            decimal devSueldo  = emp.SueldoDiarioUSD * diasTrabajados;
            decimal devPiezasBs = emp.TipEmpleadoEs("Produccion")
                ? CalcularBonoProduccion(emp.Id, periodo.FechaDesde, periodo.FechaHasta)
                : 0;
            decimal devPiezas = tasa > 0 ? Math.Round(devPiezasBs / tasa, 4) : 0;
            decimal devVentasBs = emp.TipEmpleadoEs("Oficina") || emp.TipEmpleadoEs("Ventas")
                ? CalcularBonoVentas(emp.Id, periodo.FechaDesde, periodo.FechaHasta)
                : 0;
            decimal devVentas = tasa > 0 ? Math.Round(devVentasBs / tasa, 4) : 0;
            decimal totalDev   = devSueldo + devPiezas + devVentas + otrosIngresos;

            // Deducciones legales (se calculan sobre el total devengado convertido a Bs)
            decimal totalDevBs = totalDev * tasa;
            decimal ivss   = Math.Round(totalDevBs * 0.04m / tasa, 2);  // resultado en USD
            decimal rpvh   = Math.Round(totalDevBs * 0.01m / tasa, 2);

            // Descuento por inasistencias (proporcional al sueldo diario)
            decimal descFaltas = emp.SueldoDiarioUSD * diasInasistencia;

            return new DetalleNomina
            {
                IdPeriodo        = periodo.Id,
                IdEmpleado       = emp.Id,
                NombreEmpleado   = emp.NombreCompleto,
                TipoEmpleado     = emp.TipoEmpleado,
                DiasTrabajados   = diasTrabajados,
                DiasInasistencia = diasInasistencia,
                PiezasProducidas = piezasProducidas,
                VentasCerradas   = ventasCerradas,
                SueldoDiarioUSD  = emp.SueldoDiarioUSD,
                DevengadoSueldo  = devSueldo,
                DevengadoPiezas  = devPiezas,
                DevengadoVentas  = devVentas,
                OtrosIngresos    = otrosIngresos,
                DeduccionIVSS    = ivss,
                DeduccionRPVH    = rpvh,
                DescuentoFaltas  = descFaltas,
                CuotaPrestamo    = cuotaPrestamo,
                OtrasDeduccion   = otrasDeduccion,
                TasaBCV          = tasa
            };
        }

        // ── Prestamos ──────────────────────────────────────────────────────
        public static List<PrestamoEmpleado> ObtenerPrestamos(int? idEmpleado = null)
        {
            var list = new List<PrestamoEmpleado>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM PrestamosEmpleado"
                + (idEmpleado.HasValue ? " WHERE IdEmpleado=$id" : "")
                + " ORDER BY FechaOtorgado DESC";
            if (idEmpleado.HasValue) cmd.Parameters.AddWithValue("$id", idEmpleado.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PrestamoEmpleado
                {
                    Id = r.GetInt32(0), IdEmpleado = r.GetInt32(1),
                    NombreEmpleado = S(r,2),
                    FechaOtorgado  = DateTime.Parse(r.GetString(3)),
                    MontoTotalUSD  = D(r,4), CuotaSemanalUSD = D(r,5),
                    SaldoPendiente = D(r,6), Estado = S(r,7), Descripcion = S(r,8)
                });
            return list;
        }

        public static void InsertarPrestamo(PrestamoEmpleado p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO PrestamosEmpleado
                (IdEmpleado,NombreEmpleado,FechaOtorgado,MontoTotalUSD,
                 CuotaSemanalUSD,SaldoPendiente,Estado,Descripcion)
                VALUES($ie,$ne,$fo,$mt,$cs,$sp,$es,$de)";
            cmd.Parameters.AddWithValue("$ie", p.IdEmpleado);
            cmd.Parameters.AddWithValue("$ne", p.NombreEmpleado ?? "");
            cmd.Parameters.AddWithValue("$fo", p.FechaOtorgado.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$mt", (double)p.MontoTotalUSD);
            cmd.Parameters.AddWithValue("$cs", (double)p.CuotaSemanalUSD);
            cmd.Parameters.AddWithValue("$sp", (double)p.SaldoPendiente);
            cmd.Parameters.AddWithValue("$es", p.Estado ?? "Activo");
            cmd.Parameters.AddWithValue("$de", p.Descripcion ?? "");
            cmd.ExecuteNonQuery();
        }

        public static void AbonarPrestamo(int idPrestamo, decimal monto)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT SaldoPendiente FROM PrestamosEmpleado WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", idPrestamo);
            decimal saldo = D(cmd.ExecuteScalar());
            decimal nuevo = Math.Max(0, saldo - monto);
            var upd = c.CreateCommand();
            upd.CommandText = @"UPDATE PrestamosEmpleado
                SET SaldoPendiente=$sp, Estado=CASE WHEN $sp<=0 THEN 'Saldado' ELSE Estado END
                WHERE Id=$id";
            upd.Parameters.AddWithValue("$sp", (double)nuevo);
            upd.Parameters.AddWithValue("$id", idPrestamo);
            upd.ExecuteNonQuery();
        }

        private static decimal D(object obj) =>
            obj == null || obj == DBNull.Value ? 0 : (decimal)(double)Convert.ChangeType(obj, typeof(double));

        // ── Beneficios anuales LOTTT ───────────────────────────────────────
        public static List<BeneficioAnual> ObtenerBeneficios(int? idEmpleado = null, int? anio = null)
        {
            var list = new List<BeneficioAnual>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            var where = new List<string>();
            if (idEmpleado.HasValue) where.Add("IdEmpleado=$ie");
            if (anio.HasValue)       where.Add("Anio=$an");
            cmd.CommandText = "SELECT * FROM BeneficiosAnuales"
                + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                + " ORDER BY Anio DESC, TipoBeneficio";
            if (idEmpleado.HasValue) cmd.Parameters.AddWithValue("$ie", idEmpleado.Value);
            if (anio.HasValue)       cmd.Parameters.AddWithValue("$an", anio.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new BeneficioAnual
                {
                    Id = r.GetInt32(0), IdEmpleado = r.GetInt32(1),
                    NombreEmpleado   = S(r,2), Anio = r.GetInt32(3),
                    TipoBeneficio    = S(r,4), DiasCorresponden = D(r,5),
                    SalarioDiarioUSD = D(r,6), MontoUSD = D(r,7),
                    TasaBCV          = D(r,8),
                    FechaPago        = r.IsDBNull(9) ? DateTime.Today : DateTime.Parse(r.GetString(9)),
                    Estado = S(r,10), Observaciones = S(r,11)
                });
            return list;
        }

        public static void InsertarBeneficio(BeneficioAnual b)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO BeneficiosAnuales
                (IdEmpleado,NombreEmpleado,Anio,TipoBeneficio,DiasCorresponden,
                 SalarioDiarioUSD,MontoUSD,TasaBCV,FechaPago,Estado,Observaciones)
                VALUES($ie,$ne,$an,$tb,$dc,$sd,$mo,$tc,$fp,$es,$ob)";
            cmd.Parameters.AddWithValue("$ie", b.IdEmpleado);
            cmd.Parameters.AddWithValue("$ne", b.NombreEmpleado ?? "");
            cmd.Parameters.AddWithValue("$an", b.Anio);
            cmd.Parameters.AddWithValue("$tb", b.TipoBeneficio);
            cmd.Parameters.AddWithValue("$dc", (double)b.DiasCorresponden);
            cmd.Parameters.AddWithValue("$sd", (double)b.SalarioDiarioUSD);
            cmd.Parameters.AddWithValue("$mo", (double)b.MontoUSD);
            cmd.Parameters.AddWithValue("$tc", (double)b.TasaBCV);
            cmd.Parameters.AddWithValue("$fp", b.FechaPago.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$es", b.Estado ?? "Pendiente");
            cmd.Parameters.AddWithValue("$ob", b.Observaciones ?? "");
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("BENEFICIO_REGISTRADO", "BeneficiosAnuales",
                b.NombreEmpleado + " — " + b.TipoBeneficio + " " + b.Anio);
        }

        public static void MarcarBeneficioPagado(int id, decimal tasaBCV)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE BeneficiosAnuales
                SET Estado='Pagado', FechaPago=$fp, TasaBCV=$tb WHERE Id=$id";
            cmd.Parameters.AddWithValue("$fp", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$tb", (double)tasaBCV);
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Calcula los beneficios anuales LOTTT para un empleado.
        /// Utilidades: min 15 dias de salario normal (art. 131 LOTTT).
        /// Vacaciones: 15 dias + 1 dia adicional por cada anio (art. 190).
        /// Bono vacacional: 7 dias + 1 adicional por anio (art. 192).
        /// Prestaciones: garantia de 15 dias de salario por trimestre (art. 142).
        /// </summary>
        public static (BeneficioAnual Utilidades, BeneficioAnual Vacaciones,
                        BeneficioAnual BonoVac, BeneficioAnual Prestaciones)
            CalcularBeneficios(Empleado emp, int anio, decimal tasaBCV,
                               decimal? salarioDiarioOverride = null)
        {
            int anios   = emp.AniosServicio;
            decimal sal = salarioDiarioOverride ?? emp.SalarioNormalUSD;

            // Utilidades — minimo 15 dias, empresas pueden dar mas
            decimal diasUtil = Math.Max(15, 15);  // parametrizable
            var util = new BeneficioAnual
            {
                IdEmpleado = emp.Id, NombreEmpleado = emp.NombreCompleto,
                Anio = anio, TipoBeneficio = "Utilidades",
                DiasCorresponden = diasUtil, SalarioDiarioUSD = sal,
                MontoUSD = Math.Round(sal * diasUtil, 2), TasaBCV = tasaBCV,
                FechaPago = new DateTime(anio, 12, 15), Estado = "Pendiente",
                Observaciones = "Art. 131 LOTTT — " + diasUtil + " dias"
            };

            // Vacaciones — 15 dias + 1 por cada anio adicional (max practico 30)
            decimal diasVac = 15 + Math.Min(anios, 15);
            var vac = new BeneficioAnual
            {
                IdEmpleado = emp.Id, NombreEmpleado = emp.NombreCompleto,
                Anio = anio, TipoBeneficio = "Vacaciones",
                DiasCorresponden = diasVac, SalarioDiarioUSD = sal,
                MontoUSD = Math.Round(sal * diasVac, 2), TasaBCV = tasaBCV,
                FechaPago = emp.FechaIngreso.AddYears(anios + 1), Estado = "Pendiente",
                Observaciones = "Art. 190 LOTTT — " + diasVac + " dias (" + anios + " anos servicio)"
            };

            // Bono vacacional — 7 dias + 1 por anio (art. 192)
            decimal diasBono = 7 + Math.Min(anios, 21);
            var bono = new BeneficioAnual
            {
                IdEmpleado = emp.Id, NombreEmpleado = emp.NombreCompleto,
                Anio = anio, TipoBeneficio = "Bono Vacacional",
                DiasCorresponden = diasBono, SalarioDiarioUSD = sal,
                MontoUSD = Math.Round(sal * diasBono, 2), TasaBCV = tasaBCV,
                FechaPago = emp.FechaIngreso.AddYears(anios + 1), Estado = "Pendiente",
                Observaciones = "Art. 192 LOTTT — " + diasBono + " dias"
            };

            // Prestaciones sociales — 15 dias por trimestre (60 dias/anio garantia)
            decimal diasPrest = 60;
            var prest = new BeneficioAnual
            {
                IdEmpleado = emp.Id, NombreEmpleado = emp.NombreCompleto,
                Anio = anio, TipoBeneficio = "Prestaciones Sociales",
                DiasCorresponden = diasPrest, SalarioDiarioUSD = sal,
                MontoUSD = Math.Round(sal * diasPrest, 2), TasaBCV = tasaBCV,
                FechaPago = new DateTime(anio, 12, 31), Estado = "Pendiente",
                Observaciones = "Art. 142 LOTTT — garantia 15 dias x 4 trimestres"
            };

            return (util, vac, bono, prest);
        }

                // ════════ APORTES PATRONALES ═════════════════════════════════════════
        /// <summary>
        /// Calcula los aportes patronales para todos los empleados de un periodo.
        /// IVSS patron: 9% (configurable 9-11% segun nivel de riesgo de la empresa).
        /// RPVH/FAOV patron: 2% sobre salario normal.
        /// INCES: 2% sobre la nomina total.
        /// Bono CESTA: 0.5 UT x dia laboral (exento ISLR, no forma parte del salario).
        /// Todos los calculos se expresan en bolivares usando la tasa BCV del periodo.
        /// </summary>
        public static List<AportePatronal> CalcularAportesPatronales(
            int idPeriodo,
            decimal pctIVSSPatron = 9.0m,
            decimal valorUTBs = 0m)
        {
            var detalles = ObtenerDetallesNomina(idPeriodo);
            var periodo  = ObtenerPeriodos().Find(p => p.Id == idPeriodo);
            if (periodo == null || detalles.Count == 0)
                return new List<AportePatronal>();

            decimal tasa = periodo.TasaBCV > 0 ? periodo.TasaBCV : 1;
            var aportes  = new List<AportePatronal>();

            foreach (var d in detalles)
            {
                decimal salBs    = d.TotalDevengado * tasa;
                decimal ivss     = Math.Round(salBs * pctIVSSPatron / 100, 2);
                decimal rpvh     = Math.Round(salBs * 0.02m, 2);
                decimal inces    = Math.Round(salBs * 0.02m, 2);
                decimal cesta    = valorUTBs > 0
                    ? Math.Round(d.DiasTrabajados * 0.5m * valorUTBs, 2)
                    : 0;

                aportes.Add(new AportePatronal
                {
                    IdPeriodo       = idPeriodo,
                    IdEmpleado      = d.IdEmpleado,
                    NombreEmpleado  = d.NombreEmpleado,
                    SalarioNormalBs = salBs,
                    TasaBCV         = tasa,
                    PorcentajeIVSS  = pctIVSSPatron,
                    IVSSPatronBs    = ivss,
                    RPVHPatronBs    = rpvh,
                    INCESBs         = inces,
                    DiasLaborales   = d.DiasTrabajados,
                    ValorUTBs       = valorUTBs,
                    CestaBs         = cesta
                });
            }

            return aportes;
        }

        public static void GuardarAportesPatronales(List<AportePatronal> aportes)
        {
            if (aportes.Count == 0) return;
            int idPeriodo = aportes[0].IdPeriodo;

            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Borrar los existentes del periodo para recalcular
                var del = conn.CreateCommand(); del.Transaction = tx;
                del.CommandText = "DELETE FROM AportesPatronales WHERE IdPeriodo=$id";
                del.Parameters.AddWithValue("$id", idPeriodo);
                del.ExecuteNonQuery();

                foreach (var a in aportes)
                {
                    var cmd = conn.CreateCommand(); cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO AportesPatronales
                        (IdPeriodo,IdEmpleado,NombreEmpleado,SalarioNormalBs,TasaBCV,
                         PorcentajeIVSS,IVSSPatronBs,RPVHPatronBs,INCESBs,
                         DiasLaborales,ValorUTBs,CestaBs)
                        VALUES($ip,$ie,$ne,$sb,$tb,$pi,$iv,$rv,$in,$dl,$vu,$ce)";
                    cmd.Parameters.AddWithValue("$ip", a.IdPeriodo);
                    cmd.Parameters.AddWithValue("$ie", a.IdEmpleado);
                    cmd.Parameters.AddWithValue("$ne", a.NombreEmpleado ?? "");
                    cmd.Parameters.AddWithValue("$sb", (double)a.SalarioNormalBs);
                    cmd.Parameters.AddWithValue("$tb", (double)a.TasaBCV);
                    cmd.Parameters.AddWithValue("$pi", (double)a.PorcentajeIVSS);
                    cmd.Parameters.AddWithValue("$iv", (double)a.IVSSPatronBs);
                    cmd.Parameters.AddWithValue("$rv", (double)a.RPVHPatronBs);
                    cmd.Parameters.AddWithValue("$in", (double)a.INCESBs);
                    cmd.Parameters.AddWithValue("$dl", (double)a.DiasLaborales);
                    cmd.Parameters.AddWithValue("$vu", (double)a.ValorUTBs);
                    cmd.Parameters.AddWithValue("$ce", (double)a.CestaBs);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                RegistrarAuditoria("APORTES_PATRONALES", "AportesPatronales",
                    "Periodo " + idPeriodo + " — " + aportes.Count + " empleados");
            }
            catch { tx.Rollback(); throw; }
        }

        public static List<AportePatronal> ObtenerAportesPatronales(int idPeriodo)
        {
            var list = new List<AportePatronal>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM AportesPatronales WHERE IdPeriodo=$id ORDER BY NombreEmpleado";
            cmd.Parameters.AddWithValue("$id", idPeriodo);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AportePatronal
                {
                    Id = r.GetInt32(0), IdPeriodo = r.GetInt32(1), IdEmpleado = r.GetInt32(2),
                    NombreEmpleado  = S(r,3),
                    SalarioNormalBs = D(r,4),  TasaBCV       = D(r,5),
                    PorcentajeIVSS  = D(r,6),  IVSSPatronBs  = D(r,7),
                    RPVHPatronBs    = D(r,8),  INCESBs       = D(r,9),
                    DiasLaborales   = D(r,10), ValorUTBs     = D(r,11),
                    CestaBs         = D(r,12)
                });
            return list;
        }

        public static ResumenAportesPatronales ObtenerResumenAportes(int idPeriodo)
        {
            var aportes  = ObtenerAportesPatronales(idPeriodo);
            var detalles = ObtenerDetallesNomina(idPeriodo);
            var periodo  = ObtenerPeriodos().Find(p => p.Id == idPeriodo);

            decimal nomTot   = detalles.Sum(d => d.TotalDevengado);
            decimal tasa     = periodo?.TasaBCV ?? 1;
            decimal nomTotBs = nomTot * tasa;

            var res = new ResumenAportesPatronales
            {
                IdPeriodo    = idPeriodo,
                DescPeriodo  = periodo?.DescPeriodo ?? "",
                NominaTotal  = nomTot,
                NominaTotalBs= nomTotBs,
                IVSSPatronBs = aportes.Sum(a => a.IVSSPatronBs),
                RPVHPatronBs = aportes.Sum(a => a.RPVHPatronBs),
                INCESBs      = aportes.Sum(a => a.INCESBs),
                CestaBs      = aportes.Sum(a => a.CestaBs),
                NumEmpleados = aportes.Count
            };
            res.CostoTotalBs = res.NominaTotalBs + res.TotalAportesBs;
            return res;
        }

                // ════════ PERFIL Y PREFERENCIAS ═════════════════════════════════════
        /// <summary>Cambia la contrasena validando la actual. Devuelve null si OK, mensaje si error.</summary>
        public static string CambiarContrasena(string nombreUsuario, string actualPlain, string nuevaPlain)
        {
            if (string.IsNullOrWhiteSpace(nuevaPlain) || nuevaPlain.Length < 8)
                return "La nueva contraseña debe tener al menos 8 caracteres.";
            if (nuevaPlain == actualPlain)
                return "La nueva contraseña no puede ser igual a la actual.";

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT PasswordHash FROM Usuarios WHERE NombreUsuario=$u AND Activo=1";
            cmd.Parameters.AddWithValue("$u", nombreUsuario);
            var obj = cmd.ExecuteScalar();
            if (obj == null) return "Usuario no encontrado.";

            // Usar VerificarPassword (tiempo constante, soporta legacy + PBKDF2)
            if (!VerificarPassword(actualPlain, (string)obj))
                return "La contraseña actual es incorrecta.";

            // Guardar siempre con PBKDF2 (nunca SHA-256 legacy)
            var upd = c.CreateCommand();
            upd.CommandText = @"UPDATE Usuarios
                SET PasswordHash=$ph, RequiereCambioPassword=0
                WHERE NombreUsuario=$u";
            upd.Parameters.AddWithValue("$ph", HashPassword(nuevaPlain));
            upd.Parameters.AddWithValue("$u",  nombreUsuario);
            upd.ExecuteNonQuery();

            RegistrarAuditoria("CAMBIO_CONTRASENA", "Usuarios",
                "Usuario cambió su contraseña: " + nombreUsuario);
            return null;   // OK
        }

        public static PreferenciasUsuario ObtenerPreferencias(string nombreUsuario)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM PreferenciasUsuario WHERE NombreUsuario=$u";
            cmd.Parameters.AddWithValue("$u", nombreUsuario);
            using var r = cmd.ExecuteReader();
            if (r.Read())
                return new PreferenciasUsuario
                {
                    NombreUsuario    = S(r,0),
                    Tema             = S(r,1),
                    ColorAcento      = S(r,2),
                    MostrarKpis      = r.GetInt32(3) == 1,
                    SonidoAlertas    = r.GetInt32(4) == 1,
                    FilasPorPagina   = r.IsDBNull(5) ? 50 : r.GetInt32(5),
                    ConfirmarEliminar= r.GetInt32(6) == 1,
                    AbrirUltimoModulo= r.GetInt32(7) == 1,
                    NombreCompleto   = S(r,8),
                    Email            = S(r,9),
                    Telefono         = S(r,10)
                };

            // Primera vez: crear con defaults
            return new PreferenciasUsuario { NombreUsuario = nombreUsuario };
        }

        public static void GuardarPreferencias(PreferenciasUsuario p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO PreferenciasUsuario
                (NombreUsuario,Tema,ColorAcento,MostrarKpis,SonidoAlertas,
                 FilasPorPagina,ConfirmarEliminar,AbrirUltimoModulo,
                 NombreCompleto,Email,Telefono)
                VALUES($u,$te,$ca,$mk,$sa,$fp,$ce,$am,$nc,$em,$tf)";
            cmd.Parameters.AddWithValue("$u",  p.NombreUsuario);
            cmd.Parameters.AddWithValue("$te", p.Tema);
            cmd.Parameters.AddWithValue("$ca", p.ColorAcento);
            cmd.Parameters.AddWithValue("$mk", p.MostrarKpis      ? 1 : 0);
            cmd.Parameters.AddWithValue("$sa", p.SonidoAlertas    ? 1 : 0);
            cmd.Parameters.AddWithValue("$fp", p.FilasPorPagina);
            cmd.Parameters.AddWithValue("$ce", p.ConfirmarEliminar? 1 : 0);
            cmd.Parameters.AddWithValue("$am", p.AbrirUltimoModulo? 1 : 0);
            cmd.Parameters.AddWithValue("$nc", p.NombreCompleto ?? "");
            cmd.Parameters.AddWithValue("$em", p.Email ?? "");
            cmd.Parameters.AddWithValue("$tf", p.Telefono ?? "");
            cmd.ExecuteNonQuery();

            RegistrarAuditoria("PREFERENCIAS_GUARDADAS", "Usuarios",
                "Usuario: " + p.NombreUsuario);
        }

                // ════════ BUSCADOR GLOBAL ════════════════════════════════════════════
        public class ResultadoBusqueda
        {
            public string Modulo      { get; set; }  // Clientes, Facturas, etc.
            public string Titulo      { get; set; }  // texto principal
            public string Subtitulo  { get; set; }  // detalle
            public string Codigo     { get; set; }  // clave primaria
            public string Categoria  { get; set; }  // para agrupar
        }

        public static List<ResultadoBusqueda> BuscarGlobal(string q)
        {
            var r = new List<ResultadoBusqueda>();
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return r;
            q = q.Trim().ToLower();

            // Variante numérica sin ceros a la izquierda — para que "03"
            // encuentre el código "3". Si no es numérico, qNum == q.
            string qNum = q;
            bool esNumerico = q.Length > 0 && q.TrimStart('-').All(char.IsDigit);
            if (esNumerico)
            {
                qNum = q.TrimStart('0');
                if (qNum.Length == 0) qNum = "0";
            }
            // Para los WHERE: armamos un LIKE que matchee ambas variantes.
            // Si q == qNum no se duplica el costo (el optimizador colapsa).
            string Likes(string colExpr) =>
                qNum == q
                    ? $"({colExpr}) LIKE $q"
                    : $"(({colExpr}) LIKE $q OR ({colExpr}) LIKE $qn)";
            void Bind(SqliteCommand cmd)
            {
                cmd.Parameters.AddWithValue("$q",  "%" + q   + "%");
                if (qNum != q) cmd.Parameters.AddWithValue("$qn", "%" + qNum + "%");
            }

            using var c = Conn();

            // Clientes
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Codigo, Descripcion, CiRif, Telefono
                FROM Clientes WHERE " +
                Likes("lower(Codigo)||lower(coalesce(Descripcion,''))||lower(coalesce(CiRif,''))") +
                " LIMIT 10";
            Bind(cmd);
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Clientes", Categoria="Clientes",
                        Codigo=rd.GetString(0), Titulo=rd.GetString(0)+" — "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="RIF: "+(rd.IsDBNull(2)?"":rd.GetString(2))+" Tel: "+(rd.IsDBNull(3)?"":rd.GetString(3)) });

            // Proveedores
            var cmd2 = c.CreateCommand();
            cmd2.CommandText = @"SELECT Codigo, Descripcion, CiRif, Telefono1
                FROM Proveedores WHERE " +
                Likes("lower(Codigo)||lower(coalesce(Descripcion,''))||lower(coalesce(CiRif,''))") +
                " LIMIT 10";
            Bind(cmd2);
            using (var rd = cmd2.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Proveedores", Categoria="Proveedores",
                        Codigo=rd.GetString(0), Titulo=rd.GetString(0)+" — "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="RIF: "+(rd.IsDBNull(2)?"":rd.GetString(2))+" Tel: "+(rd.IsDBNull(3)?"":rd.GetString(3)) });

            // Inventario
            var cmd3 = c.CreateCommand();
            cmd3.CommandText = @"SELECT Codigo, Descripcion, Departamento, Existencia, Precio
                FROM Inventario WHERE " +
                Likes("lower(Codigo)||lower(coalesce(Descripcion,''))||lower(coalesce(Departamento,''))") +
                " LIMIT 15";
            Bind(cmd3);
            using (var rd = cmd3.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Inventario", Categoria="Inventario",
                        Codigo=rd.GetString(0), Titulo=rd.GetString(0)+" — "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="Existencia: "+(rd.IsDBNull(3)?0:rd.GetDouble(3)).ToString("N0")+" | Precio: "+(rd.IsDBNull(4)?0:rd.GetDouble(4)).ToString("C2") });

            // Facturas
            var cmd4 = c.CreateCommand();
            cmd4.CommandText = @"SELECT NumeroF, Codigo, TotalF, Fecha
                FROM Facturas WHERE " +
                Likes("lower(NumeroF)||lower(coalesce(Codigo,''))") +
                " LIMIT 10";
            Bind(cmd4);
            using (var rd = cmd4.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Facturas", Categoria="Facturas",
                        Codigo=rd.GetString(0), Titulo="Factura "+rd.GetString(0)+" — Cliente: "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="Total: "+((decimal)rd.GetDouble(2)).ToString("C2")+" | Fecha: "+DateTime.Parse(rd.GetString(3)).ToShortDateString() });

            // Compras
            var cmd5 = c.CreateCommand();
            cmd5.CommandText = @"SELECT CompraN, CodigoP, TotalC, FechaC
                FROM Compras WHERE " +
                Likes("lower(CompraN)||lower(coalesce(CodigoP,''))") +
                " LIMIT 10";
            Bind(cmd5);
            using (var rd = cmd5.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Compras", Categoria="Compras",
                        Codigo=rd.GetString(0), Titulo="Compra "+rd.GetString(0)+" — Proveedor: "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="Total: "+((decimal)rd.GetDouble(2)).ToString("C2")+" | Fecha: "+DateTime.Parse(rd.GetString(3)).ToShortDateString() });

            // Ordenes de Produccion
            var cmd6 = c.CreateCommand();
            cmd6.CommandText = @"SELECT NumOrden, ProductoCod, Estado, CantidadPlan
                FROM OrdenesProduccion WHERE " +
                Likes("lower(NumOrden)||lower(coalesce(ProductoCod,''))") +
                " LIMIT 10";
            Bind(cmd6);
            using (var rd = cmd6.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Ordenes de Produccion", Categoria="Produccion",
                        Codigo=rd.GetString(0), Titulo="OP "+rd.GetString(0)+" — "+rd.GetString(1),
                        Subtitulo="Estado: "+rd.GetString(2)+" | Cantidad: "+rd.GetDouble(3).ToString("N0") });

            // Moldes
            var cmd7 = c.CreateCommand();
            cmd7.CommandText = @"SELECT CodigoMold, Descripcion, ModeloQfabrica, FechaLastMant
                FROM TbMoldes WHERE " +
                Likes("lower(CodigoMold)||lower(coalesce(Descripcion,''))||lower(coalesce(ModeloQfabrica,''))") +
                " LIMIT 10";
            Bind(cmd7);
            using (var rd = cmd7.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Moldes", Categoria="Produccion",
                        Codigo=rd.GetString(0), Titulo=rd.GetString(0)+" — "+(rd.IsDBNull(1)?"":rd.GetString(1)),
                        Subtitulo="Modelo: "+(rd.IsDBNull(2)?"":rd.GetString(2))+" | Ult.Mant: "+(rd.IsDBNull(3)?"":DateTime.Parse(rd.GetString(3)).ToShortDateString()) });

            // Kardex
            var cmd8 = c.CreateCommand();
            cmd8.CommandText = @"SELECT CodigoProducto, TipoMovimiento, Concepto, Cantidad, FechaHora
                FROM MovimientosInventario WHERE " +
                Likes("lower(CodigoProducto)||lower(coalesce(Concepto,''))||lower(coalesce(Referencia,''))") +
                " LIMIT 8";
            Bind(cmd8);
            using (var rd = cmd8.ExecuteReader())
                while (rd.Read())
                    r.Add(new ResultadoBusqueda { Modulo="Kardex", Categoria="Inventario",
                        Codigo=rd.GetString(0), Titulo=rd.GetString(1)+" — "+rd.GetString(0)+" | "+rd.GetString(2),
                        Subtitulo="Cantidad: "+rd.GetDouble(3).ToString("N2")+" | "+DateTime.Parse(rd.GetString(4)).ToString("dd/MM/yyyy HH:mm") });

            return r;
        }

                // ════════ HELPERS ════════════════════════════════════════════════════
        private static string S(SqliteDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
        private static decimal D(SqliteDataReader r, int i) => r.IsDBNull(i) ? 0m : (decimal)r.GetDouble(i);

        private static void Exec(SqliteConnection c, string sql, params (string k, object v)[] pars)
        {
            var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (k, v) in pars) cmd.Parameters.AddWithValue(k, v);
            cmd.ExecuteNonQuery();
        }

        // ════════ EDICION DIRECTA DE INVENTARIO ════════════════════════
        public static bool ActualizarCampoInventario(
            string codigo, string campo, string valor, out string error)
        {
            error = null;
            var permitidos = new[]
            {
                "Descripcion","Departamento","Unidad","Enser",
                "CostoActual","CostoAnt","CostoPro","Precio",
                "Existencia","ExistenMin","ExistenMax","Proveedores","CodProveedor","PesoKg"
            };
            if (!permitidos.Contains(campo))
            { error = $"Campo '{campo}' no es editable."; return false; }
            try
            {
                using var c = Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = $"UPDATE Inventario SET {campo}=$val WHERE Codigo=$cod";
                cmd.Parameters.AddWithValue("$val", valor ?? "");
                cmd.Parameters.AddWithValue("$cod", codigo);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MODELOS FALTANTES
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    //  KARDEX - MOVIMIENTOS DE INVENTARIO
    // ════════════════════════════════════════════════════════════════════════
    public class MovimientoInventario
    {
        public int      IdMovimiento  { get; set; }
        public DateTime FechaHora     { get; set; }
        public string   CodigoProducto{ get; set; }
        public string   TipoMovimiento{ get; set; }  // ENTRADA / SALIDA
        public string   Concepto      { get; set; }
        public decimal  Cantidad      { get; set; }
        public decimal  SaldoAnterior { get; set; }
        public decimal  NuevoSaldo    { get; set; }
        public string   Referencia    { get; set; }  // Nro Factura, Compra, etc.
        public string   Usuario       { get; set; }
        /// <summary>"PRODUCTO" o "MATERIA_PRIMA" — discrimina Kardex B vs A</summary>
        public string   TipoKardex    { get; set; }
        /// <summary>Costo unitario en el momento del movimiento (para CPP)</summary>
        public decimal  CostoUnitario { get; set; }
    }

    public class IngresoProduccion
    {
        public int      Id          { get; set; }
        public string   Producto    { get; set; }
        public string   Lote        { get; set; }
        public int      Cantidad    { get; set; }
        public string   Unidad      { get; set; }
        public DateTime Fecha       { get; set; }
        public string   Responsable { get; set; }
        public string   Observacion { get; set; }
        public string   Estado      { get; set; }
    }

    public class CuentaCobrar
    {
        public int      Id           { get; set; }
        public string   Cliente      { get; set; }
        public string   Concepto     { get; set; }
        public decimal  Monto        { get; set; }
        public decimal  Pagado       { get; set; }
        public decimal  Saldo        => Monto - Pagado;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVence   { get; set; }
        public string   Estado       { get; set; }
    }

    public class ProductoPrecio
    {
        public int      Id                  { get; set; }
        public string   Codigo              { get; set; }
        public string   Nombre              { get; set; }
        public string   Categoria           { get; set; }
        public decimal  CostoActual         { get; set; }
        public decimal  PrecioActual        { get; set; }
        public decimal  CostoNuevo          { get; set; }
        public decimal  PrecioNuevo         { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }


    // ════════════════════════════════════════════════════════════════════════
    //  FLUJO DE CAJA
    // ════════════════════════════════════════════════════════════════════════
    public class MovimientoCaja
    {
        public DateTime Fecha       { get; set; }
        public string   Tipo        { get; set; }  // Ingreso / Egreso
        public string   Categoria   { get; set; }  // CxC / Compra / Nomina / Gasto / Manual
        public string   Concepto    { get; set; }
        public decimal  MontoUSD    { get; set; }
        public string   Referencia  { get; set; }
        public bool     EsReal      { get; set; }  // true=real, false=proyectado
    }

    public class ResumenFlujoCaja
    {
        public decimal SaldoInicial      { get; set; }
        public decimal IngreSos30        { get; set; }
        public decimal Egresos30         { get; set; }
        public decimal IngreSos60        { get; set; }
        public decimal Egresos60         { get; set; }
        public decimal IngreSos90        { get; set; }
        public decimal Egresos90         { get; set; }
        public decimal NetoAcum30        => IngreSos30 - Egresos30;
        public decimal NetoAcum60        => IngreSos60 - Egresos60;
        public decimal NetoAcum90        => IngreSos90 - Egresos90;
        public decimal SaldoProyectado30 => SaldoInicial + NetoAcum30;
        public decimal SaldoProyectado60 => SaldoInicial + NetoAcum30 + NetoAcum60;
        public decimal SaldoProyectado90 => SaldoInicial + NetoAcum30 + NetoAcum60 + NetoAcum90;
    }

    // ── Clases remanentes para compatibilidad ────────────────────────────────
    public class Gasto
    {
        public int      Id          { get; set; }
        public string   Categoria   { get; set; }
        public string   Descripcion { get; set; }
        public decimal  Monto       { get; set; }
        public DateTime Fecha       { get; set; }
        public string   Proveedor   { get; set; }
        public string   Estado      { get; set; }
    }

    public class ArchivoSistema
    {
        public int      Id      { get; set; }
        public string   Nombre  { get; set; }
        public string   Tipo    { get; set; }
        public string   Ruta    { get; set; }
        public DateTime Fecha   { get; set; }
        public string   Usuario { get; set; }


    }

    // ════════════════════════════════════════════════════════════════════════
    //  SESIÓN GLOBAL
    // ════════════════════════════════════════════════════════════════════════
    public static class Sesion
    {
        public static int    UsuarioId    { get; set; }
        public static string NombreUsuario { get; set; }
        public static string NombreCompleto{ get; set; }
        public static string Rol          { get; set; }  // Admin / Ventas / Almacen / Contabilidad

        public static bool EsAdmin        => Rol == "Admin";
        public static bool TienePermiso(string modulo) => PermisosRol.Permite(Rol, modulo);

        // ── Visibilidad de Notas de Entrega (v22) ───────────────────────────
        // Default false. Sólo Admin puede activarla con UsarMostrarNotasEntrega.
        // Se resetea SIEMPRE en cada login (no persiste entre sesiones).
        // Cuando true, todos los reportes/dashboard/CxC incluyen las NE.
        // Cuando false (normal), las NE son invisibles para reportes y totales,
        // pero siguen apareciendo en el listado de FrmFacturas con badge.
        private static bool _mostrarNE;
        public static bool MostrarNotasEntrega
        {
            get => _mostrarNE;
            private set => _mostrarNE = value;
        }

        // Activa/desactiva con auditoría. Solo Admin puede invocarla.
        public static void SetMostrarNotasEntrega(bool valor)
        {
            if (!EsAdmin)
                throw new InvalidOperationException(
                    "Solo Admin puede cambiar la visibilidad de Notas de Entrega.");
            if (_mostrarNE == valor) return;
            _mostrarNE = valor;
            DatabaseService.RegistrarAuditoria(
                valor ? "NE_VISTA_ACTIVADA" : "NE_VISTA_DESACTIVADA",
                "Sistema",
                $"{NombreUsuario} {(valor ? "activó" : "desactivó")} la visibilidad " +
                "de Notas de Entrega para su sesión.");
        }

        /// <summary>
        /// Carga todos los datos de sesión desde un objeto Usuario autenticado.
        /// Llamar desde FrmLogin justo después de ValidarLogin exitoso.
        /// </summary>
        public static void IniciarDesde(Usuario u)
        {
            UsuarioId     = u.Id;
            NombreUsuario  = u.NombreUsuario;
            NombreCompleto = u.NombreCompleto;
            Rol            = u.Rol;
            _mostrarNE     = false;   // siempre false en cada login
        }

        public static void Cerrar()
        {
            UsuarioId     = 0;
            NombreUsuario  = null;
            NombreCompleto = null;
            Rol            = null;
            _mostrarNE     = false;
        }
    }

    public static class PermisosRol
    {
        // Devuelve true si el rol tiene acceso al módulo
        public static bool Permite(string rol, string modulo)
        {
            if (rol == "Admin") return true;   // Admin accede a todo
            return (rol, modulo) switch
            {
                ("Ventas",       "Facturas")         => true,
                ("Ventas",       "CuentasCobrar")    => true,
                ("Ventas",       "Clientes")         => true,
                ("Ventas",       "Reportes")         => true,
                ("Ventas",       "TasaConsulta")     => true,
                ("Ventas",       "TasaIngreso")      => true,  // ingreso/aceptación rutinaria
                ("Almacen",      "Inventario")       => true,
                ("Almacen",      "Kardex")           => true,
                ("Almacen",      "Compras")          => true,
                ("Almacen",      "Proveedores")      => true,
                ("Almacen",      "IngresoProduccion")=> true,
                ("Almacen",      "Moldes")           => true,
                ("Almacen",      "TasaConsulta")     => true,
                ("Contabilidad", "Reportes")         => true,
                ("Contabilidad", "Facturas")         => true,
                ("Contabilidad", "Compras")          => true,
                ("Contabilidad", "Gastos")           => true,
                ("Contabilidad", "CambioPrecios")    => true,
                ("Contabilidad", "TasaConsulta")     => true,
                ("Contabilidad", "TasaIngreso")      => true,
                ("Contabilidad", "TasaModificar")    => true,  // editar tasa ya registrada
                // ── EGRESOS ─────────────────────────────────────────────────
                ("Ventas",       "EgresosVer")       => true,  // solo lectura
                ("Almacen",      "EgresosVer")       => true,
                ("Almacen",      "EgresosRegistrar") => true,  // puede registrar MP
                ("Contabilidad", "EgresosVer")       => true,
                ("Contabilidad", "EgresosRegistrar") => true,
                ("Contabilidad", "EgresosAnular")    => true,  // puede anular
                ("Contabilidad", "EgresosCategorias")=> true,  // CRUD categorias
                // ── ANULACIÓN DE FACTURAS (v21) ─────────────────────────────
                //  No es eliminar (rompe correlativo SENIAT). Marca la factura
                //  como anulada con motivo y deja la fila para auditoría.
                //  Solo Contabilidad y Admin pueden anular.
                ("Contabilidad", "FacturasAnular")  => true,
                // ── NOTAS DE ENTREGA (v22) ──────────────────────────────────
                //  Crear:    cualquier rol que ya pueda crear factura
                //  Convertir: solo Admin (cambiar NE a factura legal)
                //  Toggle visibilidad: solo Admin (auditado por sesión)
                ("Ventas",       "NotasEntregaCrear") => true,
                ("Contabilidad", "NotasEntregaCrear") => true,
                // ── PERMISOS DE TASA DE CAMBIO ──────────────────────────────
                //  TasaConsulta : ver tasa actual y consultar histórico
                //  TasaIngreso  : ingresar tasa nueva del día (no existe aún)
                //  TasaModificar: sobrescribir tasa ya registrada (sensible)
                //  Solo Admin tiene los 3 implícitamente. El resto se asigna
                //  por rol arriba según la responsabilidad operativa.
                _ => false
            };
        }
    }

    public class Usuario
    {
        public int    Id                     { get; set; }
        public string NombreUsuario          { get; set; }
        public string NombreCompleto         { get; set; }
        public string PasswordHash           { get; set; }
        public string Rol                    { get; set; }
        public bool   Activo                 { get; set; }
        public DateTime  FechaCreacion       { get; set; }
        public DateTime? UltimoAcceso        { get; set; }
        /// <summary>True si el usuario debe cambiar la contraseña en el próximo login.</summary>
        public bool   RequiereCambioPassword { get; set; }
        /// <summary>Intentos de login fallidos hoy (reset al login exitoso).</summary>
        public int    IntentosHoy            { get; set; }
        /// <summary>Si no null, el usuario está bloqueado hasta esta fecha/hora.</summary>
        public DateTime? BloqueadoHasta      { get; set; }

        // ── Helpers de rol ───────────────────────────────────────────────
        /// <summary>True si el usuario tiene rol Admin.</summary>
        public bool EsAdmin => Rol == "Admin";

        /// <summary>True si el usuario tiene el rol especificado.</summary>
        public bool TieneRol(string rol) =>
            string.Equals(Rol, rol, StringComparison.OrdinalIgnoreCase);

        /// <summary>Descripción legible del rol para mostrar en la UI.</summary>
        public string RolesDescripcion => Rol ?? "(sin rol)";

        /// <summary>
        /// CSV de roles. Asignar este campo actualiza Rol automáticamente.
        /// Mantiene compatibilidad con código que usaba multi-rol.
        /// </summary>
        public string RolesCSV
        {
            get => Rol ?? "";
            set => Rol = string.IsNullOrWhiteSpace(value) ? "" : value.Split(',')[0].Trim();
        }
    }



}
