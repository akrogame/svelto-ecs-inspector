import * as React from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Autocomplete from "@mui/material/Autocomplete";
import CircularProgress from "@mui/material/CircularProgress";
import TextField from "@mui/material/TextField";
import { useQuery } from "react-query";
import axios, { AxiosError } from "axios";
import { styled } from "@mui/material/styles";
import Paper from "@mui/material/Paper";
import Masonry from "@mui/lab/Masonry";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0.5),
  textAlign: "center",
  color: theme.palette.text.secondary,
}));

type Engine = {
  name: string;
  components: Array<Array<string>>;
};

const hasComponent = (engine: Engine, component: string) => {
  return engine.components.some((q) => q.some((c) => c === component));
};

export default function Engines() {
  const { isError, isLoading, data, error } = useQuery<Engine[], AxiosError>(
    ["engines"],
    async () => {
      const x = await axios.get<Engine[]>("/debug/engines");
      return x.data;
    },
    {
      refetchInterval: 1000,
    }
  );
  const components = React.useMemo(() => {
    const set =
      data === undefined
        ? new Set<string>()
        : new Set<string>(data.flatMap((x) => x.components.flatMap((q) => q)));
    return Array.from(set).sort();
  }, [data]);

  const [filter, setFilter] = React.useState<string[]>([]);

  if (isLoading || data === undefined) return <CircularProgress />;
  if (isError || error !== null)
    return (
      <Typography color="text.primary">
        Error: {error.message ?? "unknown error happened"}
      </Typography>
    );
  if (data.length === 0)
    return <Typography color="text.primary">No systems</Typography>;
  return (
    <React.Fragment>
      <Box marginBottom={2}>
        <Autocomplete
          multiple
          id="tags-standard"
          options={components}
          getOptionLabel={(option) => option}
          defaultValue={undefined}
          onChange={(event, value) => setFilter(value)}
          renderInput={(params) => (
            <TextField
              {...params}
              variant="standard"
              label="Component Filter"
              placeholder="Component"
            />
          )}
        />
      </Box>
      <Box>
        <Masonry columns={4} spacing={2}>
          {data
            .filter((x) => filter.every((f) => hasComponent(x, f)))
            .map((system, index) => {
              if (system.components === undefined)
                console.error("undefined", system);
              return (
                <Item key={index}>
                  <Typography
                    sx={{ fontSize: 12 }}
                    color="text.secondary"
                    gutterBottom
                  >
                    <b>{system.name}</b>
                  </Typography>
                  <hr></hr>
                  {system.components.map((queryInvocation, qi) => {
                    return (
                      <div key={qi}>
                        {queryInvocation.map((component, ci) => {
                          return (
                            <Typography
                              key={`${qi}-${ci}`}
                              fontSize={10}
                              fontWeight={
                                filter.some((f) => f === component)
                                  ? "bold"
                                  : "initial"
                              }
                              color={
                                filter.some((f) => f === component)
                                  ? "primary"
                                  : "text.secondary"
                              }
                              gutterBottom
                            >
                              {component}
                            </Typography>
                          );
                        })}
                        <hr></hr>
                      </div>
                    );
                  })}
                </Item>
              );
            })}
        </Masonry>
      </Box>
    </React.Fragment>
  );
}
