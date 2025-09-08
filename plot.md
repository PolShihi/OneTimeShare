# Plan (plot.md) for ASP.NET Core MVC One‑Time File Sharing App

## 1) Problem Overview and Goals
- Build a simple, secure web app where a user logs in with a Google account, uploads a file, and gets a one‑time download link.
- The file must be deleted immediately after the first successful download.
- Implement automated cleanup of files older than 30 days to prevent storage from filling up.
- All secrets and settings must be sourced from environment variables.
- Use SQLite as the database for file metadata and operational data.

## 2) Key Functional Requirements
- Authentication
  - Sign in with Google (OAuth 2.0 / OpenID Connect) and use cookie authentication for session.
- Upload
  - Authenticated users can upload a file via web UI.
  - Enforce a configurable max upload size (environment variable).
  - Persist metadata in SQLite and the file on disk (or configurable storage root path via env).
- Generate one‑time download link
  - After upload, user receives a unique link that can be used once to download the file.
  - The link must be hard to guess (cryptographically strong token); link is not listed/browsable.
- One‑time download
  - On successful first download, delete the file from storage and mark metadata as deleted; link becomes invalid thereafter.
  - Must be concurrency‑safe (two people clicking at the same time only one succeeds).
- Cleanup policy
  - Background job deletes files and metadata older than 30 days (configurable via env).
  - Job runs at startup (catch‑up) and then periodically (e.g., daily, configurable).
- Configuration
  - All secrets and settings in environment variables. No secrets committed to repo.

## 3) Non‑Functional Requirements
- Security: OAuth 2.0/OpenID Connect with Google, CSRF protection for uploads, secure cookies, HTTPS, validation of inputs.
- Performance: Stream uploads/downloads to minimize memory usage; avoid buffering large files.
- Reliability: Safe deletion logic with transactions; robust cleanup service; idempotent operations.
- Observability: Structured logging; minimal metrics (number of uploads, deletions, failed downloads); health endpoint.
- Portability: SQLite DB file; local disk storage via configurable path; Docker support.

## 4) High‑Level Architecture
- ASP.NET Core MVC app (.NET 8 recommended)
  - Controllers + Razor Views for upload UI and status pages.
  - Authentication via Google + Cookie middleware.
  - Database access via EF Core with SQLite provider.
  - Services layer for storage, token generation, and one‑time download orchestration.
  - Background hosted service for periodic cleanup.
- Storage: Local filesystem under a configured root directory (default: ./storage). Each uploaded file saved by an internal ID, not original name.
- Database: SQLite file (default: ./App_Data/app.db). EF Core migrations manage schema.

## 5) Data Model (EF Core + SQLite)
- Entities
  - UserAccount
    - Id (string) — Google subject (sub claim) or local GUID keyed by Google sub
    - Email (string)
    - DisplayName (string?)
    - CreatedAtUtc (DateTime)
    - LastLoginAtUtc (DateTime)
  - StoredFile
    - Id (Guid)
    - OwnerId (string, FK -> UserAccount.Id)
    - OriginalFileName (string)
    - ContentType (string)
    - SizeBytes (long)
    - StoragePath (string) — relative path from storage root
    - UploadAtUtc (DateTime)
    - DeletedAtUtc (DateTime?) — set when one‑time download is completed or during cleanup
    - ExpiresAtUtc (DateTime?) — set to UploadAtUtc + 30 days (configurable)
    - OneTimeTokenHash (string) — hash of token (never store raw token)
    - TokenCreatedAtUtc (DateTime)
    - TokenUsedAtUtc (DateTime?) — when first successful download occurs
    - ConcurrencyStamp (string) — random value updated on state changes for optimistic concurrency

- Indexes and Constraints
  - Unique index on OneTimeTokenHash (to prevent duplicates); nullable allowed until token generation completed.
  - Index on ExpiresAtUtc for cleanup query.
  - Index on DeletedAtUtc for cleanup/query.

- Concurrency Handling Pattern (SQLite)
  - Use EF Core optimistic concurrency with a string `ConcurrencyStamp` or rowversion alternative.
  - Perform conditional update in a transaction: try to mark token as used (set TokenUsedAtUtc and DeletedAtUtc) only if TokenUsedAtUtc IS NULL and DeletedAtUtc IS NULL. Check affected rows == 1 before streaming file.
  - File deletion order: mark DB first, then delete file; if file deletion fails, a retryable cleanup pass will remove orphaned files.

