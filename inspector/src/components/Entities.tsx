import { TextField } from "@mui/material";
import Typography from "@mui/material/Typography";
import * as React from "react";
import { Route, Routes } from "react-router-dom";
import EntityInspector from "./EntityInspector";
import EntityList from "./EntityList";

function NoEntitySelected() {
  return (
    <Typography fontSize={20} color="text.primary" gutterBottom>
      Please select an entity
    </Typography>
  );
}

export default function Entities() {
  const [searchQuery, setSearchQuery] = React.useState<string | undefined>(
    undefined
  );
  const updateSearchQuery = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.value === "") setSearchQuery(undefined);
    else setSearchQuery(event.target.value);
  };
  return (
    <div>
      <TextField
        label="Search"
        variant="standard"
        value={searchQuery || ""}
        onChange={updateSearchQuery}
      />
      <EntityList searchQuery={searchQuery} />
      <hr></hr>
      <Routes>
        <Route index element={<NoEntitySelected />} />
        <Route path="/:groupId/:entityId" element={<EntityInspector />} />
      </Routes>
    </div>
  );
}
