# VidBrary

A personal media catalog desktop application built with WPF and .NET 8. VidBrary scans your local directories for movies and TV shows, enriches them with metadata from [TMDb](https://www.themoviedb.org/), and presents everything in a clean, themeable interface.

## Features

- 🎬 **Movie & TV Show Cataloging** — Scan local folders and automatically organize your media library
- 🔍 **TMDb Metadata** — Fetch titles, posters, descriptions, cast, and more from The Movie Database
- 📺 **TV Season Support** — TMDB Auto or Manual (folder/filename) season ordering modes
- 👤 **User Profiles** — Multiple profiles with an auto-seeded Default profile on first launch
- 🎨 **Theming** — Dark, Light, and Follow Windows system theme modes
- 🖌️ **Custom Colors** — Override background, surface, accent, and secondary colors per-theme
- 🗂️ **Collections, People & Tags** — Browse and organize media by collections, cast/crew, and custom tags
- 📋 **My List** — Personal watchlist per profile
- ⚙️ **Auto-scan on Launch** — Optionally scan your library every time the app starts
- 📝 **Logging** — Rolling daily log files via Serilog

## Tech Stack

| Area | Library |
|---|---|
| UI Framework | WPF (.NET 8) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4.2 |
| Database | EF Core 9 + SQLite |
| Metadata | [TMDbLib](https://github.com/LordMike/TMDbLib) 3.0.0 |
| Media Info | [MediaInfo.Wrapper](https://github.com/AchimTuran/MediaInfoWrapper) |
| Logging | Serilog (File sink, rolling daily) |
| DI / Hosting | Microsoft.Extensions.Hosting 9 |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows 10 or later
- A [TMDb API key](https://www.themoviedb.org/settings/api)

### Build & Run
Clone the repo and run from the project directory:
git clone https://github.com/Auseroth/VidBrary cd VidBrary/MediaCatalog dotnet run


### Data Location

All application data is stored in:


### Data Location

All application data is stored in:
%ProgramData%\VidBrary
├── catalog.db       # SQLite database └── logs\            # Rolling daily log files


## Configuration

Configure media directories and preferences from the **Settings** page inside the app:

- **Movie Directories** — folders to scan for movie files
- **TV Show Directories** — folders to scan for TV episodes
- **Allowed Extensions** — file extensions to include during scans
- **Theme** — Dark / Light / Follow Windows
- **Custom Colors** — per-channel hex overrides (background, surface, accent, secondary)
- **Season Order Mode** — TMDB Auto or Manual (folder/filename)
- **Scan on Launch** — automatically scan on startup

## License

Source available — you're welcome to fork and modify it, but please don't redistribute it unchanged. See [LICENSE](LICENSE) for details.