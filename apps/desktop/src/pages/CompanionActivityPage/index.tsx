import { Activity } from "lucide-react";

import { CompanionActivity } from "@features/companion-activity";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionActivityPage() {
  return (
    <CompanionScreen
      title="Activity"
      description="See what LocalMind is doing on the computer."
      icon={Activity}
    >
      <CompanionActivity />
    </CompanionScreen>
  );
}
