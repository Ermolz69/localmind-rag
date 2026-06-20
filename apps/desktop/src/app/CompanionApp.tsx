import { useEffect, useState } from "react";
import {
  Navigate,
  RouterProvider,
  createBrowserRouter,
} from "react-router-dom";

import { CompanionActionPage } from "@pages/CompanionActionPage";
import { CompanionActivityPage } from "@pages/CompanionActivityPage";
import { CompanionChatPage } from "@pages/CompanionChatPage";
import { CompanionDocumentsPage } from "@pages/CompanionDocumentsPage";
import { CompanionFilesPage } from "@pages/CompanionFilesPage";
import { CompanionFoldersPage } from "@pages/CompanionFoldersPage";
import { CompanionPage } from "@pages/CompanionPage";
import { CompanionSearchPage } from "@pages/CompanionSearchPage";

import { bootstrapCompanionSession } from "./companionBootstrap";
import { AppProviders } from "./providers";

const router = createBrowserRouter([
  { path: "/companion", element: <CompanionPage /> },
  { path: "/companion/chat", element: <CompanionChatPage /> },
  { path: "/companion/search", element: <CompanionSearchPage /> },
  { path: "/companion/documents", element: <CompanionDocumentsPage /> },
  { path: "/companion/files", element: <CompanionFilesPage /> },
  { path: "/companion/folders", element: <CompanionFoldersPage /> },
  { path: "/companion/activity", element: <CompanionActivityPage /> },
  { path: "/companion/:action", element: <CompanionActionPage /> },
  { path: "*", element: <Navigate to="/companion" replace /> },
]);

type BootstrapState = "loading" | "ready" | "unpaired" | "error";

/**
 * The phone entry point. When the SPA is loaded from the LAN gateway (not Tauri),
 * it talks to its own origin, completes pairing from the QR `?token=`, stores the
 * device token, and then runs the companion screens.
 */
export function CompanionApp() {
  const [state, setState] = useState<BootstrapState>("loading");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void bootstrapCompanionSession().then((result) => {
      setError(result.error ?? null);
      setState(result.state);
    });
  }, []);

  return (
    <AppProviders>
      {state === "ready" ? (
        <RouterProvider router={router} />
      ) : state === "loading" ? (
        <CompanionMessage title="Connecting…" />
      ) : state === "error" ? (
        <CompanionMessage
          title="Couldn’t connect"
          body={error ?? "Try scanning the code again."}
        />
      ) : (
        <CompanionMessage
          title="Not connected"
          body="Open LocalMind on your computer, turn on Companion Mode, and scan the QR code."
        />
      )}
    </AppProviders>
  );
}

function CompanionMessage({ title, body }: { title: string; body?: string }) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-6 text-center text-foreground">
      <h1 className="text-lg font-semibold">{title}</h1>
      {body ? (
        <p className="max-w-xs text-sm text-muted-foreground">{body}</p>
      ) : null}
    </div>
  );
}
