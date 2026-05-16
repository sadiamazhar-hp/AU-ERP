-- ============================================================
-- AU-ERP Tile Manufacturing Seed Data
-- Run against: AU_Proj on .\SQLEXPRESS
-- Idempotent: safe to run multiple times
-- ============================================================
USE [AU_Proj];
SET NOCOUNT ON;
GO

-- ============================================================
-- 1. Unit of Measurements
-- ============================================================
MERGE [dbo].[UnitOfMeasurements] AS tgt
USING (VALUES
    (1, 'BOX',  'Box'),
    (2, 'SQM',  'Square Meter'),
    (3, 'KG',   'Kilogram'),
    (4, 'BAG',  'Bag (50 kg)'),
    (5, 'TON',  'Metric Ton'),
    (6, 'PCS',  'Pieces')
) AS src(Id, Code, Description)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Code = src.Code, tgt.Description = src.Description
WHEN NOT MATCHED THEN INSERT (Id, Code, Description) VALUES (src.Id, src.Code, src.Description);
GO

-- ============================================================
-- 2. Material Types (FERT / ROH may already be seeded by migrations)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialTypes] WHERE [MaterialTypeCode] = 'FERT')
    INSERT INTO [dbo].[MaterialTypes] ([MaterialTypeCode],[Description],[EnabledTabsJson])
    VALUES ('FERT','Finished Product','[]');

IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialTypes] WHERE [MaterialTypeCode] = 'ROH')
    INSERT INTO [dbo].[MaterialTypes] ([MaterialTypeCode],[Description],[EnabledTabsJson])
    VALUES ('ROH','Raw Material','[]');
GO

-- ============================================================
-- 3. Material Masters
-- ============================================================
-- FERT – finished tile products
MERGE [dbo].[CreateMaterialMaster] AS tgt
USING (VALUES
    ('TILE-32X32','FERT','32x32 Ceramic Floor Tile',    'BOX', 850.0000, 750.0000, 650.0000),
    ('TILE-24X24','FERT','24x24 Ceramic Wall Tile',     'BOX', 680.0000, 600.0000, 520.0000),
    ('TILE-18X18','FERT','18x18 Vitrified Glazed Tile', 'BOX', 520.0000, 460.0000, 400.0000)
) AS src(MaterialNumber, MaterialTypeCode, Description, BaseUnitCode,
         SalesPriceGradeAPerBaseUom, SalesPriceGradeBPerBaseUom, SalesPriceGradeCPerBaseUom)
ON tgt.MaterialNumber = src.MaterialNumber
WHEN MATCHED THEN UPDATE SET
    tgt.Description = src.Description,
    tgt.SalesPriceGradeAPerBaseUom = src.SalesPriceGradeAPerBaseUom,
    tgt.SalesPriceGradeBPerBaseUom = src.SalesPriceGradeBPerBaseUom,
    tgt.SalesPriceGradeCPerBaseUom = src.SalesPriceGradeCPerBaseUom
WHEN NOT MATCHED THEN INSERT
    (MaterialNumber, MaterialTypeCode, Description, BaseUnitCode,
     SalesPriceGradeAPerBaseUom, SalesPriceGradeBPerBaseUom, SalesPriceGradeCPerBaseUom)
VALUES (src.MaterialNumber, src.MaterialTypeCode, src.Description, src.BaseUnitCode,
        src.SalesPriceGradeAPerBaseUom, src.SalesPriceGradeBPerBaseUom, src.SalesPriceGradeCPerBaseUom);

-- ROH – raw materials
MERGE [dbo].[CreateMaterialMaster] AS tgt
USING (VALUES
    ('CLAY-01',   'ROH','Raw Clay',         'TON', NULL, NULL, NULL),
    ('CEMENT-01', 'ROH','Portland Cement',  'BAG', NULL, NULL, NULL),
    ('GLAZE-01',  'ROH','Ceramic Glaze',    'KG',  NULL, NULL, NULL)
) AS src(MaterialNumber, MaterialTypeCode, Description, BaseUnitCode,
         SalesPriceGradeAPerBaseUom, SalesPriceGradeBPerBaseUom, SalesPriceGradeCPerBaseUom)
