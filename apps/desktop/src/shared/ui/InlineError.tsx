type InlineErrorProps = {
  message?: string | null;
};

export function InlineError({ message }: InlineErrorProps) {
  if (!message) {
    return null;
  }

  return <p className="text-xs text-muted-foreground">{message}</p>;
}
