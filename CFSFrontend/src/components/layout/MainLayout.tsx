
import React, { useState } from 'react';
import { Routes, Route, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Drawer,
  AppBar,
  Toolbar,
  List,
  Typography,
  Divider,
  IconButton,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  AddCircle as AddCircleIcon,
  CheckCircle as CheckCircleIcon,
  Edit as EditIcon,
  Assignment as AssignmentIcon,
  Settings as SettingsIcon,
  People as PeopleIcon,
  AccountCircle as AccountCircleIcon,
  ExitToApp as ExitToAppIcon,
} from '@mui/icons-material';
import { logout } from '../../features/auth/authSlice';
import { RootState, AppDispatch } from '../../store';
import { PositionEntryForm } from '../position/PositionEntryForm';
import { ApprovalList } from '../position/ApprovalList';
import { SystemControl } from '../system/SystemControl';
import { Dashboard } from '../dashboard/Dashboard';
import { IncompleteList } from '../position/IncompleteList';
import { CorrectionList } from '../position/CorrectionList';
import { UserManagement } from '../admin/UserManagement';
import { Reports } from '../reports/Reports';
import { AccessLevel } from '../../common/types';

const DRAWER_WIDTH = 260;

interface MenuItem {
  text: string;
  icon: React.ElementType;
  path: string;
  access: AccessLevel;
}

export const MainLayout: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const user = useSelector((state: RootState) => state.auth.user);

  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const handleDrawerToggle = (): void => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>): void => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = (): void => {
    setAnchorEl(null);
  };

  const handleLogout = (): void => {
    dispatch(logout());
    navigate('/login');
  };

  const hasAccess = (level: AccessLevel): boolean => {
    if (!user?.accessMask) return false;
    const mask = parseInt(user.accessMask, 10);
    return (mask & level) === level;
  };

  const menuItems: MenuItem[] = [
    {
      text: 'Dashboard',
      icon: DashboardIcon,
      path: 'dashboard',
      access: AccessLevel.QUERY,
    },
    {
      text: 'Position Entry',
      icon: AddCircleIcon,
      path: 'entry',
      access: AccessLevel.INPUT,
    },
    {
      text: 'Approval',
      icon: CheckCircleIcon,
      path: 'approval',
      access: AccessLevel.CHECK,
    },
    {
      text: 'Incomplete Entries',
      icon: EditIcon,
      path: 'incomplete',
      access: AccessLevel.INPUT,
    },
    {
      text: 'Corrections',
      icon: AssignmentIcon,
      path: 'corrections',
      access: AccessLevel.INPUT,
    },
    {
      text: 'Reports',
      icon: AssignmentIcon,
      path: 'reports',
      access: AccessLevel.REPORT,
    },
    {
      text: 'User Management',
      icon: PeopleIcon,
      path: 'users',
      access: AccessLevel.USER_ADMIN,
    },
    {
      text: 'System Control',
      icon: SettingsIcon,
      path: 'system',
      access: AccessLevel.SYSTEM_CONTROL,
    },
  ];

  const drawer = (
    <Box>
      <Toolbar>
        <Typography variant="h6" noWrap component="div">
          Position Reporting
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {menuItems
          .filter((item) => hasAccess(item.access))
          .map((item) => (
            <ListItem key={item.text} disablePadding>
              <ListItemButton
                onClick={() => {
                  navigate(item.path);
                  if (isMobile) setMobileOpen(false);
                }}
              >
                <ListItemIcon>
                  <item.icon />
                </ListItemIcon>
                <ListItemText primary={item.text} />
              </ListItemButton>
            </ListItem>
          ))}
      </List>
      <Divider />
      <Box sx={{ p: 2 }}>
        <Typography variant="caption" color="text.secondary">
          Version 1.4.0
        </Typography>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { md: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            {user?.department} - Position Reporting Entry System
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography
              variant="body2"
              sx={{ display: { xs: 'none', sm: 'block' } }}
            >
              {user?.fullName || user?.userid}
            </Typography>
            <IconButton
              size="large"
              edge="end"
              onClick={handleProfileMenuOpen}
              color="inherit"
            >
              <AccountCircleIcon />
            </IconButton>
          </Box>
        </Toolbar>
      </AppBar>
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleProfileMenuClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        <MenuItem disabled>
          <Typography variant="body2">
            <strong>{user?.userid}</strong>
          </Typography>
        </MenuItem>
        <MenuItem disabled>
          <Typography variant="caption" color="text.secondary">
            {user?.department}
          </Typography>
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleLogout}>
          <ListItemIcon>
            <ExitToAppIcon fontSize="small" />
          </ListItemIcon>
          Logout
        </MenuItem>
      </Menu>

      <Box
        component="nav"
        sx={{
          width: { md: DRAWER_WIDTH },
          flexShrink: { md: 0 },
        }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true,
          }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: DRAWER_WIDTH,
            },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: DRAWER_WIDTH,
            },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          mt: 8,
        }}
      >
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="entry" element={<PositionEntryForm />} />
          <Route path="approval" element={<ApprovalList />} />
          <Route path="incomplete" element={<IncompleteList />} />
          <Route path="corrections" element={<CorrectionList />} />
          <Route path="reports" element={<Reports />} />
          <Route path="users" element={<UserManagement />} />
          <Route path="system" element={<SystemControl />} />
        </Routes>
      </Box>
    </Box>
  );
};
