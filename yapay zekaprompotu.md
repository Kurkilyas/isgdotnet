# Görev: "Invoice DB" için EF Core Entity, DbContext ve DTO Sınıflarını Oluştur

## Bağlam
Projede iki ayrı veritabanı var:
- **Auth DB**: Mevcut `UserDbContext` (Users, Roles, Permissions, Departments, UserRoles, RolePermissions) — DOKUNMA, sadece referans için biliyorum.
- **Invoice DB**: Yeni oluşturulacak, aşağıdaki DBML şemasına göre. Bu görevde SADECE Invoice DB'yi oluşturacaksın.

İki veritabanı fiziksel olarak ayrı. Invoice DB'den Auth DB'ye olan ilişkiler gerçek FK DEĞİL, "SoftFK" (sadece int Id tutan, veritabanı seviyesinde FK constraint kurulmayan, uygulama katmanında doğrulanan) ilişkilerdir. Bu alanlar DBML'de yorum satırı olarak (`// Ref: ...`) belirtilmiştir, gerçek Ref değildir.

## Kaynak Şema (DBML)

Aşağıdaki DBML, tüm tabloları, kolonları, tipleri, index'leri ve ilişkileri (Ref) tanımlıyor. Bunu birebir referans alarak C# tarafına çevir:

[BURAYA docs/invoice_workflow_db_design.dbml dosyasının TAM İÇERİĞİNİ YAPIŞTIR]

## Yapman Gerekenler

### 1. Entity Sınıfları (`Entities/Invoice/*.cs`)
- Her DBML tablosu için 1 entity class oluştur (namespace: mevcut projenin namespace'ine uygun, `Entities` klasör yapısını takip et).
- Kolon isimlerini PascalCase C# property'e çevir (örn. `invoice_number` → `InvoiceNumber`).
- Veri tiplerini doğru eşle: `int` → `int`, `varchar(n)` → `string` (+ `[MaxLength(n)]`), `decimal(p,s)` → `decimal` (+ `[Column(TypeName = "decimal(p,s)")]`), `datetime` → `DateTime?` (nullable ise) / `DateTime` (not null ise), `date` → `DateOnly` ya da `DateTime` (projenin genel kullanımına bak, tutarlı ol), `bool` → `bool`, `text` → `string`.
- `not null` olmayan kolonlar → nullable property (`int?`, `string?`, `DateTime?`).
- SoftFK alanları (yorumdaki `// Ref:` satırlarına bakarak, örn. `assigned_user_id`, `auditor1_user_id`, `created_by_user_id` vb.) için gerçek EF Core navigation property EKLEME — sadece `int?` Id alanı olarak bırak, XML doc comment ile "SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok" diye belirt.
- Gerçek FK'lar (aynı Invoice DB içindekiler, `Ref: X.y > Z.id` olanlar) için hem Id alanı hem de navigation property (ve varsa collection navigation) ekle.
- Soft delete alanı olan (`deleted_at`) tüm entity'lere `DeletedAt` (DateTime?) property'si ekle; bunları ortak bir `ISoftDeletable` interface'inden türet (interface'i de oluştur).
- `created_at`/`updated_at` olan tüm entity'lere de ortak bir `IAuditableEntity` (CreatedAt, UpdatedAt) interface'i uygula.
- Enum'ları C# enum olarak ayrı bir `Enums/` klasöründe tanımla (aşağıdaki listeye göre), ama DB kolonunda `varchar` olarak saklandığı için entity property tipi enum olabilir (EF Core'da `HasConversion<string>()` ile Fluent API'de string'e çevrilecek, bunu DbContext'te belirt).

