import { useRuntimeStatus } from "@shared/model";
import { RuntimePanel } from "@features/settings";

export function DashboardPage() {
  const runtime = useRuntimeStatus();

  return (
    <section className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Local runtime status and knowledge workspace overview.
        </p>
      </div>

      <RuntimePanel
        health={runtime.health}
        isSettingUpAi={runtime.isSettingUpAi}
        onSetupAi={() => void runtime.setupAiRuntime()}
        runtime={runtime.runtime}
        setupProgress={runtime.setupProgress}
        sync={runtime.sync}
      />
    </section>
  );
}