ON tgt.MaterialNumber = src.MaterialNumber
WHEN MATCHED THEN UPDATE SET tgt.Description = src.Description
WHEN NOT MATCHED THEN INSERT
    (MaterialNumber, MaterialTypeCode, Description, BaseUnitCode,
     SalesPriceGradeAPerBaseUom, SalesPriceGradeBPerBaseUom, SalesPriceGradeCPerBaseUom)
VALUES (src.MaterialNumber, src.MaterialTypeCode, src.Description, src.BaseUnitCode,
        src.SalesPriceGradeAPerBaseUom, src.SalesPriceGradeBPerBaseUom, src.SalesPriceGradeCPerBaseUom);
GO

-- ============================================================
-- 4. Production Orders
-- ============================================================
SET IDENTITY_INSERT [dbo].[ProductionOrders] ON;
MERGE [dbo].[ProductionOrders] AS tgt
USING (VALUES
    (101, 40001, 'PO-40001', 'TILE-32X32', 1000, 1, '2026-04-01', '2026-04-20', 'High',     'Completed', GETDATE()),
    (102, 40002, 'PO-40002', 'TILE-24X24',  500, 1, '2026-04-10', '2026-04-30', 'Medium',   'Completed', GETDATE()),
    (103, 40003, 'PO-40003', 'TILE-18X18',  800, 1, '2026-05-01', '2026-05-20', 'High',     'InProgress', GETDATE()),
    (104, 40004, 'PO-40004', 'TILE-32X32',  600, 1, '2026-05-10', '2026-05-30', 'Medium',   'Released',   GETDATE()),
    (105, 40005, 'PO-40005', 'TILE-24X24',  400, 1, '2026-03-01', '2026-03-18', 'Low',      'Completed',  GETDATE())
) AS src(Id, ProductionNumber, ProductionDocumentNumber, FinishedMaterialNumber,
         TargetQuantity, UomId, PlannedStartDate, PlannedEndDate, Priority, Status, CreatedAt)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Status = src.Status
WHEN NOT MATCHED THEN INSERT
    (Id, ProductionNumber, ProductionDocumentNumber, FinishedMaterialNumber,
     TargetQuantity, UomId, PlannedStartDate, PlannedEndDate, Priority, Status, CreatedAt)
VALUES (src.Id, src.ProductionNumber, src.ProductionDocumentNumber, src.FinishedMaterialNumber,
        src.TargetQuantity, src.UomId, src.PlannedStartDate, src.PlannedEndDate,
        src.Priority, src.Status, src.CreatedAt);
SET IDENTITY_INSERT [dbo].[ProductionOrders] OFF;
GO

