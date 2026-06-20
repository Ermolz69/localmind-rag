import { Search } from "lucide-react";

import { CompanionSearch } from "@features/companion-search";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionSearchPage() {
  return (
    <CompanionScreen
      title="Search"
      description="Find documents by meaning."
      icon={Search}
    >
      <CompanionSearch />
    </CompanionScreen>
  );
}
