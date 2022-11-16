import DescriptionIcon from "@mui/icons-material/Description";
import HomeIcon from "@mui/icons-material/Home";
import PeopleIcon from "@mui/icons-material/People";
import TwoWheelerIcon from "@mui/icons-material/TwoWheeler";
import AppBar from "@mui/material/AppBar";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import TextField from "@mui/material/TextField";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import axios from "axios";
import * as React from "react";
import {
  NavLink,
  Outlet,
  useLocation,
  useParams,
  useSearchParams,
} from "react-router-dom";

const drawerWidth = 260;

const pages = [
  {
    display: "Summary",
    link: "",
    icon: <HomeIcon />,
  },
  {
    display: "Groups",
    link: "groups",
    icon: <DescriptionIcon />,
  },
  {
    display: "Entities",
    link: "entities",
    icon: <PeopleIcon />,
  },
  {
    display: "Engines",
    link: "engines",
    icon: <TwoWheelerIcon />,
  },
];

export const ServerContext = React.createContext("localhost:9300");

const servers = ["localhost:9300", "localhost:9301", "localhost:9302"];

export default function PermanentDrawerLeft() {
  const [searchParams, setSearchParams] = useSearchParams();
  const location = useLocation();
  console.log(location);

  const findPage = () => {
    const p = pages.findIndex(
      (x, i) => i > 0 && location.pathname.startsWith(`/${x.link}`)
    );
    return p < 0 ? 0 : p;
  };

  const [currentPage, setCurrentPage] = React.useState<number>(findPage());
  console.log(currentPage);
  if (!searchParams.has("url")) setSearchParams({ url: servers[0] });
  const urlFromParam = searchParams.get("url") || servers[0];
  const [url, setUrl] = React.useState<string>(urlFromParam);
  React.useEffect(() => {
    axios.defaults.baseURL = url;
  }, [url]);
  return (
    <Box sx={{ display: "flex" }}>
      <CssBaseline />
      <AppBar
        position="fixed"
        sx={{ width: `calc(100% - ${drawerWidth}px)`, ml: `${drawerWidth}px` }}
      >
        <Toolbar>
          <Typography variant="h6" noWrap component="div">
            {pages[currentPage].display}
          </Typography>
        </Toolbar>
      </AppBar>
      <Drawer
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          "& .MuiDrawer-paper": {
            width: drawerWidth,
            boxSizing: "border-box",
          },
        }}
        variant="permanent"
        anchor="left"
      >
        <Toolbar>
          <Autocomplete
            freeSolo
            value={url}
            options={servers}
            renderInput={(params) => (
              <TextField
                {...params}
                sx={{ width: "230px", padding: 0, margin: 0 }}
              />
            )}
            onChange={(_, newValue) => {
              const url = newValue === null ? "" : newValue;
              setUrl(url);
              setSearchParams({
                url: url,
              });
            }}
          />
        </Toolbar>
        <Divider />
        <List>
          {pages.map((page, index) => (
            <ListItemButton
              onClick={() => setCurrentPage(index)}
              component={NavLink}
              key={index}
              to={page.link}
              selected={currentPage === index}
            >
              <ListItemIcon>{page.icon}</ListItemIcon>
              <ListItemText primary={page.display} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>
      <Box
        component="main"
        sx={{ flexGrow: 1, bgcolor: "background.default", p: 3 }}
      >
        <Toolbar />
        <ServerContext.Provider value={url}>
          <Outlet />
        </ServerContext.Provider>
      </Box>
    </Box>
  );
}
