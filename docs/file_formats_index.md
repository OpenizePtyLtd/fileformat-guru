# File Formats Master Index: FileFormat Guru

> **Brand Context:** [FileFormat.com](https://www.fileformat.com/) is our knowledge base cataloging static file format specifications, structures, and developer documentation across **1,464 distinct formats and 32 categories**. [Aspose.com](https://www.aspose.com/) builds commercial enterprise-grade file format APIs.  
> **App Mission:** **FileFormat Guru** is an intelligent AI desktop assistant designed for **talking to files** (chat, question answering, summarization, and semantic RAG). It unifies both **open-source .NET/Node libraries** and **Aspose APIs**, enabling users to converse with any file format listed on `fileformat.com`.  
> **Machine-Readable Registry:** The full inventory of all 1,464 formats crawled directly from `docs.fileformat.com` is persisted in [`docs/fileformat_com_catalog.json`](file:///D:/Data/Aspose/GitHub/fileformat-studio/docs/fileformat_com_catalog.json) for automated engine resolution, metadata lookup, and in-app compatibility discovery.

---

## 1. Index Legend & Architecture

Each format in this index tracks:
- **Format / Extension**: The canonical extension and name as listed on `fileformat.com`.
- **Status**: 
  - `✅ Implemented`: Fully wired into `IDocumentParser`, chat attachments, and the vector ingestion pipeline.
  - `🔄 In Progress`: Engine spike or basic text extraction implemented, undergoing optimization.
  - `⏳ Planned`: Scheduled for implementation in Milestone 11 (M11) and subsequent releases.
- **Open Source Engine**: Recommended .NET, npm, or Python open-source library for extraction.
- **Aspose API**: Corresponding commercial Aspose library for enterprise high-fidelity parsing.
- **Extraction Modality for LLM Chat**: How content is converted into conversational context (e.g., Structured Text, Tabular Grid, Speech-to-Text Transcript, Embedded OCR, Vector Metadata).

---

## 2. File Format Master Registry

### 2.1 Word Processing & Document Formats
*Primary Conversational Use Case: Reading reports, legal contracts, manuals, and drafting documentation.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Word Open XML** | `.docx` | Word Processing | ✅ Implemented | `DocumentFormat.OpenXml`, `officeparser` | `Aspose.Words for .NET` | Paragraphs, headings, tables, embedded OCR |
| **Word 97-2003** | `.doc` | Word Processing | ✅ Implemented | `officeparser` (AST fallback) | `Aspose.Words for .NET` | Styled binary text flow, tables |
| **Word Macro-Enabled** | `.docm` | Word Processing | ✅ Implemented | `DocumentFormat.OpenXml` | `Aspose.Words for .NET` | Structured document content |
| **Word Template** | `.dotx`, `.dotm` | Word Processing | ✅ Implemented | `DocumentFormat.OpenXml` | `Aspose.Words for .NET` | Boilerplate & layout text |
| **Rich Text Format** | `.rtf` | Word Processing | ✅ Implemented | `RtfPipe` / regex streaming | `Aspose.Words for .NET` | Styled text, table blocks |
| **OpenDocument Text** | `.odt` | Word Processing | ✅ Implemented | `officeparser` | `Aspose.Words for .NET` | XML content stream, notes |
| **OpenDocument Template**| `.ott` | Word Processing | ⏳ Planned | `officeparser` | `Aspose.Words for .NET` | Template XML content stream |
| **Apple Pages** | `.pages` | Word Processing | ⏳ Planned | Zip stream + `index.xml` | `Aspose.Words for .NET` | Extracted XML document text |
| **Corel WordPerfect** | `.wpd` | Word Processing | ⏳ Planned | `libwpd` interop / binary reader | `Aspose.Words for .NET` | Filtered text stream |
| **MIME HTML Archive** | `.mhtml`, `.mht` | Word Processing | ✅ Implemented | `MimeKit` + HTML parsing | `Aspose.Words for .NET` | Multipart text & inline tables |

---

### 2.2 Spreadsheets & Financial / Tabular Data
*Primary Conversational Use Case: Asking analytical questions, querying financial tables, summarizing ledger metrics.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Excel Workbook** | `.xlsx` | Spreadsheet | ✅ Implemented | `ExcelDataReader`, `officeparser` | `Aspose.Cells for .NET` | Multi-sheet markdown tables & formulas |
| **Excel 97-2003** | `.xls` | Spreadsheet | ✅ Implemented | `ExcelDataReader` | `Aspose.Cells for .NET` | Sheet-by-sheet tabular grid |
| **Excel Binary** | `.xlsb` | Spreadsheet | ✅ Implemented | `ExcelDataReader` (Biff12) | `Aspose.Cells for .NET` | High-speed binary sheet streaming |
| **Excel Macro-Enabled** | `.xlsm` | Spreadsheet | ✅ Implemented | `ExcelDataReader` | `Aspose.Cells for .NET` | Data sheets & cell values |
| **Comma-Separated Values**| `.csv` | Spreadsheet / Data | ✅ Implemented | `CsvHelper` | `Aspose.Cells for .NET` | Clean tabular rows, column headers |
| **Tab-Separated Values** | `.tsv` | Spreadsheet / Data | ✅ Implemented | `CsvHelper` | `Aspose.Cells for .NET` | Tab-delimited row schema |
| **OpenDocument Spreadsheet**| `.ods` | Spreadsheet | ✅ Implemented | `officeparser` | `Aspose.Cells for .NET` | Sheet XML cells & tables |
| **Apple Numbers** | `.numbers` | Spreadsheet | ⏳ Planned | Snappy / Protobuf decomp | `Aspose.Cells for .NET` | Extracted sheet tables |

---

### 2.3 Presentations & Slide Decks
*Primary Conversational Use Case: Summarizing slide agendas, speaker notes, bullet points, and visual diagrams.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **PowerPoint Presentation**| `.pptx` | Presentation | ✅ Implemented | `officeparser`, `OpenXml` | `Aspose.Slides for .NET` | Per-slide bullets, shapes, speaker notes, OCR |
| **PowerPoint 97-2003** | `.ppt` | Presentation | ✅ Implemented | `officeparser` | `Aspose.Slides for .NET` | Slide text chunks & notes |
| **PowerPoint Show** | `.ppsx`, `.pps` | Presentation | ✅ Implemented | `officeparser` | `Aspose.Slides for .NET` | Display slide text & hierarchy |
| **PowerPoint Macro** | `.pptm`, `.potx` | Presentation | ✅ Implemented | `officeparser` | `Aspose.Slides for .NET` | Master layouts & slide text |
| **OpenDocument Presentation**| `.odp` | Presentation | ✅ Implemented | `officeparser` | `Aspose.Slides for .NET` | Slide XML text & annotations |
| **Apple Keynote** | `.key` | Presentation | ⏳ Planned | Zip archive + Protobuf | `Aspose.Slides for .NET` | Extracted slide content |

---

### 2.4 PDF, Fixed Layout & Page Description Formats
*Primary Conversational Use Case: Chatting with textbooks, legal briefs, whitepapers, scanned bills, and forms.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Portable Document Format**| `.pdf` | PDF / Page Layout | ✅ Implemented | `PdfPig`, `officeparser` | `Aspose.PDF for .NET` | Flow text, layout heuristics, page OCR |
| **XML Paper Specification** | `.xps`, `.oxps` | Page Layout | ⏳ Planned | `System.IO.Packaging` + XAML | `Aspose.Page for .NET` | Structured glyph runs & pages |
| **PostScript** | `.ps` | Page Description | ⏳ Planned | Ghostscript CLI / wrapper | `Aspose.Page for .NET` | Rendered text stream |
| **Encapsulated PostScript** | `.eps` | Page Description | ⏳ Planned | Postscript tokenizer | `Aspose.Page for .NET` | Vector text & metadata headers |

---

### 2.5 E-Books & Digital Publications
*Primary Conversational Use Case: In-depth reading companion, chapter summaries, character tracking, and quote lookup.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Electronic Publication** | `.epub` | EBook | ⏳ Planned | `VersOne.Epub` | `Aspose.Words / Aspose.HTML`| Chapter hierarchy, XHTML text, TOC |
| **Mobipocket / Kindle** | `.mobi`, `.azw`, `.azw3`| EBook | ⏳ Planned | PalmDOC stream unpacker | `Aspose.Words for .NET` | Flowing chapter text & metadata |
| **FictionBook 2.0** | `.fb2` | EBook | ⏳ Planned | `System.Xml.Linq` | `Aspose.Words for .NET` | Sectioned story XML & notes |
| **Comic Book Archive** | `.cbz`, `.cbr` | EBook / Comics | ⏳ Planned | `SharpCompress` + OCR | `Aspose.OCR for .NET` | Per-page OCR panel text |

---

### 2.6 Email, Messages & Personal Information
*Primary Conversational Use Case: Searching correspondence history, summarizing email threads, tracking action items.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Email Message** | `.eml` | Email | ⏳ Planned | `MimeKit` | `Aspose.Email for .NET` | Sender, recipients, subject, body, attachments |
| **Outlook Message** | `.msg` | Email | ⏳ Planned | `MsgReader` / `MimeKit` | `Aspose.Email for .NET` | Compound MAPI properties, RTF/HTML body |
| **Outlook Data File** | `.pst`, `.ost` | Email Storage | ⏳ Planned | `XstReader` | `Aspose.Email for .NET` | Folder hierarchy, threaded messages |
| **Mailbox Archive** | `.mbox` | Email Storage | ⏳ Planned | `MimeKit` (MboxReader) | `Aspose.Email for .NET` | Sequential email messages |
| **vCard Contact** | `.vcf` | Contacts | ⏳ Planned | `MixERP.Net.VCards` | `Aspose.Email for .NET` | Name, title, contact details, org |
| **iCalendar Schedule** | `.ics` | Calendar | ⏳ Planned | `Ical.Net` | `Aspose.Email for .NET` | Event agenda, attendees, recurrence |

---

### 2.7 Note Taking & Knowledge Capturing
*Primary Conversational Use Case: Synthesizing personal notes, meeting minutes, and knowledge bases.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Microsoft OneNote** | `.one`, `.onetoc2`| Note Taking | ⏳ Planned | OpenXml / MS-ONE spec parser | `Aspose.Note for .NET` | Note pages, indented outlines, tags |
| **Evernote XML Export** | `.enex` | Note Taking | ⏳ Planned | `System.Xml.Linq` | `Aspose.Words for .NET` | Note titles, timestamps, XML content |
| **Markdown Notes** | `.md`, `.markdown` | Note Taking | ✅ Implemented | `Markdig`, `PlainTextParser` | `Aspose.Words for .NET` | Headings, checklists, code blocks |

---

### 2.8 Project Management & Task Scheduling
*Primary Conversational Use Case: Querying project timelines, critical path tasks, resource allocation, and deadlines.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Microsoft Project** | `.mpp`, `.mpt` | Project Management| ⏳ Planned | `MPXJ` | `Aspose.Tasks for .NET` | WBS tasks, dates, dependencies, resources |
| **Project XML Interchange** | `.xml` (MSPDI) | Project Management| ⏳ Planned | `MPXJ` / `System.Xml` | `Aspose.Tasks for .NET` | Gantt task trees, milestones |
| **Primavera Project** | `.xer` | Project Management| ⏳ Planned | `MPXJ` | `Aspose.Tasks for .NET` | Activities, WBS hierarchy, costs |

---

### 2.9 Diagrams & Visual Workflows
*Primary Conversational Use Case: Explaining architecture diagrams, process flowcharts, and system schematics.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Microsoft Visio** | `.vsdx`, `.vsd` | Diagrams | ⏳ Planned | `System.IO.Compression` + XML | `Aspose.Diagram for .NET` | Shape labels, connectors, page diagrams |
| **Visio Stencils / Templates**| `.vssx`, `.vstx` | Diagrams | ⏳ Planned | Open Packaging XML | `Aspose.Diagram for .NET` | Master shape text & metadata |
| **Draw.io Diagram** | `.drawio` | Diagrams | ⏳ Planned | XML / Deflate reader | `Aspose.Diagram for .NET` | Graph nodes, edges, labels |

---

### 2.10 Archives, Bundles & Container Formats
*Primary Conversational Use Case: Unpacking multi-file bundles to talk across all contained documents simultaneously.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **ZIP Archive** | `.zip` | Archive | ⏳ Planned | `System.IO.Compression`, `SharpZipLib` | `Aspose.ZIP for .NET` | Recursive file tree & inner file text |
| **TAR / GZ Archive** | `.tar`, `.gz`, `.tgz`| Archive | ⏳ Planned | `SharpCompress` | `Aspose.ZIP for .NET` | Extracted inner document contents |
| **7-Zip Archive** | `.7z` | Archive | ⏳ Planned | `SharpCompress` | `Aspose.ZIP for .NET` | Nested document tree extraction |
| **RAR Archive** | `.rar` | Archive | ⏳ Planned | `SharpCompress` (unrar) | `Aspose.ZIP for .NET` | Contained documents extraction |

---

### 2.11 Programming, Web & Structured Configuration
*Primary Conversational Use Case: Code explanation, bug hunting, API documentation parsing, and config analysis.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **HyperText Markup** | `.html`, `.htm` | Web / Markup | ✅ Implemented | `HtmlAgilityPack`, `PlainTextParser` | `Aspose.HTML for .NET` | Clean DOM text, headings, hyperlinks |
| **JavaScript Object Notation**| `.json` | Data / Config | ✅ Implemented | `System.Text.Json` | `Aspose.Cells for .NET` | Pretty-printed JSON, key-value tree |
| **YAML / TOML** | `.yaml`, `.yml`, `.toml`| Data / Config | ✅ Implemented | `YamlDotNet`, `PlainTextParser` | `Aspose.Words for .NET` | Hierarchical configuration outline |
| **Extensible Markup** | `.xml` | Data / Markup | ✅ Implemented | `System.Xml.Linq` | `Aspose.Words for .NET` | Semantic element text & attributes |
| **Source Code (C#, Python, JS, etc.)**| `.cs`, `.py`, `.js`, `.ts`, `.cpp`, `.java`, `.go`, `.rs`, `.sql`, `.sh`, `.ps1` | Source Code | ✅ Implemented | Native streaming text reader | `Aspose.Words for .NET` | Code blocks with syntax context & line numbers |

---

### 2.12 Images & Visual Documents (OCR & Vision AI)
*Primary Conversational Use Case: Extracting text from screenshots, scanned invoices, diagrams, and photos.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Portable Network Graphics**| `.png` | Image / OCR | ✅ Implemented | `Windows.Media.Ocr`, `SkiaSharp` | `Aspose.OCR for .NET` | Extracted OCR text + AI diagram description |
| **JPEG Image** | `.jpg`, `.jpeg` | Image / OCR | ✅ Implemented | `Windows.Media.Ocr`, `SkiaSharp` | `Aspose.OCR for .NET` | Extracted OCR text + AI diagram description |
| **Bitmap** | `.bmp` | Image / OCR | ✅ Implemented | `Windows.Media.Ocr` | `Aspose.OCR for .NET` | OCR line & word segmentation |
| **Tagged Image File** | `.tiff`, `.tif` | Image / OCR | ✅ Implemented | `Windows.Media.Ocr` (multi-frame) | `Aspose.OCR / Aspose.Imaging` | Multi-page OCR text streams |
| **WebP Image** | `.webp` | Image / OCR | ⏳ Planned | `SkiaSharp` + OCR | `Aspose.Imaging for .NET` | OCR & vision captions |
| **Photoshop Document** | `.psd` | Graphics | ⏳ Planned | `PaintDotNet.Loaded` / Skia | `Aspose.PSD for .NET` | Layer names, text layers, visual OCR |

---

### 2.13 Audio & Voice (Conversational Speech Transcripts)
*Primary Conversational Use Case: Talking to podcast episodes, recorded meetings, voice notes, and lectures.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **MP3 Audio** | `.mp3` | Audio / Speech | ⏳ Planned | `Whisper.net` + `TagLibSharp` | `Aspose.Audio API` | Timestamped speech-to-text transcript & ID3 |
| **WAV Audio** | `.wav` | Audio / Speech | ⏳ Planned | `Whisper.net` | `Aspose.Audio API` | High-fidelity speech transcription |
| **AAC / M4A Audio** | `.m4a`, `.aac` | Audio / Speech | ⏳ Planned | `Whisper.net` (MediaFoundation) | `Aspose.Audio API` | Spoken audio transcript & chapters |
| **FLAC Audio** | `.flac` | Audio / Speech | ⏳ Planned | `Whisper.net` | `Aspose.Audio API` | Lossless audio speech-to-text |
| **Ogg Vorbis** | `.ogg` | Audio / Speech | ⏳ Planned | `Whisper.net` | `Aspose.Audio API` | Voice transcript & audio metadata |

---

### 2.14 Video & Subtitles (Media Chat)
*Primary Conversational Use Case: Inquiring about webinar contents, video tutorials, and conference presentations.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **SubRip Subtitles** | `.srt` | Subtitles | ⏳ Planned | Subtitle parser stream | `Aspose.Words for .NET` | Timecoded dialog & speaker script |
| **WebVTT Subtitles** | `.vtt` | Subtitles | ⏳ Planned | WebVTT parser | `Aspose.Words for .NET` | Timestamped dialogue flow |
| **MP4 Video** | `.mp4` | Video | ⏳ Planned | Windows MediaFoundation audio track demux + `Whisper.net` | `Aspose.Video API` | Extracted speech transcript & chapter cues |
| **MKV / AVI Video** | `.mkv`, `.avi` | Video | ⏳ Planned | Audio stream demux + `Whisper.net` | `Aspose.Video API` | Audio transcript & embedded subtitle tracks |

---

### 2.15 CAD & 3D Engineering Models
*Primary Conversational Use Case: Inquiring about architectural floorplans, mechanical component dimensions, and 3D meshes.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **AutoCAD Drawing** | `.dwg`, `.dxf` | CAD | ⏳ Planned | `netDxf` (DXF) | `Aspose.CAD for .NET` | Layer names, entity counts, dimensions, block text |
| **3D Wavefront** | `.obj` | 3D Model | ⏳ Planned | `SharpGLTF` / custom text parser | `Aspose.3D for .NET` | Material names, vertex counts, mesh structure |
| **Stereolithography** | `.stl` | 3D Model | ⏳ Planned | Binary/ASCII STL reader | `Aspose.3D for .NET` | Facet counts, bounding box, volume statistics |
| **GL Transmission** | `.gltf`, `.glb` | 3D Model | ⏳ Planned | `SharpGLTF` | `Aspose.3D for .NET` | Scene node hierarchy, mesh names, animations |

---

### 2.16 GIS & Spatial Data
*Primary Conversational Use Case: Asking geographical questions, boundary queries, GPS track analysis.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Keyhole Markup** | `.kml`, `.kmz` | GIS | ⏳ Planned | `SharpKml` | `Aspose.GIS for .NET` | Placemarks, coordinates, feature descriptions |
| **GPS Exchange** | `.gpx` | GIS | ⏳ Planned | `System.Xml.Linq` | `Aspose.GIS for .NET` | Waypoints, track statistics, elevations, routes |
| **GeoJSON** | `.geojson` | GIS | ⏳ Planned | `GeoJSON.Net` / `System.Text.Json` | `Aspose.GIS for .NET` | Feature properties, geometry summary |
| **ESRI Shapefile** | `.shp`, `.dbf` | GIS | ⏳ Planned | `NetTopologySuite.IO.Esri` | `Aspose.GIS for .NET` | Attribute table records & layer geometries |

---

### 2.17 Database & Structured Data Containers
*Primary Conversational Use Case: Conversing with offline databases, data dumps, and tabular record sets.*

| Format | Extension | Category | Status | Open-Source Engine | Aspose API | Extraction Modality |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **SQLite Database** | `.sqlite`, `.db` | Database | ⏳ Planned | `Microsoft.Data.Sqlite` | `Aspose.Cells for .NET` | Schema introspector, table sample rows, SQL query |
| **Apache Parquet** | `.parquet` | Analytics Data | ⏳ Planned | `Parquet.Net` | `Aspose.Cells for .NET` | Columnar schema, row counts, statistical sample |
| **Microsoft Access** | `.mdb`, `.accdb` | Database | ⏳ Planned | `System.Data.OleDb` / Jackcess | `Aspose.Cells for .NET` | Table listings & row extractions |

---

## 3. Comprehensive Category Breakdown from `docs.fileformat.com`

Crawled directly from the live knowledge base at [`docs.fileformat.com`](https://docs.fileformat.com/), the site documents **1,464 distinct file formats** across **32 primary categories**. The complete format dataset is recorded in [`docs/fileformat_com_catalog.json`](file:///D:/Data/Aspose/GitHub/fileformat-studio/docs/fileformat_com_catalog.json).

| Category | Formats on `docs.fileformat.com` | Primary Aspose API | Recommended Open-Source Engine | Status in FileFormat Guru |
| :--- | :---: | :--- | :--- | :---: |
| **Web** | 148 | Aspose.HTML for .NET | HtmlAgilityPack / AngleSharp | ✅ Implemented (Core) / ⏳ Expanding |
| **Programming** | 129 | Aspose.Words (Syntax formatting) | Native UTF-8 Stream Reader | ✅ Implemented (Core) / ⏳ Expanding |
| **Game** | 109 | Aspose.3D / Aspose.ZIP | Binary asset decoders | ⏳ Planned |
| **Video** | 96 | Aspose Video APIs | Whisper.net (Demux STT) | ⏳ Planned (M11) |
| **Database** | 92 | Aspose.Cells (Data grids) | Microsoft.Data.Sqlite / Parquet.Net | ⏳ Planned (M11) |
| **Compression / Archives** | 89 | Aspose.ZIP for .NET | SharpCompress / SharpZipLib | ⏳ Planned (M11) |
| **Audio** | 76 | Aspose Audio APIs | Whisper.net / TagLibSharp | ⏳ Planned (M11) |
| **GIS** | 74 | Aspose.GIS for .NET | NetTopologySuite / SharpKml | ⏳ Planned |
| **Misc** | 66 | Aspose Total Suite | Pluggable stream readers | ⏳ Planned |
| **EBook** | 65 | Aspose.Words / Aspose.HTML | VersOne.Epub / SharpZipLib | ⏳ Planned (M11) |
| **Spreadsheet** | 60 | Aspose.Cells for .NET | ExcelDataReader / CsvHelper | ✅ Implemented (Core) / ⏳ Expanding |
| **CAD** | 49 | Aspose.CAD for .NET | netDxf / LibreCAD C# | ⏳ Planned |
| **3D** | 44 | Aspose.3D for .NET | SharpGLTF / AssimpNet | ⏳ Planned |
| **System** | 38 | Aspose Total Suite | System diagnostics / Windows APIs | ⏳ Planned |
| **Data** | 32 | Aspose Total Suite | System.Text.Json / YamlDotNet | ✅ Implemented (Core) / ⏳ Expanding |
| **Executable** | 32 | Aspose Total Suite | PE file inspector / symbol reader | ⏳ Planned |
| **Word Processing** | 30 | Aspose.Words for .NET | DocumentFormat.OpenXml / officeparser | ✅ Implemented (Core) / ⏳ Expanding |
| **Disc and Media** | 29 | Aspose.ZIP for .NET | DiscUtils | ⏳ Planned |
| **Font** | 25 | Aspose.Font for .NET | Typography.OpenFont | ⏳ Planned |
| **Email** | 14 | Aspose.Email for .NET | MimeKit / MailKit | ⏳ Planned (M11) |
| **Visio** | 13 | Aspose.Diagram for .NET | System.IO.Packaging / SharpVectors | ⏳ Planned (M11) |
| **Presentation** | 13 | Aspose.Slides for .NET | officeparser / OpenXml | ✅ Implemented (Core) / ⏳ Expanding |
| **Page Description Language**| 13 | Aspose.Page for .NET | System.IO.Packaging / Ghostscript | ⏳ Planned |
| **Image** | 98 | Aspose.Imaging / Aspose.OCR | Windows.Media.Ocr / SkiaSharp | ✅ Implemented (Core) / ⏳ Expanding |
| **PDF** | 8 | Aspose.PDF for .NET | PdfPig / officeparser | ✅ Implemented (Core) / ⏳ Expanding |
| **Reporting** | 5 | Aspose.Words / Cells Reporting | RDL Parser / FastReport Open | ⏳ Planned |
| **Project Management** | 5 | Aspose.Tasks for .NET | MPXJ | ⏳ Planned (M11) |
| **Finance** | 4 | Aspose.Finance for .NET | JeffFerguson.Gepsio (XBRL) | ⏳ Planned |
| **Fonts** | 3 | Aspose.Font for .NET | Typography.OpenFont | ⏳ Planned |
| **Note Taking** | 2 | Aspose.Note for .NET | Markdig / OpenXml | ✅ Implemented (Core) / ⏳ Expanding |
| **Publisher** | 2 | Aspose.PUB for .NET | libmspub port | ⏳ Planned |
| **Output / Styles** | 1 | Aspose.HTML for .NET | CSS parsers | ✅ Implemented |
| **TOTAL** | **1,464** | **Aspose Total Ecosystem** | **Full OSS Ecosystem** | **36 Implemented / 1,428 Planned** |