-- ============================================================
-- 5. Goods Produce Batches (production receipts)
-- ============================================================
SET IDENTITY_INSERT [dbo].[GoodsProduceBatches] ON;
MERGE [dbo].[GoodsProduceBatches] AS tgt
USING (VALUES
    -- PO 101 – TILE-32X32, 3 batches across April
    (201, 101, 'TILE-32X32', 1, '2026-04-08', 350.0, 310.0, 25.0, 10.0, 5.0,  'BATCH-32X32-001', GETDATE()),
    (202, 101, 'TILE-32X32', 1, '2026-04-15', 380.0, 340.0, 28.0,  8.0, 4.0,  'BATCH-32X32-002', GETDATE()),
    (203, 101, 'TILE-32X32', 1, '2026-04-19', 270.0, 240.0, 20.0,  6.0, 4.0,  'BATCH-32X32-003', GETDATE()),
    -- PO 102 – TILE-24X24
    (204, 102, 'TILE-24X24', 1, '2026-04-18', 260.0, 230.0, 18.0,  8.0, 4.0,  'BATCH-24X24-001', GETDATE()),
    (205, 102, 'TILE-24X24', 1, '2026-04-28', 240.0, 210.0, 20.0,  7.0, 3.0,  'BATCH-24X24-002', GETDATE()),
    -- PO 103 – TILE-18X18, in-progress (batches in May)
    (206, 103, 'TILE-18X18', 1, '2026-05-05', 200.0, 178.0, 14.0,  5.0, 3.0,  'BATCH-18X18-001', GETDATE()),
    (207, 103, 'TILE-18X18', 1, '2026-05-12', 220.0, 195.0, 16.0,  6.0, 3.0,  'BATCH-18X18-002', GETDATE()),
    -- PO 105 – TILE-24X24 March
    (208, 105, 'TILE-24X24', 1, '2026-03-10', 200.0, 178.0, 14.0,  5.0, 3.0,  'BATCH-24X24-M01', GETDATE()),
    (209, 105, 'TILE-24X24', 1, '2026-03-17', 200.0, 175.0, 16.0,  6.0, 3.0,  'BATCH-24X24-M02', GETDATE())
) AS src(Id, ProductionOrderId, MaterialNumber, UomId, GrDate,
         ProducedQty, QtyFirstQuality, QtySecondQuality, QtyThirdQuality, RejectedScrapQty,
         BatchNo, CreatedAt)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.ProducedQty = src.ProducedQty
WHEN NOT MATCHED THEN INSERT
    (Id, ProductionOrderId, MaterialNumber, UomId, GrDate,
     ProducedQty, QtyFirstQuality, QtySecondQuality, QtyThirdQuality, RejectedScrapQty,
     BatchNo, CreatedAt)
VALUES (src.Id, src.ProductionOrderId, src.MaterialNumber, src.UomId, src.GrDate,
        src.ProducedQty, src.QtyFirstQuality, src.QtySecondQuality, src.QtyThirdQuality,
        src.RejectedScrapQty, src.BatchNo, src.CreatedAt);
SET IDENTITY_INSERT [dbo].[GoodsProduceBatches] OFF;
GO

-- ============================================================
-- 6. Goods Issue Documents (raw material consumption)
-- ============================================================
SET IDENTITY_INSERT [dbo].[GoodsIssueDocuments] ON;
MERGE [dbo].[GoodsIssueDocuments] AS tgt
USING (VALUES
    (301, 101, '2026-04-08', 'GI-40001', 'Completed'),
    (302, 102, '2026-04-18', 'GI-40002', 'Completed'),
    (303, 103, '2026-05-05', 'GI-40003', 'Completed'),
    (304, 105, '2026-03-10', 'GI-40005', 'Completed')
) AS src(Id, ProductionOrderId, DocumentDate, DocumentNumber, Status)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Status = src.Status
WHEN NOT MATCHED THEN INSERT (Id, ProductionOrderId, DocumentDate, DocumentNumber, Status)
VALUES (src.Id, src.ProductionOrderId, src.DocumentDate, src.DocumentNumber, src.Status);
SET IDENTITY_INSERT [dbo].[GoodsIssueDocuments] OFF;
GO

