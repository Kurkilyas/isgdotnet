# Frontend entegrasyon notu

Bu dosya, Fatura Takip backend’ine bağlanacak TypeScript (React / Next / Vite) istemcisi içindir. JSON alan adları **camelCase**.

## Temel kurallar

- Base URL: `import.meta.env.VITE_API_URL`
  - Sizin PC / LAN: `http://192.168.3.230:5161` (veya Vite proxy kullanıyorsanız boş bırakıp `/api` ile gidin)
  - Port **3000 kullanmayın** (dolu). Frontend **5173**, API **5161**.
- Tüm isteklerde `credentials: "include"` — refresh token **HttpOnly cookie** (`refresh_token`, path `/api/auth`)
- Access token **cevap gövdesinde** gelir; `localStorage`’a yazmayın, bellekte tutun
- Korunan API: `Authorization: Bearer {accessToken}`
- Access ~15 dk, refresh cookie **2 gün**
- Login / register / verify-email / refresh / logout **herkese açık**
- Diğer mevcut controller’lar `[Authorize]` — token yoksa 401
- İzin (`Permission`) attribute’ları henüz endpoint’lere bağlanmadı; token claim’inde gelecek, UI’da sonra kullanılacak

**Users / profil / imza API’si:** ayrı dosya → [FRONTEND_USERS.md](FRONTEND_USERS.md)

## Hata gövdesi

```ts
export interface ApiError {
  success: false;
  statusCode: number;
  error: string;
  debugInfo?: { detailedError?: string; stackTrace?: string } | null;
}
```

| HTTP | Anlam |
|------|--------|
| 400 | İş kuralı / doğrulama |
| 401 | Giriş yok veya token/şifre geçersiz |
| 403 | İzin yok (sonra) |
| 404 | Kayıt yok |
| 409 | Çakışma (e-posta kayıtlı) |
| 429 | Rate limit |
| 503 | Vega / e-posta / DB ulaşılamıyor |

## Önerilen klasör

```
src/
  api/
    http.ts              // axios instance + interceptor
    authService.ts
    userService.ts
    activityLogService.ts
    vegaService.ts
  types/
    api.ts
    auth.ts
    vega.ts
  auth/
    tokenStore.ts        // memory
```

---

## `src/types/api.ts`

```ts
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiError {
  success: false;
  statusCode: number;
  error: string;
}
```

## `src/types/auth.ts`

```ts
export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string; // min 8
}

export interface RegisterResponse {
  id: number;
  email: string;
  isVerified: boolean;
  message: string;
  developmentCode?: string | null; // sadece Development
}

export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface VerifyEmailResponse {
  id: number;
  email: string;
  isVerified: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
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

export interface AuthResponse {
  accessToken: string;
  accessExpiresAt: string; // ISO datetime
  user: User;
}

export type AuthActivityType =
  | "REGISTERED"
  | "LOGIN_SUCCESS"
  | "LOGIN_FAILED"
  | "LOGOUT"
  | "PASSWORD_CHANGED"
  | "PASSWORD_RESET_REQUESTED"
  | "PASSWORD_RESET_COMPLETED"
  | "EMAIL_VERIFIED"
  | "ACCOUNT_LOCKED"
  | "ACCOUNT_UNLOCKED"
  | "ROLE_ASSIGNED"
  | "ROLE_REVOKED"
  | "PROFILE_UPDATED"
  | "OUT_OF_OFFICE_CHANGED"
  | "FILE_UPLOADED";

export interface UserActivityLog {
  id: number;
  userId?: number | null;
  activityType: AuthActivityType | number;
  description?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
}
```

Not: `activityType` şu an varsayılan JSON ile **sayı** da gelebilir (enum sırası 0 = REGISTERED). İkisini de kabul edin.

## `src/types/vega.ts`

```ts
export interface TblCariListItem {
  ind: number;
  firmaKodu?: string | null;
  firmaAdi?: string | null;
  adresFatura?: string | null;
}

export interface TblMuhCariHesapKodlari {
  ind: number;
  firmaNo?: number | null;
  borcKisaVade?: string | null;
  alacakKisaVade?: string | null;
  borcUzunVade?: string | null;
  alacakUzunVade?: string | null;
  giderCesitKodu?: string | null;
}
```

