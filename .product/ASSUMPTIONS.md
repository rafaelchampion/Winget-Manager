# Assumptions

Status: unvalidated | validated | invalidated. Rank by risk (impact if wrong x lack of evidence).

| ID | Category | Assumption | Status | Evidence (rung 1-5) | Cheapest test | Pass / fail threshold | Last updated |
|---|---|---|---|---|---|---|---|
| A-001 | desirability | Everyday Windows users prefer a visual GUI over running `winget upgrade --all` in PowerShell | validated | 3 observed past behavior | Community adoption of winget GUI tools & user feedback | Active recurring usage | 2026-10-07 |
| A-002 | feasibility | WinUI 3 + winget CLI wrapper can reliably parse inconsistent manifest outputs across all package formats (MSI, EXE, MSIX, portable) | unvalidated | 2 stated intention | Batch upgrade test of 5+ heterogeneous packages in Phase 1 | >95% parse & upgrade execution accuracy | 2026-10-07 |
| A-003 | compliance-security | Elevating via UAC helper when required preserves system trust without alarming end users | unvalidated | 1 opinion | User testing UAC flow on elevation-requiring packages | Clear plain-language explanation before prompt triggers | 2026-10-07 |
| A-004 | compliance-security | Microsoft Store will certify a packaged Win32 application that includes a helper executable for UAC elevation | unvalidated | 2 stated intention | WACK scan & Store pre-submission compliance check in Phase 3 | Clean WACK pass & Store certification approval | 2026-10-07 |
| A-005 | desirability | Users value a dedicated, streamlined Winget experience more than a multi-manager hub (UniGetUI) | unvalidated | 2 stated intention | Early Store reviews & GitHub stars comparison | Positive feedback praising simplicity and performance | 2026-10-07 |

Categories: desirability, viability, feasibility, compliance-security.
Evidence ladder: 1 opinion, 2 stated intention, 3 observed past behavior, 4 commitment, 5 payment or repeated use.
