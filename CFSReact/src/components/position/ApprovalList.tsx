
import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Paper,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import { DataGrid, GridColDef, GridRowSelectionModel } from '@mui/x-data-grid';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { format } from 'date-fns';
import {
  fetchPendingApprovals,
  approveEntry,
  rejectEntry,
} from '../../features/position/positionSlice';
import { AppDispatch, RootState } from '../../store';
import {
  DATE_FORMAT,
  DISPLAY_DATE_FORMAT,
  STATUS_COLORS,
} from '../../constants/app.constants';
import { PositionEntry } from '../../common/types';

export const ApprovalList: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const user = useSelector((state: RootState) => state.auth.user);
  const pendingApprovals = useSelector(
    (state: RootState) => state.position.pendingApprovals
  );
  const loading = useSelector((state: RootState) => state.position.loading);

  const [transactionDate, setTransactionDate] = useState<Date>(new Date());
  const [selectedRowIds, setSelectedRowIds] = useState<(string | number)[]>([]);
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

  useEffect(() => {
    if (user?.department) {
      loadApprovals();
    }
  }, [transactionDate, user]);

  const loadApprovals = (): void => {
    dispatch(
      fetchPendingApprovals({
        department: user!.department,
        date: format(transactionDate, DATE_FORMAT),
      })
    );
  };

  const handleApprove = async (): Promise<void> => {
    try {
      for (const uid of selectedRowIds) {
        await dispatch(approveEntry(uid as string));
      }
      setSelectedRowIds([]);
      loadApprovals();
    } catch (error) {
      console.error('Error approving entries:', error);
    }
  };

  const handleRejectClick = (): void => {
    if (selectedRowIds.length === 0) return;
    setRejectDialogOpen(true);
  };

  const handleRejectConfirm = async (): Promise<void> => {
    try {
      for (const uid of selectedRowIds) {
        await dispatch(
          rejectEntry({
            uid: uid as string,
            reason: rejectReason,
          })
        );
      }
      setSelectedRowIds([]);
      setRejectDialogOpen(false);
      setRejectReason('');
      loadApprovals();
    } catch (error) {
      console.error('Error rejecting entries:', error);
    }
  };

  const columns: GridColDef<PositionEntry>[] = [
    {
      field: 'entryDate',
      headerName: 'Entry Date',
      width: 120,
      valueFormatter: (value: string) =>
        value ? format(new Date(value), DISPLAY_DATE_FORMAT) : '',
    },
    {
      field: 'valueDate',
      headerName: 'Value Date',
      width: 120,
      valueFormatter: (value: string) =>
        value ? format(new Date(value), DISPLAY_DATE_FORMAT) : '',
    },
    {
      field: 'reference',
      headerName: 'Reference',
      width: 200,
    },
    {
      field: 'inwardCurrency',
      headerName: 'In Curr',
      width: 80,
    },
    {
      field: 'inwardAmount',
      headerName: 'In Amount',
      width: 130,
      type: 'number',
      valueFormatter: (value: number | null) =>
        value
          ? value.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })
          : '0.00',
    },
    {
      field: 'outwardCurrency',
      headerName: 'Out Curr',
      width: 80,
    },
    {
      field: 'outwardAmount',
      headerName: 'Out Amount',
      width: 130,
      type: 'number',
      valueFormatter: (value: number | null) =>
        value
          ? value.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })
          : '0.00',
    },
    {
      field: 'inwardAccount',
      headerName: 'In Account',
      width: 140,
    },
    {
      field: 'outwardAccount',
      headerName: 'Out Account',
      width: 140,
    },
    {
      field: 'createdBy',
      headerName: 'Created By',
      width: 150,
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 120,
      renderCell: (params) => {
        const statusValue = params.value as string;
        const bgColor = STATUS_COLORS[statusValue] || '#757575';
        return (
          <Box
            sx={{
              bgcolor: bgColor,
              px: 1,
              py: 0.5,
              borderRadius: 1,
              color: 'white',
              fontWeight: 'bold',
              textAlign: 'center',
            }}
          >
            {statusValue}
          </Box>
        );
      },
    },
  ];

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Paper sx={{ p: 2 }}>
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            mb: 2,
          }}
        >
          <Typography variant="h6">Pending Approvals</Typography>
          <DatePicker
            label="Transaction Date"
            value={transactionDate}
            onChange={(date) => setTransactionDate(date || new Date())}
            slotProps={{ textField: { size: 'small' } }}
          />
        </Box>

        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            color="success"
            onClick={handleApprove}
            disabled={selectedRowIds.length === 0 || loading}
          >
            Approve Selected ({selectedRowIds.length})
          </Button>

          <Button
            variant="contained"
            color="error"
            onClick={handleRejectClick}
            disabled={selectedRowIds.length === 0 || loading}
          >
            Reject Selected ({selectedRowIds.length})
          </Button>

          <Button variant="outlined" onClick={loadApprovals} disabled={loading}>
            Refresh
          </Button>
        </Box>

        <DataGrid
          rows={pendingApprovals || []}
          columns={columns}
          checkboxSelection
          disableRowSelectionOnClick
         
          onRowSelectionModelChange={(newSelection: GridRowSelectionModel) => {
            // GridRowSelectionModel can be an array or Set of row IDs
            if (Array.isArray(newSelection)) {
              setSelectedRowIds(newSelection);
            } else if (newSelection instanceof Set) {
              setSelectedRowIds(Array.from(newSelection));
            } else {
              setSelectedRowIds([]);
            }
          }}
          loading={loading}
          autoHeight
          pageSizeOptions={[10, 20, 50]}
          initialState={{
            pagination: {
              paginationModel: { pageSize: 20 },
            },
          }}
          sx={{
            '& .MuiDataGrid-row': {
              '&:hover': {
                backgroundColor: '#f5f5f5',
              },
            },
          }}
        />

        <Dialog
          open={rejectDialogOpen}
          onClose={() => setRejectDialogOpen(false)}
          maxWidth="sm"
          fullWidth
        >
          <DialogTitle>Reject Entry</DialogTitle>
          <DialogContent sx={{ pt: 2 }}>
            <TextField
              fullWidth
              multiline
              rows={4}
              label="Rejection Reason"
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="Enter reason for rejection..."
              required
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRejectDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={handleRejectConfirm}
              variant="contained"
              color="error"
              disabled={!rejectReason.trim()}
            >
              Confirm Rejection
            </Button>
          </DialogActions>
        </Dialog>
      </Paper>
    </LocalizationProvider>
  );
};
