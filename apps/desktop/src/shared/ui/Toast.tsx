type ToastProps = {
  message: string | null;
};

export function Toast({ message }: ToastProps) {
  if (!message) {
    return null;
  }

  return (
    <div className="fixed bottom-4 right-4 z-50 rounded-md border border-border bg-card px-4 py-3 text-sm text-card-foreground shadow-lg">
      {message}
    </div>
  );
}
