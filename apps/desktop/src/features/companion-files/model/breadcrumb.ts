import type { CompanionFileRoot } from "./useCompanionFiles";

export type CompanionBreadcrumb = { label: string; path: string };

function normalize(path: string): string {
  return path.replace(/\\/g, "/").replace(/\/+$/, "").toLowerCase();
}

function leafName(path: string): string {
  const segments = path.split(/[\\/]+/).filter(Boolean);
  return segments[segments.length - 1] ?? path;
}

/**
 * Turns an absolute folder path into a friendly breadcrumb relative to the allowed
 * root it lives under, e.g. `Study / AI / Lectures` instead of the full disk path.
 * Each crumb carries the absolute path to navigate to. Falls back to the folder's
 * leaf name when no matching root is found.
 */
export function buildBreadcrumb(
  roots: CompanionFileRoot[],
  currentPath: string,
): CompanionBreadcrumb[] {
  const normalizedCurrent = normalize(currentPath);
  const root = roots.find((candidate) => {
    const normalizedRoot = normalize(candidate.path);
    return (
      normalizedCurrent === normalizedRoot ||
      normalizedCurrent.startsWith(`${normalizedRoot}/`)
    );
  });

  if (!root) {
    return [{ label: leafName(currentPath), path: currentPath }];
  }

  const crumbs: CompanionBreadcrumb[] = [{ label: root.name, path: root.path }];

  const separator = currentPath.includes("\\") ? "\\" : "/";
  const baseLength = root.path.replace(/[\\/]+$/, "").length;
  const relative = currentPath.slice(baseLength).replace(/^[\\/]+/, "");

  if (relative.length > 0) {
    let accumulated = root.path.replace(/[\\/]+$/, "");
    for (const segment of relative.split(/[\\/]+/).filter(Boolean)) {
      accumulated = `${accumulated}${separator}${segment}`;
      crumbs.push({ label: segment, path: accumulated });
    }
  }

  return crumbs;
}
