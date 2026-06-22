import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "@widgets/AppShell/AppShell";
import { BucketDetailsPage } from "@pages/BucketDetailsPage";
import { BucketsPage } from "@pages/BucketsPage";
import { ChatPage } from "@pages/ChatPage";
import { CompanionActionPage } from "@pages/CompanionActionPage";
import { CompanionActivityPage } from "@pages/CompanionActivityPage";
import { CompanionChatPage } from "@pages/CompanionChatPage";
import { CompanionDocumentsPage } from "@pages/CompanionDocumentsPage";
import { CompanionFilesPage } from "@pages/CompanionFilesPage";
import { CompanionFoldersPage } from "@pages/CompanionFoldersPage";
import { CompanionPage } from "@pages/CompanionPage";
import { CompanionSearchPage } from "@pages/CompanionSearchPage";
import { DashboardPage } from "@pages/DashboardPage";
import { DocumentsPage } from "@pages/DocumentsPage";
import { NotesPage } from "@pages/NotesPage";
import { SemanticSearchPage } from "@pages/SemanticSearchPage";
import { SettingsPage } from "@pages/SettingsPage";
import { DiagnosticsPage } from "@pages/DiagnosticsPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppShell />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "buckets", element: <BucketsPage /> },
      { path: "buckets/:bucketId", element: <BucketDetailsPage /> },
      { path: "documents", element: <DocumentsPage /> },
      { path: "search", element: <SemanticSearchPage /> },
      { path: "notes", element: <NotesPage /> },
      { path: "chat", element: <ChatPage /> },
      { path: "settings", element: <SettingsPage /> },
      { path: "diagnostics", element: <DiagnosticsPage /> },
    ],
  },
  // Standalone, mobile-first companion shell (no desktop chrome). A phone loads
  // these routes once the local-network transport ships.
  { path: "/companion", element: <CompanionPage /> },
  { path: "/companion/chat", element: <CompanionChatPage /> },
  { path: "/companion/search", element: <CompanionSearchPage /> },
  { path: "/companion/documents", element: <CompanionDocumentsPage /> },
  { path: "/companion/files", element: <CompanionFilesPage /> },
  { path: "/companion/folders", element: <CompanionFoldersPage /> },
  { path: "/companion/activity", element: <CompanionActivityPage /> },
  { path: "/companion/:action", element: <CompanionActionPage /> },
]);
