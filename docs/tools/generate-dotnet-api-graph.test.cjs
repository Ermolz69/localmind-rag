"use strict";

const { test } = require("node:test");
const assert = require("node:assert/strict");
const fs = require("fs");
const os = require("os");
const path = require("path");

const { buildGraphData, docfxHref } = require("./generate-dotnet-api-graph.cjs");
const { validateGraph } = require("./validate-dotnet-api-graph.cjs");

// ---- Fixtures --------------------------------------------------------------

function mkdir() {
  return fs.mkdtempSync(path.join(os.tmpdir(), "graph-fixture-"));
}

function write(dir, name, body) {
  fs.writeFileSync(path.join(dir, name), `${body}\nreferences:\n- uid: x\n`, "utf8");
}

function namespaceYml(uid, project) {
  return `### YamlMime:ManagedReference
items:
- uid: ${uid}
  id: ${uid}
  name: ${uid}
  fullName: ${uid}
  type: Namespace
  assemblies:
  - ${project}`;
}

function typeYml(opts) {
  const lines = [
    "### YamlMime:ManagedReference",
    "items:",
    `- uid: ${opts.uid}`,
    `  id: ${opts.id || opts.uid}`,
    `  name: ${opts.name || opts.uid}`,
    `  fullName: ${opts.fullName || opts.uid}`,
    `  type: ${opts.type}`,
  ];
  if (opts.project) lines.push("  assemblies:", `  - ${opts.project}`);
  if (opts.namespace) lines.push(`  namespace: ${opts.namespace}`);
  if (opts.syntax) lines.push("  syntax:", `    content: '${opts.syntax}'`);
  if (opts.inheritance) {
    lines.push("  inheritance:");
    opts.inheritance.forEach((b) => lines.push(`  - ${b}`));
  }
  if (opts.implements) {
    lines.push("  implements:");
    opts.implements.forEach((i) => lines.push(`  - ${i}`));
  }
  return lines.join("\n");
}

/** A realistic fixture: one project, namespaces, several type kinds, an
 *  internal base/interface so inherits/implements edges are exercised. */
function richFixture() {
  const dir = mkdir();
  const P = "Demo.Contracts";

  write(dir, "Demo.Contracts.Buckets.yml", namespaceYml("Demo.Contracts.Buckets", P));

  write(dir, "Demo.Contracts.Buckets.BucketDto.yml", typeYml({
    uid: "Demo.Contracts.Buckets.BucketDto", name: "BucketDto",
    fullName: "Demo.Contracts.Buckets.BucketDto", type: "Class",
    project: P, namespace: "Demo.Contracts.Buckets",
    syntax: "public sealed record BucketDto",
  }));

  write(dir, "Demo.Contracts.Buckets.BucketService.yml", typeYml({
    uid: "Demo.Contracts.Buckets.BucketService", name: "BucketService",
    fullName: "Demo.Contracts.Buckets.BucketService", type: "Class",
    project: P, namespace: "Demo.Contracts.Buckets",
    syntax: "public class BucketService",
  }));

  write(dir, "Demo.Contracts.Common.Status.yml", typeYml({
    uid: "Demo.Contracts.Common.Status", name: "Status",
    fullName: "Demo.Contracts.Common.Status", type: "Enum",
    project: P, namespace: "Demo.Contracts.Common",
    syntax: "public enum Status",
  }));

  // Generic type: filename/href must encode the backtick as a dash.
  write(dir, "Demo.Contracts.Common.ApiResponse-1.yml", typeYml({
    uid: "Demo.Contracts.Common.ApiResponse`1", name: "ApiResponse<T>",
    fullName: "Demo.Contracts.Common.ApiResponse`1", type: "Class",
    project: P, namespace: "Demo.Contracts.Common",
    syntax: "public class ApiResponse<T>",
  }));

  // Internal interface + base + derived → implements/inherits edges.
  write(dir, "Demo.Contracts.Abc.IThing.yml", typeYml({
    uid: "Demo.Contracts.Abc.IThing", name: "IThing",
    fullName: "Demo.Contracts.Abc.IThing", type: "Interface",
    project: P, namespace: "Demo.Contracts.Abc",
    syntax: "public interface IThing",
  }));
  write(dir, "Demo.Contracts.Abc.BaseThing.yml", typeYml({
    uid: "Demo.Contracts.Abc.BaseThing", name: "BaseThing",
    fullName: "Demo.Contracts.Abc.BaseThing", type: "Class",
    project: P, namespace: "Demo.Contracts.Abc",
    syntax: "public class BaseThing",
  }));
  write(dir, "Demo.Contracts.Abc.DerivedThing.yml", typeYml({
    uid: "Demo.Contracts.Abc.DerivedThing", name: "DerivedThing",
    fullName: "Demo.Contracts.Abc.DerivedThing", type: "Class",
    project: P, namespace: "Demo.Contracts.Abc",
    syntax: "public class DerivedThing : BaseThing, IThing",
    inheritance: ["System.Object", "Demo.Contracts.Abc.BaseThing"],
    implements: ["Demo.Contracts.Abc.IThing"],
  }));

  return dir;
}

