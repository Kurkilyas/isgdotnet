# Kullanıcı güncelleme ve pasife alma

Bu dosya yalnızca admin / müdür uçları içindir:

- `PUT /api/users/{id}`
- `PUT /api/users/{id}/status`

Profil (`/api/users/me`) ve liste için [FRONTEND_USERS.md](FRONTEND_USERS.md). Cookie, token ve hata gövdesi için [FRONTEND.md](FRONTEND.md).

JSON alan adları **camelCase**. İkisi de `[Authorize]` — `Authorization: Bearer {accessToken}` zorunlu, yoksa **401**. `credentials: "include"` kullanın.

---

## Kim çağırabilir

Kapı: `UserRead` okuma, `UserWrite` yazma. Kapsam tek kural: `IsManager` → actor `DepartmentId`, değilse tüm kullanıcılar. SystemAdmin’de `IsManager` kapalı olsun.

Kendi profilini güncellemek için bu uçları kullanmayın — `PUT /api/users/me`.

E-posta, şifre ve `departmentId` bu gövdelerde **yoktur**. Departman taşıma bu API’de yoktur.

---

## Endpoint özeti

| Metod | Yol | Gövde | Cevap |
|-------|-----|--------|--------|
| PUT | `/api/users/{id}` | `UpdateUserRequest` | `User` |
| PUT | `/api/users/{id}/status` | `SetUserActiveRequest` | `User` |

Rate limit: `GlobalLimit`.

Log (`auth_activity_logs`): güncelleme `USER_UPDATED`; pasif `USER_DEACTIVATED`; tekrar aktif `ACCOUNT_UNLOCKED`. Durum zaten istenen değerdeyse log yazılmaz.

---

## `PUT /api/users/{id}` — güncelleme

Ad, telefon. `position` **gönderilmez** — cevapta aktif rollerin `displayName` birleşimi gelir.

```ts
export interface UpdateUserRequest {
  fullName: string; // zorunlu, max 100
  phone?: string | null; // max 20
}
```

Örnek:

```json
{
  "fullName": "Ali Veli",
  "phone": "5551112233"
}
```

Başarılı cevap **200**, `GET /api/users/{id}` ile aynı `User`. `position` örnek: `"Muhasebe, Çalışan"` (rol yoksa `null`).

```json
{
  "id": 12,
  "fullName": "Ali Veli",
  "email": "ali@cazgir.com.tr",
  "isActive": true,
  "isVerified": true,
  "departmentId": 3,
  "phone": "5551112233",
  "position": "Muhasebe",
  "createdAt": "2026-08-01T10:00:00Z"
}
```

Boş `phone` gönderilirse alan `null` olur.

---

## `PUT /api/users/{id}/status` — aktif / pasif

```ts
export interface SetUserActiveRequest {
  isActive: boolean; // zorunlu
}
```

Pasife alma:

```json
{ "isActive": false }
```

Tekrar aktif (kilitli hesap dahil):

```json
{ "isActive": true }
```

Cevap yine `User` (`isActive` güncel).

Kurallar:

- **Kendi `id`’niz** ile çağrı **403**: `"Kendi hesabınızın durumunu bu endpoint ile değiştiremezsiniz."`
- `isActive: false`: hesabı kapatır, aktif refresh token’ları iptal eder; kullanıcı hemen oturumdan düşer.
- `isActive: true`: hesabı açar; `failedLoginCount` ve `lockedUntil` sıfırlanır (5 hatalı e-posta doğrulama kilidi de böyle açılır).
- Değer zaten aynıysa **200** + mevcut `User`, ekstra işlem yok.

Pasif kullanıcı login / refresh yapamaz.

---

## `src/api/userService.ts` (ek)

`User` tipi [FRONTEND_USERS.md](FRONTEND_USERS.md) ile aynıdır.

```ts
updateUser: (id: number, body: UpdateUserRequest) =>
  http.put<User>(`/api/users/${id}`, body).then((r) => r.data),

setUserActive: (id: number, isActive: boolean) =>
  http
    .put<User>(`/api/users/${id}/status`, { isActive })
    .then((r) => r.data),
```

---

## Hatalar

| HTTP | Ne zaman |
|------|----------|
| 400 | `fullName` boş / max aşımı, `isActive` yok, doğrulama |
| 401 | Token yok / geçersiz |
| 403 | `UserWrite` yok ve müdür değil; müdür başka departman; kendi hesabını `/status` ile kapatma |
| 404 | `{id}` kullanıcısı yok |
| 429 | Rate limit (`GlobalLimit`) |

Hata gövdesi: `{ success: false, statusCode, error }`.

---

## UI önerisi

1. Liste / detay: `getList` / `getById` — bu PUT’lar aynı yetki kapsamındadır (`UserWrite` tümü, müdür kendi departmanı).
2. Düzenle formu: `fullName`, `phone` — e-posta ve unvan (rol) disabled; unvan rollerden gelir
3. Pasif / aktif: onay diyaloğu; kendi satırınızda “pasife al” göstermeyin (403).
4. 403’te “yetkiniz yok”; 404’te kullanıcı silinmiş / yok.
