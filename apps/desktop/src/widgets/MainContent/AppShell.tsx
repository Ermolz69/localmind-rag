import { Outlet } from "react-router-dom";
import { AppSidebar } from "../AppSidebar/AppSidebar";
import { TopBar } from "../TopBar/TopBar";

export function AppShell() {
  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <AppSidebar />
      <main className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <div className="min-w-0 flex-1 p-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
