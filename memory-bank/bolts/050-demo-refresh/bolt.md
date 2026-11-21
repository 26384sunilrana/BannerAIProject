---
id: 050-demo-refresh
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
started: 2026-11-21T09:59:46Z
current_stage: complete
stages_completed:
  - name: plan
    completed: 2026-11-21T10:01:59Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-11-21T11:20:24Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-11-21T11:52:49Z
    artifact: test-walkthrough.md
stories: [recheck-flows, refresh-script, rebuild-documents, refresh-guides]
created: 2026-11-21T08:39:37Z
requires_bolts: []
enables_bolts: []
blocks: false
---

# Bolt 050 - Demo and walkthrough refresh

**Goal**: the demo script, screenshots, Word walkthroughs and guides match the current application (including screens, two-step sign-in, ad bookings, takeover). Payment and email steps are left out: neither is part of the demo.

## Stories
1. **recheck-flows** - Run `demo/e2e-story.mjs` and `demo/take-screenshots.mjs` against the running stack; list every step that fails or no longer matches a screen (login now has two-step sign-in; Screens, Ad statement and Takeover pages exist).
2. **refresh-script** - Fix selectors and expectations; add steps for managed screens and anything new since bolt 045; drop any renewal-by-payment or email step.
3. **rebuild-documents** - Rebuild `docs/guides/walkthroughs/docx/*.docx` and the markdown walkthroughs (`admin.md`, `executive.md`, shop owner) from the new run.
4. **refresh-guides** - Re-read `docs/guides/*.md` against the UI and correct what changed.

## Notes
- Uncommitted work already in the tree: `demo/e2e-story.mjs`, `demo/build-docx.mjs`, `demo/package.json`, `docs/guides/walkthroughs/docx/`, edited `demo/take-screenshots.mjs`. Start from these.
- Needs the stack running (API http://localhost:5000, web http://localhost:3000) and an admin login (see docs/guides/start-here.md).
- Can run in parallel with 044a; both touch the Subscription screen only in the demo's expectations.
