import type { Meta, StoryObj } from "@storybook/react";
import { ErrorBanner } from "@shared/ui";

const meta = {
  component: ErrorBanner,
  title: "Shared UI/ErrorBanner",
} satisfies Meta<typeof ErrorBanner>;

export default meta;

type Story = StoryObj<typeof meta>;

export const WithMessage: Story = {
  args: {
    message: "The local API request failed.",
  },
};