-- ============================================================
-- 7. Goods Issue Document Lines
-- ============================================================
SET IDENTITY_INSERT [dbo].[GoodsIssueDocumentLines] ON;
MERGE [dbo].[GoodsIssueDocumentLines] AS tgt
USING (VALUES
    -- GI-40001 (PO 101 / TILE-32X32)
    (401, 301, 'CLAY-01',   'Raw Clay',        'TILE-32X32', 5, 50.0, 50.0, 0.0),
    (402, 301, 'CEMENT-01', 'Portland Cement', 'TILE-32X32', 4, 80.0, 80.0, 0.0),
    (403, 301, 'GLAZE-01',  'Ceramic Glaze',   'TILE-32X32', 3, 20.0, 20.0, 0.0),
    -- GI-40002 (PO 102 / TILE-24X24)
    (404, 302, 'CLAY-01',   'Raw Clay',        'TILE-24X24', 5, 28.0, 28.0, 0.0),
    (405, 302, 'CEMENT-01', 'Portland Cement', 'TILE-24X24', 4, 45.0, 45.0, 0.0),
    (406, 302, 'GLAZE-01',  'Ceramic Glaze',   'TILE-24X24', 3, 12.0, 12.0, 0.0),
    -- GI-40003 (PO 103 / TILE-18X18 partial)
    (407, 303, 'CLAY-01',   'Raw Clay',        'TILE-18X18', 5, 22.0, 22.0, 0.0),
    (408, 303, 'CEMENT-01', 'Portland Cement', 'TILE-18X18', 4, 36.0, 36.0, 0.0),
    -- GI-40005 (PO 105 / TILE-24X24 March)
    (409, 304, 'CLAY-01',   'Raw Clay',        'TILE-24X24', 5, 24.0, 24.0, 0.0),
    (410, 304, 'CEMENT-01', 'Portland Cement', 'TILE-24X24', 4, 40.0, 40.0, 0.0)
) AS src(Id, GoodsIssueDocumentId, MaterialNumber, MaterialDescription,
         FertMaterialNumber, RequiredUomId, RequiredQty, IssuedQty, RemainingQty)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.IssuedQty = src.IssuedQty
WHEN NOT MATCHED THEN INSERT
    (Id, GoodsIssueDocumentId, MaterialNumber, MaterialDescription,
     FertMaterialNumber, RequiredUomId, RequiredQty, IssuedQty, RemainingQty)
VALUES (src.Id, src.GoodsIssueDocumentId, src.MaterialNumber, src.MaterialDescription,
        src.FertMaterialNumber, src.RequiredUomId, src.RequiredQty, src.IssuedQty, src.RemainingQty);
SET IDENTITY_INSERT [dbo].[GoodsIssueDocumentLines] OFF;
GO

-- ============================================================
-- 8. Stock Inventory Lines (finished goods on-hand)
-- ============================================================
SET IDENTITY_INSERT [dbo].[StockInventoryLines] ON;
MERGE [dbo].[StockInventoryLines] AS tgt
USING (VALUES
    (501, 'TILE-32X32', 'Man102', 450.0, 1, 'Active', 'A', 850.0, 382500.0, 'LOT-32X32-A', GETDATE(), GETDATE()),
    (502, 'TILE-32X32', 'Man102', 120.0, 1, 'Active', 'B', 750.0,  90000.0, 'LOT-32X32-B', GETDATE(), GETDATE()),
    (503, 'TILE-32X32', 'Man102',  45.0, 1, 'Active', 'C', 650.0,  29250.0, 'LOT-32X32-C', GETDATE(), GETDATE()),
    (504, 'TILE-24X24', 'Man102', 280.0, 1, 'Active', 'A', 680.0, 190400.0, 'LOT-24X24-A', GETDATE(), GETDATE()),
    (505, 'TILE-24X24', 'Man102',  90.0, 1, 'Active', 'B', 600.0,  54000.0, 'LOT-24X24-B', GETDATE(), GETDATE()),
    (506, 'TILE-18X18', 'Man102', 178.0, 1, 'Active', 'A', 520.0,  92560.0, 'LOT-18X18-A', GETDATE(), GETDATE()),
    (507, 'TILE-18X18', 'Man102',  55.0, 1, 'Active', 'B', 460.0,  25300.0, 'LOT-18X18-B', GETDATE(), GETDATE())
) AS src(Id, MaterialNumber, PlantID, Quantity, QuantityUomId, Status, Grade,
         StandardCostPerUom, StockValue, BatchOrLot, CreatedAt, UpdatedAt)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Quantity = src.Quantity, tgt.StockValue = src.StockValue
WHEN NOT MATCHED THEN INSERT
    (Id, MaterialNumber, PlantID, Quantity, QuantityUomId, Status, Grade,
     StandardCostPerUom, StockValue, BatchOrLot, CreatedAt, UpdatedAt)
VALUES (src.Id, src.MaterialNumber, src.PlantID, src.Quantity, src.QuantityUomId, src.Status,
        src.Grade, src.StandardCostPerUom, src.StockValue, src.BatchOrLot, src.CreatedAt, src.UpdatedAt);
