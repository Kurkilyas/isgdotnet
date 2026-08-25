# Rol kataloğu (oluşturma / güncelleme / pasif / silme)

Bu dosya `RolesController` (`/api/roles`) içindir. Kullanıcıya rol atama: [FRONTEND_USER_ROLES.md](FRONTEND_USER_ROLES.md).

JSON **camelCase**. `[Authorize]` — `Authorization: Bearer {accessToken}`. `credentials: "include"`. Müdür (`is_manager`) **yok**.

| İş | İzin |
|----|------|
| Liste, detay | `AdminRead` |
| Oluştur, güncelle, pasif/aktif, sil | `AdminWrite` |

`AdminRead` / `AdminWrite` JWT claim’de yoksa **403**. Seed: `Scripts/seed_permissions.sql` çalıştırın; SystemAdmin rolüne bu izinleri bağlayın, sonra **yeniden login**.

---

## Endpoint özeti

| Metod | Yol | Gövde | Cevap | İzin |
|-------|-----|--------|--------|------|
| GET | `/api/roles` | — | `Role[]` | AdminRead |
| GET | `/api/roles/{id}` | — | `Role` | AdminRead |
| GET | `/api/roles/users/{userId}` | — | kullanıcı rolleri | AdminRead |
| POST | `/api/roles` | `CreateRoleRequest` | `Role` | AdminWrite |
| POST | `/api/roles/users/{userId}` | `AssignRoleRequest` | atama | AdminWrite |
| PUT | `/api/roles/{id}` | `UpdateRoleRequest` | `Role` | AdminWrite |
| PUT | `/api/roles/{id}/status` | `SetRoleActiveRequest` | `Role` | AdminWrite |
| DELETE | `/api/roles/{id}` | — | `{ message }` | AdminWrite |
| DELETE | `/api/roles/users/{userId}/{roleId}` | — | `{ message }` | AdminWrite |

Liste **pasif rolleri de** döner (`isActive`). Atama select’inde `isActive === true` filtreleyin.

Log: `ROLE_CREATED`, `ROLE_UPDATED`, `ROLE_DEACTIVATED`, `ROLE_DELETED`.

---

## Tipler

```ts
export interface Role {
  id: number;
  name: string;
  displayName: string;
  description?: string | null;
  isActive: boolean;
  isManager: boolean;
  departmentId?: number | null;
}

export interface CreateRoleRequest {
  name: string; // zorunlu, max 50, benzersiz
  displayName: string; // zorunlu, max 100
  description?: string | null;
  isManager?: boolean; // varsayılan false
  departmentId?: number | null;
}

export interface UpdateRoleRequest {
  name: string;
  displayName: string;
  description?: string | null;
  isManager: boolean;
  departmentId?: number | null;
}

export interface SetRoleActiveRequest {
  isActive: boolean;
}
```

`name` kod adı (`Employee`, `Accounting`). `displayName` ekran metni. `isManager: true` olan rol, `UserRead` olmadan kendi departmanındaki kullanıcı listesini açar.

---

## Davranış

- **Oluştur:** `isActive = true`. Aynı `name` **409**.
- **Güncelle:** `name` değişebilir; çakışırsa **409**. Geçersiz `departmentId` **400**.
- **Status:** `{ isActive: false }` pasif — bu rolle yeni atama **400**. Mevcut atamalar durur; JWT’de rol `IsActive` değilse izinler düşer (yeniden login).
- **Sil:** soft delete. Aktif kullanıcı atamaları da kapatılır. Aynı `name` unique index yüzünden yeniden oluşturulmayabilir.

---

## `src/api/roleCatalogService.ts`

```ts
create: (body: CreateRoleRequest) =>
  http.post<Role>("/api/roles", body).then((r) => r.data),

update: (id: number, body: UpdateRoleRequest) =>
  http.put<Role>(`/api/roles/${id}`, body).then((r) => r.data),

setActive: (id: number, isActive: boolean) =>
  http.put<Role>(`/api/roles/${id}/status`, { isActive }).then((r) => r.data),

remove: (id: number) =>
  http.delete<{ message: string }>(`/api/roles/${id}`).then((r) => r.data),

getById: (id: number) => http.get<Role>(`/api/roles/${id}`).then((r) => r.data),

getList: () => http.get<Role[]>("/api/roles").then((r) => r.data),
```

---

## Hatalar

| HTTP | Ne zaman |
|------|----------|
| 400 | Boş ad, geçersiz departman, doğrulama |
| 401 | Token yok |
| 403 | `AdminRead` / `AdminWrite` yok |
| 404 | Rol yok |
| 409 | `name` zaten var |
| 429 | Rate limit |
