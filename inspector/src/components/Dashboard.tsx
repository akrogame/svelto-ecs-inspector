import { Button, Grid, Typography } from "@mui/material";
import { useCallback } from "react";
import { useState, useEffect } from "react";
import useWebSocket, { ReadyState } from "react-use-websocket";

export function Dashboard() {
  const enc = new TextEncoder();
  const dec = new TextDecoder();
  const { sendMessage, lastMessage, readyState } = useWebSocket(
    "ws://localhost:7777",
    {
      onOpen: () => console.log("opened"),
      //Will attempt to reconnect on all close events, such as server shutting down
      shouldReconnect: (closeEvent) => true,
      reconnectInterval: 2000,
    }
  );
  //const [messageHistory, setMessageHistory] = useState<string[]>([]);
  const [currentGroupData, setCurrentGroupDate] = useState<string>("");

  const onClick = useCallback(() => {
    console.log("sending message");
    sendMessage(enc.encode("Hello from the other side"));
  }, []);

  useEffect(() => {
    const f = async () => {
      if (lastMessage !== null) {
        const txt = await (lastMessage.data as Blob).text();
        setCurrentGroupDate(txt);
      }
    };
    f();
  }, [lastMessage, setCurrentGroupDate]);

  useEffect(() => {
    const interval = setInterval(() => {
      if (readyState == ReadyState.OPEN) sendMessage(enc.encode("ping"));
    }, 1000);
    sendMessage(enc.encode("sub groups"));
    return () => clearInterval(interval);
  }, [readyState]);

  return (
    <Grid container direction="row">
      <Button onClick={onClick}></Button>
      {/* {messageHistory.map((message, idx) => (
        <Grid item key={idx}>
          <Typography>{message ? message : "???"}</Typography>
        </Grid>
      ))} */}
      <Typography>{currentGroupData}</Typography>
    </Grid>
  );
}
