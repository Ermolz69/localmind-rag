import { FileText } from "lucide-react";

import { CompanionDocuments } from "@features/companion-documents";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionDocumentsPage() {
  return (
    <CompanionScreen
      title="Documents"
      description="See what is indexed and what is still processing."
      icon={FileText}
    >
      <CompanionDocuments />
    </CompanionScreen>
  );
}
