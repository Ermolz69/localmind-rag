import { Activity, Database, HardDrive, WifiOff } from "lucide-react";

const cards = [
  { label: "Local API", value: "Ready", icon: Activity },
  { label: "SQLite", value: "Auto-created", icon: Database },
  { label: "Storage", value: "Portable", icon: HardDrive },
  { label: "Sync", value: "Offline", icon: WifiOff },
];

export function DashboardPage() {
  return (
    <section className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Local runtime status and knowledge workspace overview.
        </p>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((card) => (
          <article
            key={card.label}
            className="rounded-md border border-border bg-card p-4"
          >
            <card.icon size={18} className="text-primary" aria-hidden />
            <div className="mt-3 text-sm text-muted-foreground">
              {card.label}
            </div>
            <div className="text-lg font-semibold">{card.value}</div>
          </article>
        ))}
      </div>
    </section>
  );
}
