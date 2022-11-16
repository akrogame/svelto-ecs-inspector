import { Container, createTheme, ThemeProvider } from "@mui/material";
import React from "react";
import { QueryClient, QueryClientProvider } from "react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import "./App.css";
import { Dashboard } from "./components/Dashboard";
import Engines from "./components/Engines";
import Entities from "./components/Entities";
import EntityInspector from "./components/EntityInspector";
import Groups from "./components/Groups";
import Main from "./layout/Main";

const queryClient = new QueryClient();

function App() {
  const theme = React.useMemo(
    () =>
      createTheme({
        palette: {
          mode: "dark",
        },
      }),
    []
  );
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <Container className="App" maxWidth={false}>
          <BrowserRouter basename="/svelto-ecs-inspector">
            <Routes>
              <Route path="" element={<Main />}>
                <Route path="/" element={<Dashboard />} />
                <Route path="groups/" element={<Groups />} />
                <Route path="entities//*" element={<Entities />}>
                  <Route
                    path=":groupId/:entityId/"
                    element={<EntityInspector />}
                  />
                </Route>
                <Route path="engines/" element={<Engines />} />
              </Route>
            </Routes>
          </BrowserRouter>
        </Container>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
