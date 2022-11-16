import { useCallback, useContext, useEffect } from "react";
import useWebSocket, { ReadyState } from "react-use-websocket";
import { ServerContext } from "../layout/Main";

export interface Envelope<T> {
  Id: string;
  Payload: T;
}
export interface InspectorStreamOptions {
  onMessageReceived: (e: Envelope<any>) => void;
  onOpen: () => void;
}
const enc = new TextEncoder();
export function useInspectorStream<T = any>(opts: InspectorStreamOptions) {
  const serverAddr = useContext(ServerContext);
  const onMessageReceived = useCallback(
    async (msg: MessageEvent<any>) => {
      const txt = await (msg.data as Blob).text();
      const e = JSON.parse(txt) as Envelope<T>;
      opts.onMessageReceived(e);
    },
    [opts]
  );

  const { sendMessage, readyState } = useWebSocket(`ws://${serverAddr}`, {
    share: true,
    onOpen: () => {
      //console.log("opened");
      //opts.onOpen();
    },
    //Will attempt to reconnect on all close events, such as server shutting down
    shouldReconnect: (closeEvent) => true,
    reconnectInterval: 2000,
    onMessage: onMessageReceived,
  });

  useEffect(() => {
    if (readyState === ReadyState.OPEN) opts.onOpen();
  }, [opts, readyState]);

  const sendStringMessage = useCallback(
    (s: string) => {
      sendMessage(enc.encode(s));
    },
    [sendMessage]
  );

  return {
    sendMessage: (msg: string) => {
      sendStringMessage(msg);
    },
    readyState,
    isOpen: readyState === ReadyState.OPEN,
  };
}
