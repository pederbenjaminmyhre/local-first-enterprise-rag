-- seed-azure-db.sql — Create minimal DimProduct schema and seed data for Azure demo
-- Azure SQL Database doesn't support .bak restore, so we create the tables directly

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DimProductSubcategory')
BEGIN
    CREATE TABLE dbo.DimProductSubcategory (
        ProductSubcategoryKey INT PRIMARY KEY,
        EnglishProductSubcategoryName NVARCHAR(50) NOT NULL
    );

    INSERT INTO dbo.DimProductSubcategory VALUES
    (1, 'Mountain Bikes'), (2, 'Road Bikes'), (3, 'Touring Bikes'),
    (4, 'Handlebars'), (5, 'Wheels'), (6, 'Saddles'),
    (7, 'Tires and Tubes'), (8, 'Helmets'), (9, 'Jerseys'),
    (10, 'Shorts'), (11, 'Gloves'), (12, 'Caps');

    PRINT 'Created DimProductSubcategory with 12 rows.';
END
ELSE PRINT 'DimProductSubcategory already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DimProduct')
BEGIN
    CREATE TABLE dbo.DimProduct (
        ProductKey INT PRIMARY KEY,
        EnglishProductName NVARCHAR(200) NOT NULL,
        EnglishDescription NVARCHAR(MAX) NULL,
        ProductSubcategoryKey INT NULL,
        Color NVARCHAR(15) NULL,
        DescriptionVector VECTOR(1024) NULL,
        CONSTRAINT FK_Product_Subcategory FOREIGN KEY (ProductSubcategoryKey) REFERENCES dbo.DimProductSubcategory(ProductSubcategoryKey)
    );

    INSERT INTO dbo.DimProduct (ProductKey, EnglishProductName, EnglishDescription, ProductSubcategoryKey, Color) VALUES
    (1, 'Mountain-100 Black, 44', 'Our top-of-the-line competition mountain bike. Performance-enhancing options include the innovative HL Frame, super-smooth front suspension, and traction for all terrain.', 1, 'Black'),
    (2, 'Mountain-100 Silver, 38', 'Our top-of-the-line competition mountain bike. Performance-enhancing options include the innovative HL Frame, super-smooth front suspension, and traction for all terrain.', 1, 'Silver'),
    (3, 'Mountain-200 Black, 38', 'Serious contender in the mountain bike category. Capable of smooth handling on the toughest single-track. A true all-around performer.', 1, 'Black'),
    (4, 'Mountain-200 Silver, 42', 'Serious contender in the mountain bike category. Capable of smooth handling on the toughest single-track. A true all-around performer.', 1, 'Silver'),
    (5, 'Mountain-300 Black, 40', 'For true trail addicts. An extremely durable bike that will go anywhere and keep you in control on challenging terrain - Loss of traction is a thing of the past.', 1, 'Black'),
    (6, 'Road-150 Red, 62', 'This bike is ridden by race winners. Extremely light and target weight is achieved by shaving weight from all components.', 2, 'Red'),
    (7, 'Road-150 Red, 56', 'This bike is ridden by race winners. Extremely light and target weight is achieved by shaving weight from all components.', 2, 'Red'),
    (8, 'Road-250 Black, 48', 'Alluminum-alloy frame provides a light, stiff ride, whether you are racing in the velodrome or on the road.', 2, 'Black'),
    (9, 'Road-250 Red, 52', 'Alluminum-alloy frame provides a light, stiff ride, whether you are racing in the velodrome or on the road.', 2, 'Red'),
    (10, 'Road-350-W Yellow, 48', 'Cross-trainer for the weekend warrior. High-quality components and aerodynamic design combine for a smooth ride.', 2, 'Yellow'),
    (11, 'Touring-1000 Blue, 46', 'Travel in style and comfort. Designed for maximum versatility. It excels on long-distance road touring.', 3, 'Blue'),
    (12, 'Touring-1000 Yellow, 50', 'Travel in style and comfort. Designed for maximum versatility. It excels on long-distance road touring.', 3, 'Yellow'),
    (13, 'Touring-2000 Blue, 54', 'Everything you need for a smooth ride. Well-designed aluminum frame provides a durable, comfortable ride.', 3, 'Blue'),
    (14, 'Touring-3000 Blue, 44', 'All-occasion value bike with many features. Has a comfortable ride and easy handling and low maintenance.', 3, 'Blue'),
    (15, 'Touring-3000 Yellow, 50', 'All-occasion value bike with many features. Has a comfortable ride and easy handling and low maintenance.', 3, 'Yellow'),
    (16, 'HL Mountain Handlebars', 'Tough, lightweight aluminum handlebar for mountain riders. Designed for all-mountain riding.', 4, NULL),
    (17, 'HL Road Handlebars', 'Ergonomic aluminum road handlebar. Lightweight and aerodynamic.', 4, NULL),
    (18, 'HL Mountain Tire', 'Unmatched traction on dry or wet terrain. Knobby tread pattern provides excellent grip.', 7, 'Black'),
    (19, 'HL Road Tire', 'High-performance clincher tire with aramid bead for enhanced durability and lower rolling resistance.', 7, 'Black'),
    (20, 'Sport-100 Helmet, Red', 'Universal fit, well-ventilated, lightweight helmet. BikeReview.com gave it 4 stars.', 8, 'Red'),
    (21, 'Sport-100 Helmet, Black', 'Universal fit, well-ventilated, lightweight helmet. BikeReview.com gave it 4 stars.', 8, 'Black'),
    (22, 'Sport-100 Helmet, Blue', 'Universal fit, well-ventilated, lightweight helmet. BikeReview.com gave it 4 stars.', 8, 'Blue'),
    (23, 'Long-Sleeve Logo Jersey, L', 'Unisex long-sleeve logo jersey, 100% breathable polyester with moisture-wicking fabric.', 9, 'Multi'),
    (24, 'Short-Sleeve Classic Jersey, M', 'Short sleeve classic jersey with a three-button placket front, matching collar and cuffs.', 9, 'Yellow'),
    (25, 'Half-Finger Gloves, M', 'Synthetic leather palm and target padding maximize comfort and minimize fatigue during long rides.', 11, 'Black');

    PRINT 'Created DimProduct with 25 sample products.';
END
ELSE PRINT 'DimProduct already exists.';
GO
