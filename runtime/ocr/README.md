# OCR runtime

This directory is used by the local OCR integration.

Expected layout:

```text
runtime/ocr/bin/tesseract.exe
runtime/ocr/bin/*.dll
runtime/ocr/tessdata/eng.traineddata
runtime/ocr/tessdata/osd.traineddata
```

Run the setup script from the repository root:

```text
scripts/setup-ocr.ps1
```