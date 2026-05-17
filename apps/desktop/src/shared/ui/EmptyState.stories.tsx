import { FileText } from "lucide-react";
import type { Meta, StoryObj } from "@storybook/react";
import { Button, EmptyState } from "@shared/ui";

const meta = {
  component: EmptyState,
  title: "Shared UI/EmptyState",
} satisfies Meta<typeof EmptyState>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    action: <Button variant="secondary">Create item</Button>,
    description: "Add content to start using this workspace.",
    icon: <FileText size={18} aria-hidden />,
    title: "Nothing here yet",
  },
};
