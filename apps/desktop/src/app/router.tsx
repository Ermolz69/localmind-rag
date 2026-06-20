import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "@widgets/AppShell/AppShell";
import { BucketDetailsPage } from "@pages/BucketDetailsPage";
import { BucketsPage } from "@pages/BucketsPage";
import { ChatPage } from "@pages/ChatPage";
import { CompanionActionPage } from "@pages/CompanionActionPage";
import { CompanionPage } from "@pages/CompanionPage";
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
  { path: "/companion/:action", element: <CompanionActionPage /> },
]);
