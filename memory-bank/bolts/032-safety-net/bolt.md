# Bolt 032 - Safety net

Status: complete (CI workflow not yet run on GitHub)

## Done
- **Web tests: 103 failing -> 0.** 542 pass. Causes, by kind:
  - Tests that checked an old design: rewritten against the real code (PlanForm, AddressForm, editor page, `useSave`, API client).
  - Test mistakes (checking state inside the same `act`, wrong expectations, stale spies): fixed.
  - **Real bugs the tests exposed, now fixed:**
    - `useShops`, `useAddressLookup` and `useSubscriptionPlans` called `fetch` with no sign-in token and the old API address, so the
      shops, address and plan-admin screens could not work against the real API. They now go through `authFetch` (token, one refresh and retry).
    - Form labels were not tied to their fields (`Input`, `Select`, PlanForm, AddressForm) - unusable with a screen reader.
    - AddressForm wiped the saved state and district when editing; sent ids as text; showed no error; used field names the API does not send.
    - PlanForm accepted a cleared price (NaN) and an annual price below the monthly price.
    - `useUndo` lost entries when two changes were made together and let the history position drift past 50 entries; `useSelection`
      lost entries on two quick additions.
    - `rectsIntersect` counted touching rectangles as overlapping.
- **API tests: 7 of the 46 quarantined files brought back** (+111 tests, Domain 443 -> 554). Real bugs found by them:
  - `GetPendingApprovalsForReviewerAsync` filtered on a computed property and would throw at run time.
  - The workflow detail reported decided approvals as "pending".
  - User search was case-sensitive when the term had capitals on some database providers.
- **Secrets out of the repository:** `appsettings.json` has no connection string or signing key; the API refuses to start without
  them; `appsettings.Development.example.json` is the template for local use.
- **CI:** `.github/workflows/ci.yml` (API build + 3 test projects, integration tests on a SQL Server container, web type check/tests/build).
- Baseline guidance for old databases written in docs/operations/deployment.md (untried on data).

## Not done (kept in backlog.md)
- 39 quarantined .NET test files remain (about 500 compile errors: they target models and APIs that were redesigned). Their
  behaviour is to be covered by HTTP-level integration tests in each area's bolt rather than by rewriting them one by one.
- The CI workflow has not run on GitHub. Docker images are still unbuilt.