const node = (g, uid) => g.nodes.find((n) => n.uid === uid);
const hasEdge = (g, s, t, kind) =>
  g.edges.some((e) => e.source === s && e.target === t && e.kind === kind);

// ---- Generator: fixture-based ---------------------------------------------

test("generator builds nodes, kinds, and edges from fixture metadata", () => {
  const dir = richFixture();
  try {
    const g = buildGraphData(dir);

    assert.equal(g.stats.projectCount, 1, "one project");
    assert.equal(g.stats.typeCount, 7, "seven type nodes");
    assert.ok(g.stats.namespaceCount >= 3, "namespaces derived (incl. on-demand)");

    assert.equal(node(g, "Demo.Contracts.Buckets.BucketDto").typeKind, "record");
    assert.equal(node(g, "Demo.Contracts.Buckets.BucketService").typeKind, "class");
    assert.equal(node(g, "Demo.Contracts.Common.Status").typeKind, "enum");
    assert.equal(node(g, "Demo.Contracts.Abc.IThing").typeKind, "interface");

    // contains edges
    assert.ok(hasEdge(g, "project:Demo.Contracts", "namespace:Demo.Contracts.Buckets", "contains"));
    assert.ok(hasEdge(g, "namespace:Demo.Contracts.Buckets", "type:Demo.Contracts.Buckets.BucketDto", "contains"));

    // internal inherits/implements edges (external System.Object skipped)
    assert.ok(hasEdge(g, "type:Demo.Contracts.Abc.DerivedThing", "type:Demo.Contracts.Abc.BaseThing", "inherits"));
    assert.ok(hasEdge(g, "type:Demo.Contracts.Abc.DerivedThing", "type:Demo.Contracts.Abc.IThing", "implements"));
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test("generic type href encodes the backtick as a dash (DocFX page name)", () => {
  const dir = richFixture();
  try {
    const g = buildGraphData(dir);
    assert.equal(
      node(g, "Demo.Contracts.Common.ApiResponse`1").href,
      "Demo.Contracts.Common.ApiResponse-1.html",
    );
    assert.equal(docfxHref("Foo`2"), "Foo-2.html");
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test("the fixture graph passes validation", () => {
  const dir = richFixture();
  try {
    const { errors } = validateGraph(buildGraphData(dir));
    assert.deepEqual(errors, [], `expected no errors, got: ${errors.join("; ")}`);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

// ---- Generator: empty metadata --------------------------------------------

test("empty metadata folder yields an empty graph that fails validation", () => {
  const dir = mkdir();
  try {
    const g = buildGraphData(dir);
    assert.equal(g.nodes.length, 0);
    assert.equal(g.edges.length, 0);

    const { errors } = validateGraph(g);
    assert.ok(errors.some((e) => /no project nodes/.test(e)), "flags missing project");
    assert.ok(errors.some((e) => /no type nodes/.test(e)), "flags missing type");
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

// ---- Generator: missing optional fields -----------------------------------

test("metadata with missing optional fields does not crash the generator", () => {
  const dir = mkdir();
  try {
    write(dir, "Bare.yml", typeYml({ uid: "Bare.OnlyType", type: "Class" }));
    const g = buildGraphData(dir);
    const n = node(g, "Bare.OnlyType");
    assert.ok(n, "node still produced");
    assert.equal(n.namespace, null);
    assert.equal(n.project, null);
    assert.equal(n.typeKind, "class");
    assert.equal(n.href, "Bare.OnlyType.html");
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

// ---- Validator: broken relation edge --------------------------------------

test("validator detects a broken relation edge", () => {
  const dir = richFixture();
  try {
    const g = buildGraphData(dir);
    g.edges.push({
      source: "type:Demo.Contracts.Abc.DerivedThing",
      target: "type:Demo.Contracts.DoesNotExist",
      kind: "inherits",
    });
    const { errors } = validateGraph(g);
    assert.ok(
      errors.some((e) => /missing node/.test(e)),
      `expected a missing-node error, got: ${errors.join("; ")}`,
    );
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

// ---- Validator: schema conformance ----------------------------------------

test("validator rejects a node with an invalid kind (schema enum)", () => {
  const dir = richFixture();
  try {
    const g = buildGraphData(dir);
    g.nodes.push({ id: "bogus:1", kind: "widget", label: "Bogus" });
    const { errors } = validateGraph(g);
    assert.ok(
      errors.some((e) => /not one of/.test(e)),
      `expected an enum error, got: ${errors.join("; ")}`,
    );
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test("validator detects duplicate node ids", () => {
  const dir = richFixture();
  try {
    const g = buildGraphData(dir);
    const dup = { ...g.nodes.find((n) => n.kind === "type") };
    g.nodes.push(dup);
    const { errors } = validateGraph(g);
    assert.ok(
      errors.some((e) => /Duplicate node id/.test(e)),
      `expected a duplicate-id error, got: ${errors.join("; ")}`,
    );
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});
