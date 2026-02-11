import React, { useEffect, useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip
} from '@mui/material';
import { systemService } from '../../service/system.service';
import { Department } from '../../common/types';
import { format } from 'date-fns';
import { DISPLAY_DATE_FORMAT } from '../../constants/app.constants';

export const SystemControl: React.FC = () => {
  const [currentDate, setCurrentDate] = useState<string>('');
  const [verifyDate, setVerifyDate] = useState('');
  const [departments, setDepartments] = useState<Department[]>([]);
  const [selectedDept, setSelectedDept] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    loadSystemData();
  }, []);

  const loadSystemData = async () => {
    try {
      const serverDate = await systemService.getSystemControl();
      setCurrentDate(serverDate.currentDate);
      const deptList = await systemService.getDepartmentList();
      setDepartments(deptList);
    } catch (err) {
      setError('Failed to load system data');
    }
  };

  const handleCloseDay = async () => {
    if (verifyDate !== currentDate) {
      setError('Verify date must match current date');
      return;
    }

    if (!selectedDept) {
      setError('Please select a department');
      return;
    }

    try {
      await systemService.closeDay(selectedDept, verifyDate);
      setSuccess('Day closed successfully');
      setVerifyDate('');
      loadSystemData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to close day');
    }
  };

  const handleToggleDepartment = async (deptCode: string, isClosed: boolean) => {
    try {
      if (isClosed) {
        await systemService.reopenDay(deptCode);
      } else {
        await systemService.closeDay(deptCode, currentDate);
      }
      loadSystemData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to toggle department status');
    }
  };

  return (
    <Box>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          System Control
        </Typography>

        {error && (
          <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {success && (
          <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>
            {success}
          </Alert>
        )}

        <Box sx={{ mb: 3 }}>
          <Typography variant="body1" gutterBottom>
            <strong>Current Date:</strong> {currentDate ? format(new Date(currentDate), DISPLAY_DATE_FORMAT) : 'Loading...'}
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', gap: 2, mb: 3, alignItems: 'flex-end' }}>
          <TextField
            label="Verify Date"
            value={verifyDate}
            onChange={(e) => setVerifyDate(e.target.value)}
            helperText="Enter current date to confirm"
            sx={{ flexGrow: 1 }}
          />
          <TextField
            select
            label="Department"
            value={selectedDept}
            onChange={(e) => setSelectedDept(e.target.value)}
            SelectProps={{ native: true }}
            sx={{ minWidth: 200 }}
          >
            <option value="">Select Department</option>
            {departments.map((dept) => (
              <option key={dept.code} value={dept.code}>
                {dept.code} - {dept.description}
              </option>
            ))}
          </TextField>
          <Button variant="contained" color="error" onClick={handleCloseDay}>
            Complete All Entries (CLOSE SYSTEM)
          </Button>
        </Box>
      </Paper>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Department Status
        </Typography>
        <Typography variant="caption" color="text.secondary" gutterBottom display="block">
          Double click on cell to OPEN/CLOSE
        </Typography>

        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Department Code</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Closed Date</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {departments.map((dept) => (
                <TableRow key={dept.code}>
                  <TableCell>{dept.code}</TableCell>
                  <TableCell>{dept.description}</TableCell>
                  <TableCell>
                    <Chip
                      label={dept.isClosed ? 'CLOSED' : 'OPEN'}
                      color={dept.isClosed ? 'error' : 'success'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    {dept.closedDate ? format(new Date(dept.closedDate), DISPLAY_DATE_FORMAT) : '-'}
                  </TableCell>
                  <TableCell>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => handleToggleDepartment(dept.code, dept.isClosed)}
                    >
                      {dept.isClosed ? 'Re-open' : 'Close'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
    </Box>
  );
};