Cari detay (`GET /api/tblcari/{id}`) çok alanlıdır; Swagger’dan üretin veya ihtiyaç duydukça ekleyin. Liste ekranı `TblCariListItem` yeter.

---

## `src/auth/tokenStore.ts`

```ts
let accessToken: string | null = null;

export const tokenStore = {
  get: () => accessToken,
  set: (token: string | null) => {
    accessToken = token;
  },
  clear: () => {
    accessToken = null;
  },
};
```

---

## `src/api/http.ts`

```ts
import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { tokenStore } from "../auth/tokenStore";
import type { AuthResponse } from "../types/auth";

const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

http.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshing: Promise<string | null> | null = null;

http.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    const status = error.response?.status;
    const url = original?.url ?? "";

    const isAuthAnonymous =
      url.includes("/api/auth/login") ||
      url.includes("/api/auth/register") ||
      url.includes("/api/auth/verify-email") ||
      url.includes("/api/auth/refresh") ||
      url.includes("/api/auth/logout");

    if (status === 401 && !original._retry && !isAuthAnonymous) {
      original._retry = true;
      try {
        refreshing ??= http
          .post<AuthResponse>("/api/auth/refresh")
          .then((r) => {
            tokenStore.set(r.data.accessToken);
            return r.data.accessToken;
          })
          .finally(() => {
            refreshing = null;
          });

        const newToken = await refreshing;
        if (newToken) {
          original.headers.Authorization = `Bearer ${newToken}`;
          return http(original);
        }
      } catch {
        tokenStore.clear();
        window.location.href = "/login";
      }
    }

    return Promise.reject(error);
  }
);

export default http;
```

---

## `src/api/authService.ts`

```ts
import http from "./http";
import { tokenStore } from "../auth/tokenStore";
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  VerifyEmailRequest,
  VerifyEmailResponse,
} from "../types/auth";

export const authService = {
  register: (body: RegisterRequest) =>
    http.post<RegisterResponse>("/api/auth/register", body).then((r) => r.data),

  verifyEmail: (body: VerifyEmailRequest) =>
    http.post<VerifyEmailResponse>("/api/auth/verify-email", body).then((r) => r.data),

  login: async (body: LoginRequest) => {
    const data = (await http.post<AuthResponse>("/api/auth/login", body)).data;
    tokenStore.set(data.accessToken);
    return data;
  },

  refresh: async () => {
    const data = (await http.post<AuthResponse>("/api/auth/refresh")).data;
    tokenStore.set(data.accessToken);
    return data;
  },

  logout: async () => {
    await http.post("/api/auth/logout");
    tokenStore.clear();
  },
};
```

Akış: kayıt → `developmentCode` (dev) veya e-posta → `verifyEmail` → `login` → `user` ile layout.

---

## `src/api/userService.ts`

```ts
import http from "./http";
import type { PagedResult } from "../types/api";
import type { User, UserFileKind, UserFileMeta, UserMe } from "../types/auth";

export const userService = {
  getMe: () => http.get<UserMe>("/api/users/me").then((r) => r.data),

  updateMe: (body: { fullName: string; phone?: string | null; position?: string | null }) =>
    http.put<UserMe>("/api/users/me", body).then((r) => r.data),

  changePassword: (body: { currentPassword: string; newPassword: string }) =>
    http.put("/api/users/me/password", body).then((r) => r.data),

  updateOutOfOffice: (body: { isOutOfOffice: boolean; outOfOfficeUntil?: string | null }) =>
    http.put<UserMe>("/api/users/me/out-of-office", body).then((r) => r.data),

  uploadMyFile: (fileKind: UserFileKind, file: File) => {
    const form = new FormData();
    form.append("fileKind", fileKind);
    form.append("file", file);
    return http
      .post<UserFileMeta>("/api/users/me/files", form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((r) => r.data);
  },

  getMyFileMeta: (fileKind: UserFileKind) =>
    http.get<UserFileMeta>(`/api/users/me/files/${fileKind}`).then((r) => r.data),

  /** img src için blob URL üret. */
  getMyFileContentUrl: async (fileKind: UserFileKind) => {
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

## `src/api/activityLogService.ts`

```ts
import http from "./http";
import type { PagedResult } from "../types/api";
import type { AuthActivityType, UserActivityLog } from "../types/auth";

