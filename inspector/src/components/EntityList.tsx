import Masonry from "@mui/lab/Masonry";
import CircularProgress from "@mui/material/CircularProgress";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import { styled } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import * as React from "react";
import { Link as RouterLink } from "react-router-dom";
import { Envelope, useInspectorStream } from "../streams/WebSocketHelper";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "center",
  color: theme.palette.text.secondary,
}));

type GroupDataStreamItem = {
  Name: string;
  Entities: number[];
};

const GroupText = styled(Typography)({
  fontSize: 12,
  color: "text.secondary",
});

const groups = (m: Map<number, GroupDataStreamItem>) => {
  const r: Array<React.ReactNode> = [];
  m.forEach((group: GroupDataStreamItem, groupId: number) => {
    r.push(
      <GroupedEntities
        key={`group-${groupId}-${group.Entities.sort().join("_")}`}
        groupId={groupId}
        name={group.Name}
        entities={group.Entities}
      />
    );
  });
  return r;
};

export type EntityListProps = {
  searchQuery: string | undefined;
};

export default function EntityList({ searchQuery }: EntityListProps) {
  const [data, setData] = React.useState<
    Map<number, GroupDataStreamItem> | undefined
  >(undefined);

  const { sendMessage, isOpen } = useInspectorStream<GroupDataStreamItem>({
    onMessageReceived: (e: Envelope<Map<number, GroupDataStreamItem>>) => {
      if (e.Id !== "entities") return;

      const newMap = new Map<string, GroupDataStreamItem>(
        Object.entries(e.Payload)
      );

      setData((existingData) => {
        const d =
          existingData === undefined
            ? new Map<number, GroupDataStreamItem>()
            : existingData;

        d.forEach((_, k) => {
          if (!newMap.has(k.toString())) d.delete(k);
        });

        newMap.forEach((v, k) => {
          const id = parseInt(k);
          d.set(id, v);
        });

        return d;
      });
    },
    onOpen: () => {},
  });

  React.useEffect(() => {
    if (isOpen)
      sendMessage(
        `sub entities ${
          searchQuery === undefined ? "" : encodeURI(searchQuery)
        }`
      );
    return () => {
      sendMessage(`un-sub entities`);
    };
  }, [sendMessage, searchQuery, isOpen]);
  if (data === undefined) return <CircularProgress />;

  if (data.size === 0)
    return <Typography color="text.primary">No Entities</Typography>;
  return (
    <div>
      <Masonry columns={4} spacing={2}>
        {groups(data)}
      </Masonry>
    </div>
  );
}

type GroupedEntitiesProps = {
  groupId: number;
  name: string;
  entities: number[];
};
const GroupedEntities = React.memo(
  ({ groupId, name, entities }: GroupedEntitiesProps) => {
    return (
      <Item>
        <GroupText gutterBottom>
          [{groupId}]: {name}
        </GroupText>
        <hr></hr>
        {entities.map((entity) => {
          return (
            <EntityLink
              key={`entity-${entity}`}
              groupId={groupId}
              entity={entity}
            />
          );
        })}
      </Item>
    );
  }
);

const EntityLinkText = styled(Typography)({
  fontSize: 10,
  color: "text.secondary",
});
type EntityLinkProps = {
  groupId: number;
  entity: number;
};
const EntityLink = React.memo(({ groupId, entity }: EntityLinkProps) => {
  return (
    <EntityLinkText gutterBottom>
      <Link component={RouterLink} to={`${groupId}/${entity}`}>
        {entity}
      </Link>
    </EntityLinkText>
  );
});
