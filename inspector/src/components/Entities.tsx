import Masonry from '@mui/lab/Masonry';
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import Paper from '@mui/material/Paper';
import { styled } from '@mui/material/styles';
import Typography from "@mui/material/Typography";
import axios from "axios";
import * as React from "react";
import { useQuery } from "react-query";
import { Link as RouterLink, Route, Routes } from "react-router-dom";
import EntityInspector from "./EntityInspector";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === 'dark' ? '#1A2027' : '#fff',
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: 'center',
  color: theme.palette.text.secondary,
}));

type Entity = {
  entityId: number;
};
type GrouppedEntities = {
  name: string;
  id: number;
  entities: Entity[];
};
function NoEntitySelected() {
  return (
    <Typography
    sx={{ fontSize: 20 }}
    color="text.primary"
    gutterBottom
    >
    Please select an entity
  </Typography>
  )
}

export default function Entities() {
  const { isError, isLoading, data, error } = useQuery(
    ["entities"],
    async () => {
      const x = await axios.get<GrouppedEntities[]>("/debug/entities");
      return x.data.map((x) => ({
        name: x.name,
        id: x.id,
        entities: x.entities.sort((a, b) => a.entityId - b.entityId),
      }));
    },
    {
      refetchInterval: 1000,
    }
  );
  if (isLoading || data === undefined) return <CircularProgress />;
  if (isError || error !== null)
    return (
      <Typography color="text.primary">
        Error: {error ?? "unknown error happened"}
      </Typography>
    );
  if (data.length === 0)
    return <Typography color="text.primary">No Entities</Typography>;
  return (
    <div>
      <Masonry columns={4} spacing={2}>
        {data.map((group, index) => {
          return (
            <Item key={index} >
              <Typography
                sx={{ fontSize: 12 }}
                color="text.secondary"
                gutterBottom
                >
                [{group.id}]: {group.name}
              </Typography>
              <hr></hr>
              {group.entities.map((entity, ei) => {
                return (
                  <Typography
                  key={ei}
                  sx={{ fontSize: 10 }}
                  color="text.secondary"
                  gutterBottom
                  >
                    <Link component={RouterLink} to={`${group.id}/${entity.entityId}`}> {entity.entityId}</Link>
                  </Typography>
                );
              })}
            </Item>
          );
        })}
      </Masonry>
      <hr></hr>
      <Routes>
        <Route index element={<NoEntitySelected />} />
        <Route path="/:groupId/:entityId" element={<EntityInspector />} />
      </Routes>
    </div>
  );
}
