#!/usr/bin/env node
"use strict";

/**
 * Validates the generated .NET API graph JSON so it cannot break silently.
 *
 * Checks:
 *  - structural conformance to docs/tools/dotnet-api-graph.schema.json
 *    (dependency-free mini JSON-Schema validator covering the keywords used);
 *  - at least one project node and at least one type node (catches empty graph);
 *  - unique node ids;
 *  - every edge references existing nodes (catches broken relation edges);
 *  - counts type nodes missing a DocFX href (reported, non-fatal).
 *
 * Prints a summary and exits non-zero on any error so `docs:build` fails on an
 * invalid graph. Importable for tests via module.exports.
 */

const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "../../");
const defaultGraphPath = path.resolve(
  projectRoot,
  "docs/auto-generated/dotnet-api-graph.json",
);
const schemaPath = path.resolve(__dirname, "dotnet-api-graph.schema.json");

const red = (s) => `\x1b[31m${s}\x1b[0m`;
const green = (s) => `\x1b[32m${s}\x1b[0m`;
const yellow = (s) => `\x1b[33m${s}\x1b[0m`;

// ---- Minimal JSON-Schema validation ---------------------------------------
// Supports only the keywords used by dotnet-api-graph.schema.json:
// type, enum, required, properties, items, minItems, and local $ref.

function jsType(value) {
  if (value === null) return "null";
  if (Array.isArray(value)) return "array";
  return typeof value;
}

function matchesType(value, type) {
  switch (type) {
    case "object":
      return value !== null && typeof value === "object" && !Array.isArray(value);
    case "array":
      return Array.isArray(value);
    case "string":
      return typeof value === "string";
    case "integer":
      return Number.isInteger(value);
    case "number":
      return typeof value === "number";
    case "boolean":
      return typeof value === "boolean";
    case "null":
      return value === null;
    default:
      return false;
  }
}

function resolveRef(root, ref) {
  // Only local refs like "#/$defs/node".
  const parts = ref.replace(/^#\//, "").split("/");
  return parts.reduce((acc, key) => (acc ? acc[key] : undefined), root);
}

function validateAgainstSchema(data, schema, root = schema, where = "$") {
  const errors = [];
  if (!schema) return errors;

  if (schema.$ref) {
    return validateAgainstSchema(data, resolveRef(root, schema.$ref), root, where);
  }

  if (schema.type) {
    const types = Array.isArray(schema.type) ? schema.type : [schema.type];
    if (!types.some((t) => matchesType(data, t))) {
      errors.push(`${where}: expected ${types.join("|")}, got ${jsType(data)}`);
      return errors; // further checks would be meaningless
    }
  }

  if (schema.enum && !schema.enum.includes(data)) {
    errors.push(`${where}: ${JSON.stringify(data)} is not one of [${schema.enum.join(", ")}]`);
  }

  if (data !== null && typeof data === "object" && !Array.isArray(data)) {
    if (Array.isArray(schema.required)) {
      for (const key of schema.required) {
        if (!(key in data)) errors.push(`${where}: missing required property "${key}"`);
      }
    }
    if (schema.properties) {
      for (const [key, sub] of Object.entries(schema.properties)) {
        if (key in data) {
          errors.push(...validateAgainstSchema(data[key], sub, root, `${where}.${key}`));
        }
      }
    }
  }

  if (Array.isArray(data) && schema.items) {
    if (typeof schema.minItems === "number" && data.length < schema.minItems) {
      errors.push(`${where}: expected at least ${schema.minItems} item(s)`);
    }
    data.forEach((item, i) => {
      errors.push(...validateAgainstSchema(item, schema.items, root, `${where}[${i}]`));
    });
  }

  return errors;
}

function loadSchema() {
  return JSON.parse(fs.readFileSync(schemaPath, "utf8"));
}

// ---- Semantic validation ---------------------------------------------------

function validateGraph(graph, schema = loadSchema()) {
  const errors = [];
  const warnings = [];

  errors.push(...validateAgainstSchema(graph, schema));

  const nodes = Array.isArray(graph && graph.nodes) ? graph.nodes : [];
  const edges = Array.isArray(graph && graph.edges) ? graph.edges : [];

  // Unique node ids.
  const ids = new Set();
  const dupes = new Set();
  for (const n of nodes) {
    if (n && typeof n.id === "string") {
      if (ids.has(n.id)) dupes.add(n.id);
      ids.add(n.id);
    }
  }
  if (dupes.size) errors.push(`Duplicate node id(s): ${[...dupes].join(", ")}`);

  // Non-empty graph: at least one project and one type.
  const projects = nodes.filter((n) => n && n.kind === "project");
  const namespaces = nodes.filter((n) => n && n.kind === "namespace");
  const types = nodes.filter((n) => n && n.kind === "type");
  if (projects.length === 0) errors.push("Graph contains no project nodes.");
  if (types.length === 0) errors.push("Graph contains no type nodes.");

  // Every edge must reference existing nodes.
  let broken = 0;
  for (const e of edges) {
    if (!e) continue;
    if (!ids.has(e.source) || !ids.has(e.target)) {
      broken++;
      if (broken <= 10) {
        errors.push(`Edge references missing node: ${e.source} -> ${e.target} (${e.kind})`);
      }
    }
  }
  if (broken > 10) errors.push(`...and ${broken - 10} more edge(s) referencing missing nodes.`);

  // Documented type nodes should carry an href (reported, not fatal).
  const missingHref = types.filter((n) => !n.href).length;
  if (missingHref > 0) {
    warnings.push(`${missingHref} type node(s) have no DocFX href.`);
  }

  const summary = {
    nodes: nodes.length,
    edges: edges.length,
    projects: projects.length,
    namespaces: namespaces.length,
    types: types.length,
    missingHref,
  };

  return { errors, warnings, summary };
}

function formatSummary(summary) {
  return [
    "Graph summary:",
    `  total nodes : ${summary.nodes}`,
    `  total edges : ${summary.edges}`,
    `  projects    : ${summary.projects}`,
    `  namespaces  : ${summary.namespaces}`,
    `  types       : ${summary.types}`,
    `  missing href: ${summary.missingHref}`,
  ].join("\n");
}

// ---- CLI -------------------------------------------------------------------

function main() {
  const file = process.argv[2] || defaultGraphPath;
  if (!fs.existsSync(file)) {
    console.error(red(`Graph JSON not found: ${path.relative(projectRoot, file)}`));
    console.error("Run `task docs:graph` (or `node docs/tools/generate-dotnet-api-graph.cjs`) first.");
    process.exit(1);
  }

  let graph;
  try {
    graph = JSON.parse(fs.readFileSync(file, "utf8"));
  } catch (error) {
    console.error(red(`Graph JSON is not valid JSON: ${error.message}`));
    process.exit(1);
  }

  const { errors, warnings, summary } = validateGraph(graph);

  console.log(formatSummary(summary));
  warnings.forEach((w) => console.warn(yellow(`warning: ${w}`)));

  if (errors.length > 0) {
    errors.forEach((e) => console.error(red(`error: ${e}`)));
    console.error(red(`\nGraph validation failed with ${errors.length} error(s).`));
    process.exit(1);
  }

  console.log(green("Graph validation passed."));
}

module.exports = { validateGraph, validateAgainstSchema, loadSchema, formatSummary };

if (require.main === module) main();
