import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Shell } from "./components/Shell";
import { AppProvider } from "./context/AppContext";
import { IngestPage } from "./pages/IngestPage";
import { OverviewPage } from "./pages/OverviewPage";
import { WorkflowDetailPage } from "./pages/WorkflowDetailPage";
import { WorkflowsPage } from "./pages/WorkflowsPage";

export default function App() {
  return (
    <AppProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Shell />}>
            <Route index element={<OverviewPage />} />
            <Route path="workflows" element={<WorkflowsPage />} />
            <Route path="workflows/:id" element={<WorkflowDetailPage />} />
            <Route path="ingest" element={<IngestPage />} />
            <Route path="*" element={<WorkflowsPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AppProvider>
  );
}
