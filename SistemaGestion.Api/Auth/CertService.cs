using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SistemaGestion.Api.Auth
{
    /// <summary>
    /// Provee un certificado X.509 para Kestrel.
    /// Prioridad:
    ///   1. Variable SG_HTTPS_PFX (+ SG_HTTPS_PWD) — para certs de producción.
    ///   2. Cert auto-firmado persistido en %AppData%\SistemaGestion\sg-https.pfx.
    ///      Se genera la primera vez y se reusa; vida 5 años.
    /// Esto elimina la dependencia de "dotnet dev-certs https --trust" que
    /// suele fallar en Windows y producir "Error inesperado de envio" en TLS.
    /// </summary>
    public static class CertService
    {
        private const string PFX_FILE = "sg-https.pfx";
        // Password fija — el archivo ya esta protegido por la ACL NTFS de
        // %AppData%\SistemaGestion. La contrasena solo evita que Windows
        // muestre el cert como "exportable libremente".
        private const string PFX_PWD  = "SistemaGestionHttps";

        public static X509Certificate2 ObtenerCertificado()
        {
            // (1) PFX externo via env vars — ruta de produccion.
            var pfxEnv = Environment.GetEnvironmentVariable("SG_HTTPS_PFX");
            var pwdEnv = Environment.GetEnvironmentVariable("SG_HTTPS_PWD");
            if (!string.IsNullOrEmpty(pfxEnv) && File.Exists(pfxEnv))
            {
                return new X509Certificate2(pfxEnv, pwdEnv,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            }

            // (2) Cert auto-firmado persistido.
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion");
            Directory.CreateDirectory(carpeta);
            var ruta = Path.Combine(carpeta, PFX_FILE);

            if (File.Exists(ruta))
            {
                try
                {
                    var existente = new X509Certificate2(ruta, PFX_PWD,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                    if (existente.NotAfter > DateTime.UtcNow.AddDays(30))
                        return existente;
                    // Cerca de expirar → regenerar.
                }
                catch { /* corrupto → regenerar */ }
            }

            var nuevo = GenerarAutoFirmado();
            File.WriteAllBytes(ruta, nuevo.Export(X509ContentType.Pfx, PFX_PWD));
            try { new FileInfo(ruta).Attributes |= FileAttributes.Hidden; } catch { }
            // Recargar desde el archivo para que Kestrel obtenga las flags
            // correctas (PersistKeySet evita errores en Windows con keys efimeras).
            return new X509Certificate2(ruta, PFX_PWD,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        private static X509Certificate2 GenerarAutoFirmado()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=SistemaGestion-LAN, O=SistemaGestion, OU=Portal",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Extensiones estandar para un cert TLS de servidor.
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(
                certificateAuthority: false, hasPathLengthConstraint: false,
                pathLengthConstraint: 0, critical: true));

            req.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") /* Server Auth */ },
                critical: false));

            // SAN — todos los hostnames habituales para LAN local.
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddDnsName(Environment.MachineName);
            san.AddDnsName(Environment.MachineName.ToLowerInvariant());
            san.AddIpAddress(System.Net.IPAddress.Loopback);
            san.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
            req.CertificateExtensions.Add(san.Build());

            // 5 anos de vida.
            return req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(5));
        }
    }
}
