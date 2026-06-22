import { describe, expect, it } from "vitest";

import { buildBreadcrumb } from "./breadcrumb";

const roots = [
  { name: "Study", path: "C:\\Users\\me\\Study" },
  { name: "Books", path: "C:\\Users\\me\\Books" },
];

describe("buildBreadcrumb", () => {
  it("shows just the root when browsing the root itself", () => {
    expect(buildBreadcrumb(roots, "C:\\Users\\me\\Study")).toEqual([
      { label: "Study", path: "C:\\Users\\me\\Study" },
    ]);
  });

  it("builds a relative breadcrumb with absolute paths per crumb", () => {
    expect(
      buildBreadcrumb(roots, "C:\\Users\\me\\Study\\AI\\Lectures"),
    ).toEqual([
      { label: "Study", path: "C:\\Users\\me\\Study" },
      { label: "AI", path: "C:\\Users\\me\\Study\\AI" },
      { label: "Lectures", path: "C:\\Users\\me\\Study\\AI\\Lectures" },
    ]);
  });

  it("works with POSIX separators too", () => {
    const posixRoots = [{ name: "Study", path: "/home/me/Study" }];
    expect(buildBreadcrumb(posixRoots, "/home/me/Study/AI")).toEqual([
      { label: "Study", path: "/home/me/Study" },
      { label: "AI", path: "/home/me/Study/AI" },
    ]);
  });

  it("matches the root case-insensitively and uses the root's canonical path", () => {
    expect(buildBreadcrumb(roots, "c:\\users\\me\\study\\AI")).toEqual([
      { label: "Study", path: "C:\\Users\\me\\Study" },
      { label: "AI", path: "C:\\Users\\me\\Study\\AI" },
    ]);
  });

  it("falls back to the leaf name when no root matches", () => {
    expect(buildBreadcrumb(roots, "D:\\Other\\Secret")).toEqual([
      { label: "Secret", path: "D:\\Other\\Secret" },
    ]);
  });
});
