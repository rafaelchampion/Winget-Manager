# Mettle pipeline rule (always on)

Project config: `.product/config.yaml`. Plan and memory: `.product/STATE.md`, `.product/DECISIONS.md`, `.product/ASSUMPTIONS.md`. Use the `mettle` skill for strategy, Pitch, and test feedback.

1. **Every deliverable has an issue** with Context, Scope, Acceptance Criteria, Dependencies, and Security notes.
2. **Exactly one status label per issue:** `status: planned` -> `status: in progress` -> `status: in review` -> closed.
3. **Work only in a feature branch:** `feature/issue-<ID>-<name>`. Never commit directly to `main` or `staging`.
4. **On starting implementation,** set the label to `status: in progress` and tick the issue checklist as you advance.
5. **On finishing code:** merge the feature into `staging` and build/validate preview. Set the label to `status: in review` and post a manual test script with verification steps.
6. **Owner feedback** goes through `/test-feedback #ID`: post the report in the issue; if approved, merge to `main` and close the issue; if not, fix on the feature branch and retest; new requirements become issues.
7. **Large changes go through `/pitch` before an issue is opened:** anything that changes roadmap order or MVP scope, or touches security, elevation, or system modifications.
8. **Security gate:** no issue is closed as shippable until its applicable security checks are recorded.
9. Keep the project's status doc (`PRODUCT.md`) up to date alongside the issue flow.
