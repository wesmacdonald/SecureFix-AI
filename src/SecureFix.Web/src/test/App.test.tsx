import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import App from "../App";

describe("SecureFix dashboard", () => {
  it("renders overview metrics and the human approval guardrail", () => {
    render(<App />);
    expect(screen.getByRole("heading", { name: /security release control center/i })).toBeInTheDocument();
    expect(screen.getByText(/human approval enforced/i)).toBeInTheDocument();
    expect(screen.getByText(/advisory automation only/i)).toBeInTheDocument();
  });

  it("filters workflow rows", async () => {
    window.history.pushState({}, "", "/workflows");
    render(<App />);
    const search = screen.getByPlaceholderText(/search cve/i);
    await userEvent.type(search, "lodash");
    expect(screen.getByText("lodash")).toBeInTheDocument();
    expect(screen.queryByText("axios")).not.toBeInTheDocument();
  });
});
