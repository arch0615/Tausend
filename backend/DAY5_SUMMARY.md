# Day 5 — backend integration testing + bug-fixing buffer

> Per the 30-Day Work Schedule: AM was "backend integration testing across all
> Phase 2 changes," PM was "bug-fixing buffer; freeze backend foundation
> before dashboard work starts." What actually happened diverged from that
> plan in one important way -- see "Scope note" below.

## Scope note: why this isn't just tests

The AM task turned up a live database connection string
(`Tausend.Core/ConnectionStrings.config`) pointed at `18.219.225.37:14444` --
the same host `UnitTesting/UDPTest.cs` hardcodes as the relay endpoint. That's
not an isolated test database; it looks like the actual production box. The
existing `AccountTesting.cs` suite creates and deletes real accounts when
run, so **nothing was executed against it this pass** -- every test added
today is written, reviewed, and registered in the project, but not run. You
need to run the suite yourself (`dotnet test` / VS Test Explorer) against a
real test database before trusting it, and ideally get `ConnectionStrings.config`
pointed at something that isn't the production server first.

While reviewing Days 1-3's code (not just the diffs) to write meaningful
integration tests, the review turned up real, exploitable authorization
vulnerabilities -- not test-coverage gaps. Given the product controls physical
alarm panels, those got fixed as part of today's "bug-fixing buffer" rather
than just documented. That's the bulk of today's work.

## AM: test suite additions (`backend/UnitTesting/`)

All written against the real `AccountBusiness`/`DeviceBusiness`/`AdminService`/
`DeviceService` classes (same pattern as the existing `AccountTesting.cs`),
using `[ClassInitialize]`/`[ClassCleanup]` rather than the existing file's
numbered-method-name convention, since MSTest doesn't actually guarantee
method execution order and the numbering was never a real guarantee.

- **`AccountRoleAndTokenTesting.cs`** -- Day 1 (roles model, token expiry,
  refresh tokens) + Day 2 (bcrypt, password reset). Covers: new accounts
  default to `EndUser`; login issues working access/refresh tokens; garbage
  tokens don't validate; refresh issues a new working token (using its own
  login, not shared class state, so it can't make other tests order-dependent
  if the stored procedure rotates tokens); garbage refresh tokens are
  rejected; wrong-old-password is rejected; a full password-reset round trip
  (bypassing the email send, pulling the token straight from the DAO) ends
  with a working login on the new password; garbage reset tokens are
  rejected; bcrypt hashing round-trips correctly.
- **`DeviceServiceAuthTesting.cs`** -- Day 3 auth checks + Day 4's system-key
  checks. Calls `DeviceService`/`NotificationService` directly with a garbage
  token (and, for relay-only endpoints, no live WCF context at all, so
  `SystemAuth.IsValidRequest()` is false the same way an unsigned request
  would be) and asserts `Unauthorized`.
