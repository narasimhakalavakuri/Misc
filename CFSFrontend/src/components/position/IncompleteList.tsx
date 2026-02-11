import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Paper,
  Typography,
  Button
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { format } from 'date-fns';
import { fetchIncompleteEntries } from '../../features/position/positionSlice';
import { AppDispatch, RootState } from '../../store';
import { DISPLAY_DATE_FORMAT } from '../../constants/app.constants';
import { PositionEntry } from '../../common/types';

export const IncompleteList: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useSelector((state: RootState) => state.auth);
  const { incompleteEntries, loading } = useSelector((state: RootState) => state.position);

  useEffect(() => {
    if (user?.department) {
      loadIncomplete();
    }
  }, [user]);

  const loadIncomplete = () => {
    dispatch(fetchIncompleteEntries(user!.department));
  };

  const columns: GridColDef<PositionEntry>[] = [
    {
      field: 'entryDate',
      headerName: 'Entry Date',
      width: 120,
      valueFormatter: (value: string) => {
        return format(new Date(value), DISPLAY_DATE_FORMAT);
      }
    },
    {
      field: 'valueDate',
      headerName: 'Value Date',
      width: 120,
      valueFormatter: (value: string) => {
        return format(new Date(value), DISPLAY_DATE_FORMAT);
      }
    },
    { field: 'reference', headerName: 'Reference', width: 200 },
    { field: 'inwardCurrency', headerName: 'In Curr', width: 80 },
    {
      field: 'inwardAmount',
      headerName: 'In Amount',
      width: 130,
      type: 'number',
      valueFormatter: (value: number) => {
        return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
      }
    },
    { field: 'outwardCurrency', headerName: 'Out Curr', width: 80 },
    {
      field: 'outwardAmount',
      headerName: 'Out Amount',
      width: 130,
      type: 'number',
      valueFormatter: (value: number) => {
        return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
      }
    },
    { field: 'createdBy', headerName: 'Created By', width: 150 }
  ];

  return (
    <Paper sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Incomplete Entries</Typography>
        <Button variant="outlined" onClick={loadIncomplete}>
          Refresh
        </Button>
      </Box>

      <DataGrid
        rows={incompleteEntries}
        columns={columns}
        loading={loading}
        autoHeight
        pageSizeOptions={[10, 20, 50]}
        initialState={{
          pagination: { paginationModel: { pageSize: 20 } }
        }}
      />
    </Paper>
  );
};
