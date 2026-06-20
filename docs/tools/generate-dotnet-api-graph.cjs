#!/usr/bin/env node
"use strict";

/**
 * Generates graph data for the .NET API graph view from DocFX-generated
 * managed-reference metadata.
 *
 * Input  : docs/auto-generated/dotnet-api/*.yml  (produced by `docfx metadata`)
 * Output : docs/auto-generated/dotnet-api-graph.json
 *
 * The output is a generated artifact and must not be edited by hand. It is
 * intentionally independent of the frontend app: it is derived purely from the
 * .NET API documentation metadata and contains no OpenAPI endpoints, no
 * frontend Storybook data, and no hand-authored architecture diagrams.
 *
 * Dependency-free on purpose (mirrors scripts/check-colors.cjs): it ships with
 * a small, focused reader for the first item block of each DocFX YAML file
 * rather than pulling in a YAML parser.
 */

const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "../../");
const metadataDir = path.resolve(
  projectRoot,
  "docs/auto-generated/dotnet-api",
);
const outputFile = path.resolve(
  projectRoot,
  "docs/auto-generated/dotnet-api-graph.json",
);

// ---- DocFX YAML reading (first item only) ---------------------------------

/**
 * Reads the scalar/list fields we care about from the first `items:` entry of
 * a DocFX ManagedReference YAML file. Returns null when the file has no item.
 *
 * We never need member-level entries (constructors, properties, fields), so we
 * stop reading at the second top-level item or the `references:` section.
 */
function readFirstItem(filePath) {
  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/);

  const itemsIndex = lines.findIndex((line) => line === "items:");
  if (itemsIndex === -1) return null;

  let start = -1;
  for (let i = itemsIndex + 1; i < lines.length; i++) {
    if (lines[i].startsWith("- ")) {
      start = i;
      break;
    }
  }
  if (start === -1) return null;

  // Collect the block belonging to the first item.
  const block = [lines[start]];
  for (let i = start + 1; i < lines.length; i++) {
    const line = lines[i];
    if (line.startsWith("- ") || /^[^\s]/.test(line)) break; // next item / references:
    block.push(line);
  }

  const item = {
    uid: undefined,
    id: undefined,
    name: undefined,
    fullName: undefined,
    type: undefined,
    namespace: undefined,
    assemblies: [],
    children: [],
    inheritance: [],
    implements: [],
    syntaxContent: undefined,
  };

  // First line carries the uid: "- uid: <value>"
  const head = /^- (\w[\w.]*): ?(.*)$/.exec(block[0]);
  if (head && head[1] === "uid") item.uid = unquote(head[2]);

  let listKey = null; // active 2-space list property
  let inSyntax = false; // inside the `syntax:` map (children are 4-space)

  for (let i = 1; i < block.length; i++) {
    const line = block[i];

    const listEntry = /^  - (.*)$/.exec(line);
    const scalarOrKey = /^  ([A-Za-z][\w.]*): ?(.*)$/.exec(line);
    const isNested = /^    /.test(line);

    if (scalarOrKey && !isNested) {
      const key = scalarOrKey[1];
      const value = scalarOrKey[2];
      inSyntax = key === "syntax";

      if (value === "") {
        listKey = key; // list (assemblies/children/...) or map (syntax/source)
        continue;
      }

      listKey = null;
      switch (key) {
        case "uid":
          item.uid = unquote(value);
          break;
        case "id":
          item.id = unquote(value);
          break;
        case "name":
          item.name = unquote(value);
          break;
        case "fullName":
          item.fullName = unquote(value);
          break;
        case "type":
          item.type = unquote(value);
          break;
        case "namespace":
          item.namespace = unquote(value);
          break;
        default:
          break;
      }
      continue;
    }

    if (listEntry && !isNested) {
      const value = unquote(listEntry[1]);
      if (listKey === "assemblies") item.assemblies.push(value);
      else if (listKey === "children") item.children.push(value);
      else if (listKey === "inheritance") item.inheritance.push(value);
      else if (listKey === "implements") item.implements.push(value);
      continue;
    }

    if (isNested && inSyntax && item.syntaxContent === undefined) {
      const content = /^    content: ?(.*)$/.exec(line);
      if (content) item.syntaxContent = unquote(content[1]);
    }
  }

  return item;
}

