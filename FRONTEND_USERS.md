# Users API — frontend entegrasyonu

Bu dosya yalnızca `UsersController` (`/api/users`) içindir. Genel auth, cookie ve hata gövdesi için [FRONTEND.md](FRONTEND.md) bakın.

JSON alan adları **camelCase**. Tüm endpoint’ler `[Authorize]` — `Authorization: Bearer {accessToken}` zorunlu, yoksa **401**.

`credentials: "include"` kullanın (aynı axios instance).

- `me` endpoint’leri: JWT’deki kullanıcı id — ekstra izin yok.
- `GET /api/users` ve `GET /api/users/{id}`:
  - `UserRead` izni → tüm kullanıcılar
  - yoksa aktif rolde `isManager = true` → yalnızca kendi departmanı
  - ikisi de yoksa **403**

---

## Endpoint özeti

| Metod | Yol | Gövde | Cevap |
|-------|-----|--------|--------|
| GET | `/api/users/me` | — | `UserMe` |
| PUT | `/api/users/me` | `UpdateMeRequest` | `UserMe` |
| PUT | `/api/users/me/password` | `ChangePasswordRequest` | `{ message }` |
| PUT | `/api/users/me/out-of-office` | `UpdateOutOfOfficeRequest` | `UserMe` |
| POST | `/api/users/me/files` | `multipart/form-data` | `UserFileMeta` |
| GET | `/api/users/me/files/{fileKind}` | — | `UserFileMeta` |
| GET | `/api/users/me/files/{fileKind}/content` | — | görsel stream (`image/png` vb.) |
| GET | `/api/users` | query: `page`, `pageSize` | `PagedResult<User>` (`UserRead` veya müdür) |
| GET | `/api/users/{id}` | — | `User` (`UserRead` veya aynı departman müdürü) |

PUT/POST işlemleri `auth_activity_logs`’a yazılır (`PROFILE_UPDATED`, `PASSWORD_CHANGED`, `OUT_OF_OFFICE_CHANGED`, `FILE_UPLOADED`). GET loglanmaz.

E-posta bu API ile **değiştirilemez**.

---

## `src/types/user.ts`

```ts
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface User {
  id: number;
  fullName: string;
  email: string;
  isActive: boolean;
  isVerified: boolean;
  departmentId?: number | null;
  phone?: string | null;
  position?: string | null;
  createdAt?: string | null;
}

export interface UserMe extends User {
  isOutOfOffice: boolean;
  outOfOfficeUntil?: string | null;
  hasProfilePhoto: boolean;
  hasSignature: boolean;
}

export type UserFileKind = "PROFILE_PHOTO" | "SIGNATURE";

export interface UserFileMeta {
  fileKind: UserFileKind;
  originalFileName: string;
  contentType: string;
  fileSizeBytes?: number | null;
  uploadedAt?: string | null;
}

export interface UpdateMeRequest {
  fullName: string; // max 100
  phone?: string | null; // max 20
  position?: string | null; // max 100
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string; // min 8, max 100
}

export interface UpdateOutOfOfficeRequest {
  isOutOfOffice: boolean;
  outOfOfficeUntil?: string | null; // ISO datetime; izin kapalıysa yok sayılır
}

export interface ChangePasswordResponse {
  message: string;
}
```

`fileKind` JSON’da string gelir (`"SIGNATURE"`). Route ve form alanında da aynı string kullanılır.

---

## `src/api/userService.ts`

