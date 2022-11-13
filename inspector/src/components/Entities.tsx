import Masonry from "@mui/lab/Masonry";
import { Autocomplete, TextField } from "@mui/material";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import axios, { AxiosError } from "axios";
import * as React from "react";
import { useQuery } from "react-query";
import { Link as RouterLink, Route, Routes } from "react-router-dom";
import { Envelope, useInspectorStream } from "../streams/WebSocketHelper";
import EntityInspector from "./EntityInspector";
import EntityList from "./EntityList";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "center",
  color: theme.palette.text.secondary,
}));

type GrouppedEntities = {
  name: string;
  id: number;
  entities: number[];
};
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
