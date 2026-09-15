# Frontend — Departmanlar

**Controller:** `DepartmentsController`  
**Base:** `/api/departments`  
**İzin:** GET `AdminRead`, yazma `AdminWrite`  
Auth: `Authorization: Bearer {accessToken}`. Token yok / geçersiz → 401.

Rol oluştururken / güncellerken combobox bu API’den dolar. Silme soft delete (`DeletedAt`); fiziksel silinmez. Soft silinmiş kayıtlar listede görünmez.

`Name` zorunlu, max 50, trim sonrası boş olamaz. `Description` opsiyonel, max 100. Aynı ad tekrar kullanılamaz.

---

## Response — `DepartmentListDto`

Liste, detay, oluşturma, güncelleme ve status aynı DTO’yu döner.

```json
{
  "id": 3,
  "name": "Muhasebe",
  "description": "Muhasebe ve finans",
  "isActive": true,
  "createdAt": "2026-09-14T06:00:00Z",
  "updatedAt": null
}
```

| Alan | Tip |
|---|---|
| id | number |
| name | string |
| description | string \| null |
| isActive | boolean |
| createdAt | string \| null |
| updatedAt | string \| null |

Sıra: `name` ASC.

---

## Combobox — `GET /api/departments/all`

Rol formu ve benzeri seçiciler için. `id` + `name`.

**İzin:** Giriş yapmış kullanıcı (permission yok)

**Query**

| Parametre | Tip | Not |
|---|---|---|
| isActive | bool | Combobox’ta `true` ver |
| name | string | `Name` içerir, opsiyonel arama |

```json
[{ "id": 3, "name": "Muhasebe" }]
```

---

## `GET /api/departments`

Admin listesi. Sayfalı.

**İzin:** `AdminRead`

**Query**

| Parametre | Tip | Varsayılan |
|---|---|---|
| page | int | 1 |
| pageSize | int | 20 |
| search | string | `name` veya `description` içerir |
| isActive | bool | aktif / pasif filtresi |

**Response 200:** `PagedResult<DepartmentListDto>`

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

---

## `GET /api/departments/{id}`

**İzin:** `AdminRead`

**Response 200:** `DepartmentListDto`  
404: `"Departman bulunamadı."`

---

## `POST /api/departments`

**İzin:** `AdminWrite`  
`application/json`. Yeni kayıt `isActive = true`.

| Alan | Tip | Zorunlu |
|---|---|---|
| name | string | evet, max 50 |
| description | string \| null | hayır, max 100 |

**Response 200:** `DepartmentListDto`

---

## `PUT /api/departments/{id}`

**İzin:** `AdminWrite`  
`application/json`.

| Alan | Tip |
|---|---|
| name | string, zorunlu, max 50 |
| description | string \| null, max 100 |

**Response 200:** `DepartmentListDto`

---

## `PUT /api/departments/{id}/status`

**İzin:** `AdminWrite`

Pasif departman yeni role atanamaz (`"Geçerli bir departman seçilmedi."`). Mevcut roller durur.

```json
{ "isActive": false }
```

**Response 200:** `DepartmentListDto`

---

## `DELETE /api/departments/{id}`

**İzin:** `AdminWrite`

Soft delete. Bağlı rol varsa silinmez (önce rollerin `departmentId` değerini değiştir / kaldır).

```json
{ "message": "Departman silindi." }
```

---

## Kullanım notları

- Rol create/update gövdesindeki `departmentId` buradaki `id` değeridir. Combobox: `GET /api/departments/all?isActive=true`.
- Liste ekranı `GET /api/departments`, detay `GET /api/departments/{id}`.
- Switch / toggle için `PUT .../status`, tam form kaydı için `PUT /{id}`.

---

## Hata

| Kod | Örnek |
|---|---|
| 400 | `"Departman adı boş olamaz."` |
| 400 | validation: `Name` required / max 50; `Description` max 100 |
| 401 | oturum yok / token geçersiz |
| 403 | `"Gerekli izinler: AdminRead"` / `"Gerekli izinler: AdminWrite"` |
| 404 | `"Departman bulunamadı."` |
| 409 | `"Bu departman adı zaten kullanılıyor."` |
| 409 | `"Bu departmana bağlı roller var. Önce rollerin departmanını değiştirin."` |
