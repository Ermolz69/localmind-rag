import { Folder } from "lucide-react";

import { CompanionFolders } from "@features/companion-folders";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionFoldersPage() {
  return (
    <CompanionScreen
      title="Watched folders"
      description="Rescan allowed folders and clean up deleted files."
      icon={Folder}
    >
      <CompanionFolders />
    </CompanionScreen>
  );
}
