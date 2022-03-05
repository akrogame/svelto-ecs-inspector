import Masonry from "@mui/lab/Masonry";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import axios from "axios";
import * as React from "react";
import { useQuery } from "react-query";
import { useParams } from "react-router-dom";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "left",
  color: theme.palette.text.secondary,
  maxWidth: 400,
}));

type EntityComponent = {
  name: string;
  data: any;
};
type EntityComponents = {
  components: EntityComponent[];
};

export default function EntityInspector() {
  const params = useParams();
  const groupId = params.groupId;
  const entityId = params.entityId;
  const { isError, isLoading, data, error } = useQuery(
    ["entity-inspector", groupId, entityId],
    async () => {
      const x = await axios.get<EntityComponents>(
        `/debug/group/${groupId}/entity/${entityId}`
      );
      return x.data;
    },
    {
      refetchInterval: 100,
    }
  );
  console.log(data);
  if (isLoading || data === undefined) return <CircularProgress />;
  if (isError || error !== null)
    return (
      <Typography color="text.primary">
        Error: {error ?? "unknown error happened"}
      </Typography>
    );
  if (data.components.length === 0)
    return <Typography color="text.primary">No Entities</Typography>;
  return (
    <div>
      <Typography color="text.primary">Showing entity: {entityId}</Typography>
      <Masonry columns={4} spacing={2}>
        {data.components.map((component, index) => {
          return (
            <Item key={index}>
              <Typography fontSize={12} color="text.secondary" gutterBottom>
                {component.name}
              </Typography>
              <hr></hr>
              <pre>{JSON.stringify(component.data, null, 1)}</pre>
            </Item>
          );
        })}
      </Masonry>
    </div>
  );
}
