import { describe, expect, it } from "vitest";
import type { BucketDto } from "@entities/bucket";
import {
  buildFilterChips,
  extractLiveCommands,
  prepareChatSubmission,
  removeFilter,
  getAutocompleteContext,
} from "./commandFilters";

const buckets: BucketDto[] = [
  {
    id: "bucket-work",
    name: "Work",
    description: null,
    syncStatus: "LocalOnly",
    createdAt: "2026-06-09T00:00:00Z",
    updatedAt: null,
  },
  {
    id: "bucket-personal",
    name: "Personal",
    description: null,
    syncStatus: "LocalOnly",
    createdAt: "2026-06-09T00:00:00Z",
    updatedAt: null,
  },
];

describe("commandFilters", () => {
  it("extracts a complete bucket command into filters while typing", () => {
    const result = extractLiveCommands("/bucket Work", {}, buckets);

    expect(result.content).toBe("");
    expect(result.filters.bucketId).toBe("bucket-work");
    expect(result.consumedCommand).toBe(true);
  });

  it("keeps incomplete commands in the draft during live parsing", () => {
    const result = extractLiveCommands("/bucket Wor", {}, buckets);

    expect(result.content).toBe("/bucket Wor");
    expect(result.filters.bucketId).toBeUndefined();
    expect(result.consumedCommand).toBe(false);
  });

  it("preserves trailing spaces in the draft during live parsing", () => {
    const result = extractLiveCommands("hello ", {}, buckets);

    expect(result.content).toBe("hello ");
  });

  it("creates date and file type filters from valid command lines on submit", () => {
    const result = prepareChatSubmission(
      [
        "/date from=10.05.2025&to=11.05.2026",
        "/file-type pdf",
        "show me the notes",
      ].join("\n"),
      {},
      buckets,
    );

    expect(result.error).toBeNull();
    expect(result.content).toBe("show me the notes");
    expect(result.filters.dateFrom).toBe("2025-05-10T00:00:00.000Z");
    expect(result.filters.dateTo).toBe("2026-05-11T00:00:00.000Z");
    expect(result.filters.fileType).toBe("pdf");
  });

  it("rejects malformed commands before submit", () => {
    const result = prepareChatSubmission("/date from=2025-05-10", {}, buckets);

    expect(result.error).toBe("Date filters must use DD.MM.YYYY format.");
  });

  it("does not send raw command text as content on mixed submit", () => {
    const result = prepareChatSubmission(
      ["/bucket Work", "what changed this week?"].join("\n"),
      {},
      buckets,
    );

    expect(result.error).toBeNull();
    expect(result.content).toBe("what changed this week?");
    expect(result.filters.bucketId).toBe("bucket-work");
  });

  it("builds chips and removes only the requested filter", () => {
    const chips = buildFilterChips(
      {
        bucketId: "bucket-work",
        dateFrom: "2025-05-10T00:00:00.000Z",
        dateTo: "2026-05-11T00:00:00.000Z",
        fileType: "pdf",
      },
      buckets,
    );

    expect(chips.map((chip) => chip.label)).toEqual([
      "Bucket: Work",
      "Date: 10.05.2025 to 11.05.2026",
      "File: pdf",
    ]);

    const remaining = removeFilter(
      {
        bucketId: "bucket-work",
        dateFrom: "2025-05-10T00:00:00.000Z",
        dateTo: "2026-05-11T00:00:00.000Z",
        fileType: "pdf",
      },
      "date",
    );

    expect(remaining.bucketId).toBe("bucket-work");
    expect(remaining.fileType).toBe("pdf");
    expect(remaining.dateFrom).toBeNull();
    expect(remaining.dateTo).toBeNull();
  });

  describe("getAutocompleteContext", () => {
    it("returns none type when cursor is not in command line", () => {
      const result = getAutocompleteContext("hello world", 5, buckets);
      expect(result.type).toBe("none");
      expect(result.suggestions).toHaveLength(0);
    });

    it("returns command type and all suggestions when typing '/'", () => {
      const result = getAutocompleteContext("/", 1, buckets);
      expect(result.type).toBe("command");
      expect(result.query).toBe("/");
      expect(result.suggestions.map((s) => s.text)).toEqual([
        "/bucket",
        "/date",
        "/file-type",
      ]);
      expect(result.startIndex).toBe(0);
      expect(result.endIndex).toBe(1);
    });

    it("returns command type and filtered suggestions when typing '/bu'", () => {
      const result = getAutocompleteContext("/bu", 3, buckets);
      expect(result.type).toBe("command");
      expect(result.query).toBe("/bu");
      expect(result.suggestions.map((s) => s.text)).toEqual(["/bucket"]);
      expect(result.startIndex).toBe(0);
      expect(result.endIndex).toBe(3);
    });

    it("returns bucket type with all buckets when command is typed and argument is empty", () => {
      const result = getAutocompleteContext("/bucket ", 8, buckets);
      expect(result.type).toBe("bucket");
      expect(result.query).toBe("");
      expect(result.suggestions.map((s) => s.text)).toEqual([
        "Work",
        "Personal",
      ]);
      expect(result.startIndex).toBe(8);
      expect(result.endIndex).toBe(8);
    });

    it("returns bucket type with filtered buckets when typing '/bucket Wo'", () => {
      const result = getAutocompleteContext("/bucket Wo", 10, buckets);
      expect(result.type).toBe("bucket");
      expect(result.query).toBe("Wo");
      expect(result.suggestions.map((s) => s.text)).toEqual(["Work"]);
      expect(result.startIndex).toBe(8);
      expect(result.endIndex).toBe(10);
    });

    it("returns file-type suggestions when typing '/file-type p'", () => {
      const result = getAutocompleteContext("/file-type p", 12, buckets);
      expect(result.type).toBe("file-type");
      expect(result.query).toBe("p");
      expect(result.suggestions.map((s) => s.text)).toEqual(["pdf", "pptx"]);
      expect(result.startIndex).toBe(11);
      expect(result.endIndex).toBe(12);
    });

    it("returns date templates when typing '/date '", () => {
      const result = getAutocompleteContext("/date ", 6, buckets);
      expect(result.type).toBe("date");
      expect(result.query).toBe("");
      expect(result.suggestions.map((s) => s.text)).toEqual([
        "from=DD.MM.YYYY",
        "to=DD.MM.YYYY",
        "from=DD.MM.YYYY&to=DD.MM.YYYY",
      ]);
      expect(result.startIndex).toBe(6);
      expect(result.endIndex).toBe(6);
    });
  });
});
