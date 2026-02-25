import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { PositionEntry, EntryStatus } from 'common/types';
import { positionService, CreatePositionRequest } from 'service/position.service';

interface PositionState {
  entries: PositionEntry[];
  pendingApprovals: PositionEntry[];
  incompleteEntries: PositionEntry[];
  correctionEntries: PositionEntry[];
  currentEntry: PositionEntry | null;
  loading: boolean;
  error: string | null;
}

const initialState: PositionState = {
  entries: [],
  pendingApprovals: [],
  incompleteEntries: [],
  correctionEntries: [],
  currentEntry: null,
  loading: false,
  error: null,
};

export const createPosition = createAsyncThunk(
  'position/create',
  async (request: CreatePositionRequest, { rejectWithValue }) => {
    try {
      return await positionService.createPosition(request);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create entry');
    }
  }
);

export const fetchPendingApprovals = createAsyncThunk(
  'position/fetchPendingApprovals',
  async ({ department, date }: { department: string; date: string }, { rejectWithValue }) => {
    try {
      return await positionService.getPendingApprovals(department, date);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch approvals');
    }
  }
);

export const fetchIncompleteEntries = createAsyncThunk(
  'position/fetchIncompleteEntries',
  async (department: string, { rejectWithValue }) => {
    try {
      return await positionService.getIncompleteEntries(department);
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch incomplete entries');
    }
  }
);

export const approveEntry = createAsyncThunk(
  'position/approve',
  async (uid: string, { rejectWithValue }) => {
    try {
      await positionService.approveEntry(uid);
      return uid;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to approve entry');
    }
  }
);

export const rejectEntry = createAsyncThunk(
  'position/reject',
  async ({ uid, reason }: { uid: string; reason: string }, { rejectWithValue }) => {
    try {
      await positionService.rejectEntry(uid, reason);
      return uid;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to reject entry');
    }
  }
);

const positionSlice = createSlice({
  name: 'position',
  initialState,
  reducers: {
    clearCurrentEntry: (state) => {
      state.currentEntry = null;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(createPosition.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(createPosition.fulfilled, (state, action) => {
        state.loading = false;
        state.entries.push(action.payload);
        state.currentEntry = action.payload;
      })
      .addCase(createPosition.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(fetchPendingApprovals.fulfilled, (state, action) => {
        state.pendingApprovals = action.payload;
      })
      .addCase(fetchIncompleteEntries.fulfilled, (state, action) => {
        state.incompleteEntries = action.payload;
      })
      .addCase(approveEntry.fulfilled, (state, action) => {
        state.pendingApprovals = state.pendingApprovals.filter(
          (entry) => entry.uid !== action.payload
        );
      })
      .addCase(rejectEntry.fulfilled, (state, action) => {
        state.pendingApprovals = state.pendingApprovals.filter(
          (entry) => entry.uid !== action.payload
        );
      });
  },
});

export const { clearCurrentEntry, clearError } = positionSlice.actions;
export default positionSlice.reducer;