using System;
using System.IO;

class Program
{
    static void Main()
    {
        try
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion", "Respaldos");

            // Buscar respaldo más reciente
            var archivos = Directory.GetFiles(appData, "*.db.enc");
            if (archivos.Length == 0)
            {
                Console.WriteLine("No hay respaldos .db.enc");
                return;
            }
            Array.Sort(archivos);
            var archivoEnc = archivos[^1];
            Console.WriteLine($"Descifrando: {Path.GetFileName(archivoEnc)}");

            var destino = @"C:\SistemaGestion\respaldo_descifrado.db";

            // Usar el mismo código de SecurityService
            SistemaGestion.SecurityService.DescifrarRespaldo(archivoEnc, destino);

            var info = new FileInfo(destino);
            Console.WriteLine($"OK: {destino} ({info.Length:N0} bytes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
