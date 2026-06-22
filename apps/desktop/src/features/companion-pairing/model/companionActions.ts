import {
  Activity,
  FileText,
  Folder,
  FolderInput,
  MessageSquare,
  RefreshCw,
  Search,
  type LucideIcon,
} from "lucide-react";

export type CompanionActionKey =
  | "chat"
  | "search"
  | "documents"
  | "files"
  | "folders"
  | "activity"
  | "indexing";

export type CompanionAction = {
  key: CompanionActionKey;
  label: string;
  description: string;
  icon: LucideIcon;
};

/** Quick actions shown on the phone companion home screen. */
export const companionActions: CompanionAction[] = [
  {
    key: "chat",
    label: "Chat",
    description: "Ask questions about your knowledge base.",
    icon: MessageSquare,
  },
  {
    key: "search",
    label: "Search",
    description: "Find documents and notes by meaning.",
    icon: Search,
  },
  {
    key: "documents",
    label: "Documents",
    description: "Browse the documents on your computer.",
    icon: FileText,
  },
  {
    key: "files",
    label: "Files",
    description: "Add files from allowed folders on your PC.",
    icon: FolderInput,
  },
  {
    key: "folders",
    label: "Folders",
    description: "Manage the folders LocalMind watches.",
    icon: Folder,
  },
  {
    key: "activity",
    label: "Activity",
    description: "See what LocalMind is doing right now.",
    icon: Activity,
  },
  {
    key: "indexing",
    label: "Indexing",
    description: "Start and track indexing jobs.",
    icon: RefreshCw,
  },
];

export function findCompanionAction(
  key: string | undefined,
): CompanionAction | undefined {
  return companionActions.find((action) => action.key === key);
}