- **`DeviceOwnershipTesting.cs`** -- new, added because of today's ownership
  fix (see below). Account A creates a device (via `DeviceDao` directly, to
  avoid `DeviceBusiness.CreateDevice`'s live-panel contact requirement);
  Account B, a real separate logged-in account, must not be able to read,
  update, delete, block, or disassociate it. This is the boundary
  `DeviceServiceAuthTesting.cs` deliberately doesn't cover (that file only
  proves "logged in as *someone*" is enforced, not "logged in as the *right*
  someone").
- **`AdminServiceTesting.cs`** -- Day 4 PM (fleet queries, role assignment).
  Bootstraps an admin directly via `AdminDao.SetAccountRole` (the thing under
  test is the endpoint's own authorization, not how someone becomes admin).
  Covers the 401-vs-403 fix below explicitly.
- **`SystemAuthTesting.cs`** -- Day 4 AM (relay shared-secret check). Exercises
  `SystemAuth.KeysMatch` directly (see below) -- no DB, no config file, no WCF
  host involved.

## PM: bugs found and fixed

### 1. Admin endpoints returned 403 for "not logged in," not just "logged in but not admin"

`AdminBusiness` (`Business/AdminBusiness.cs`) previously gated every method
on `AccountBusiness.IsAdmin()` alone, which returns `false` identically for
an invalid/expired token and for a valid non-admin token -- both landed on
`InformForbidden()`. This contradicted the Day 4 commit's own stated intent
("added because 'not logged in' and 'logged in but not allowed' were
previously indistinguishable... this is the first endpoint set where that
distinction actually matters") and the dashboard's own `ResponseState` type,
which already has separate `UNAUTHORIZED`/`FORBIDDEN` values. Fixed with a
`CheckAdmin` helper that checks `ValidateAccessToken` first (401) before
`IsAdmin` (403).

### 2. IDOR: several DeviceService endpoints never checked device *ownership*

This is the real finding. `DeleteDevice`, `GetDeviceByID`, `UpdateDevice`,
`BlockPIN` (both Block and Reset), and `DisassociateCentral` all checked "is
this a valid access token" and then acted on a caller-supplied `DeviceId`/
`Identifier` with **no check that the token's account is actually linked to
that device**. Any logged-in customer could, by guessing/incrementing a
device id or knowing another panel's identifier:

- Read (`GetDeviceByID`) or rewrite (`UpdateDevice`) another customer's panel
  description/identifier/PIN.
- Delete (`DeleteDevice`) another customer's panel outright.
- Block (`BlockPIN` Action=Block) or wipe (`BlockPIN` Action=Reset) another
  customer's panel -- both of which also force-logout every real owner
  (`AccessTokens`/`AccountDeviceTokens` deleted for every account linked to
  that device).
- Unlink (`DisassociateCentral`) another customer's panel from **every**
  account it was linked to, not just their own.

Also found in the same pass: `GetDeviceByIdentifier` in `DeviceService.svc.cs`
hardcoded `bz.GetDevice(device.Identifier, 0)` -- the `0` disables ownership
scoping that already existed in `GetDeviceByIdentifier.sql`, so this endpoint
was wide open despite the SQL-side logic looking correct on a skim.
`UpdateDeviceSMS.sql`'s "ownership check" derived the expected account id
from the very row being checked, so it could never actually reject a
cross-account edit. `DeleteDeviceSMS.sql` had no check at all.

**Fix**, applied consistently: the caller's account id is now resolved from
their access token (`AccountBusiness.ResolveAccountId`, which -- unlike
`AccountBusiness.GetAccount` -- goes through `dbo.ValidateAccessToken` and so
correctly enforces token expiry) and threaded through
`DeviceService.svc.cs` -> `DeviceBusiness` -> `DeviceDao` -> the stored
procedure, which now requires the account to actually be linked to the
device via `AccountDevicePins` (or `SMS_Devices.AccountId` for SMS devices).
Two endpoints are also called by the relay (`GetDeviceByID`/
`GetDeviceByIdentifier` for device lookups, `DisassociateCentral` for e.g. a
panel factory reset) via the Day 4 system key; those pass `accountId = 0` to
mean "internal/system caller, not scoped to one customer" -- `GetDevice.sql`
and `DisassociateCentral.sql` both special-case `0` to preserve that
existing unscoped/unlink-everyone behavior for the relay, while every other
caller is ownership-checked.

Changed: `AccountBusiness.cs` (new `ResolveAccountId`), `DeviceService.svc.cs`,
`DeviceBusiness.cs`, `DeviceDao.cs`, and the stored procedures `DeleteDevice`,
`UpdateDevice`, `GetDevice`, `BlockDevice`, `ResetDevice`,
`DisassociateCentral`, `UpdateDeviceSMS`, `DeleteDeviceSMS`.

### 3. Expired tokens still passed `GetAccount`-based checks

`dbo.ValidateAccessToken` checks `ExpirationDateTime`; `dbo.GetAccount` never
did, and `AccountBusiness.IsAdmin()` (feeding the admin endpoints) resolved
the caller through `GetAccount`. Fixed at the SQL layer (`GetAccount.sql`)
for defense in depth, on top of finding #1's fix (which now checks
`ValidateAccessToken` first anyway).

### 4. Password change didn't check `Accounts.Enabled`

`AccountDao.UpdateAccount()` (the change-password flow) verified the old
password but never checked whether the account was disabled -- unlike
`Login()`, which explicitly gates on `Enabled`. A disabled account whose
token hadn't yet expired (up to 7 days) could still change its own password.
Fixed in `AccountDao.cs` to match `Login()`'s check.

### 5. `CreateDeviceToken.sql` was still vulnerable to the Day 3 "malformed token" crash

Day 3 added `TRY_CAST` guards to the stored procedures comparing a
client-supplied `NVARCHAR` token against a `UNIQUEIDENTIFIER` column, to stop
a malformed (non-GUID) token from throwing a SQL conversion error. This one
was missed. Currently unreachable in practice (the caller validates the
token via the already-fixed `ValidateAccessToken.sql` first), but it's a
latent landmine if that ordering ever changes, and it contradicts the Day 3
commit's "all 7 affected stored procedures" claim. Fixed the same way, plus
added the same missing expiry check while in the file.

## Deploying this

Everything above is a **source change only** -- nothing was run against the
live database this pass (see "Scope note"). The `.sql` files under
`backend/TausendDB/StoredProcedures/` need to actually be applied
(re-`CREATE OR ALTER`/re-deployed) to the real SQL Server before any of the
C# fixes take effect; until then the C# layer will pass an `@AccountId`
parameter the currently-deployed stored procedures don't expect, which will
fail loudly (missing-parameter error) rather than silently -- so this can't
partially deploy without being obvious.

## Also flagged, not fixed (out of scope for this pass)

- `TausendServer2`/WinForms relay -- already covered in `RELAY_DECISION.md`.
- `CommandBusiness.Execute` / relay `ServiceBusiness.Execute` both set fields
  on a `null` response inside their own catch block on an HTTP failure (NRE
  masking the real error) -- also already flagged in `RELAY_DECISION.md`.
- The panel<->relay wire protocol's own weak authentication (16-bit XOR
  cipher) -- a firmware-level concern, out of scope for backend/SQL changes.
