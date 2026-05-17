import type { StorybookConfig } from "@storybook/react-vite";
import { fileURLToPath, URL } from "node:url";

const config: StorybookConfig = {
  addons: ["@storybook/addon-essentials"],
  framework: {
    name: "@storybook/react-vite",
    options: {},
  },
  stories: ["../src/**/*.stories.@(ts|tsx)"],
  viteFinal(config) {
    config.resolve = {
      ...(config.resolve ?? {}),
      alias: {
        ...(config.resolve?.alias ?? {}),
        "@app": fileURLToPath(new URL("../src/app", import.meta.url)),
        "@pages": fileURLToPath(new URL("../src/pages", import.meta.url)),
        "@widgets": fileURLToPath(new URL("../src/widgets", import.meta.url)),
        "@features": fileURLToPath(new URL("../src/features", import.meta.url)),
        "@entities": fileURLToPath(new URL("../src/entities", import.meta.url)),
        "@shared": fileURLToPath(new URL("../src/shared", import.meta.url)),
      },
    };

    return config;
  },
};

export default config;
