import { Send } from "lucide-react";
import { Button } from "../../shared/ui/Button";

export function ChatPage() {
  return (
    <section className="grid h-full min-h-[640px] grid-cols-[minmax(0,1fr)_320px] gap-4">
      <div className="flex min-h-0 flex-col rounded-md border border-border bg-card">
        <div className="border-b border-border p-4">
          <h1 className="text-xl font-semibold">Chat</h1>
          <p className="text-sm text-muted-foreground">
            Answers will include source references from local chunks.
          </p>
        </div>
        <div className="flex-1 p-4 text-sm text-muted-foreground">
          Start a local RAG conversation.
        </div>
        <form className="flex gap-2 border-t border-border p-3">
          <input
            className="h-10 flex-1 rounded-md border border-border bg-background px-3 text-sm outline-none"
            placeholder="Ask your documents"
          />
          <Button type="submit" className="w-10 px-0">
            <Send size={16} aria-hidden />
          </Button>
        </form>
      </div>
      <aside className="rounded-md border border-border bg-card p-4">
        <h2 className="text-sm font-semibold">Sources</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Source snippets appear here.
        </p>
      </aside>
    </section>
  );
}
