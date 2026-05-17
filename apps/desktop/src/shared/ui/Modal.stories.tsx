import type { Meta, StoryObj } from "@storybook/react";
import { Button, Modal } from "@shared/ui";

const meta = {
  component: Modal,
  title: "Shared UI/Modal",
} satisfies Meta<typeof Modal>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Open: Story = {
  args: {
    children: <Button>Modal action</Button>,
    description: "A focused overlay for short forms and decisions.",
    onClose: () => undefined,
    open: true,
    title: "Modal title",
  },
};
