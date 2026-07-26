# Open-Source Components

ClipVault Studio 2.1.0 is built with permissively licensed open-source software. This inventory is generated from the production project references and the resolved NuGet dependency graph used by the Store build.

The application itself is licensed under MIT. Third-party projects and the optional AI model keep their own licenses. This page is a practical inventory, not legal advice. Redistributable notices are included with every build in `THIRD-PARTY-NOTICES.txt`.

## Direct Production Dependencies

| Component | Version | Purpose | License | Project |
| --- | ---: | --- | --- | --- |
| AvalonEdit | 6.3.1.120 | Structured text and source editor | MIT | [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) |
| DiffPlex | 1.9.0 | Side-by-side line and word comparison | Apache-2.0 | [DiffPlex](https://github.com/mmanela/diffplex) |
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

## Resolved Runtime Dependencies

The NuGet graph also resolves supporting packages used by the components above:

- CommunityToolkit.HighPerformance 8.4.2 and ControlzEx 4.4.0 under MIT;
- Microsoft BCL, Entity Framework Core, Extensions, XAML Behaviors, and tensor packages under MIT;
- System.Interactive.Async and System.Linq.Async 7.0.0 under MIT;
- SQLitePCLRaw core/config/provider packages 3.0.3 under Apache-2.0;
- SourceGear.sqlite3 3.50.4.5, which distributes SQLite; SQLite is dedicated to the public domain.

`MahApps.Metro.IconPacks` resolves its generated icon-pack assemblies. ClipVault Studio currently renders Lucide, Material, and Font Awesome glyphs. The control library is MIT; the original icon projects retain their licenses:

- Lucide icons: ISC;
- Material Design Icons: Apache-2.0;
- Font Awesome Free icons: CC BY 4.0, with supporting code under MIT.

## Optional Qwen Model

AI Assist recommends `Qwen3-4B-Q4_K_M.gguf` from the official [Qwen3-4B-GGUF repository](https://huggingface.co/Qwen/Qwen3-4B-GGUF).

- Model family: Qwen3 4B.
- Quantization: `Q4_K_M`.
- Approximate download size: 2.5 GB.
- Expected SHA-256: `7485FE6F11AF29433BC51CAB58009521F205840F5B4AE3A32FA7F92E8534FDF5`.
- License: Apache-2.0.
- Delivery: downloaded only after an explicit user action.
- Packaging: not included in the Microsoft Store package, GitHub releases, source repository, or backups.
- Runtime: processed locally on the CPU through LLamaSharp; prompts and responses are not sent to a cloud AI endpoint.

The application verifies the expected file with SHA-256 before first use. A future model replacement must update the model ID, download URL, hash, size, license link, privacy text, this inventory, and `THIRD-PARTY-NOTICES.txt` in the same release.

## License Files In Builds

`ClipboardManager/THIRD-PARTY-NOTICES.txt` is copied to build and publish output by `ClipboardManager.csproj`. It identifies the distributed libraries, selected icon sources, SQLite, and the separately downloaded Qwen model.

Before every Store release:

1. Compare `PackageReference` entries with this table.
2. Inspect `obj/project.assets.json` for changed transitive dependencies.
3. Confirm every license remains compatible with commercial distribution.
4. Keep Qwen marked as optional and separately downloaded unless packaging terms are reviewed again.
5. Rebuild and confirm `THIRD-PARTY-NOTICES.txt` is present in the packaged output.
