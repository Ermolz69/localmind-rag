import type { Meta, StoryObj } from "@storybook/react";
import { ConfirmDialog } from "@shared/ui";

const meta = {
  component: ConfirmDialog,
  title: "Shared UI/ConfirmDialog",
} satisfies Meta<typeof ConfirmDialog>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Open: Story = {
  args: {
    confirmLabel: "Delete",
    description: "This action cannot be undone.",
    onClose: () => undefined,
    onConfirm: () => undefined,
    open: true,
    title: "Delete item",
  },
};
