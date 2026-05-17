import type { Meta, StoryObj } from "@storybook/react";
import { StatusBadge } from "@shared/ui";

const meta = {
  component: StatusBadge,
  title: "Shared UI/StatusBadge",
} satisfies Meta<typeof StatusBadge>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Ready: Story = {
  args: {
    className: "border-border bg-muted text-muted-foreground",
    label: "Ready",
  },
};
