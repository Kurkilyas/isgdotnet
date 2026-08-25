# Kullanıcıya rol atama

Bu dosya kullanıcı–rol ataması içindir. Rol oluşturma / güncelleme / pasif / silme: [FRONTEND_ROLES.md](FRONTEND_ROLES.md).

JSON **camelCase**. `[Authorize]` — `Authorization: Bearer {accessToken}`. `credentials: "include"`. Müdür (`is_manager`) **yok**.

| İş | İzin |
|----|------|
| Kullanıcının rollerini gör | `AdminRead` |
| Ata / al | `AdminWrite` |

Rol değişikliği **mevcut access token’a yansımaz**. Yeni izinler için kullanıcının tekrar giriş yapması gerekir.

---

## Endpoint özeti

| Metod | Yol | Gövde | Cevap | İzin |
|-------|-----|--------|--------|------|
| GET | `/api/roles` | — | `Role[]` | AdminRead |
| GET | `/api/roles/users/{userId}` | — | `UserRole[]` | AdminRead |
| POST | `/api/roles/users/{userId}` | `AssignRoleRequest` | `UserRole` | AdminWrite |
| DELETE | `/api/roles/users/{userId}/{roleId}` | — | `{ message }` | AdminWrite |

Rate limit: `GlobalLimit`. Log: atama `ROLE_ASSIGNED`, alma `ROLE_REVOKED`.

Atama select’inde yalnızca `isActive === true` roller. Katalog listesi pasifleri de döner.

---

## `GET /api/roles`

Rol kataloğu (pasifler dahil). Atama select’inde `isActive === true` kullanın.

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
```

---

## `GET /api/roles/users/{userId}`

Kullanıcının **aktif ve süresi dolmamış** rolleri.

```ts
export interface UserRole {
  roleId: number;
  name: string;
  displayName: string;
  isManager: boolean;
  assignedAt?: string | null;
  assignedBy?: number | null;
  expiresAt?: string | null;
}
```

Kullanıcı yoksa **404**.

---

## `POST /api/roles/users/{userId}`

```ts
export interface AssignRoleRequest {
  roleId: number;
  expiresAt?: string | null; // ISO datetime; yoksa süresiz
}
```

Örnek:

```json
{ "roleId": 2 }
```

Süreli:

```json
{ "roleId": 2, "expiresAt": "2027-01-01T00:00:00Z" }
```

Kurallar:

- Pasif rol **400**
- `expiresAt` geçmişte ise **400**
- Aynı aktif rol zaten varsa **409** `"Bu rol kullanıcıya zaten atanmış."`
- Rol veya kullanıcı yoksa **404**

---

## `DELETE /api/roles/users/{userId}/{roleId}`

Rolü kullanıcıdan alır (kayıt pasif + soft delete). Aktif atama yoksa **404**.

```json
{ "message": "Rol kullanıcıdan alındı." }
```

---

## `src/api/roleService.ts`

```ts
import http from "./http";
import type { AssignRoleRequest, Role, UserRole } from "../types/role";

export const roleService = {
  getList: () => http.get<Role[]>("/api/roles").then((r) => r.data),

  getUserRoles: (userId: number) =>
    http.get<UserRole[]>(`/api/roles/users/${userId}`).then((r) => r.data),

  assignToUser: (userId: number, body: AssignRoleRequest) =>
    http.post<UserRole>(`/api/roles/users/${userId}`, body).then((r) => r.data),

  revokeFromUser: (userId: number, roleId: number) =>
    http
      .delete<{ message: string }>(`/api/roles/users/${userId}/${roleId}`)
      .then((r) => r.data),
};
```

---

## Hatalar

| HTTP | Ne zaman |
|------|----------|
| 400 | Pasif rol, geçmiş `expiresAt`, doğrulama |
| 401 | Token yok / geçersiz |
| 403 | `AdminRead` / `AdminWrite` yok (müdür dahil) |
| 404 | Kullanıcı yok, rol yok, kullanıcıda o rol yok |
| 409 | Rol zaten atanmış |
| 429 | Rate limit |

---

## UI önerisi

1. Kullanıcı detayında roller: `getUserRoles(id)`
2. Ekle: `getList()` → select → `assignToUser`
3. Kaldır: onay → `revokeFromUser`
4. Bu ekranı yalnızca `AdminRead` / `AdminWrite` (admin) gösterin; müdür menüsünde olmasın
5. Atama sonrası kullanıcıya “yeniden giriş” notu
