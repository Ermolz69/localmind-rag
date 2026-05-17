import type { Preview } from "@storybook/react";
import "../src/app/styles/globals.css";
import "../src/app/styles/theme.css";

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
};

export default preview;
