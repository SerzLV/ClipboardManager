# Open-Source Components

ClipVault Studio uses permissively licensed open-source software while retaining
the original licenses of every third-party component listed here. This public
repository contains product documentation and notices, not application source
code.

The Store build includes a redistributable `THIRD-PARTY-NOTICES.txt` file. This page is a readable inventory for users and reviewers; it is not legal advice.

## Production Dependencies

| Component | Version | Purpose | License | Project |
| --- | ---: | --- | --- | --- |
| AvalonEdit | 6.3.1.120 | Structured text and source editor | MIT | [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) |
| DiffPlex | 1.9.0 | Local side-by-side line and word comparison | Apache-2.0 | [DiffPlex](https://github.com/mmanela/diffplex) |
| HtmlAgilityPack | 1.12.4 | Link metadata HTML parsing | MIT | [Html Agility Pack](https://html-agility-pack.net/) |
| LLamaSharp | 0.27.0 | Local GGUF model inference | MIT | [LLamaSharp](https://github.com/SciSharp/LLamaSharp) |
| LLamaSharp.Backend.Cpu | 0.27.0 | CPU backend for local inference | MIT | [LLamaSharp](https://github.com/SciSharp/LLamaSharp) |
| MahApps.Metro | 2.4.11 | WPF controls and window styling | MIT | [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) |
| MahApps.Metro.IconPacks | 6.2.1 | WPF icon controls | MIT; source icon licenses also apply | [MahApps.Metro.IconPacks](https://github.com/MahApps/MahApps.Metro.IconPacks) |
| Markdig | 1.3.2 | Markdown parsing and rendering | BSD-2-Clause | [Markdig](https://github.com/xoofx/markdig) |
| Microsoft.Extensions.DependencyInjection | 10.0.8 | Dependency injection | MIT | [.NET](https://github.com/dotnet/runtime) |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.8 | Main SQLite persistence | MIT | [EF Core](https://github.com/dotnet/efcore) |
| Microsoft.Data.Sqlite | 10.0.8 | AI library SQLite access | MIT | [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) |
| Microsoft.SqlServer.TransactSql.ScriptDom | 180.37.3 | SQL parsing, formatting, and validation | MIT | [ScriptDOM](https://github.com/microsoft/SqlScriptDOM) |
| SQLitePCLRaw.bundle_e_sqlite3 | 3.0.3 | Native SQLite provider bundle | Apache-2.0 | [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) |

Supporting NuGet packages include MIT-licensed CommunityToolkit.HighPerformance, ControlzEx, Microsoft BCL/EF Core/Extensions/XAML Behaviors packages, System.Interactive.Async, and System.Linq.Async. SQLitePCLRaw supporting packages use Apache-2.0. SQLite itself is dedicated to the public domain.

## Icons

MahApps.Metro.IconPacks control code is MIT licensed. ClipVault Studio uses glyphs from icon projects that retain their own licenses:

- Lucide icons: ISC;
- Material Design Icons: Apache-2.0;
- Font Awesome Free icons: CC BY 4.0, with supporting code under MIT.

## Optional Qwen Model

AI Assist recommends `Qwen3-4B-Q4_K_M.gguf` from the official [Qwen3-4B-GGUF repository](https://huggingface.co/Qwen/Qwen3-4B-GGUF).

- Model family: Qwen3 4B.
- Quantization: `Q4_K_M`.
- Approximate download size: 2.5 GB.
- Expected SHA-256: `7485FE6F11AF29433BC51CAB58009521F205840F5B4AE3A32FA7F92E8534FDF5`.
- License: [Apache-2.0](https://huggingface.co/Qwen/Qwen3-4B-GGUF/blob/main/LICENSE).
- Delivery: downloaded only after an explicit user action.
- Packaging: not included in the Microsoft Store package, this documentation repository, or backups.
- Runtime: processed locally on the CPU through LLamaSharp; prompts and responses are not sent to a cloud AI endpoint.

ClipVault Studio verifies the expected model file with SHA-256 before first use. See the [Privacy Notice](../PRIVACY.md) for storage, network, and local-processing details.