## 6) Security Design
- Authentication
  - Use Google authentication (OpenID Connect) with OAuth 2.0. Store only sub, email, and optional name.
  - Cookie auth with secure, HttpOnly, SameSite=Lax (or Strict) cookies; sliding expiration (configurable).
- Authorization
  - Only authenticated users can upload; download link is bearer of knowledge (token), no login required.
- Token
  - One‑time download token is a cryptographically secure random string (>= 128 bits of entropy), URL‑safe.
  - Store only a hash (e.g., SHA‑256) and compare in constant‑time to avoid timing attacks; include per‑token salt.
  - Token lifetime optional; minimally bounded by 30‑day storage expiry.
- Input validation
  - Enforce max upload size at the server and via the MVC request size limit; optionally validate file types.
  - Sanitize original file name only for display; do not use it in storage path.
- Download headers
  - Set Content-Disposition with safe filename; set X-Content-Type-Options: nosniff; set Cache-Control: no-store.
- CSRF
  - Enable antiforgery on upload POST.
- HTTPS
  - Enforce HTTPS redirection and HSTS in production.
- Rate limiting
  - Optional: basic per-IP or per-user rate limiting for uploads and download attempts.

## 7) API and Routing Design
- UI Pages (Razor Views)
  - GET / — Home page with login CTA or upload form if authenticated.
  - GET /files — List user's uploaded files and statuses (optional, can be minimal)
  - GET /files/upload — Upload form.
  - POST /files/upload — Handle upload; returns page with success and the one‑time link.
  - GET /files/success/{id} — Show confirmation with copyable link and basic metadata.

- Download
  - GET /d/{fileId}?t={token}
    - Verifies token → DB lookup by token hash and fileId
    - Conditional DB update in transaction to mark as used+deleted
    - If update succeeded: stream file and delete from disk; if disk deletion fails, retry in background
    - If update failed: respond 410 Gone or 404 Not Found

- Auth
  - GET /auth/login → Challenge Google
  - GET /signin-google (callback) → Standard Google OIDC callback endpoint
  - POST /auth/logout → Sign out cookie

## 8) Services and Components
- IStorageService
  - SaveAsync(Stream file, string extension) → returns relative path, size
  - OpenReadAsync(string relativePath) → Stream
  - DeleteAsync(string relativePath)
- ITokenService
  - GenerateToken() → (tokenPlain, tokenHash, salt)
  - Verify(string tokenPlain, string tokenHash, string salt) → bool
- IFileService
  - CreateUploadRecordAsync(user, metadata, stream) → returns StoredFile and one‑time token (plain for presentation)
  - TryConsumeTokenAndDownloadAsync(fileId, token) → returns stream and safe filename OR a result enum (Success, AlreadyUsed, NotFound, Expired)
- CleanupHostedService (IHostedService / BackgroundService)
  - On a schedule, finds files where DeletedAtUtc != null (ensure disk removal) and where ExpiresAtUtc < now; deletes files and rows.
  - Also runs once at startup to reconcile any orphans.

## 9) Environment Variables and Configuration
- Google Auth
  - GOOGLE_CLIENT_ID
  - GOOGLE_CLIENT_SECRET
