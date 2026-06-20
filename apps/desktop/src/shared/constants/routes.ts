export const routes = {
  dashboard: "/",
  buckets: "/buckets",
  bucketDetails: (bucketId: string) => `/buckets/${bucketId}`,
  documents: "/documents",
  search: "/search",
  notes: "/notes",
  chat: "/chat",
  settings: "/settings",
  diagnostics: "/diagnostics",
} as const;
