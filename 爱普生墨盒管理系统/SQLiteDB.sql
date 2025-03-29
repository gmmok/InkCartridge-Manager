--
-- 爱普生11色打印机数据库
-- 
-- 所用的文本编码：UTF-8
--
PRAGMA foreign_keys = false;
BEGIN TRANSACTION;

-- 表：CartridgeColors
DROP TABLE IF EXISTS "CartridgeColors";
CREATE TABLE "CartridgeColors" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "Name" TEXT NOT NULL,
  "ColorCode" TEXT,
  "DisplayOrder" INTEGER DEFAULT 0,
  UNIQUE ("Name" ASC)
);

-- 数据：CartridgeColors
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (1, '青色', '#00FFFF', 1);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (2, '橙色', '#FFA500', 2);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (3, '黄色', '#FFFF00', 3);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (4, '淡青色', '#AFEEEE', 4);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (5, '粗面黑色', '#333333', 5);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (6, '照片黑色', '#000000', 6);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (7, '鲜洋红色', '#FF00FF', 7);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (8, '淡黑色', '#666666', 8);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (9, '绿色', '#008000', 9);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (10, '淡淡黑色', '#808080', 10);
INSERT INTO "CartridgeColors" ("Id", "Name", "ColorCode", "DisplayOrder") VALUES (11, '淡鲜洋红色', '#FF77FF', 11);

-- 表：Cartridges
DROP TABLE IF EXISTS "Cartridges";
CREATE TABLE "Cartridges" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "Color" TEXT NOT NULL,
  "Model" TEXT NOT NULL,
  "CurrentStock" INTEGER NOT NULL,
  "MinimumStock" INTEGER NOT NULL,
  "Notes" TEXT,
  "UpdateTime" DATETIME NOT NULL,
  "Capacity" REAL DEFAULT 0
);

-- 表：Operators
DROP TABLE IF EXISTS "Operators";
CREATE TABLE "Operators" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "Name" TEXT NOT NULL,
  "LastUsed" DATETIME,
  "UseCount" INTEGER DEFAULT 0,
  UNIQUE ("Name" ASC)
);

-- 表：StockRecords
DROP TABLE IF EXISTS "StockRecords";
CREATE TABLE "StockRecords" (
  "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "CartridgeId" INTEGER NOT NULL,
  "OperationType" INTEGER NOT NULL,
  "Quantity" INTEGER NOT NULL,
  "OperationTime" DATETIME NOT NULL,
  "Operator" TEXT,
  "Project" TEXT,
  "Notes" TEXT,
  FOREIGN KEY ("CartridgeId") REFERENCES "Cartridges" ("Id") ON DELETE NO ACTION ON UPDATE NO ACTION
);

COMMIT TRANSACTION;
PRAGMA foreign_keys = true;