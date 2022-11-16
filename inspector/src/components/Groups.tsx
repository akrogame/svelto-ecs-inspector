import Masonry from "@mui/lab/Masonry";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import axios, { AxiosError } from "axios";
import * as React from "react";
import { useState } from "react";
import { useQuery } from "react-query";
import useWebSocket from "react-use-websocket";
import { Envelope, useInspectorStream } from "../streams/WebSocketHelper";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "center",
  color: theme.palette.text.secondary,
}));

type Group = {
  name: string;
  id: number;
  components: string[];
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
          {component}
        </Typography>
      );
    });
};

export default function Groups() {
  const enc = new TextEncoder();
  const dec = new TextDecoder();
  const [data, setData] = useState<Group[] | undefined>(undefined);

  const { sendMessage } = useInspectorStream({
    onMessageReceived: (e: Envelope<any>) => {
      if (e.Id !== "groups") return;
      var result: Array<Group> = [];

      for (var i in e.Payload)
        result.push({
          name: i,
          id: 0,
          components: e.Payload[i].sort((a: any, b: any) => a.localeCompare(b)),
        });

      setData(result);
    },
    onOpen: () => {
      sendMessage("sub groups");
    },
  });
  if (data === undefined) return <CircularProgress />;

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
