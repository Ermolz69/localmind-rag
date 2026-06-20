import { ArrowLeft } from "lucide-react";
import { Link, useParams } from "react-router-dom";

import { findCompanionAction } from "@features/companion-pairing";

export function CompanionActionPage() {
  const { action } = useParams();
  const companionAction = findCompanionAction(action);

  return (
    <div className="min-h-screen bg-background px-5 py-8 text-foreground">
      <div className="mx-auto flex max-w-md flex-col gap-6">
        <Link
          to="/companion"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Home
        </Link>

        {companionAction ? (
          <header className="flex items-center gap-3">
            <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <companionAction.icon className="h-6 w-6" />
            </span>
            <div>
              <h1 className="text-xl font-semibold">{companionAction.label}</h1>
              <p className="text-sm text-muted-foreground">
                {companionAction.description}
              </p>
            </div>
          </header>
        ) : (
          <h1 className="text-xl font-semibold">Unknown action</h1>
        )}

        <div className="rounded-xl border border-border bg-card p-5 text-sm text-muted-foreground">
          {companionAction
            ? "This will be available from your phone in a later step. For now, use the desktop app for this."
            : "This companion action does not exist."}
        </div>
      </div>
    </div>
  );
}
