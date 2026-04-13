using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    /// <summary>
    /// Helper para cargar datos de prueba (10 clientes) en la base de datos
    /// </summary>
    public static class CargarClientesHelper
    {
        /// <summary>
        /// Inserta 10 clientes de prueba en la base de datos
        /// </summary>
        public static void InsertarClientesDePrueba(string connectionString)
        {
            var clientes = new List<(string nombre, string apellido, string email, string telefono, 
                string direccion, string ciudad, string estado, string codigoPostal, string tipoCliente)>
            {
                ("Juan", "García López", "juan.garcia@email.com", "555-0101", "Calle Principal 123", "Madrid", "Madrid", "28001", "Premium"),
                ("María", "Rodríguez Pérez", "maria.rodriguez@email.com", "555-0102", "Avenida Central 456", "Barcelona", "Barcelona", "08002", "Regular"),
                ("Carlos", "Martínez Silva", "carlos.martinez@email.com", "555-0103", "Plaza Mayor 789", "Valencia", "Valencia", "46001", "Premium"),
                ("Ana", "López Fernández", "ana.lopez@email.com", "555-0104", "Calle del Comercio 321", "Sevilla", "Sevilla", "41001", "Regular"),
                ("Luis", "González Ruiz", "luis.gonzalez@email.com", "555-0105", "Avenida del Sol 654", "Bilbao", "Vizcaya", "48001", "Premium"),
                ("Elena", "Sánchez Torres", "elena.sanchez@email.com", "555-0106", "Calle San Juan 987", "Málaga", "Málaga", "29001", "Regular"),
                ("Roberto", "Díaz Gómez", "roberto.diaz@email.com", "555-0107", "Plaza de la República 135", "Murcia", "Murcia", "30001", "Premium"),
                ("Isabel", "Moreno Castillo", "isabel.moreno@email.com", "555-0108", "Calle Nueva 246", "Palma", "Islas Baleares", "07001", "Regular"),
                ("Francisco", "Jiménez Alonso", "francisco.jimenez@email.com", "555-0109", "Avenida de la Paz 357", "Alicante", "Alicante", "03001", "Premium"),
                ("Carmen", "Romero Navarro", "carmen.romero@email.com", "555-0110", "Calle del Río 468", "Córdoba", "Córdoba", "14001", "Regular")
            };

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var (nombre, apellido, email, telefono, direccion, ciudad, estado, codigoPostal, tipoCliente) in clientes)
            {
                var sql = @"INSERT INTO Clientes (Nombre, Apellido, Email, Telefono, Direccion, Ciudad, Estado, CodigoPostal, TipoCliente, FechaRegistro, Activo)
                           VALUES (@nombre, @apellido, @email, @telefono, @direccion, @ciudad, @estado, @codigoPostal, @tipoCliente, @fecha, 1)";

                using var cmd = new SqliteCommand(sql, connection);
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@apellido", apellido);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@telefono", telefono);
                cmd.Parameters.AddWithValue("@direccion", direccion);
                cmd.Parameters.AddWithValue("@ciudad", ciudad);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@codigoPostal", codigoPostal);
                cmd.Parameters.AddWithValue("@tipoCliente", tipoCliente);
                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd"));

                cmd.ExecuteNonQuery();
            }

            connection.Close();
        }

        /// <summary>
        /// Verifica si los clientes ya existen en la base de datos
        /// </summary>
        public static int ContarClientesExistentes(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var sql = "SELECT COUNT(*) FROM Clientes";
            using var cmd = new SqliteCommand(sql, connection);
            var result = cmd.ExecuteScalar();

            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}
