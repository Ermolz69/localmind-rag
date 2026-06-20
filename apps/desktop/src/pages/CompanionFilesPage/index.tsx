import { FolderInput } from "lucide-react";

import { CompanionFiles } from "@features/companion-files";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionFilesPage() {
  return (
    <CompanionScreen
      title="Files on this PC"
      description="Browse allowed folders and add files to LocalMind."
      icon={FolderInput}
    >
      <CompanionFiles />
    </CompanionScreen>
  );
}
