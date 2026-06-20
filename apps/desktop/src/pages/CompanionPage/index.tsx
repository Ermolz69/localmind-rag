import { ArrowLeft, Smartphone } from "lucide-react";
import { Link, useSearchParams } from "react-router-dom";

import {
  companionActions,
  useCompanionInfo,
} from "@features/companion-pairing";
import { CompanionLivePanel } from "@features/companion-activity";

export function CompanionPage() {
  const { computerName, isLoading } = useCompanionInfo();
  const [searchParams] = useSearchParams();
  const isPreview = searchParams.get("preview") === "1";

  return (
    <div className="min-h-screen bg-background px-5 py-8 text-foreground">
      <div className="mx-auto flex max-w-md flex-col gap-6">
        {isPreview ? (
          <Link
            to="/"
            className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            Exit preview
          </Link>
        ) : null}

        <header className="flex items-center gap-3">
          <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Smartphone className="h-6 w-6" />
          </span>
          <div>
            <h1 className="text-xl font-semibold">LocalMind Companion</h1>
            <p className="text-sm text-muted-foreground">
              {isLoading ? (
                "Connecting…"
              ) : computerName ? (
                <>
                  Connected to{" "}
                  <span className="font-medium text-foreground">
                    {computerName}
                  </span>
                </>
              ) : (
                "Connected to your computer"
              )}
            </p>
          </div>
        </header>

        <CompanionLivePanel />

        <section className="flex flex-col gap-3">
          <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Actions
          </h2>
          <ul className="grid grid-cols-2 gap-3">
            {companionActions.map((action) => {
              const Icon = action.icon;
              return (
                <li key={action.key}>
                  <Link
                    to={`/companion/${action.key}`}
                    className="flex h-full flex-col gap-2 rounded-xl border border-border bg-card p-4 text-card-foreground transition hover:bg-muted active:scale-[0.98]"
                  >
                    <Icon className="h-6 w-6 text-primary" />
                    <span className="text-base font-medium">
                      {action.label}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {action.description}
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
      </div>
    </div>
  );
}
