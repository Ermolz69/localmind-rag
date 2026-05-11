import { Button } from "../../shared/ui/Button";

export function NotesPage() {
  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Notes</h1>
          <p className="text-sm text-muted-foreground">
            Local markdown notes and future document references.
          </p>
        </div>
        <Button>New note</Button>
      </div>
      <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
        No notes yet.
      </div>
    </section>
  );
}
