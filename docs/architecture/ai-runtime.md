# AI Runtime

Development can use Ollama. Portable production targets llama.cpp sidecars and `.gguf` models.

Runtime status is provider-backed. Providers advertise a stable id, display name, status, capabilities, setup guidance, paths, and model listing support. The current implementation registers llama.cpp as the first provider while keeping the API shape ready for additional providers.