SET IDENTITY_INSERT [dbo].[StockInventoryLines] OFF;
GO

-- ============================================================
-- 9. Business Partners (customers)
-- ============================================================
MERGE [dbo].[BusinessPartnerMasterSamples] AS tgt
USING (VALUES
    ('BP-CUST-001', 'Al-Hassan Tiles Dealer',  'Lahore',    '0300-1234567', 'alhassan@tiles.pk', GETDATE()),
    ('BP-CUST-002', 'Pak Building Supplies',   'Karachi',   '0321-7654321', 'pak@building.pk',  GETDATE()),
    ('BP-CUST-003', 'Metro Ceramics',          'Islamabad', '0333-9876543', 'metro@ceramics.pk',GETDATE())
) AS src(BPID, FullName, City, Mobile, Email, CreatedAt)
ON tgt.BPID = src.BPID
WHEN MATCHED THEN UPDATE SET tgt.FullName = src.FullName
WHEN NOT MATCHED THEN INSERT (BPID, FullName, City, Mobile, Email, IsActive, CreatedAt)
VALUES (src.BPID, src.FullName, src.City, src.Mobile, src.Email, 1, src.CreatedAt);
GO

-- ============================================================
-- 10. Sales Orders
-- ============================================================
SET IDENTITY_INSERT [dbo].[SalesOrders] ON;
MERGE [dbo].[SalesOrders] AS tgt
USING (VALUES
    (501, 'SO-2026-001', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', '2026-04-20', '2026-05-05', 'Confirmed'),
    (502, 'SO-2026-002', 'BP-CUST-002', 'Pak Building Supplies',  '2026-04-25', '2026-05-10', 'Confirmed'),
    (503, 'SO-2026-003', 'BP-CUST-003', 'Metro Ceramics',         '2026-05-02', '2026-05-18', 'Confirmed'),
    (504, 'SO-2026-004', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', '2026-05-08', '2026-05-25', 'Open')
) AS src(Id, SalesOrderNumber, CustomerBusinessPartnerId, CustomerName, OrderDate, RequestedDeliveryDate, Status)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Status = src.Status
WHEN NOT MATCHED THEN INSERT
    (Id, SalesOrderNumber, CustomerBusinessPartnerId, CustomerName, OrderDate, RequestedDeliveryDate, Status)
VALUES (src.Id, src.SalesOrderNumber, src.CustomerBusinessPartnerId, src.CustomerName,
        src.OrderDate, src.RequestedDeliveryDate, src.Status);
SET IDENTITY_INSERT [dbo].[SalesOrders] OFF;
GO

-- ============================================================
-- 11. Delivery Challans (required FK for SalesInvoice)
-- ============================================================
SET IDENTITY_INSERT [dbo].[DeliveryChallans] ON;
MERGE [dbo].[DeliveryChallans] AS tgt
USING (VALUES
    (601, 'DC-2026-001', 'Man102', 'Standard', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', '2026-05-03', 501),
    (602, 'DC-2026-002', 'Man102', 'Standard', 'BP-CUST-002', 'Pak Building Supplies',  '2026-05-07', 502),
    (603, 'DC-2026-003', 'Man102', 'Standard', 'BP-CUST-003', 'Metro Ceramics',         '2026-05-12', 503),
    (604, 'DC-2026-004', 'Man102', 'Standard', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', '2026-05-15', 504),
    (605, 'DC-2026-005', 'Man102', 'Standard', 'BP-CUST-002', 'Pak Building Supplies',  '2026-04-22', 502)
) AS src(Id, DeliveryChallanNumber, PlantId, DeliveryType, ShipToBusinessPartnerId,
         ShipToDisplayName, DocumentDate, SalesOrderId)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.ShipToDisplayName = src.ShipToDisplayName
WHEN NOT MATCHED THEN INSERT
    (Id, DeliveryChallanNumber, PlantId, DeliveryType, ShipToBusinessPartnerId,
     ShipToDisplayName, DocumentDate, SalesOrderId)
VALUES (src.Id, src.DeliveryChallanNumber, src.PlantId, src.DeliveryType,
        src.ShipToBusinessPartnerId, src.ShipToDisplayName, src.DocumentDate, src.SalesOrderId);
SET IDENTITY_INSERT [dbo].[DeliveryChallans] OFF;
GO

-- ============================================================
-- 12. Sales Invoices
-- ============================================================
SET IDENTITY_INSERT [dbo].[SalesInvoices] ON;
MERGE [dbo].[SalesInvoices] AS tgt
USING (VALUES
    -- May invoices (visible on dashboard this month)
    (701, 'INV-2026-001', '2026-05-04', '2026-06-04', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', 601, 'DC-2026-001', 212500.0, 212500.0, 'Collected', GETDATE()),
    (702, 'INV-2026-002', '2026-05-08', '2026-06-08', 'BP-CUST-002', 'Pak Building Supplies',  602, 'DC-2026-002', 136000.0, 136000.0, 'Open',      GETDATE()),
    (703, 'INV-2026-003', '2026-05-13', '2026-06-13', 'BP-CUST-003', 'Metro Ceramics',         603, 'DC-2026-003',  93600.0,  93600.0, 'Open',      GETDATE()),
    (704, 'INV-2026-004', '2026-05-16', '2026-06-16', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', 604, 'DC-2026-004', 170000.0, 170000.0, 'Open',      GETDATE()),
    -- April invoice
    (705, 'INV-2026-005', '2026-04-23', '2026-05-23', 'BP-CUST-002', 'Pak Building Supplies',  605, 'DC-2026-005',  85000.0,  85000.0, 'Collected', GETDATE())
) AS src(Id, DocumentNumber, DocumentDate, DueDate,
         DealerBusinessPartnerId, DealerDisplayName, DeliveryChallanId, DcNumber,
         Subtotal, GrandTotal, Status, CreatedAt)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Status = src.Status
WHEN NOT MATCHED THEN INSERT
    (Id, DocumentNumber, DocumentDate, DueDate,
     DealerBusinessPartnerId, DealerDisplayName, DeliveryChallanId, DcNumber,
     Subtotal, GrandTotal, Status, CreatedAt)
VALUES (src.Id, src.DocumentNumber, src.DocumentDate, src.DueDate,
        src.DealerBusinessPartnerId, src.DealerDisplayName, src.DeliveryChallanId, src.DcNumber,
        src.Subtotal, src.GrandTotal, src.Status, src.CreatedAt);
SET IDENTITY_INSERT [dbo].[SalesInvoices] OFF;
GO

-- ============================================================
-- 13. Sales Invoice Lines
-- ============================================================
SET IDENTITY_INSERT [dbo].[SalesInvoiceLines] ON;
MERGE [dbo].[SalesInvoiceLines] AS tgt
USING (VALUES
    -- INV-2026-001 (250 BOX TILE-32X32 @ 850)
    (801, 701, 1, 'TILE-32X32', '32x32 Ceramic Floor Tile', 250.0, 1, 850.0, 212500.0, 212500.0),
    -- INV-2026-002 (200 BOX TILE-24X24 @ 680)
    (802, 702, 1, 'TILE-24X24', '24x24 Ceramic Wall Tile',  200.0, 1, 680.0, 136000.0, 136000.0),
    -- INV-2026-003 (80 BOX TILE-32X32 @ 850 + 100 BOX TILE-18X18 @ 520 — wait let me make simpler)
    (803, 703, 1, 'TILE-18X18', '18x18 Vitrified Glazed Tile', 180.0, 1, 520.0, 93600.0, 93600.0),
    -- INV-2026-004 (100 BOX TILE-32X32 + 100 BOX TILE-24X24)
    (804, 704, 1, 'TILE-32X32', '32x32 Ceramic Floor Tile', 100.0, 1, 850.0, 85000.0,  85000.0),
    (805, 704, 2, 'TILE-24X24', '24x24 Ceramic Wall Tile',  125.0, 1, 680.0, 85000.0,  85000.0),
    -- INV-2026-005 (100 BOX TILE-24X24 @ 850 April)
    (806, 705, 1, 'TILE-32X32', '32x32 Ceramic Floor Tile', 100.0, 1, 850.0, 85000.0,  85000.0)
) AS src(Id, SalesInvoiceId, LineNo, MaterialNumber, MaterialDescription,
         Quantity, QuantityUomId, UnitPrice, LineSubtotal, LineTotal)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Quantity = src.Quantity
WHEN NOT MATCHED THEN INSERT
    (Id, SalesInvoiceId, LineNo, MaterialNumber, MaterialDescription,
     Quantity, QuantityUomId, UnitPrice, LineSubtotal, LineTotal)
VALUES (src.Id, src.SalesInvoiceId, src.LineNo, src.MaterialNumber, src.MaterialDescription,
        src.Quantity, src.QuantityUomId, src.UnitPrice, src.LineSubtotal, src.LineTotal);
SET IDENTITY_INSERT [dbo].[SalesInvoiceLines] OFF;
GO

-- ============================================================
-- 14. Sales Payments
-- ============================================================
SET IDENTITY_INSERT [dbo].[SalesPayments] ON;
MERGE [dbo].[SalesPayments] AS tgt
USING (VALUES
    -- May payment for INV-2026-001 (collected)
    (901, 'PAY-2026-001', '2026-05-06', 701, 'INV-2026-001', 'BP-CUST-001', 'Al-Hassan Tiles Dealer', 212500.0, 'online',  GETDATE()),
    -- May partial payment for INV-2026-002
    (902, 'PAY-2026-002', '2026-05-10', 702, 'INV-2026-002', 'BP-CUST-002', 'Pak Building Supplies',   80000.0, 'cash',    GETDATE()),
    -- April payment for INV-2026-005
    (903, 'PAY-2026-003', '2026-04-25', 705, 'INV-2026-005', 'BP-CUST-002', 'Pak Building Supplies',   85000.0, 'cheque',  GETDATE())
) AS src(Id, DocumentNumber, DocumentDate, SalesInvoiceId, InvoiceDocumentNumber,
         DealerBusinessPartnerId, DealerDisplayName, Amount, PaymentMethod, CreatedAt)
ON tgt.Id = src.Id
WHEN MATCHED THEN UPDATE SET tgt.Amount = src.Amount
WHEN NOT MATCHED THEN INSERT
    (Id, DocumentNumber, DocumentDate, SalesInvoiceId, InvoiceDocumentNumber,
     DealerBusinessPartnerId, DealerDisplayName, Amount, PaymentMethod, CreatedAt)
VALUES (src.Id, src.DocumentNumber, src.DocumentDate, src.SalesInvoiceId, src.InvoiceDocumentNumber,
        src.DealerBusinessPartnerId, src.DealerDisplayName, src.Amount, src.PaymentMethod, src.CreatedAt);
SET IDENTITY_INSERT [dbo].[SalesPayments] OFF;
GO

PRINT '✓ Seed complete. Summary:';
PRINT '  Materials: TILE-32X32, TILE-24X24, TILE-18X18 (FERT) + CLAY-01, CEMENT-01, GLAZE-01 (ROH)';
PRINT '  Production Orders: 5 orders (2 Completed, 1 InProgress, 1 Released, 1 Completed-March)';
PRINT '  Goods Produce Batches: 9 batches across April & May';
PRINT '  Goods Issue Documents: 4 with 10 lines';
PRINT '  Stock: 7 lines (TILE-32X32/24X24/18X18, Grade A/B/C)';
PRINT '  Customers: Al-Hassan Tiles Dealer, Pak Building Supplies, Metro Ceramics';
PRINT '  Sales Orders: 4 orders';
PRINT '  Delivery Challans: 5';
PRINT '  Sales Invoices: 5 (4 May + 1 April)';
PRINT '  Sales Payments: 3';
GO
