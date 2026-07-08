# Developer Guide

## Architecture Overview

RecallIQ uses a layered architecture with strict dependency rules:

- **RecallIQ.Core** — Shared models, interfaces, enums. No dependencies on other projects.
- **RecallIQ.Storage** — SQLite data access, vector serialization. Depends on Core.
- **RecallIQ.AI** — ONNX embedding generation, Tesseract OCR, text chunking. Depends on Core.
- **RecallIQ.Indexing** — Document parsing, file watching, indexing orchestration. Depends on Core, Storage, AI.
- **RecallIQ.Search** — Vector similarity search. Depends on Core, Storage, AI.
- **RecallIQ.UI** — WinUI 3 application. Depends on all projects.

## Key Design Decisions

### Vector Search
Embeddings are stored as byte arrays in SQLite. Cosine similarity computed with SIMD acceleration.

### Document Chunking
Documents split into overlapping chunks at sentence boundaries (256 tokens, 32 overlap).

### ONNX Fallback
Hash-based embeddings when no model file present — functional but lower quality.

### Concurrency
SemaphoreSlim for thread-safe DB access. Configurable parallelism for indexing.

## Adding a New Document Type

1. Create a parser implementing `IDocumentParser`
2. Add the type to `DocumentType` enum
3. Update `DocumentTypeExtensions`
4. Register the parser in DI configuration

## Adding a New Page

1. Create a ViewModel in `ViewModels/`
2. Create a XAML Page in `Pages/`
3. Register in DI, add NavigationViewItem in MainWindow
