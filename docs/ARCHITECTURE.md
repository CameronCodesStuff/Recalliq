# Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        RecallIQ.UI                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │
│  │Dashboard │ │ Search   │ │Documents │ │   Settings   │   │
│  │  Page    │ │  Page    │ │  Page    │ │    Page      │   │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └──────┬───────┘   │
│       │             │            │               │           │
│  ┌────┴─────┐ ┌────┴─────┐ ┌────┴─────┐ ┌──────┴───────┐   │
│  │Dashboard │ │ Search   │ │Documents │ │  Settings    │   │
│  │ViewModel │ │ ViewModel│ │ ViewModel│ │  ViewModel   │   │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └──────┬───────┘   │
│       └─────────┬───┴────────────┴───────────────┘           │
│                 │ Dependency Injection                        │
├─────────────────┼────────────────────────────────────────────┤
│                 ▼                                            │
│  ┌──────────────────────┐  ┌──────────────────────┐          │
│  │  RecallIQ.Search     │  │  RecallIQ.Indexing   │          │
│  │  ┌────────────────┐  │  │  ┌────────────────┐  │          │
│  │  │VectorSearch    │  │  │  │IndexingService │  │          │
│  │  │Service         │  │  │  │                │  │          │
│  │  └────────────────┘  │  │  │FileWatcher     │  │          │
│  └──────────┬───────────┘  │  │                │  │          │
│             │              │  │DocumentParsers │  │          │
│             │              │  │ PDF│DOCX│TXT│MD│  │          │
│             │              │  │ PNG│JPG│TIFF   │  │          │
│             │              │  └────────────────┘  │          │
│             │              └──────────┬───────────┘          │
├─────────────┼─────────────────────────┼──────────────────────┤
│             ▼                         ▼                      │
│  ┌──────────────────────┐  ┌──────────────────────┐          │
│  │  RecallIQ.Storage    │  │  RecallIQ.AI         │          │
│  │  ┌────────────────┐  │  │  ┌────────────────┐  │          │
│  │  │SQLite DB       │  │  │  │ONNX Embeddings │  │          │
│  │  │Vector Ops      │  │  │  │Tesseract OCR   │  │          │
│  │  │Activity Log    │  │  │  │Text Chunker    │  │          │
│  │  └────────────────┘  │  │  └────────────────┘  │          │
│  └──────────────────────┘  └──────────────────────┘          │
├──────────────────────────────────────────────────────────────┤
│                      RecallIQ.Core                           │
│  Models │ Interfaces │ Enums │ Extensions │ Events           │
└──────────────────────────────────────────────────────────────┘
```

## Data Flow

### Indexing Flow
```
Folder → FileWatcher → DocumentParser → TextChunker → EmbeddingService → SQLite
```

### Search Flow
```
Query → EmbeddingService → CosineSimilarity(query, all chunks) → Ranked Results
```
