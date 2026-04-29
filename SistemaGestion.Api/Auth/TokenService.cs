using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SistemaGestion.Api.Auth
{
    public static class TokenService
    {
        // ── Persistencia de la clave JWT ─────────────────────────────────────
        // Antes la clave se generaba en memoria en cada arranque, lo que
        // invalidaba TODAS las sesiones al reiniciar el servicio. Ahora se
        // persiste cifrada con DPAPI (CurrentUser) en %AppData%\SistemaGestion,
        // junto al resto de material criptográfico del sistema.
        private const string KEY_FILE = "jwt.key";
        private const int    HORAS    = 8;

        private static readonly byte[] _claveBytes = ObtenerOCrearClave();
        private static readonly SymmetricSecurityKey _signingKey = new(_claveBytes);

        private static string RutaClave =>
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion", KEY_FILE);

        private static byte[] ObtenerOCrearClave()
        {
            var path = RutaClave;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

            if (System.IO.File.Exists(path))
            {
                try
                {
                    var enc = System.IO.File.ReadAllBytes(path);
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser)
                        : enc;
                }
                catch { /* clave corrupta o cambio de usuario → regenerar */ }
            }

            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);

            var protegida = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)
                : bytes;
            System.IO.File.WriteAllBytes(path, protegida);

            // Restringir lectura del archivo de clave
            try
            {
                var fi = new System.IO.FileInfo(path);
                fi.Attributes |= System.IO.FileAttributes.Hidden;
            }
            catch { }

            return bytes;
        }

        public static SymmetricSecurityKey ObtenerClaveSeguridad() => _signingKey;

        /// <summary>
        /// Genera un JWT con jti (JWT ID) único — necesario para revocación.
        /// Devuelve el token y el jti para que el caller lo registre en la
        /// tabla SesionesActivas.
        /// </summary>
        public static (string token, string jti) GenerarToken(int id, string usuario, string nombre, string rol)
        {
            var jti = Guid.NewGuid().ToString("N");
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    System.Security.Claims.ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name,           usuario),
                new Claim("NombreCompleto",          nombre),
                new Claim(ClaimTypes.Role,           rol),
            };
            var creds = new SigningCredentials(ObtenerClaveSeguridad(), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer:   "SistemaGestion.Api",
                audience: "SistemaGestion.Portal",
                claims:   claims,
                expires:  DateTime.UtcNow.AddHours(HORAS),
                signingCredentials: creds);
            return (new JwtSecurityTokenHandler().WriteToken(token), jti);
        }

        public static string ObtenerJti(ClaimsPrincipal u) =>
            u.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "";

        public static int ObtenerIdUsuario(ClaimsPrincipal u) =>
            int.TryParse(u.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        public static string ObtenerNombreUsuario(ClaimsPrincipal u) =>
            u.FindFirst(ClaimTypes.Name)?.Value ?? "";
        public static string ObtenerRol(ClaimsPrincipal u) =>
            u.FindFirst(ClaimTypes.Role)?.Value ?? "";
        public static bool EsAdmin(ClaimsPrincipal u) => ObtenerRol(u) == "Admin";
        public static bool PuedeEscribirKardex(ClaimsPrincipal u) =>
            ObtenerRol(u) is "Admin" or "Almacen";
        public static bool PuedeAjustarKardex(ClaimsPrincipal u) => EsAdmin(u);
        public static bool PuedeVerCxC(ClaimsPrincipal u) =>
            ObtenerRol(u) is "Admin" or "Ventas" or "Contabilidad";
    }
}
