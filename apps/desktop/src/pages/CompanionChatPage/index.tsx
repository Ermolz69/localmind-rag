import { MessageSquare } from "lucide-react";

import { CompanionChat } from "@features/companion-chat";
import { CompanionScreen } from "@widgets/CompanionScreen/CompanionScreen";

export function CompanionChatPage() {
  return (
    <CompanionScreen
      title="Chat"
      description="Ask questions about your knowledge base."
      icon={MessageSquare}
    >
      <CompanionChat />
    </CompanionScreen>
  );
}
