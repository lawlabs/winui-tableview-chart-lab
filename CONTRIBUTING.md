# Contributing

This repository is a native **WinUI 3 TableView and Chart** playground for **Windows App SDK 2.5.4-experimental**.

1. Install the prerequisites listed in [README](README.md).
2. Run `./BuildAndRun.ps1` and reproduce the behavior on the pinned SDK package.
3. Keep UI text in both `Strings/en-US/Resources.resw` and `Strings/ru-RU/Resources.resw`.
4. Run `./tests/repository-checks.ps1` and the relevant UI scenarios. Describe which SDK/API assertion and visual behavior you checked.
5. Update documentation when the component contract or observed preview limitations change.

Use English for issues, pull requests, and primary documentation. Include the Windows version, SDK package version, scenario, and reproduction steps for a bug report. Keep generated binaries, certificates, local logs, and diagnostic screenshots out of commits. Curated README screenshots belong in `docs/screenshots` and must show the English interface.

The lab intentionally uses direct SDK objects and event handlers to expose TableView and Chart APIs. Application-level streaming, point editing, and column reordering must be described as sample code rather than built-in SDK features.
