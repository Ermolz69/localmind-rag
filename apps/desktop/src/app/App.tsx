import { RouterProvider } from "react-router-dom";
import { AppProviders } from "./providers";
import { router } from "./router";
import { AppBootstrap } from "./startup/AppBootstrap";

export function App() {
  return (
    <AppProviders>
      <AppBootstrap>
        <RouterProvider router={router} />
      </AppBootstrap>
    </AppProviders>
  );
}
