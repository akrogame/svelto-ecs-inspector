import Masonry from "@mui/lab/Masonry";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import axios from "axios";
import * as React from "react";
import { useQuery } from "react-query";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "center",
  color: theme.palette.text.secondary,
}));

type Component = {
  name: string;
};
type Group = {
  name: string;
  id: number;
  components: Component[];
};

const renderComponents = (group: Group) => {
  if (group.components === undefined)
    return (
      <Typography
        fontSize={10}
        color="text.secondary"
        gutterBottom
        maxWidth="100%"
      >
        No component found
      </Typography>
    );
  else
    return group.components.map((component, ci) => {
      return (
        <Typography
          key={ci}
          fontSize={10}
          color="text.secondary"
          noWrap
          gutterBottom
          maxWidth="100%"
        >
          {component.name}
        </Typography>
      );
    });
};

export default function Groups() {
  const { isError, isLoading, data, error } = useQuery(
    ["groups"],
    async () => {
      const x = await axios.get<Group[]>("/debug/groups");
      return x.data.map((x) => ({
        name: x.name,
        id: x.id,
        components: x.components.sort((a, b) => a.name.localeCompare(b.name)),
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
    return <Typography color="text.primary">No groups</Typography>;
  return (
    <Masonry columns={4} spacing={2}>
      {data.map((group, index) => {
        return (
          <Item key={index}>
            <Typography
              sx={{ fontSize: 12 }}
              color="text.secondary"
              gutterBottom
            >
              [{group.id}]: {group.name}
            </Typography>
            <hr></hr>
            {renderComponents(group)}
          </Item>
        );
      })}
    </Masonry>
  );
}