export const activityLogService = {
  getList: (params?: {
    page?: number;
    pageSize?: number;
    userId?: number;
    activityType?: AuthActivityType | number;
  }) =>
    http
      .get<PagedResult<UserActivityLog>>("/api/useractivitylogs", { params })
      .then((r) => r.data),

  getById: (id: number) =>
    http.get<UserActivityLog>(`/api/useractivitylogs/${id}`).then((r) => r.data),
};
```

## `src/api/vegaService.ts`

```ts
import http from "./http";
import type { PagedResult } from "../types/api";
import type { TblCariListItem, TblMuhCariHesapKodlari } from "../types/vega";

export const vegaService = {
  getCariList: (page = 1, pageSize = 20) =>
    http
      .get<PagedResult<TblCariListItem>>("/api/tblcari", { params: { page, pageSize } })
      .then((r) => r.data),

  getCariById: (id: number) => http.get(`/api/tblcari/${id}`).then((r) => r.data),

  getHesapKodlariList: (page = 1, pageSize = 20) =>
    http
      .get<PagedResult<TblMuhCariHesapKodlari>>("/api/tblmuhcarihesapkodlari", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getHesapKoduById: (id: number) =>
    http.get<TblMuhCariHesapKodlari>(`/api/tblmuhcarihesapkodlari/${id}`).then((r) => r.data),
};
```

---

## Endpoint özeti

| Metod | Yol | Auth | Açıklama |
|-------|-----|------|----------|
| POST | `/api/auth/register` | Hayır | Kayıt, doğrulama kodu mail |
| POST | `/api/auth/verify-email` | Hayır | `{ email, code }` |
| POST | `/api/auth/login` | Hayır | Cookie set + `AuthResponse` |
| POST | `/api/auth/refresh` | Hayır (cookie) | Yeni access |
| POST | `/api/auth/logout` | Hayır (cookie) | Cookie sil |
| GET | `/api/users` | Bearer | `?page&pageSize` |
| GET | `/api/users/{id}` | Bearer | |
| GET | `/api/useractivitylogs` | Bearer | `?page&pageSize&userId&activityType` |
| GET | `/api/useractivitylogs/{id}` | Bearer | |
| GET | `/api/tblcari` | Bearer | Vega cari liste |
| GET | `/api/tblcari/{id}` | Bearer | Cari detay |
| GET | `/api/tblmuhcarihesapkodlari` | Bearer | |
| GET | `/api/tblmuhcarihesapkodlari/{id}` | Bearer | |

Fatura controller’ları henüz yok; mock kullanın.

## CORS / cookie / LAN

Backend CORS origin’leri `appsettings.json` → `Cors:Origins` (sunucuya geçince sadece orayı değiştirin):

- `http://localhost:5173`
- `http://192.168.3.230:5173`

API ağda: `http://192.168.3.230:5161` (Swagger: `http://192.168.3.230:5161/swagger`)

**Vite (`vite.config.ts`) — önerilen LAN kurulumu** (cookie aynı origin olsun diye proxy):

```ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    host: "0.0.0.0",
    port: 5173,
    proxy: {
      "/api": {
        target: "http://192.168.3.230:5161",
        changeOrigin: true,
      },
    },
  },
});
```

`.env`:

```
VITE_API_URL=
```

`http.ts` `baseURL` boş veya `window.location.origin` → istekler `http://192.168.3.230:5173/api/...` gider, Vite API’ye iletir. Çalışma arkadaşları tarayıcıda **http://192.168.3.230:5173** açar.

Windows Güvenlik Duvarı’nda **5161** ve **5173** inbound izin verin.

Geliştirmede login sonrası `accessToken`’ı kopyalayıp Swagger **Authorize** ile de deneyebilirsiniz.

## Rate limit

- Auth endpoint: IP başına 10/dk
- Diğerleri: kullanıcı başına 200/dk
- 429 → kullanıcıya “biraz bekleyin”
