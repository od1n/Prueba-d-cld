// Script C# para insertar datos de prueba en la BD
// Ejecutar: dotnet script insertar-prueba.csx
// O compilar como consola si dotnet-script no esta instalado

using System;
using System.IO;

// Buscar la BD
string[] rutas = {
    @"C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db",
    @"C:\SistemaGestion\sistema_gestion.db"
};
string dbPath = null;
foreach (var r in rutas) if (File.Exists(r)) { dbPath = r; break; }
if (dbPath == null) { Console.WriteLine("ERROR: No se encontro la BD"); return; }
Console.WriteLine($"BD: {dbPath}");

// Conectar via Microsoft.Data.Sqlite
var connStr = $"Data Source={dbPath}";
using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
conn.Open();

void Exec(string sql) {
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    try { cmd.ExecuteNonQuery(); Console.WriteLine($"  OK: {sql[..Math.Min(60,sql.Length)]}..."); }
    catch (Exception ex) { Console.WriteLine($"  SKIP: {ex.Message}"); }
}

// Productos de prueba
Console.WriteLine("\n=== INSERTANDO PRODUCTOS ===");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-001','Tubo PVC 1/2 pulg','General','UND',500,50,1000,8.50,5.00,'2026-06-10')");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-002','Codo PVC 1/2 pulg','General','UND',300,30,600,3.25,1.80,'2026-06-10')");
Exec("INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-003','Tee PVC 1/2 pulg','General','UND',200,20,400,4.75,2.50,'2026-06-10')");

Console.WriteLine("\n=== LISTO ===");
Console.WriteLine("Ahora ejecute el script crear-datos-prueba.ps1 de nuevo.");
conn.Close();
