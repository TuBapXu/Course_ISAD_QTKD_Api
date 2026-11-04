UPDATE "Contracts" 
SET "Status" = 'Draft', 
    "FileUrl" = NULL, 
    "ContractContent" = NULL, 
    "CurrentApprovalId" = NULL, 
    "LastIdempotencyKey" = NULL 
WHERE "Id" = 1;

SELECT * FROM "Contracts" WHERE "Id" = 1;

-- 1. Xóa sạch dữ liệu các bảng và reset bộ đếm ID về lại 1
TRUNCATE TABLE "Contracts", "PriceItems", "Customers", "PriceLists" RESTART IDENTITY CASCADE;

-- 2. Thêm sẵn 1 Bảng giá mẫu số 1 (để phục vụ Mục 2 test thêm đơn giá dịch vụ vào Bảng giá ID = 1)
INSERT INTO "PriceLists" ("Id", "Name", "EffectiveDate") 
VALUES (1, 'Bảng giá 2026', '2026-01-01 00:00:00+00');

-- 3. Đồng bộ lại bộ đếm ID của bảng PriceLists để lần thêm bảng giá sau sẽ tự tăng lên ID = 2
SELECT setval(pg_get_serial_sequence('"PriceLists"', 'Id'), 1, true);