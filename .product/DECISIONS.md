# Decision log (append-only)

Never edit past entries. To change a decision, add a new entry that supersedes it and link both.

## D-001: Mettle system initialization
- **Date:** 2026-10-07
- **Mode:** Init
- **Question or idea:** Initialize Mettle product governance, state management, and pipeline rules for Winget-Manager.
- **Verdict:** Adopt
- **Confidence:** high
- **Reasons:** Project has substantial codebase (WinUI 3, .NET 10 Clean Architecture) and existing PRODUCT.md, benefiting from structured delivery, test feedback loops, and assumption tracking.
- **Roadmap impact:** Establishes `.product/` state structure and pipeline rule.
- **Dissent or risk accepted:** None.
- **Revisit trigger:** N/A
- **Supersedes / superseded by:** None.

## D-002: Consolidate Strategy, Shippable v1.0 Target & Store Distribution
- **Date:** 2026-10-07
- **Mode:** Consolidate
- **Question or idea:** Define positioning, scope, distribution channel, and elevation UX for v1.0 release.
- **Verdict:** Adopt
- **Confidence:** high
- **Reasons:**
  - Purpose: Free community open-source utility and craft showcase.
  - Positioning: Focused exclusively on Winget with native Fluent 2 / Mica "Consumer Velvet", rejecting multi-manager complexity (UniGetUI alternative).
  - Target: Microsoft Store release (MSIX packaged) with GitHub release mirroring.
  - Scope: Full core trio for v1.0 (Updates + Search/Install + Settings/Pins/Exclusions).
  - Elevation: Standard user execution default; on-demand UAC helper only when required by installer.
- **Roadmap impact:** Establishes 3-phase gated roadmap in `.product/ROADMAP.md`.
- **Dissent or risk accepted:** Microsoft Store review may scrutinize child process elevation; addressed via Phase 3 security gate and contingency pivot criterion.
- **Revisit trigger:** Phase 3 WACK / Store certification review.
- **Supersedes / superseded by:** None.
