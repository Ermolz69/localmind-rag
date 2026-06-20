import { useParams } from "react-router-dom";

import { findCompanionAction } from "@features/companion-pairing";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionActionPage() {
  const { action } = useParams();
  const companionAction = findCompanionAction(action);

  return (
    <CompanionScreen
      title={companionAction?.label ?? "Unknown action"}
      description={companionAction?.description}
      icon={companionAction?.icon}
    >
      <div className="rounded-xl border border-border bg-card p-5 text-sm text-muted-foreground">
        {companionAction
          ? "This will be available from your phone in a later step. For now, use the desktop app for this."
          : "This companion action does not exist."}
      </div>
    </CompanionScreen>
  );
}
