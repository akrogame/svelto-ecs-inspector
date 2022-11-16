import Masonry from "@mui/lab/Masonry";
import CircularProgress from "@mui/material/CircularProgress";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import * as React from "react";
import ReactJson, { InteractionProps } from "react-json-view";
import { useParams } from "react-router-dom";
import { Envelope, useInspectorStream } from "../streams/WebSocketHelper";

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
  prettyName: string;
  data: any;
};
export default function EntityInspector() {
  const params = useParams();
  const groupId = params.groupId;
  const entityId = params.entityId;
  const [data, setData] = React.useState<EntityComponent[] | undefined>(
    undefined
  );
  // const { isError, isLoading, data, error } = useQuery<
  //   EntityComponents,
  //   AxiosError
  // >(
  //   ["entity-inspector", groupId, entityId],
  //   async () => {
  //     const x = await axios.get<EntityComponents>(
  //       `/debug/group/${groupId}/entity/${entityId}`
  //     );
  //     return x.data;
  //   },
  //   {
  //     refetchInterval: 100,
  //   }
  // );
  const { sendMessage, isOpen } = useInspectorStream({
    onMessageReceived: (e: Envelope<any>) => {
      if (e.Id !== "entity-data") return;
      var result: Array<EntityComponent> = [];

      for (var i in e.Payload)
        result.push({
          name: i,
          prettyName: e.Payload[i].PrettyName,
          data: e.Payload[i].Data,
        });

      setData(result);
    },
    onOpen: () => {
      sendMessage(`sub entity-data ${groupId} ${entityId}`);
    },
  });

  React.useEffect(() => {
    if (isOpen) sendMessage(`sub entity-data ${groupId} ${entityId}`);
    return () => {
      sendMessage(`un-sub entity-data ${groupId} ${entityId}`);
    };
  }, [isOpen, sendMessage, groupId, entityId]);

  if (data === undefined) return <CircularProgress />;

  if (groupId === undefined || entityId === undefined)
    return (
      <Typography>
        You need to provide an entityId and a groupId to use this component
      </Typography>
    );

  if (data.length === 0)
    return <Typography color="text.primary">No Entities</Typography>;
  return (
    <div>
      <Typography color="text.primary">Showing entity: {entityId}</Typography>
      <Masonry columns={3} spacing={2}>
        {data.map((component, index) => {
          return (
            <EditableEntityComponent
              key={component.name}
              groupId={groupId}
              entityId={entityId}
              data={component.data}
              name={component.name}
              prettyName={component.prettyName}
              sendMessage={sendMessage}
            />
          );
        })}
      </Masonry>
    </div>
  );
}

type EditableEntityComponentProps = {
  data: any;
  name: string;
  prettyName: string;
  groupId: string;
  entityId: string;
  sendMessage: (msg: string) => void;
};

const EditableEntityComponent = React.memo(
  ({
    groupId,
    entityId,
    data,
    prettyName,
    name,
    sendMessage,
  }: EditableEntityComponentProps) => {
    const editCommit = React.useCallback(
      (edit: InteractionProps) => {
        sendMessage(
          `update ${groupId} ${entityId} ${name} ${JSON.stringify(
            edit.updated_src
          )}`
        );
        return true;
      },
      [sendMessage, groupId, entityId, name]
    );
    return (
      <Item>
        <ReactJson
          src={data}
          name={prettyName}
          indentWidth={2}
          theme={"flat"}
          onEdit={editCommit}
        />
      </Item>
    );
  },
  (prevProps, nextProps) =>
    prevProps.name === nextProps.name &&
    JSON.stringify(prevProps.data) === JSON.stringify(nextProps.data)
);
