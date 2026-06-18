using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        try
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion");

            // 1. Cargar clave DPAPI (idéntico a SecurityService.CargarClave)
            var rutaClave = Path.Combine(appData, "sg.key");
            byte[] claveCifrada = File.ReadAllBytes(rutaClave);
            byte[] clave = ProtectedData.Unprotect(claveCifrada, null, DataProtectionScope.CurrentUser);
            Console.WriteLine($"Clave: {clave.Length * 8} bits, primeros bytes: {Convert.ToHexString(clave[..4])}");

            // 2. Encontrar respaldo más reciente
            var carpeta = Path.Combine(appData, "Respaldos");
            var archivos = Directory.GetFiles(carpeta, "*.db.enc");
            Array.Sort(archivos);

            // Probar todos empezando por el más reciente
            for (int i = archivos.Length - 1; i >= 0; i--)
            {
                var enc = archivos[i];
                var destino = @"C:\SistemaGestion\respaldo_descifrado.db";
                Console.WriteLine($"\nIntentando: {Path.GetFileName(enc)} ({new FileInfo(enc).Length:N0} bytes)");

                try
                {
                    // Código EXACTO de SecurityService.DescifrarRespaldo
                    using var fsOrigen  = new FileStream(enc, FileMode.Open, FileAccess.Read);
                    using var fsDestino = new FileStream(destino, FileMode.Create, FileAccess.Write);

                    byte[] iv = new byte[16];
                    int leidos = fsOrigen.Read(iv, 0, iv.Length);
                    Console.WriteLine($"  IV ({leidos} bytes): {Convert.ToHexString(iv)}");

                    using var aes = Aes.Create();
                    aes.Key = clave;
                    aes.IV  = iv;

                    using var decryptor = aes.CreateDecryptor();
                    using var csOrigen  = new CryptoStream(fsOrigen, decryptor, CryptoStreamMode.Read);

                    csOrigen.CopyTo(fsDestino);

                    Console.WriteLine($"  OK: {new FileInfo(destino).Length:N0} bytes descifrados");

                    // Verificar que es SQLite
                    fsDestino.Close();
                    var header = new byte[16];
                    using var check = File.OpenRead(destino);
                    check.Read(header, 0, 16);
                    var headerStr = System.Text.Encoding.ASCII.GetString(header);
                    Console.WriteLine($"  Header: {headerStr}");

                    if (headerStr.StartsWith("SQLite format"))
                    {
                        Console.WriteLine($"\n=== EXITO: {destino} ===");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("  No es SQLite, probando siguiente...");
                    }
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"  Crypto error: {ex.Message}, probando siguiente...");
                }
            }
            Console.WriteLine("\nNingún respaldo pudo descifrarse.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
