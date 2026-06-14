export const vaultItemDragType = "application/x-localmind-vault-item";

export type VaultItemDragPayload = {
  type: "note" | "folder";
  id: string;
};

export function setVaultItemDragData(
  dataTransfer: DataTransfer,
  payload: VaultItemDragPayload,
) {
  const serialized = JSON.stringify(payload);

  dataTransfer.effectAllowed = "move";
  dataTransfer.setData(vaultItemDragType, serialized);
  dataTransfer.setData("text/plain", serialized);
}

export function getVaultItemDragData(
  dataTransfer: DataTransfer,
): VaultItemDragPayload | null {
  const serialized =
    dataTransfer.getData(vaultItemDragType) ||
    dataTransfer.getData("text/plain");

  if (!serialized) {
    return null;
  }

  try {
    const value = JSON.parse(serialized) as unknown;

    if (
      typeof value !== "object" ||
      value === null ||
      !("type" in value) ||
      !("id" in value)
    ) {
      return null;
    }

    const { type, id } = value;
    if (
      (type !== "note" && type !== "folder") ||
      typeof id !== "string" ||
      !id
    ) {
      return null;
    }

    return { type, id };
  } catch {
    return null;
  }
}
