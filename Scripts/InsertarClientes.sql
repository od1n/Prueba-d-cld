-- ═════════════════════════════════════════════════════════════════════════════
-- Script para insertar 10 clientes de prueba
-- Ejecutar en SQLite después de crear la tabla de Clientes
-- ═════════════════════════════════════════════════════════════════════════════

INSERT INTO Clientes (Nombre, Apellido, Email, Telefono, Direccion, Ciudad, Estado, CodigoPostal, TipoCliente, FechaRegistro, Activo)
VALUES 
('Juan', 'García López', 'juan.garcia@email.com', '555-0101', 'Calle Principal 123', 'Madrid', 'Madrid', '28001', 'Premium', CURRENT_DATE, 1),
('María', 'Rodríguez Pérez', 'maria.rodriguez@email.com', '555-0102', 'Avenida Central 456', 'Barcelona', 'Barcelona', '08002', 'Regular', CURRENT_DATE, 1),
('Carlos', 'Martínez Silva', 'carlos.martinez@email.com', '555-0103', 'Plaza Mayor 789', 'Valencia', 'Valencia', '46001', 'Premium', CURRENT_DATE, 1),
('Ana', 'López Fernández', 'ana.lopez@email.com', '555-0104', 'Calle del Comercio 321', 'Sevilla', 'Sevilla', '41001', 'Regular', CURRENT_DATE, 1),
('Luis', 'González Ruiz', 'luis.gonzalez@email.com', '555-0105', 'Avenida del Sol 654', 'Bilbao', 'Vizcaya', '48001', 'Premium', CURRENT_DATE, 1),
('Elena', 'Sánchez Torres', 'elena.sanchez@email.com', '555-0106', 'Calle San Juan 987', 'Málaga', 'Málaga', '29001', 'Regular', CURRENT_DATE, 1),
('Roberto', 'Díaz Gómez', 'roberto.diaz@email.com', '555-0107', 'Plaza de la República 135', 'Murcia', 'Murcia', '30001', 'Premium', CURRENT_DATE, 1),
('Isabel', 'Moreno Castillo', 'isabel.moreno@email.com', '555-0108', 'Calle Nueva 246', 'Palma', 'Islas Baleares', '07001', 'Regular', CURRENT_DATE, 1),
('Francisco', 'Jiménez Alonso', 'francisco.jimenez@email.com', '555-0109', 'Avenida de la Paz 357', 'Alicante', 'Alicante', '03001', 'Premium', CURRENT_DATE, 1),
('Carmen', 'Romero Navarro', 'carmen.romero@email.com', '555-0110', 'Calle del Río 468', 'Córdoba', 'Córdoba', '14001', 'Regular', CURRENT_DATE, 1);

-- Verificar que los clientes fueron insertados
SELECT COUNT(*) as 'Total Clientes Insertados' FROM Clientes;
SELECT * FROM Clientes ORDER BY FechaRegistro DESC LIMIT 10;
