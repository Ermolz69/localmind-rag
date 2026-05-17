import { AlertCircle } from "lucide-react";

type ErrorBannerProps = {
  message: string | null;
};

export function ErrorBanner({ message }: ErrorBannerProps) {
  if (!message) {
    return null;
  }

  return (
    <div className="flex items-start gap-2 rounded-md border border-border bg-muted p-3 text-sm text-foreground">
      <AlertCircle
        className="mt-0.5 shrink-0 text-muted-foreground"
        size={16}
        aria-hidden
      />
      <p>{message}</p>
    </div>
  );
}
