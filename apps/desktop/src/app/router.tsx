import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "../widgets/MainContent/AppShell";
import { BucketsPage } from "../pages/BucketsPage";
import { ChatPage } from "../pages/ChatPage";
import { DashboardPage } from "../pages/DashboardPage";
import { DocumentsPage } from "../pages/DocumentsPage";
import { NotesPage } from "../pages/NotesPage";
import { SettingsPage } from "../pages/SettingsPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppShell />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "buckets", element: <BucketsPage /> },
      { path: "documents", element: <DocumentsPage /> },
      { path: "notes", element: <NotesPage /> },
      { path: "chat", element: <ChatPage /> },
      { path: "settings", element: <SettingsPage /> },
    ],
  },
]);
