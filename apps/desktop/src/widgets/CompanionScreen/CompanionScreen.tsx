import { ArrowLeft, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";

import { CompanionTabBar } from "./CompanionTabBar";

type CompanionScreenProps = {
  title: string;
  description?: string;
  icon?: LucideIcon;
  children: ReactNode;
};

/** Mobile-first frame for a companion sub-screen: back link, header, content. */
export function CompanionScreen({
  title,
  description,
  icon: Icon,
  children,
}: CompanionScreenProps) {
  return (
    <div className="min-h-screen bg-background px-5 py-6 text-foreground">
      <div className="mx-auto flex min-h-[calc(100vh-3rem)] max-w-md flex-col gap-4">
        <Link
          to="/companion"
          className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Home
        </Link>

        <header className="flex items-center gap-3">
          {Icon ? (
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Icon className="h-5 w-5" />
            </span>
          ) : null}
          <div className="min-w-0">
            <h1 className="text-xl font-semibold">{title}</h1>
            {description ? (
              <p className="text-sm text-muted-foreground">{description}</p>
            ) : null}
          </div>
        </header>

        <div className="flex min-h-0 flex-1 flex-col">{children}</div>

        <CompanionTabBar />
      </div>
    </div>
  );
}
