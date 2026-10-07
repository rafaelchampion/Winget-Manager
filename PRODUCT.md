# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Users
Everyday Windows consumers and PC enthusiasts who want a clean, effortless, visual experience for keeping their installed software up-to-date and discovering new apps without wrestling with command lines, cryptic package IDs, or terminal flags.

## Product Purpose
Winget-Manager transforms the underlying power of the Windows Package Manager (winget) into a delightful, consumer-grade "App Store & Maintenance Hub" for Windows 11. It provides 1-click updates, clear PC software health status, and reliable installation management.

## Positioning
Unlike raw CLI wrappers or dense IT-admin utilities that expose technical terminal output and cryptic package names, Winget-Manager feels like a native first-party Windows 11 app: humanized software titles, visual iconography, progressive disclosure of technical details, and effortless 1-click "Update All" automation.

## Operating Context
Windows 10/11 desktop environments, leveraging WinUI 3 (Windows App SDK 2.5) and .NET 10. Operates both in standard user mode and seamlessly interacts with elevated privileges (UAC) for installers requiring administrative rights.

## Capabilities and Constraints
- **Capabilities**: Automated background scanning for software updates, 1-click batch upgrade execution, visual queue with concurrent download/install tracking, app search and installation, version pinning, exclusions management, and detailed error diagnostics.
- **Constraints**: Relies on Microsoft `winget` engine and community repository manifests; must handle inconsistent package metadata, varying installer types (MSI, EXE, MSIX, portable), and UAC prompts gracefully.
- **Progressive Disclosure**: Plain-language status and error handling for everyday users, with technical console logs collapsed into on-demand diagnostic drawers.

## Brand Commitments
- **Design Metaphor**: Modern Fluent 2 "Consumer Velvet" (Windows 11 Mica materials, rounded layered surfaces, subtle elevation, warm spacing, purposeful micro-animations).
- **Tone & Voice**: Helpful, reassuring, non-intimidating, and swift. Never show raw terminal dumps or error codes unless the user expands the diagnostic drawer.

## Evidence on Hand
- Working WinUI 3 application with complete Clean Architecture (.NET 10, MVVM, Domain, Application, Infrastructure).
- Existing functional services for scanning, queue execution, search, version pins, exclusions, and elevation helper.

## Product Principles
1. **Zero-Intimidation Simplicity**: The primary action (keeping apps updated) should always be one click away with crystal-clear plain-language feedback.
2. **Native Windows 11 Craft**: Look and feel like an award-winning first-party Microsoft application using Fluent 2, Mica backdrop, subtle elevation, and responsive typography.
3. **Progressive Disclosure**: Shield users from CLI jargon and cryptic IDs by default, while keeping full diagnostic power one click away.
4. **Reliability & Trust**: Software installation touches system integrity. Always communicate clearly what is happening, handle errors gracefully, and respect user exclusions and pins.
