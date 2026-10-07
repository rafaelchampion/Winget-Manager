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
- **Supersedes / superseded by:** None.

## D-003: Prioritize Consumer Velvet UI/UX Overhaul in Phase 1
- **Date:** 2026-10-07
- **Mode:** Pitch
- **Question or idea:** Prioritize an end-to-end UI/UX overhaul focusing on Fluent 2 design craft, hero status surfaces, typography, and delight before continuing backend/security issues.
- **Verdict:** Adopt with changes
- **Confidence:** high
- **Reasons:**
  - Craft is the core product wedge: against alternatives with 10+ package managers, Winget-Manager's sole reason to exist is native Windows 11 elegance, speed, and zero intimidation.
  - Changes adopted: bound the work to a concrete design system audit, hero health card, segmented pill filters, and polished package card hierarchy using the `impeccable` skill.
- **Roadmap impact:** Inserts Issue #4 at the top of Phase 1 before Issue #2.
- **Dissent or risk accepted:** Minor delay to IPC hardening; fully acceptable given existing unit test pass and architecture stability.
- **Revisit trigger:** Manual review of overhauled UI in `staging`.
- **Supersedes / superseded by:** None.

