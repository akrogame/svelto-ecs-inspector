import DescriptionIcon from "@mui/icons-material/Description";
import HomeIcon from '@mui/icons-material/Home';
import PeopleIcon from "@mui/icons-material/People";
import TwoWheelerIcon from "@mui/icons-material/TwoWheeler";
import AppBar from "@mui/material/AppBar";
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
import { NavLink, Outlet } from "react-router-dom";

const drawerWidth = 240;

const pages = [
  {
    display: "Inspector",
    link: "/",
    icon: <HomeIcon />,
  },
  {
    display: "Groups",
    link: "/groups",
    icon: <DescriptionIcon />,
  },
  {
    display: "Entities",
    link: "/entities",
    icon: <PeopleIcon />,
  },
  {
    display: "Engines",
    link: "/engines",
    icon: <TwoWheelerIcon />,
  },
];

export default function PermanentDrawerLeft() {
  const [currentPage, setCurrentPage] = React.useState<number>(0);
  const [url, setUrl] = React.useState<string>("http://localhost:3001");
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
          <TextField
            value={url}
            onChange={(event) => setUrl(event.target.value)}
          ></TextField>
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
        <Outlet />
      </Box>
    </Box>
  );
}
