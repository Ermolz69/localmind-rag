import { DiagnosticsPanel } from "@features/diagnostics";
import { PageHeader } from "@shared/ui";

export function DiagnosticsPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Diagnostics"
        description="View local runtime status, counts, and ingestion errors."
      />
      <DiagnosticsPanel />
    </div>
  );
}
