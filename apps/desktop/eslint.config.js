import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: [
      "dist/**",
      "node_modules/**",
      "storybook-static/**",
      "src-tauri/target/**",
      "src-tauri/gen/**",
      "src-tauri/icons/**",
      "src/shared/contracts/generated.ts",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        projectService: {
          allowDefaultProject: [".storybook/*.ts"],
        },
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "react-hooks": reactHooks,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "@typescript-eslint/no-explicit-any": "error",
    },
  },
  {
    files: ["src/shared/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: ["@features/*", "@pages/*", "@widgets/*"],
              message: "shared layer must not import upper layers",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/entities/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: ["@features/*", "@pages/*", "@widgets/*"],
              message: "entities layer must not import upper layers",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@features/chat",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/sync",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/semantic-search",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: [
                "@features/*/api/*",
                "@features/*/model/*",
                "@features/*/ui/*",
                "@pages/*",
                "@widgets/*",
                "@shared/api/*",
                "@shared/lib/hooks/*",
                "@shared/ui/*",
              ],
              message:
                "features must use public layer entrypoints and must not import pages/widgets",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/pages/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@features/chat",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/sync",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/semantic-search",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: [
                "@features/*/api/*",
                "@features/*/model/*",
                "@features/*/ui/*",
                "@shared/api",
                "@shared/api/*",
                "@shared/lib/hooks/*",
                "@shared/ui/*",
              ],
              message:
                "pages must use feature public APIs and shared public entrypoints",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/widgets/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@features/chat",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/sync",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@features/semantic-search",
              message: "root placeholder feature slices are not allowed",
            },
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: [
                "@features/*",
                "@pages/*",
                "@shared/api/*",
                "@shared/lib/hooks/*",
                "@shared/model",
                "@shared/model/*",
                "@shared/ui/*",
              ],
              message:
                "widgets must not import pages/features and must use shared public entrypoints",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/entities/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: [
                "@features/*",
                "@pages/*",
                "@widgets/*",
                "@shared/api/*",
                "@shared/lib/hooks/*",
                "@shared/model",
                "@shared/model/*",
                "@shared/ui/*",
              ],
              message: "use shared public entrypoints",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/shared/ui/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "@shared/api/client",
              message: "use named API slices from @shared/api instead",
            },
          ],
          patterns: [
            {
              group: ["@shared/model", "@shared/model/*"],
              message: "shared UI primitives must receive state through props",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["**/*.cjs"],
    languageOptions: {
      globals: globals.node,
    },
    rules: {
      "@typescript-eslint/no-require-imports": "off",
    },
  },
);
