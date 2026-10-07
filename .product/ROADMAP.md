# Roadmap: Winget-Manager

_Shippable target: Winget-Manager v1.0 certified and published on Microsoft Store (and available via GitHub Releases), enabling Windows 10/11 users to scan, 1-click update, discover/install packages, and manage pins/exclusions with zero terminal friction._
_WIP limit: 1 phase in progress._

## Phase 1: Core Update Engine & IPC Hardening
- **Outcome (shippable increment):** Resilient, crash-proof "Scan -> Queue -> Update All" workflow with secure IPC elevation and diagnostic drawer.
- **Scope:**
  - Robust winget CLI/COM execution: pass required agreements (`--accept-source-agreements --accept-package-agreements`), timeout handling, and unhandled exit codes.
  - Secure Named Pipe IPC between main UI and `WingetManager.Elevated.exe` (ACL verification, parameter validation to prevent command injection).
  - Diagnostic drawer in UI: collapsible drawer for raw technical logs without cluttering the clean UI.
  - Solution cleanup: remove redundant `WingetManager.slnx` or align build targets.
- **Validation gate:** Successful batch upgrade of 5+ diverse packages (MSI, Inno, EXE, portable) without UI hangs, crashes, or unhandled exceptions.
- **Security gate:**
  - STRIDE review on Named Pipe IPC (privilege escalation check).
  - CLI argument sanitization check on package IDs.
  - Pass/Fail record documented in issue.
- **Stop or pivot criterion:** If COM API proves too unstable across heterogeneous Windows 11 builds, fall back 100% to CLI repository with robust streaming output parser.
- **Issues:** #1 (IPC & Argument Hardening), #2 (Diagnostic Drawer & Error Telemetry UI), #3 (Winget Execution Resilience & Flags)
- **Depends on:** Phase 0 (Baseline architecture - complete)
- **Appetite:** 1-2 weeks

## Phase 2: Discovery, Pins & Settings Completion
- **Outcome (shippable increment):** Complete Search & Install experience, visual package indicators, and rock-solid Settings management.
- **Scope:**
  - SearchPage polish: rich search results cards with source badges, version info, and install progress state.
  - Pins and Exclusions enforcement: verify and test that pinned and excluded packages are strictly respected across all use cases.
  - Fallback icon/glyph system: elegant Fluent iconography when winget packages lack embedded icon URLs.
  - Settings backup/restore and log export feature.
- **Validation gate:** User can search, install a new app, pin an app version, exclude an app from updates, and confirm all constraints hold across rescans and app restarts.
- **Security gate:**
  - Package source constraint: verify installs originate only from authorized winget / msstore sources.
- **Stop or pivot criterion:** If package icons cannot be fetched reliably, default to clean categorized Fluent glyphs rather than broken image placeholders.
- **Issues:** #4 (Search & Install UX Polish), #5 (Pins & Exclusions Verification & Badging), #6 (Settings & Log Export)
- **Depends on:** Phase 1
- **Appetite:** 1 week

## Phase 3: Store Packaging, Compliance & Distribution (v1.0 Release)
- **Outcome (shippable increment):** Microsoft Store approved package and GitHub v1.0 release.
- **Scope:**
  - MSIX packaging configuration (Single-project MSIX / Store packaging).
  - High-resolution Fluent 2 visual assets (StoreLogo, Square logos, AppIcon).
  - Windows App Certification Kit (WACK) validation and fixes.
  - Store submission collateral: Privacy Policy document, app description, and screenshots.
  - GitHub Actions CI workflow for release builds.
- **Validation gate:** 100% pass on Windows App Certification Kit (WACK); successful installation and execution from clean Windows sandbox without developer mode.
- **Security gate:**
  - Binary signing verified.
  - Store `runFullTrust` capability declared and justified.
  - Privacy policy published confirming zero personal data collection.
- **Stop or pivot criterion:** If Store certification rejects child process elevation helper, pivot to explicit single-prompt elevation or standard external winget UAC trigger.
- **Issues:** #7 (MSIX Packaging & Visual Assets), #8 (WACK Certification & Privacy Policy), #9 (Release CI & Store Submission)
- **Depends on:** Phase 2
- **Appetite:** 1 week

## Deferred / killed
| Item | Reason | Revisit trigger |
|---|---|---|
| Multi-package-manager support (Scoop, Choco) | Killed: Violates core differentiation ("Consumer Velvet" native Winget focus, zero clutter) | Never (intentional non-goal) |
| Cloud telemetry & remote crash tracking | Deferred: Privacy-first local tool; zero personal data | User feedback requesting opt-in diagnostics |
| Background automated update scheduler | Deferred: Keep v1.0 strictly user-initiated to build trust | Community feature request after v1.0 |
| Custom installer scripting engine | Killed: Security risk; rely exclusively on official winget manifests | Never |

## Change log
| Date | Change | Reason | Decision |
|---|---|---|---|
| 2026-10-07 | Initial Roadmap created | Consolidate mode baseline for v1.0 Store release | D-002 |
