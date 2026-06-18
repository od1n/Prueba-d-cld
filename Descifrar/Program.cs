using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion");

            // Cargar clave DPAPI
            var rutaClave = Path.Combine(appData, "sg.key");
            if (!File.Exists(rutaClave))
            {
                Console.WriteLine($"ERROR: No se encontró {rutaClave}");
                return;
            }
            var claveCifrada = File.ReadAllBytes(rutaClave);
            var clave = ProtectedData.Unprotect(claveCifrada, null, DataProtectionScope.CurrentUser);
            Console.WriteLine($"Clave cargada ({clave.Length * 8} bits)");

            // Buscar respaldo más reciente si no se especifica
            var carpetaResp = Path.Combine(appData, "Respaldos");
            string archivoEnc;
            if (args.Length > 0 && File.Exists(args[0]))
            {
                archivoEnc = args[0];
            }
            else
            {
                var archivos = Directory.GetFiles(carpetaResp, "*.db.enc");
                if (archivos.Length == 0)
                {
                    Console.WriteLine("ERROR: No hay respaldos .db.enc");
                    return;
                }
                Array.Sort(archivos);
                archivoEnc = archivos[^1]; // el más reciente por nombre
            }
            Console.WriteLine($"Descifrando: {Path.GetFileName(archivoEnc)}");

            // Destino
            var destino = args.Length > 1
                ? args[1]
                : @"C:\SistemaGestion\respaldo_descifrado.db";

            // Descifrar AES-256-CBC, IV en primeros 16 bytes
            using var fsIn = new FileStream(archivoEnc, FileMode.Open, FileAccess.Read);
            var iv = new byte[16];
            fsIn.Read(iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = clave;
            aes.IV = iv;

            using var fsOut = new FileStream(destino, FileMode.Create, FileAccess.Write);
            using var cs = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cs.CopyTo(fsOut);

            Console.WriteLine($"OK: {destino} ({new FileInfo(destino).Length:N0} bytes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}
