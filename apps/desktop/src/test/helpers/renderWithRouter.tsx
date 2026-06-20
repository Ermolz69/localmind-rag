import {
  render,
  type RenderOptions,
  type RenderResult,
} from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter, type MemoryRouterProps } from "react-router-dom";

interface RenderWithRouterOptions extends Omit<RenderOptions, "wrapper"> {
  routerProps?: MemoryRouterProps;
}

export function renderWithRouter(
  ui: ReactElement,
  { routerProps, ...options }: RenderWithRouterOptions = {},
): RenderResult {
  return render(<MemoryRouter {...routerProps}>{ui}</MemoryRouter>, options);
}
