using System;
using System.Linq;

namespace SistemaGestion
{
    public static partial class DatabaseService
    {
        /// <summary>
        /// Verifica si el rango [desde, hasta] se solapa con algun periodo existente.
        /// Retorna null si no hay solapamiento, o el periodo conflictivo si lo hay.
        /// Se ignoran periodos Cerrados opcionalemnte (por defecto se verifican todos).
        /// </summary>
        public static PeriodoNomina ValidarSolapamientoPeriodo(
            DateTime desde,
            DateTime hasta,
            int excluirId = 0,
            bool soloAbiertos = false)
        {
            var periodos = ObtenerPeriodos();

            return periodos.FirstOrDefault(p =>
            {
                // Excluir el propio periodo al editar
                if (p.Id == excluirId) return false;

                // Filtrar por estado si se pide
                if (soloAbiertos && p.Estado != "Abierto") return false;

                // Dos rangos [A,B] y [C,D] se solapan si A <= D && C <= B
                return desde <= p.FechaHasta && p.FechaDesde <= hasta;
            });
        }
    }
}
