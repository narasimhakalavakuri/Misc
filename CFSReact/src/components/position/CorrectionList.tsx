import React, { useEffect } from 'react';
import { useSelector } from 'react-redux';
import {
  Box,
  Paper,
  Typography,
  Button
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { format } from 'date-fns';
import { RootState } from '../../store';
import { positionService } from '../../service/position.service';
import { DISPLAY_DATE_FORMAT } from '../../constants/app.constants';
import { PositionEntry } from '../../common/types';

export const CorrectionList: React.FC = () => {
  const { user } = useSelector((state: RootState) => state.auth);
  const [corrections, setCorrections] = React.useState<PositionEntry[]>([]);
  const [loading, setLoading] = React.useState(false);

  useEffect(() => {
    if (user?.department) {
      loadCorrections();
    }
  }, [user]);

  const loadCorrections = async () => {
    if (!user?.department) return;
    
    try {
      setLoading(true);
      const data = await positionService.getCorrectionEntries(user.department);
      setCorrections(data);
    } catch (error) {
      console.error('Failed to load corrections', error);
    } finally {
      setLoading(false);
    }
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
    { field: 'modifiedBy', headerName: 'Modified By', width: 150 }
  ];

  return (
    <Paper sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Correction Entries</Typography>
        <Button variant="outlined" onClick={loadCorrections}>
          Refresh
        </Button>
      </Box>

      <DataGrid
        rows={corrections}
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
