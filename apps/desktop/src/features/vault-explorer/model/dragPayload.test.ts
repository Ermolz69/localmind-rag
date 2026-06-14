import { describe, expect, it } from "vitest";
import {
  getVaultItemDragData,
  setVaultItemDragData,
  vaultItemDragType,
} from "./dragPayload";

function createDataTransfer() {
  const values = new Map<string, string>();

  return {
    dataTransfer: {
      dropEffect: "none",
      effectAllowed: "none",
      getData: (type: string) => values.get(type) ?? "",
      setData: (type: string, value: string) => values.set(type, value),
    } as unknown as DataTransfer,
    values,
  };
}

describe("vault drag payload", () => {
  it("writes a move payload that can be read back", () => {
    const { dataTransfer, values } = createDataTransfer();

    setVaultItemDragData(dataTransfer, { type: "note", id: "note-id" });

    expect(dataTransfer.effectAllowed).toBe("move");
    expect(values.has(vaultItemDragType)).toBe(true);
    expect(getVaultItemDragData(dataTransfer)).toEqual({
      type: "note",
      id: "note-id",
    });
  });

  it("rejects malformed payloads", () => {
    const { dataTransfer } = createDataTransfer();
    dataTransfer.setData(vaultItemDragType, JSON.stringify({ type: "note" }));

    expect(getVaultItemDragData(dataTransfer)).toBeNull();
  });
});