### 2. Enum Tanımları
DBML notlarında geçen enum'ları oluştur:
- `InvoiceStatus` (current_status alanında kullanılan değerler — DBML'de tam liste yok, akışa göre mantıklı bir set öner: Received, PendingErpCheck, ErrorReturned, PendingAssignment, InDepartmentChain, PendingAuditor1, PendingAuditor2, PendingAccounting, Archived, Completed, Rejected)
- `WorkflowActionType` (invoice_workflow_history.action_type + workflow_transition_rules.trigger_action için — StatusChanged, MissingDocumentFlagged, Auditor1Rejected, Auditor2Rejected, Approved, Completed vb.)
- `InvoiceRelationType` (DuplicateOf, PriceDifferenceOf, CreditNoteOf)
- `StepKind` (Fixed, DepartmentChainPlaceholder)
- `AssignmentMethod` (SupplierSingleCandidate, MlPrediction, Manual)
- `Currency` (Try, Usd, Eur)
- `ActivityType` (Viewed, OpenedAttachment, Downloaded, Commented, StatusChecked)
- `NotificationType` (Assigned, SlaReminder, SlaEscalated, Rejected)

Değer isimlerini DBML'deki notlardan çıkarsayarak mantıklı şekilde tamamla, eksik gördüğün yerde en makul ismi seç.

### 3. DbContext (`Data/InvoiceDbContext.cs`)
- `UserDbContext`'in kod stiline/konvansiyonlarına bak, aynı yapıyı takip et (constructor, OnModelCreating pattern vb.).
- Her entity için `DbSet<T>` tanımla.
- `OnModelCreating` içinde:
  - Tüm unique index'leri DBML'deki gibi tanımla (örn. `uq_invoice_dedup`, `uq_exchange_rate_day`, `uq_supplier_invoice_type`, `uq_invoice_type_step_order`, `uq_invoice_relation`).
  - Tüm normal index'leri tanımla (örn. `ix_workflow_steps_due`, `ix_workflow_steps_current_lookup`, `ix_transition_rule_lookup`).
  - Gerçek FK'ları `Ref:` satırlarındaki `delete:` davranışına göre `OnDelete(DeleteBehavior.X)` ile ayarla (cascade → Cascade, set null → SetNull, restrict → Restrict).
  - Enum'ları `HasConversion<string>()` ile string kolona çevir.
  - `decimal` kolonlar için `HasColumnType("decimal(p,s)")` DBML'deki hassasiyetlere birebir uy.
  - Global query filter ekle: `ISoftDeletable` uygulayan tüm entity'ler için `DeletedAt == null` filtresi (soft-deleted kayıtlar varsayılan sorgularda gelmesin).
  - `invoice_workflow_steps` tablosunda `step_definition_id` ve `department_step_id` için CHECK constraint ekle (EF Core 7+ `ToTable(b => b.HasCheckConstraint(...))` ile): ikisinden TAM OLARAK BİRİ dolu olmalı, ikisi birden null veya ikisi birden dolu OLAMAZ.

### 4. DTO Sınıfları (`DTOs/Invoice/*.cs`)
Her ana entity için ihtiyaç duyulabilecek temel DTO setlerini oluştur (CRUD ve listeleme senaryoları için):
- `{Entity}ResponseDto` — okuma/listeleme için (tüm alanlar, navigation'lar flat/nested DTO olarak).
- `Create{Entity}RequestDto` — oluşturma için (Id, CreatedAt/UpdatedAt, DeletedAt hariç, gerekli tüm alanlar).
- `Update{Entity}RequestDto` — güncelleme için (senin projenin update pattern'ine göre - genelde Id + değiştirilebilir alanlar).
- Özellikle şu entity'ler için DTO'lara özel dikkat et:
  - `Invoice`: liste ekranı için özet DTO (`InvoiceListItemDto` - id, invoice_number, supplier adı, tutar, current_status, current step bilgisi gibi sık kullanılan alanlar) ile detay DTO'yu (`InvoiceDetailDto` - tüm alanlar + line items + son workflow adımları + attachments) AYRI tut, hepsini tek dev DTO'da toplama.
  - `InvoiceWorkflowStep`: workflow motorunun action endpoint'lerinde kullanılacak `ApproveStepRequestDto`, `RejectStepRequestDto` gibi aksiyon bazlı DTO'lar da ekle (reason, result gibi alanlarla).
- DTO'larda validation attribute'ları ekle (`[Required]`, `[MaxLength]`, `[Range]` vb.) DBML'deki `not null` ve uzunluk kısıtlarına uygun şekilde.

### 5. Genel Kurallar
- Kod stilini mevcut projedeki (`UserDbContext.cs`, `Entities/*.cs`) dosyalarla TUTARLI tut — aynı isimlendirme, aynı using düzeni, aynı yorum stili.
- Gereksiz/anlamsız yorum ekleme, sadece SoftFK ve önemli iş kuralı notlarını (DBML'deki notlardan) XML doc comment olarak taşı.
- Migration OLUŞTURMA, sadece entity/DbContext/DTO kod dosyalarını üret; migration'ı ben ayrıca çalıştıracağım.
- Üretimin sonunda oluşturduğun dosyaların bir listesini ver (kaç entity, kaç DTO, hangi klasörlere yazıldı).

Başlamadan önce mevcut `UserDbContext.cs` ve `Entities/*.cs` dosyalarını incele, isimlendirme/klasör/stil konvansiyonlarını çıkar, sonra yukarıdaki adımları uygula.