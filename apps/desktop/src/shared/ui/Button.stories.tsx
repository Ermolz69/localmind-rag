import type { Meta, StoryObj } from "@storybook/react";
import { Button } from "@shared/ui";

const meta = {
  component: Button,
  title: "Shared UI/Button",
} satisfies Meta<typeof Button>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Primary: Story = {
  args: {
    children: "Primary action",
  },
};

export const Secondary: Story = {
  args: {
    children: "Secondary action",
    variant: "secondary",
  },
};
