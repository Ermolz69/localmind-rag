# AI Runtime

Portable AI runtime binaries live in `bin/` and local models live in `models/`.

The default embedding model is `bge-m3` in GGUF `Q4_K_M` format. The model file is
not committed to git. Install both llama.cpp and the embedding model locally with:

```powershell
.\scripts\setup-ai.ps1
```

The setup downloads a portable `llama-server.exe` into `runtime/ai/bin/` and
downloads `bge-m3-Q4_K_M.gguf` into `runtime/ai/models/`. The model checksum is
verified before the application uses it.
