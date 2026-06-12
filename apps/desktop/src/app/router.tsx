import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "@widgets/AppShell/AppShell";
import { BucketsPage } from "@pages/BucketsPage";
import { ChatPage } from "@pages/ChatPage";
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
      { path: "documents", element: <DocumentsPage /> },
      { path: "search", element: <SemanticSearchPage /> },
      { path: "notes", element: <NotesPage /> },
      { path: "chat", element: <ChatPage /> },
      { path: "settings", element: <SettingsPage /> },
      { path: "diagnostics", element: <DiagnosticsPage /> },
    ],
  },
]);
