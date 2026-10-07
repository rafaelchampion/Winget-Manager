# Product state: Winget-Manager

_Last updated: 2026-10-07_

## Vision
Winget-Manager transforms the underlying power of the Windows Package Manager (winget) into a delightful, consumer-grade "App Store & Maintenance Hub" for Windows 11.

## Goals (and how success is measured)
- **Zero-Intimidation Updates**: Users can scan and update their entire app ecosystem in 1 click without CLI friction. (Measure: 1-click update success rate and scan completion time)
- **Native Windows 11 Craft**: Fluent 2 design language with Mica material, rounded layered surfaces, and micro-animations. (Measure: UI consistency and UX friction audit)
- **Progressive Disclosure**: Plain-language diagnostics with terminal/CLI details collapsed into on-demand drawers. (Measure: reduction in confusing error reports)
- **Store Delivery**: Certified and available on the Microsoft Store as a native modern Windows app. (Measure: Microsoft Store certification approval and review ratings)

## Audience and job
- **Primary audience**: Everyday Windows consumers and PC enthusiasts.
- **The job they hire it for**: When my apps become outdated or need maintenance, I want a single, trustworthy visual hub to keep everything updated with one click, so I can save time, avoid security vulnerabilities, and steer clear of complex terminal commands.

## Positioning
For Windows consumers who need an effortless way to keep PC software updated, Winget-Manager is a native Windows 11 maintenance hub that delivers 1-click updates and polished visual diagnostics, unlike raw CLI wrappers or dense IT-admin utilities (such as UniGetUI).

## Monetization
- **Model**: Open-source desktop utility / community tool.
- **Pricing hypothesis**: 100% Free and open source on GitHub & Microsoft Store.
- **Unit economics (confidence: high)**: Local client execution; zero recurring infrastructure costs.

## Current phase and "shippable" target
- **Shippable Target (v1.0)**: Winget-Manager v1.0 certified on Microsoft Store (and mirrored on GitHub Releases), allowing any Windows 10/11 user to scan, batch-update, search/install packages, and manage pins/exclusions with seamless on-demand UAC handling.
- **Active phase**: Phase 1 — Core Update Engine & IPC Hardening (Validation gate: successful 5+ app batch upgrade without UI hang or crash).

## Roadmap
Full gated roadmap documented in [.product/ROADMAP.md](file:///d:/Arquivos/Documentos/Projetos/Winget-Manager/.product/ROADMAP.md).
- Phase 1: Core Update Engine & IPC Hardening (Active)
- Phase 2: Discovery, Pins & Settings Completion
- Phase 3: Store Packaging, Compliance & Distribution (v1.0 Release)

## Non-goals
- Multi-package manager support (Scoop, Chocolatey).
- Enterprise fleet remote management (MDM).
- Intrusive telemetry or ad-supported monetization.

## Kill / defer list
| Item | Decision | Reason | Revisit trigger |
|---|---|---|---|
| Multi-package managers | Kill | Preserves "Consumer Velvet" focus and reliability | Never |
| Cloud telemetry | Defer | Privacy-first local tool; zero personal data | User demand for crash reporting |
| Background scheduler | Defer | Build trust with explicit user-initiated updates first | Post-v1.0 community feedback |
| Custom installer scripting | Kill | Security risk; stick to official winget manifests | Never |

## Status doc
Linked to project product definition: [PRODUCT.md](file:///d:/Arquivos/Documentos/Projetos/Winget-Manager/PRODUCT.md).
