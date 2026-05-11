import { Button } from "../../shared/ui/Button";

export function BucketsPage() {
  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Buckets</h1>
          <p className="text-sm text-muted-foreground">
            Organize documents and notes into local workspaces.
          </p>
        </div>
        <Button>New bucket</Button>
      </div>
      <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
        No buckets yet.
      </div>
    </section>
  );
}
