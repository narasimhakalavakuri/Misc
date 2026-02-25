import React, { useEffect, useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { userService } from '../../service/user.service';
import { User } from '../../common/types';

export const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const data = await userService.getAllUsers();
      setUsers(data);
    } catch (error) {
      console.error('Failed to load users', error);
    } finally {
      setLoading(false);
    }
  };

  const columns: GridColDef[] = [
    { field: 'userid', headerName: 'User ID', width: 150 },
    { field: 'fullName', headerName: 'Full Name', width: 200 },
    { field: 'department', headerName: 'Department', width: 150 },
    { field: 'accessMask', headerName: 'Access Level', width: 120 },
    { field: 'email', headerName: 'Email', width: 200 }
  ];

  return (
    <Paper sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">User Management</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="contained" onClick={() => setDialogOpen(true)}>
            Add User
          </Button>
          <Button variant="outlined" onClick={loadUsers}>
            Refresh
          </Button>
        </Box>
      </Box>

      <DataGrid
        rows={users}
        columns={columns}
        loading={loading}
        autoHeight
        pageSizeOptions={[10, 20, 50]}
        initialState={{
          pagination: { paginationModel: { pageSize: 20 } }
        }}
      />

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add New User</DialogTitle>
        <DialogContent>
          <TextField fullWidth label="User ID" margin="normal" />
          <TextField fullWidth label="Full Name" margin="normal" />
          <TextField fullWidth label="Department" margin="normal" />
          <TextField fullWidth label="Email" margin="normal" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button variant="contained">Create User</Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
};