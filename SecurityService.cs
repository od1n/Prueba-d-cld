using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace SistemaGestion
{
    /// <summary>
    /// Centraliza toda la seguridad de archivos del sistema:
    ///   1. Ruta canónica de la BD en %AppData% (fuera del directorio del exe)
    ///   2. DPAPI: cifra/descifra la cadena de conexión con la clave del usuario Windows
    ///   3. AES-256-CBC: cifra/descifra los archivos de respaldo
    ///   4. NTFS ACL: restringe el directorio de datos solo al usuario actual
    /// </summary>
    public static partial class SecurityService
    {
        // ── Rutas ──────────────────────────────────────────────────────────
        private const string APP_FOLDER  = "SistemaGestion";
        private const string DB_FILENAME = "sistema_gestion.db";
        private const string KEY_FILENAME = "sg.key";   // clave AES cifrada con DPAPI

        /// <summary>Carpeta de datos: %AppData%\SistemaGestion\</summary>
        public static string CarpetaDatos =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                APP_FOLDER);

        /// <summary>
        /// Ruta completa de la base de datos.
        /// Si la variable de entorno SG_DB_PATH está definida se usa esa ruta
        /// (permite compartir la BD en una carpeta de red).
        /// </summary>
        public static string RutaBD =>
            Environment.GetEnvironmentVariable("SG_DB_PATH")
            ?? Path.Combine(CarpetaDatos, DB_FILENAME);

        /// <summary>Cadena de conexión SQLite lista para usar.</summary>
        public static string CadenaConexion =>
            $"Data Source={RutaBD};";

        // ── Inicialización ─────────────────────────────────────────────────
        /// <summary>
        /// Prepara el entorno de seguridad al arrancar la app:
        ///   - Crea la carpeta de datos si no existe
        ///   - Aplica permisos NTFS restrictivos
        ///   - Migra la BD si todavía está en el directorio del ejecutable
        ///   - Genera la clave AES si no existe
        /// </summary>
        public static void Inicializar()
        {
            Directory.CreateDirectory(CarpetaDatos);
            AplicarPermisosNTFS(CarpetaDatos);
            MigrarBDSiNecesario();
            EnsureAesKey();
        }

        // ── Migración de BD existente ──────────────────────────────────────
        private static void MigrarBDSiNecesario()
        {
            // BD vieja junto al ejecutable
            string bdVieja = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, DB_FILENAME);

            if (File.Exists(bdVieja) && !File.Exists(RutaBD))
            {
                File.Move(bdVieja, RutaBD);

                // Mover también WAL y SHM si existen
                foreach (var ext in new[] { "-wal", "-shm" })
                {
                    string src = bdVieja + ext;
                    if (File.Exists(src))
                        File.Move(src, RutaBD + ext);
                }
            }
        }

        // ── Permisos NTFS ──────────────────────────────────────────────────
        /// <summary>
        /// Restringe la carpeta de datos al usuario actual y al grupo
        /// Administrators. Cualquier otro usuario del sistema no puede leer
        /// el archivo .db aunque tenga acceso físico a la máquina.
        /// </summary>
        private static void AplicarPermisosNTFS(string ruta)
        {
            try
            {
                var info   = new DirectoryInfo(ruta);
                var acl    = info.GetAccessControl();
                var usuario = WindowsIdentity.GetCurrent().Name;

                // Deshabilitar herencia
                acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

                // Permiso completo solo al usuario actual
                acl.AddAccessRule(new FileSystemAccessRule(
                    usuario,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                // Permiso completo al grupo Administrators
                acl.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                // SYSTEM necesita acceso para que Windows funcione correctamente
                acl.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                info.SetAccessControl(acl);
            }
            catch
            {
                // Si falla (ej. sistema de archivos sin soporte ACL) continuar
            }
        }

        // ── DPAPI: proteger cadenas sensibles ──────────────────────────────
        /// <summary>
        /// Cifra un texto con DPAPI (clave derivada de las credenciales
        /// del usuario Windows actual). Solo ese usuario puede descifrar.
        /// </summary>
        public static string DpapiCifrar(string texto)
        {
            byte[] datos    = Encoding.UTF8.GetBytes(texto);
            byte[] cifrado  = ProtectedData.Protect(
                datos, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cifrado);
        }

        /// <summary>Descifra un texto previamente cifrado con DpapiCifrar.</summary>
        public static string DpapiDescifrar(string base64)
        {
            byte[] cifrado  = Convert.FromBase64String(base64);
            byte[] descifrado = ProtectedData.Unprotect(
                cifrado, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(descifrado);
        }

        // ── AES-256: cifrado de respaldos ──────────────────────────────────
        private static readonly string RutaClave =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                APP_FOLDER, KEY_FILENAME);

        /// <summary>Genera y persiste la clave AES si no existe.</summary>
        private static void EnsureAesKey()
        {
            if (File.Exists(RutaClave)) return;

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();

            // Proteger la clave con DPAPI antes de guardarla en disco
            byte[] claveCifrada = ProtectedData.Protect(
                aes.Key, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(RutaClave, claveCifrada);
        }

        private static byte[] CargarClave()
        {
            byte[] claveCifrada = File.ReadAllBytes(RutaClave);
            return ProtectedData.Unprotect(
                claveCifrada, null, DataProtectionScope.CurrentUser);
        }

        /// <summary>
        /// Cifra un archivo de respaldo (.db) y produce un archivo .db.enc.
        /// Usa AES-256-CBC con IV aleatorio prepended al archivo.
        /// </summary>
        public static string CifrarRespaldo(string rutaOrigen)
        {
            string rutaDestino = rutaOrigen + ".enc";
            byte[] clave       = CargarClave();

            using var aes = Aes.Create();
            aes.Key     = clave;
            aes.KeySize = 256;
            aes.GenerateIV();   // IV aleatorio por archivo

            using var fsOrigen  = new FileStream(rutaOrigen,  FileMode.Open,   FileAccess.Read);
            using var fsDestino = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);

            // Escribir IV al inicio del archivo cifrado (16 bytes)
            fsDestino.Write(aes.IV, 0, aes.IV.Length);

            using var encryptor = aes.CreateEncryptor();
            using var csDestino = new CryptoStream(fsDestino, encryptor, CryptoStreamMode.Write);

            fsOrigen.CopyTo(csDestino);

            return rutaDestino;
        }

        /// <summary>
        /// Descifra un archivo .db.enc y produce el .db original.
        /// Devuelve la ruta del archivo descifrado.
        /// </summary>
        public static string DescifrarRespaldo(string rutaEnc, string rutaDestino = null)
        {
            rutaDestino ??= rutaEnc.Replace(".db.enc", "_descifrado.db");
            byte[] clave = CargarClave();

            using var fsOrigen  = new FileStream(rutaEnc,     FileMode.Open,   FileAccess.Read);
            using var fsDestino = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);

            // Leer IV (primeros 16 bytes)
            byte[] iv = new byte[16];
            fsOrigen.Read(iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = clave;
            aes.IV  = iv;

            using var decryptor = aes.CreateDecryptor();
            using var csOrigen  = new CryptoStream(fsOrigen, decryptor, CryptoStreamMode.Read);

            csOrigen.CopyTo(fsDestino);

            return rutaDestino;
        }

        // ── Verificar integridad ───────────────────────────────────────────
        /// <summary>
        /// Calcula el hash SHA-256 de un archivo.
        /// Usar para verificar que un respaldo no fue alterado.
        /// </summary>
        public static string CalcularHash(string ruta)
        {
            using var sha = SHA256.Create();
            using var fs  = File.OpenRead(ruta);
            byte[] hash   = sha.ComputeHash(fs);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>Guarda el hash de un archivo en un .sha256 junto a él.</summary>
        public static void GuardarHash(string rutaArchivo)
        {
            string hash = CalcularHash(rutaArchivo);
            File.WriteAllText(rutaArchivo + ".sha256", hash);
        }

        /// <summary>
        /// Verifica que el hash actual coincide con el guardado.
        /// Devuelve true si el archivo es íntegro.
        /// </summary>
        public static bool VerificarHash(string rutaArchivo)
        {
            string rutaHash = rutaArchivo + ".sha256";
            if (!File.Exists(rutaHash)) return false;

            s