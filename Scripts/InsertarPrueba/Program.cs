using Microsoft.Data.Sqlite;

string[] rutas = [
    @"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db",
    @"C:\SistemaGestion\sistema_gestion.db"
];

string? dbPath = rutas.FirstOrDefault(File.Exists);
if (dbPath == null) { Console.WriteLine("ERROR: BD no encontrada"); return; }
Console.WriteLine($"BD: {dbPath}");

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

void Exec(string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    try { cmd.ExecuteNonQuery(); Console.WriteLine($"  OK: {sql[..Math.Min(60, sql.Length)]}"); }
    catch (Exception ex) { Console.WriteLine($"  SKIP: {ex.Message[..Math.Min(80, ex.Message.Length)]}"); }
}

Console.WriteLine("\n=== INSERTANDO PRODUCTOS DE PRUEBA ===");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz,Activo) VALUES ('TEST-001','Tubo PVC 1/2 pulg','General','UND',500,50,1000,8.50,5.00,'2026-06-10',1)");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz,Activo) VALUES ('TEST-002','Codo PVC 1/2 pulg','General','UND',300,30,600,3.25,1.80,'2026-06-10',1)");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz,Activo) VALUES ('TEST-003','Tee PVC 1/2 pulg','General','UND',200,20,400,4.75,2.50,'2026-06-10',1)");

// Verificar
using var chk = conn.CreateCommand();
chk.CommandText = "SELECT COUNT(*) FROM Inventario WHERE Codigo LIKE 'TEST-%'";
var count = (long)(chk.ExecuteScalar() ?? 0);
Console.WriteLine($"\nProductos TEST en BD: {count}");
Console.WriteLine("Ahora ejecute crear-datos-prueba.ps1 de nuevo.");