```ts
import http from "./http";
import type {
  ChangePasswordRequest,
  ChangePasswordResponse,
  PagedResult,
  UpdateMeRequest,
  UpdateOutOfOfficeRequest,
  User,
  UserFileKind,
  UserFileMeta,
  UserMe,
} from "../types/user";

export const userService = {
  getMe: () => http.get<UserMe>("/api/users/me").then((r) => r.data),

  updateMe: (body: UpdateMeRequest) =>
    http.put<UserMe>("/api/users/me", body).then((r) => r.data),

  changePassword: (body: ChangePasswordRequest) =>
    http.put<ChangePasswordResponse>("/api/users/me/password", body).then((r) => r.data),

  updateOutOfOffice: (body: UpdateOutOfOfficeRequest) =>
    http.put<UserMe>("/api/users/me/out-of-office", body).then((r) => r.data),

  uploadMyFile: (fileKind: UserFileKind, file: File) => {
    const form = new FormData();
    form.append("fileKind", fileKind);
    form.append("file", file);
    return http.post<UserFileMeta>("/api/users/me/files", form).then((r) => r.data);
  },

  getMyFileMeta: (fileKind: UserFileKind) =>
    http.get<UserFileMeta>(`/api/users/me/files/${fileKind}`).then((r) => r.data),

  /** img src için blob URL. Unmount’ta URL.revokeObjectURL çağırın. */
  getMyFileBlobUrl: async (fileKind: UserFileKind) => {
    const res = await http.get(`/api/users/me/files/${fileKind}/content`, {
      responseType: "blob",
    });
    return URL.createObjectURL(res.data);
  },

  getList: (page = 1, pageSize = 20) =>
    http
      .get<PagedResult<User>>("/api/users", { params: { page, pageSize } })
      .then((r) => r.data),

  getById: (id: number) => http.get<User>(`/api/users/${id}`).then((r) => r.data),
};
```

`FormData` gönderirken `Content-Type`’ı elle `multipart/form-data` yazmayın; axios boundary’yi kendisi ekler.

---

## Dosya yükleme kuralları

- Form alanları: `fileKind` + `file` (ikisi de zorunlu)
- `fileKind`: `PROFILE_PHOTO` | `SIGNATURE`
- İzin verilen tipler: `image/png`, `image/jpeg`, `image/webp`
- Maksimum boyut: **2 MB**
- Disk yolu API cevabında **yoktur** (NAS/lokal path sızmaz)
- Yeni yükleme eski dosyayı “current” olmaktan çıkarır; geçmiş dosya diskte kalır
- Okuma yalnızca kendi dosyanız (`/me/files/...`)

Örnek `<input>`:

```tsx
<input
  type="file"
  accept="image/png,image/jpeg,image/webp"
  onChange={async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await userService.uploadMyFile("SIGNATURE", file);
  }}
/>
```

İmza/foto göstermek:

```tsx
const [url, setUrl] = useState<string | null>(null);

useEffect(() => {
  let revoked: string | null = null;
  userService.getMyFileBlobUrl("SIGNATURE").then((u) => {
    revoked = u;
    setUrl(u);
  });
  return () => {
    if (revoked) URL.revokeObjectURL(revoked);
  };
}, []);

return url ? <img src={url} alt="İmza" /> : null;
```

Dosya yoksa content/meta **404** — `hasSignature` / `hasProfilePhoto` `getMe()` ile kontrol edin, yoksa content çağırma.

---

## Hatalar (bu controller)

| HTTP | Ne zaman |
|------|----------|
| 400 | Şifre hatalı, yeni şifre eskiyle aynı, dosya tipi/boyut, boş dosya, doğrulama |
| 401 | Token yok / geçersiz |
| 403 | Liste/detay: `UserRead` yok ve müdür değil; veya müdür başka departmanın kullanıcısını açıyor |
| 404 | Kullanıcı yok, imza/foto yok |
| 429 | Rate limit (`GlobalLimit`) |

---

## UI önerisi

1. Profil sayfası: `getMe()` → form (`fullName`, `phone`, `position`) + `updateMe`
2. Şifre ayrı form: `changePassword` (e-posta gösterme, disabled)
3. İzinli toggle: `updateOutOfOffice` — kapatınca `outOfOfficeUntil` göndermeseniz de backend null’lar
4. İmza / foto: `hasSignature` ise blob URL, yoksa yükleme alanı
5. Kullanıcı listesi: admin (`UserRead`) tümünü görür; müdür yalnızca kendi departmanını. Normal çalışan `getList` çağırırsa 403.
