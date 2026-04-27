-- Run once against an existing database that had the old single (Base,Alt) unique conversion model.
-- Adds Title, migrates data, reindexes; adds optional FK on per-material UnitConversions.

-- 1) GlobalUnitConversions: Title (globally unique) + new indexes
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'GlobalUnitConversions') AND name = 'Title')
BEGIN
    ALTER TABLE GlobalUnitConversions ADD Title NVARCHAR(100) NULL;
    UPDATE g SET g.Title = N'Migrated_' + CAST(g.Id AS NVARCHAR(20)) FROM GlobalUnitConversions g WHERE g.Title IS NULL;
    ALTER TABLE GlobalUnitConversions ALTER COLUMN Title NVARCHAR(100) NOT NULL;
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GlobalUnitConversions_BaseUnitId_AltUnitId' AND object_id = OBJECT_ID(N'GlobalUnitConversions'))
    DROP INDEX IX_GlobalUnitConversions_BaseUnitId_AltUnitId ON GlobalUnitConversions;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GlobalUnitConversions_Title' AND object_id = OBJECT_ID(N'GlobalUnitConversions'))
    CREATE UNIQUE INDEX IX_GlobalUnitConversions_Title ON GlobalUnitConversions(Title);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GlobalUnitConversions_BaseUnitId_AltUnitId' AND object_id = OBJECT_ID(N'GlobalUnitConversions'))
    CREATE NONCLUSTERED INDEX IX_GlobalUnitConversions_BaseUnitId_AltUnitId ON GlobalUnitConversions(BaseUnitId, AltUnitId);

-- 2) UnitConversions: pointer to master definition (optional; legacy NULL ok)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'UnitConversions') AND name = 'GlobalUnitConversionId')
    ALTER TABLE UnitConversions ADD GlobalUnitConversionId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UnitConversions_GlobalUnitConversions_GlobalUnitConversionId')
    ALTER TABLE UnitConversions ADD CONSTRAINT FK_UnitConversions_GlobalUnitConversions_GlobalUnitConversionId
        FOREIGN KEY (GlobalUnitConversionId) REFERENCES GlobalUnitConversions(Id);

-- Enforce at most one conversion row per material + alternate UOM (if not already; ignore error if index exists)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UnitConversions_MaterialNumber_AltUnitId' AND object_id = OBJECT_ID(N'UnitConversions'))
BEGIN
    CREATE UNIQUE INDEX IX_UnitConversions_MaterialNumber_AltUnitId ON UnitConversions(MaterialNumber, AltUnitId);
END
