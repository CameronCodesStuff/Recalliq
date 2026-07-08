# RecallIQ — AI-Powered Local File Search for Windows

RecallIQ is a production-quality Windows desktop application that indexes your documents and lets you search them using natural language queries. Everything runs locally — no cloud, no telemetry, no analytics.

## Features

- **Natural Language Search** — Ask questions like "Find the invoice from April" or "What document mentions Kubernetes?" and get ranked results with matching paragraphs.
- **Multi-Format Indexing** — Automatically indexes PDF, DOCX, TXT, and Markdown files from watched folders.
- **OCR Support** — Extracts text from scanned PDFs and image files (PNG, JPG, TIFF) using Tesseract.
- **AI Embeddings** — Generates vector embeddings with ONNX Runtime for semantic search.
- **SQLite Storage** — All data stored locally in a SQLite database with SIMD-accelerated vector similarity.
- **Real-Time Monitoring** — Watches folders for changes and automatically re-indexes modified files.
- **Modern UI** — Windows 11 Fluent Design with NavigationView, dark/light modes, and keyboard shortcuts.
- **Performance** — Multi-threaded indexing, incremental updates, cancellation support, and designed for 1M+ files.
- **Privacy** — Everything local. Zero telemetry. Zero cloud dependencies.

## Technology Stack

| Layer | Technology |
|-------|-----------|
| UI | WinUI 3, Fluent Design, MVVM |
| Framework | .NET 9, C# 13 |
| Storage | SQLite (Microsoft.Data.Sqlite) |
| AI | ONNX Runtime (all-MiniLM-L6-v2) |
| OCR | Tesseract 5 |
| PDF | PdfPig |
| DOCX | OpenXML SDK |
| Markdown | Markdig |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Serilog (rotating file logs) |
| Testing | xUnit, Moq, FluentAssertions |

## Project Structure

```
RecallIQ.sln
├── RecallIQ.Core        # Models, interfaces, enums, extensions
├── RecallIQ.Storage     # SQLite storage, vector operations
├── RecallIQ.AI          # ONNX embeddings, Tesseract OCR, text chunking
├── RecallIQ.Indexing    # File watchers, document parsers, indexing engine
├── RecallIQ.Search      # Vector similarity search
├── RecallIQ.UI          # WinUI 3 application, pages, view models
└── RecallIQ.Tests       # Unit tests (xUnit)
```

## Quick Start

### Prerequisites

- Windows 10 version 1809+ or Windows 11
- Visual Studio 2022 (17.8+) with **.NET Desktop Development** and **Windows App SDK** workloads
- .NET 9 SDK

### Build

1. Clone the repository
2. Open `RecallIQ.sln` in Visual Studio 2022
3. Set `RecallIQ.UI` as the startup project
4. Select **x64** platform
5. Build and run (F5)

### AI Model Setup (Optional)

RecallIQ works out of the box with hash-based fallback embeddings. For production-quality semantic search, download an ONNX model:

1. Download `all-MiniLM-L6-v2` from Hugging Face (ONNX format)
2. Place it at `models/all-MiniLM-L6-v2.onnx` relative to the executable
3. The model loads automatically on startup

### OCR Setup (Optional)

1. Download Tesseract trained data (`eng.traineddata`) from the tessdata repository
2. Place it in a `tessdata/` folder relative to the executable
3. OCR initializes automatically on startup

## Usage

1. Open **Settings** and add folders to watch
2. RecallIQ begins indexing automatically in the background
3. Navigate to **Search** and type a natural language query
4. Click any result to open the file or reveal it in Explorer
5. View all indexed documents on the **Documents** page
6. Monitor indexing progress on the **Activity** page

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+1 | Dashboard |
| Ctrl+2 | Search |
| Ctrl+3 | Documents |
| Ctrl+4 | Activity |
| Ctrl+Shift+S | Settings |

## Architecture

RecallIQ follows a clean layered architecture with dependency injection throughout:

```
UI Layer (WinUI 3 / MVVM)
    ↓
Service Layer (Search, Indexing)
    ↓
Data Layer (Storage, AI)
    ↓
Core Layer (Models, Interfaces)
```

All services are registered in the DI container and injected via constructors. The MVVM pattern with CommunityToolkit.Mvvm provides clean separation between UI and business logic.

### Search Pipeline

1. User enters a natural language query
2. Query is converted to a 384-dimensional embedding vector
3. All stored chunk embeddings are compared via SIMD-accelerated cosine similarity
4. Results are ranked by relevance score, deduplicated by document, and returned

### Indexing Pipeline

1. File watcher detects new/modified files in watched folders
2. Document parser extracts text (PDF, DOCX, TXT, MD, or OCR for images)
3. Text is split into overlapping chunks at sentence boundaries
4. Each chunk is embedded into a 384-dimensional vector
5. Document metadata and chunk embeddings are stored in SQLite

## License

MIT
