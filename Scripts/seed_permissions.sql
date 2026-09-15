-- İzin (permissions) seed script
-- permissions tablosuna nihai izin listesini ekler.
-- Aynı Name ile tekrar kayıt açılmasın diye NOT EXISTS ile idempotent yazıldı.

INSERT INTO permissions (Name, DisplayName, IsActive, CreatedAt, DeletedAt)
SELECT v.Name, v.DisplayName, 1, UTC_TIMESTAMP(), NULL
FROM (
    SELECT 'UserRead' AS Name, 'Kullanıcı Görüntüleme' AS DisplayName
    UNION ALL SELECT 'UserWrite', 'Kullanıcı Yönetimi'
    UNION ALL SELECT 'AdminRead', 'Rol ve Yetki Görüntüleme'
    UNION ALL SELECT 'AdminWrite', 'Rol ve Yetki Yönetimi'
    UNION ALL SELECT 'SettingsRead', 'Sistem Ayarları Görüntüleme'
    UNION ALL SELECT 'SettingsWrite', 'Sistem Ayarları Yönetimi'
) AS v
WHERE NOT EXISTS (
    SELECT 1 FROM permissions p
    WHERE p.Name = v.Name AND p.DeletedAt IS NULL
);
