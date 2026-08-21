-- İzin (permissions) seed script
-- UserDb.dbo.permissions tablosuna nihai izin listesini ekler.
-- Aynı Name ile tekrar kayıt açılmasın diye WHERE NOT EXISTS ile idempotent yazıldı.

INSERT INTO [UserDb].[dbo].[permissions] ([Name], [DisplayName], [IsActive], [CreatedAt], [DeletedAt])
SELECT v.[Name], v.[DisplayName], 1, GETUTCDATE(), NULL
FROM (VALUES
    ('UserRead',        N'Kullanıcı Görüntüleme'),
    ('UserWrite',       N'Kullanıcı Yönetimi'),
    ('SettingsRead',    N'Sistem Ayarları Görüntüleme'),
    ('SettingsWrite',   N'Sistem Ayarları Yönetimi'),
    ('InvoiceRead',     N'Fatura Görüntüleme'),
    ('InvoiceWrite',    N'Fatura Onaylama/Reddetme'),
    ('InvoiceBulkExport', N'Toplu Fatura Dışa Aktarma')
) AS v([Name], [DisplayName])
WHERE NOT EXISTS (
    SELECT 1 FROM [UserDb].[dbo].[permissions] p
    WHERE p.[Name] = v.[Name] AND p.[DeletedAt] IS NULL
);
