import { Upload } from "lucide-react";
import { Button } from "../../shared/ui/Button";

export function DocumentsPage() {
  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Documents</h1>
          <p className="text-sm text-muted-foreground">
            Upload files for local extraction, chunking, indexing, and RAG.
          </p>
        </div>
        <Button>
          <Upload size={16} aria-hidden />
          Upload
        </Button>
      </div>
      <div className="rounded-md border border-dashed border-border bg-card p-8 text-center text-sm text-muted-foreground">
        Document upload flow skeleton is wired to LocalApi.
      </div>
    </section>
  );
}