function unquote(raw) {
  let value = (raw ?? "").trim();
  if (value.length >= 2) {
    const first = value[0];
    const last = value[value.length - 1];
    if ((first === "'" && last === "'") || (first === '"' && last === '"')) {
      value = value.slice(1, -1);
      if (first === "'") value = value.replace(/''/g, "'");
    }
  }
  return value;
}

// ---- Type-kind resolution -------------------------------------------------

function resolveTypeKind(docfxType, syntaxContent) {
  switch (docfxType) {
    case "Enum":
      return "enum";
    case "Interface":
      return "interface";
    case "Struct":
      return "struct";
    case "Class": {
      // DocFX reports records as `Class`; the record-ness is only visible in
      // the declaration syntax.
      if (syntaxContent && /\brecord\b/.test(syntaxContent)) return "record";
      return "class";
    }
    case "Delegate":
      return "class";
    default:
      return "unknown";
  }
}

const TYPE_DOCFX_KINDS = new Set([
  "Class",
  "Interface",
  "Enum",
  "Struct",
  "Delegate",
]);

/**
 * Strips generic arity (`` `1 ``) and generic argument lists (`{...}`) so an
 * inheritance/implements reference can be matched against a known type uid.
 */
function normalizeUid(uid) {
  const cut = uid.search(/[`{]/);
  return (cut === -1 ? uid : uid.slice(0, cut)).trim();
}

/**
 * Builds the DocFX HTML page name for a uid. DocFX encodes the generic-arity
 * backtick (e.g. `ApiResponse`1`) as a dash (`ApiResponse-1.html`), so the href
 * must use the same encoding to resolve inside the built site.
 */
function docfxHref(uid) {
  return `${uid.replace(/`/g, "-")}.html`;
}

// ---- Build the graph ------------------------------------------------------

/**
 * Builds the graph data object from a directory of DocFX managed-reference
 * YAML files. Pure (no file writes / no process.exit) so it can be unit tested.
 * A missing or empty directory yields an empty graph.
 */
function buildGraphData(dir) {
  const files = fs.existsSync(dir)
    ? fs.readdirSync(dir).filter((file) => file.endsWith(".yml") && file !== "toc.yml")
    : [];

  const projects = new Map(); // id -> node
  const namespaces = new Map(); // id -> node
  const types = new Map(); // id -> node
  const typeNodeByUid = new Map(); // normalized uid -> type node id
  const pendingTypes = []; // raw items, resolved into edges after all nodes exist

  const projectId = (name) => `project:${name}`;
  const namespaceId = (fullName) => `namespace:${fullName}`;
  const typeId = (uid) => `type:${uid}`;

  function ensureProject(name) {
    if (!name) return undefined;
    const id = projectId(name);
    if (!projects.has(id)) {
      projects.set(id, { id, kind: "project", label: name, fullName: name });
    }
    return id;
  }

  function ensureNamespace(fullName, project) {
    if (!fullName) return undefined;
    const id = namespaceId(fullName);
    if (!namespaces.has(id)) {
      namespaces.set(id, {
        id,
        kind: "namespace",
        label: fullName,
        fullName,
        project: project ?? null,
        href: docfxHref(fullName),
      });
    } else if (project && !namespaces.get(id).project) {
      namespaces.get(id).project = project;
    }
    return id;
  }

  for (const file of files) {
    const item = readFirstItem(path.join(dir, file));
    if (!item || !item.uid) continue;

    const project = item.assemblies[0];

    if (item.type === "Namespace") {
      ensureProject(project);
      ensureNamespace(item.fullName ?? item.name ?? item.uid, project);
      continue;
    }

    if (TYPE_DOCFX_KINDS.has(item.type)) {
      const fullName = item.fullName ?? item.uid;
      const id = typeId(item.uid);
      const node = {
        id,
        kind: "type",
        label: item.name ?? item.uid,
        fullName,
        namespace: item.namespace ?? null,
        project: project ?? null,
        typeKind: resolveTypeKind(item.type, item.syntaxContent),
        uid: item.uid,
        href: docfxHref(item.uid),
      };
      types.set(id, node);
      typeNodeByUid.set(normalizeUid(item.uid), id);

      ensureProject(project);
      if (item.namespace) ensureNamespace(item.namespace, project);

      pendingTypes.push(item);
    }
  }

  // ---- Edges --------------------------------------------------------------

  const edges = [];
  const edgeKeys = new Set();

  function addEdge(source, target, kind) {
    if (!source || !target || source === target) return;
    const key = `${source} ${target} ${kind}`;
    if (edgeKeys.has(key)) return;
    edgeKeys.add(key);
    edges.push({ source, target, kind });
  }

  // project contains namespace
  for (const ns of namespaces.values()) {
    if (ns.project) addEdge(projectId(ns.project), ns.id, "contains");
  }

  // namespace contains type
  for (const type of types.values()) {
    if (type.namespace) {
      addEdge(namespaceId(type.namespace), type.id, "contains");
    }
  }

  // type inherits base type / implements interface (internal targets only)
  for (const item of pendingTypes) {
    const source = typeId(item.uid);

    // Direct base = nearest internal ancestor (DocFX lists root-first).
    for (let i = item.inheritance.length - 1; i >= 0; i--) {
      const baseId = typeNodeByUid.get(normalizeUid(item.inheritance[i]));
      if (baseId) {
        addEdge(source, baseId, "inherits");
        break;
      }
    }

    for (const implemented of item.implements) {
      const ifaceId = typeNodeByUid.get(normalizeUid(implemented));
      if (ifaceId) addEdge(source, ifaceId, "implements");
    }
  }

  // ---- Assemble output ----------------------------------------------------

  const byId = (a, b) => a.id.localeCompare(b.id);
  const nodes = [
    ...[...projects.values()].sort(byId),
    ...[...namespaces.values()].sort(byId),
    ...[...types.values()].sort(byId),
  ];

  edges.sort(
    (a, b) =>
      a.kind.localeCompare(b.kind) ||
      a.source.localeCompare(b.source) ||
      a.target.localeCompare(b.target),
  );

  const missingHrefCount = [...types.values()].filter((t) => !t.href).length;

  return {
    $schema: "../tools/dotnet-api-graph.schema.json",
    generator: "docs/tools/generate-dotnet-api-graph.cjs",
    source: "docs/auto-generated/dotnet-api",
    generatedAt: new Date().toISOString(),
    stats: {
      nodeCount: nodes.length,
      edgeCount: edges.length,
      projectCount: projects.size,
      namespaceCount: namespaces.size,
      typeCount: types.size,
      missingHrefCount,
    },
    nodes,
    edges,
  };
}

// ---- CLI -------------------------------------------------------------------

function main() {
  if (!fs.existsSync(metadataDir)) {
    console.error(
      `\x1b[31mError: DocFX metadata not found at ${path.relative(projectRoot, metadataDir)}.\x1b[0m`,
    );
    console.error("Run `task docs:build` (or `docfx metadata`) first.");
    process.exit(1);
  }

  const graph = buildGraphData(metadataDir);

  fs.mkdirSync(path.dirname(outputFile), { recursive: true });
  fs.writeFileSync(outputFile, `${JSON.stringify(graph, null, 2)}\n`, "utf8");

  console.log(
    `\x1b[32mGenerated .NET API graph data\x1b[0m -> ${path.relative(projectRoot, outputFile)}`,
  );
  console.log(
    `  ${graph.stats.nodeCount} nodes ` +
      `(${graph.stats.projectCount} projects, ` +
      `${graph.stats.namespaceCount} namespaces, ` +
      `${graph.stats.typeCount} types), ` +
      `${graph.stats.edgeCount} edges`,
  );
}

module.exports = { buildGraphData, docfxHref, resolveTypeKind, normalizeUid };

if (require.main === module) main();