- Application
  - ASPNETCORE_ENVIRONMENT (Development/Production)
  - APP_BASE_URL (e.g., https://example.com)
  - STORAGE_ROOT (default ./storage)
  - SQLITE_CONN_STRING (default Data Source=./App_Data/app.db)
  - MAX_UPLOAD_BYTES (default 104857600 for 100 MB)
  - FILE_RETENTION_DAYS (default 30)
  - CLEANUP_INTERVAL_CRON or CLEANUP_INTERVAL_MINUTES (default 1440)
  - COOKIE_NAME (default .OneTimeShare.Auth)
  - COOKIE_SECURE (true/false; default true in Production)
  - LOG_LEVEL (default Information)

- Configuration Loading Order
  - Environment variables > appsettings.{Environment}.json > appsettings.json (no secrets in files; google secrets only via env)

## 10) UI/UX
- Simple, responsive pages using Bootstrap:
  - Home page: login link (if anonymous) or upload form (if authenticated)
  - Upload page: drag-and-drop and file picker; show max size
  - Success page: copyable one‑time link; warning that link works once only
  - Minimal error pages for 404/410 and general errors

## 11) Error Handling and Edge Cases
- Upload too large → 413 Payload Too Large (friendly message)
- Invalid/expired/used token → 410 Gone or 404 Not Found (avoid leaking existence)
- Concurrent downloads → only first succeeds (transaction ensures single use)
- Disk write failures → return error and do not persist DB record; log
- Disk delete failures → flag for retry; cleanup service handles later
- Database unavailable → fail gracefully with clear logs; present generic error page

## 12) Logging and Observability
- Structured logging via Microsoft.Extensions.Logging
- Log key events: login, upload completed, link issued, download attempted, download succeeded/failed, deletions
- Optional metrics counters (Prometheus endpoint behind a feature flag)
- Health checks: /health/live and /health/ready

## 13) Testing Strategy
- Unit tests
  - TokenService generation/verification
  - FileService upload logic (mock storage) and conditional token consumption
- Integration tests
  - EF Core with SQLite in-memory for metadata flows
  - End‑to‑end: upload then single download; verify second attempt fails
- Concurrency tests
  - Simulate two parallel download requests; assert only one succeeds
- Cleanup tests
  - Seed old files; run CleanupHostedService; assert files/rows removed
- Security tests
  - CSRF on upload, cookie flags, HTTPS redirects in Production, content disposition safety

## 14) Deployment and DevOps
- Dockerfile
  - Multi‑stage build (SDK → runtime), copy app, expose port 8080, `ASPNETCORE_URLS=http://+:8080`
  - Mount volume for STORAGE_ROOT and for SQLite DB (if persistent beyond container)
- Kubernetes (optional)
  - Single deployment; readiness/liveness probes
  - PersistentVolumeClaim for storage and DB (or ephemeral if acceptable)
- CI/CD
  - Build, run tests, publish image, run EF migrations on deploy
- Secrets management
  - Use environment variables injected by orchestrator or secret manager (e.g., Kubernetes Secrets)

## 15) Implementation Steps (Milestones)
1) Bootstrap ASP.NET Core MVC project and basic pages
2) Add Google auth + cookie authentication
3) Add EF Core + SQLite, implement migrations and schema for UserAccount and StoredFile
4) Implement StorageService (local filesystem) with safe paths
5) Implement TokenService (secure generation, hashing, constant‑time compare)
6) Implement FileService (upload flow and single‑use download flow)
7) Implement Upload page + controller actions + validations and size limits
8) Implement Download endpoint and streaming with transactional single‑use logic
9) Implement CleanupHostedService with schedule and startup reconciliation
10) Harden security (headers, HTTPS, CSRF, rate limiting, logging)
11) Add tests (unit, integration, concurrency, cleanup)
12) Dockerization and deployment docs

## 16) Open Questions / Decisions
- Should one‑time link have an additional short‑lived expiry (e.g., 7 days) separate from 30‑day retention?
- Any file type restrictions or virus scanning requirement?
- Should we allow anonymous uploads (no login) with stricter limits? (Current scope: auth required)
- Should users be able to revoke/delete a link before it’s used?
- Multi‑file upload support, or only single file per link?

## 17) Risks and Mitigations
- Token leakage: present token only once; do not log token; store only hash
- Race conditions on download: use transactional conditional update; verify affected rows == 1
- Storage growth: enforce max size and 30‑day cleanup; optional per‑user quotas
- Path traversal: store with GUID names and controlled directories; never trust original filename for paths
- Misconfiguration: validate env vars at startup; fail fast with clear error messages

## 18) Example Pseudocode Highlights
- Conditional single‑use update (EF Core):
  - Begin transaction
  - SELECT file WHERE Id = :id AND TokenUsedAtUtc IS NULL AND DeletedAtUtc IS NULL AND OneTimeTokenHash = :hash
  - If not found → 404/410
  - UPDATE same row: set TokenUsedAtUtc = now, DeletedAtUtc = now, ConcurrencyStamp = newGuid WHERE Id = :id AND TokenUsedAtUtc IS NULL AND DeletedAtUtc IS NULL
  - If affected rows == 1 → stream file; after streaming, try delete from disk; commit
  - Else → 410 Gone

## 19) Minimal Directory Layout (target)
- src/
  - OneTimeShare.Web/ (MVC app)
    - Controllers/
    - Views/
    - Services/
    - Models/
    - Data/
    - Hosted/
    - wwwroot/
  - OneTimeShare.Tests/
- storage/ (gitignored)
- App_Data/ (gitignored for db)
- Dockerfile
- .dockerignore

## 20) Documentation Deliverables
- README with setup, env vars, and run instructions
- SECURITY notes on token handling and headers
- OPERATIONS guide on cleanup, backups, rotations

---

This plan provides a clear path to implement the application with secure one‑time downloads, Google authentication, SQLite metadata, and automated cleanup governed by environment‑based configuration.