import { useParams } from "react-router-dom";

import { findCompanionAction } from "@features/companion-pairing";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";
import { EmptyState } from "@shared/ui";

export function CompanionActionPage() {
  const { action } = useParams();
  const companionAction = findCompanionAction(action);
  const Icon = companionAction?.icon;

  return (
    <CompanionScreen
      title={companionAction?.label ?? "Unknown action"}
      description={companionAction?.description}
      icon={companionAction?.icon}
    >
      {companionAction ? (
        <EmptyState
          icon={Icon ? <Icon className="h-5 w-5" /> : undefined}
          title="Not on the phone yet"
          description="This control lives in LocalMind on your computer for now. You can still watch progress in Documents and Activity."
        />
      ) : (
        <EmptyState
          title="Screen not found"
          description="This companion screen doesn't exist. Head back home to pick an action."
        />
      )}
    </CompanionScreen>
  );
}
