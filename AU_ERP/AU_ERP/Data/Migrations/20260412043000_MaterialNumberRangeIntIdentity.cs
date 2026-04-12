using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <summary>
    /// Replaces string PK RangeID with int IDENTITY so new rows get unique keys at SQL level
    /// (fixes duplicate RangeID when saving multiple new ranges in one request).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260412043000_MaterialNumberRangeIntIdentity")]
    public class MaterialNumberRangeIntIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[MaterialNumberRanges] DROP CONSTRAINT [FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MaterialNumberRanges_MaterialTypeCode' AND object_id = OBJECT_ID(N'dbo.MaterialNumberRanges'))
    DROP INDEX [IX_MaterialNumberRanges_MaterialTypeCode] ON [dbo].[MaterialNumberRanges];
");

            migrationBuilder.Sql(@"
CREATE TABLE [dbo].[MaterialNumberRanges_New] (
    [RangeID] int NOT NULL IDENTITY(1, 1),
    [MaterialTypeCode] nvarchar(450) NOT NULL,
    [FromNumber] nvarchar(max) NOT NULL,
    [ToNumber] nvarchar(max) NOT NULL,
    [CurrentNumber] nvarchar(max) NOT NULL,
    [IsExternal] bit NOT NULL,
    CONSTRAINT [PK_MaterialNumberRanges_New] PRIMARY KEY ([RangeID])
);

INSERT INTO [dbo].[MaterialNumberRanges_New] ([MaterialTypeCode], [FromNumber], [ToNumber], [CurrentNumber], [IsExternal])
SELECT [MaterialTypeCode], [FromNumber], [ToNumber], [CurrentNumber], [IsExternal]
FROM [dbo].[MaterialNumberRanges];

DROP TABLE [dbo].[MaterialNumberRanges];

EXEC sp_rename N'dbo.MaterialNumberRanges_New', N'MaterialNumberRanges', N'OBJECT';
");

            migrationBuilder.Sql(@"
EXEC sp_rename N'PK_MaterialNumberRanges_New', N'PK_MaterialNumberRanges', N'OBJECT';
");

            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[MaterialNumberRanges] WITH CHECK
ADD CONSTRAINT [FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode]
FOREIGN KEY ([MaterialTypeCode]) REFERENCES [dbo].[MaterialTypes] ([MaterialTypeCode]);
");

            migrationBuilder.Sql(@"
CREATE NONCLUSTERED INDEX [IX_MaterialNumberRanges_MaterialTypeCode]
ON [dbo].[MaterialNumberRanges] ([MaterialTypeCode]);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[MaterialNumberRanges] DROP CONSTRAINT [FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode];
DROP INDEX [IX_MaterialNumberRanges_MaterialTypeCode] ON [dbo].[MaterialNumberRanges];
");

            migrationBuilder.Sql(@"
CREATE TABLE [dbo].[MaterialNumberRanges_OldStr] (
    [RangeID] nvarchar(450) NOT NULL,
    [MaterialTypeCode] nvarchar(450) NOT NULL,
    [FromNumber] nvarchar(max) NOT NULL,
    [ToNumber] nvarchar(max) NOT NULL,
    [CurrentNumber] nvarchar(max) NOT NULL,
    [IsExternal] bit NOT NULL,
    CONSTRAINT [PK_MaterialNumberRanges_OldStr] PRIMARY KEY ([RangeID])
);

INSERT INTO [dbo].[MaterialNumberRanges_OldStr] ([RangeID], [MaterialTypeCode], [FromNumber], [ToNumber], [CurrentNumber], [IsExternal])
SELECT CAST([RangeID] AS nvarchar(450)), [MaterialTypeCode], [FromNumber], [ToNumber], [CurrentNumber], [IsExternal]
FROM [dbo].[MaterialNumberRanges];

DROP TABLE [dbo].[MaterialNumberRanges];

EXEC sp_rename N'dbo.MaterialNumberRanges_OldStr', N'MaterialNumberRanges', N'OBJECT';
EXEC sp_rename N'PK_MaterialNumberRanges_OldStr', N'PK_MaterialNumberRanges', N'OBJECT';
");

            migrationBuilder.Sql(@"
ALTER TABLE [dbo].[MaterialNumberRanges] WITH CHECK
ADD CONSTRAINT [FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode]
FOREIGN KEY ([MaterialTypeCode]) REFERENCES [dbo].[MaterialTypes] ([MaterialTypeCode]);

CREATE NONCLUSTERED INDEX [IX_MaterialNumberRanges_MaterialTypeCode]
ON [dbo].[MaterialNumberRanges] ([MaterialTypeCode]);
");
        }
    }
}
