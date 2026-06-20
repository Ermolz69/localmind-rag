import { RouterProvider } from "react-router-dom";
import { AppProviders } from "./providers";
import { router } from "./router";
import { AppBootstrap } from "./startup/AppBootstrap";
import { CompanionApp } from "./CompanionApp";

const runningInTauri =
  typeof window !== "undefined" && "__TAURI_INTERNALS__" in window;

export function App() {
  // On a phone the SPA is served by the LAN gateway (no Tauri) — run the
  // companion shell, which bootstraps from its own origin instead of Tauri.
  if (!runningInTauri) {
    return <CompanionApp />;
  }

  return (
    <AppProviders>
      <AppBootstrap>
        <RouterProvider router={router} />
      </AppBootstrap>
    </AppProviders>
  );
}
