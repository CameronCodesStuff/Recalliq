# Installation Guide

## System Requirements

- **OS**: Windows 10 version 1809 (build 17763) or later, or Windows 11
- **Architecture**: x64
- **Disk**: 500 MB minimum (plus space for the database)
- **RAM**: 4 GB minimum, 8 GB recommended

## Development Environment

### Required Software

1. **Visual Studio 2022** version 17.8 or later
   - Workload: .NET Desktop Development
   - Workload: Windows application development (Windows App SDK)

2. **.NET 9 SDK**
   - Download from https://dotnet.microsoft.com/download/dotnet/9.0

### Building from Source

```bash
git clone <repository-url>
cd RecallIQ
dotnet restore
dotnet build -c Release -p:Platform=x64
```

Or open `RecallIQ.sln` in Visual Studio 2022, select Release|x64, and build.

### Running Tests

```bash
dotnet test -c Release -p:Platform=x64
```

## Deployment

Build in Release mode and copy the output from:
```
RecallIQ.UI/bin/x64/Release/net9.0-windows10.0.22621.0/
```

### Optional: AI Model

Download the all-MiniLM-L6-v2 ONNX model and place it at:
```
<app-directory>/models/all-MiniLM-L6-v2.onnx
```

### Optional: Tesseract OCR

Download `eng.traineddata` and place it at:
```
<app-directory>/tessdata/eng.traineddata
```
