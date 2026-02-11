
import React, { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Grid,
  TextField,
  Button,
  Paper,
  Typography,
  MenuItem,
  Alert,
  CircularProgress,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { createPosition } from '../../features/position/positionSlice';
import { AppDispatch, RootState } from '../../store';
import { CreatePositionRequest } from '../../common/types'; // Use the one from types.ts, NOT service
import { lookupService } from '../../service/lookup.service';
import { format } from 'date-fns';
import { DATE_FORMAT, TRANSACTION_TYPES } from '../../constants/app.constants';

export const PositionEntryForm: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const user = useSelector((state: RootState) => state.auth.user);
  const { loading, error } = useSelector((state: RootState) => state.position);

  const [formData, setFormData] = useState<CreatePositionRequest>({
    entryDate: format(new Date(), DATE_FORMAT),
    valueDate: format(new Date(), DATE_FORMAT),
    department: user?.department || '',
    transactionType: '',
    reference: '',
    theirReference: '',
    inwardCurrency: '',
    inwardAmount: 0,
    outwardCurrency: '',
    outwardAmount: 0,
    exchangeRate: 1,
    calcOperator: '+',
    inwardAccount: '',
    outwardAccount: '',
    isFeExchange: false,
  });

  const [inwardAccountName, setInwardAccountName] = useState('');
  const [outwardAccountName, setOutwardAccountName] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const [accountLoadingInward, setAccountLoadingInward] = useState(false);
  const [accountLoadingOutward, setAccountLoadingOutward] = useState(false);

  // Load inward account name when inwardAccount changes
  useEffect(() => {
    if (formData.inwardAccount && formData.inwardAccount.trim().length > 0) {
      loadAccountName(formData.inwardAccount, setInwardAccountName, setAccountLoadingInward);
    } else {
      setInwardAccountName('');
    }
  }, [formData.inwardAccount]);

  // Load outward account name when outwardAccount changes
  useEffect(() => {
    if (formData.outwardAccount && formData.outwardAccount.trim().length > 0) {
      loadAccountName(formData.outwardAccount, setOutwardAccountName, setAccountLoadingOutward);
    } else {
      setOutwardAccountName('');
    }
  }, [formData.outwardAccount]);

  // Calculate outward amount when inward amount, exchange rate, or operator changes
  useEffect(() => {
    calculateAmount();
  }, [formData.inwardAmount, formData.exchangeRate, formData.calcOperator]);

  const loadAccountName = async (
    accountNo: string,
    setter: (name: string) => void,
    setLoading: (loading: boolean) => void
  ): Promise<void> => {
    try {
      setLoading(true);
      console.log(`Loading account details for: ${accountNo}`);

      const account = await lookupService.getAccountDetails(accountNo);
      console.log(`Account found: ${account.customerName}`);
      setter(account.customerName);

      setValidationError(null);
    } catch (error) {
      console.error(`Failed to load account ${accountNo}:`, error);
      setter('ACCOUNT NOT FOUND');
    } finally {
      setLoading(false);
    }
  };

  const calculateAmount = (): void => {
    if (formData.inwardAmount && formData.exchangeRate) {
      let result: number;
      if (formData.calcOperator === '+' || formData.calcOperator === '*') {
        result = Number(formData.inwardAmount) * Number(formData.exchangeRate);
      } else {
        result = Number(formData.inwardAmount) / Number(formData.exchangeRate);
      }

      setFormData((prev) => ({
        ...prev,
        outwardAmount: Math.round(result * 100) / 100,
      }));
    }
  };

  const validateForm = (): boolean => {
    if (!formData.transactionType) {
      setValidationError('Transaction type is required');
      return false;
    }

    if (!formData.inwardCurrency || formData.inwardCurrency.length !== 3) {
      setValidationError('Valid 3-letter inward currency code required');
      return false;
    }

    if (!formData.outwardCurrency || formData.outwardCurrency.length !== 3) {
      setValidationError('Valid 3-letter outward currency code required');
      return false;
    }

    if (!formData.inwardAmount || formData.inwardAmount <= 0) {
      setValidationError('Inward amount must be greater than zero');
      return false;
    }

    if (!formData.exchangeRate || formData.exchangeRate <= 0) {
      setValidationError('Exchange rate must be greater than zero');
      return false;
    }

    if (!formData.inwardAccount || formData.inwardAccount.trim().length === 0) {
      setValidationError('Inward account is required');
      return false;
    }

    if (!formData.outwardAccount || formData.outwardAccount.trim().length === 0) {
      setValidationError('Outward account is required');
      return false;
    }

    if (!inwardAccountName || inwardAccountName === 'ACCOUNT NOT FOUND') {
      setValidationError('Inward account not found in system');
      return false;
    }

    if (!outwardAccountName || outwardAccountName === 'ACCOUNT NOT FOUND') {
      setValidationError('Outward account not found in system');
      return false;
    }

    setValidationError(null);
    return true;
  };

  const handleSubmit = async (clearForm: boolean): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    const result = await dispatch(createPosition(formData));

    if (createPosition.fulfilled.match(result)) {
      if (clearForm) {
        handleClear();
      }
    }
  };

  const handleClear = (): void => {
    setFormData({
      entryDate: format(new Date(), DATE_FORMAT),
      valueDate: format(new Date(), DATE_FORMAT),
      department: user?.department || '',
      transactionType: '',
      reference: '',
      theirReference: '',
      inwardCurrency: '',
      inwardAmount: 0,
      outwardCurrency: '',
      outwardAmount: 0,
      exchangeRate: 1,
      calcOperator: '+',
      inwardAccount: '',
      outwardAccount: '',
      isFeExchange: false,
    });
    setInwardAccountName('');
    setOutwardAccountName('');
    setValidationError(null);
  };

  const handleInputChange = (
    field: keyof CreatePositionRequest,
    value: string | number | boolean
  ): void => {
    if (
      field === 'inwardCurrency' ||
      field === 'outwardCurrency' ||
      field === 'inwardAccount' ||
      field === 'outwardAccount' ||
      field === 'reference' ||
      field === 'theirReference'
    ) {
      setFormData({
        ...formData,
        [field]: String(value).toUpperCase(),
      });
    } else {
      setFormData({
        ...formData,
        [field]: value,
      });
    }
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Position Entry
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {validationError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {validationError}
          </Alert>
        )}

        <Grid container spacing={2}>
          {/* ===== HEADER SECTION ===== */}
          <Grid item xs={12} sm={4}>
            <TextField fullWidth label="Department" value={formData.department} disabled />
          </Grid>

          <Grid item xs={12} sm={4}>
            <DatePicker
              label="Entry Date"
              value={new Date(formData.entryDate)}
              onChange={(date) =>
                handleInputChange('entryDate', format(date || new Date(), DATE_FORMAT))
              }
              slotProps={{ textField: { fullWidth: true } }}
            />
          </Grid>

          <Grid item xs={12} sm={4}>
            <DatePicker
              label="Value Date"
              value={new Date(formData.valueDate)}
              onChange={(date) =>
                handleInputChange('valueDate', format(date || new Date(), DATE_FORMAT))
              }
              slotProps={{ textField: { fullWidth: true } }}
            />
          </Grid>

          {/* ===== REFERENCE SECTION ===== */}
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Reference"
              value={formData.reference}
              onChange={(e) => handleInputChange('reference', e.target.value)}
              inputProps={{ maxLength: 200 }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Their Reference / Remarks"
              value={formData.theirReference}
              onChange={(e) => handleInputChange('theirReference', e.target.value)}
              inputProps={{ maxLength: 200 }}
            />
          </Grid>

          {/* ===== TRANSACTION TYPE ===== */}
          <Grid item xs={12}>
            <TextField
              fullWidth
              select
              label="Transaction Type"
              value={formData.transactionType}
              onChange={(e) => handleInputChange('transactionType', e.target.value)}
              required
            >
              <MenuItem value="">Select Transaction Type</MenuItem>
              {TRANSACTION_TYPES.map((type: string) => (
                <MenuItem key={type} value={type}>
                  {type}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          {/* ===== INWARD SECTION ===== */}
          <Grid item xs={12}>
            <Typography variant="subtitle2" sx={{ color: 'error.main' }} gutterBottom>
              INWARD Currency & Amount
            </Typography>
          </Grid>

          <Grid item xs={12} sm={2}>
            <TextField
              fullWidth
              label="Currency"
              value={formData.inwardCurrency}
              onChange={(e) => handleInputChange('inwardCurrency', e.target.value)}
              inputProps={{ maxLength: 3 }}
              placeholder="USD"
            />
          </Grid>

          <Grid item xs={12} sm={3}>
            <TextField
              fullWidth
              type="number"
              label="Amount"
              value={formData.inwardAmount}
              onChange={(e) =>
                handleInputChange('inwardAmount', parseFloat(e.target.value) || 0)
              }
              inputProps={{ step: 0.01 }}
            />
          </Grid>

          <Grid item xs={12} sm={2}>
            <TextField
              fullWidth
              select
              label="Operator"
              value={formData.calcOperator}
              onChange={(e) => handleInputChange('calcOperator', e.target.value)}
            >
              <MenuItem value="+">× Multiply</MenuItem>
              <MenuItem value="-">÷ Divide</MenuItem>
            </TextField>
          </Grid>

          <Grid item xs={12} sm={3}>
            <TextField
              fullWidth
              type="number"
              label="Exchange Rate"
              value={formData.exchangeRate}
              onChange={(e) =>
                handleInputChange('exchangeRate', parseFloat(e.target.value) || 0)
              }
              inputProps={{ step: 0.000001 }}
              placeholder="83.750000"
            />
          </Grid>

          {/* ===== OUTWARD SECTION ===== */}
          <Grid item xs={12}>
            <Typography variant="subtitle2" sx={{ color: 'success.main' }} gutterBottom>
              OUTWARD Currency & Amount (Auto-calculated)
            </Typography>
          </Grid>

          <Grid item xs={12} sm={3}>
            <TextField
              fullWidth
              label="Currency"
              value={formData.outwardCurrency}
              onChange={(e) => handleInputChange('outwardCurrency', e.target.value)}
              inputProps={{ maxLength: 3 }}
              placeholder="INR"
            />
          </Grid>

          <Grid item xs={12} sm={4}>
            <TextField
              fullWidth
              type="number"
              label="Amount"
              value={formData.outwardAmount}
              disabled
              inputProps={{ step: 0.01 }}
              sx={{ backgroundColor: '#f5f5f5' }}
            />
          </Grid>

          {/* ===== INWARD ACCOUNT SECTION ===== */}
          <Grid item xs={12}>
            <Typography variant="subtitle2" gutterBottom>
              INWARD ACCOUNT
            </Typography>
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Account Code"
              value={formData.inwardAccount}
              onChange={(e) => handleInputChange('inwardAccount', e.target.value)}
              placeholder="NOSTRO-USD"
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <TextField
                fullWidth
                label="Account Name"
                value={inwardAccountName}
                disabled
                sx={{ backgroundColor: '#f5f5f5' }}
              />
              {accountLoadingInward && <CircularProgress size={24} />}
            </Box>
          </Grid>

          {/* ===== OUTWARD ACCOUNT SECTION ===== */}
          <Grid item xs={12}>
            <Typography variant="subtitle2" gutterBottom>
              OUTWARD ACCOUNT
            </Typography>
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Account Code"
              value={formData.outwardAccount}
              onChange={(e) => handleInputChange('outwardAccount', e.target.value)}
              placeholder="ACC001"
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <TextField
                fullWidth
                label="Account Name"
                value={outwardAccountName}
                disabled
                sx={{ backgroundColor: '#f5f5f5' }}
              />
              {accountLoadingOutward && <CircularProgress size={24} />}
            </Box>
          </Grid>

          {/* ===== ACTION BUTTONS ===== */}
          <Grid item xs={12}>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-start' }}>
              <Button
                variant="contained"
                onClick={() => handleSubmit(true)}
                disabled={loading}
              >
                {loading ? <CircularProgress size={20} /> : 'Post Clear (F10)'}
              </Button>

              <Button
                variant="contained"
                color="secondary"
                onClick={() => handleSubmit(false)}
                disabled={loading}
              >
                {loading ? <CircularProgress size={20} /> : 'Post Same (F8)'}
              </Button>

              <Button variant="outlined" onClick={handleClear}>
                Clear
              </Button>
            </Box>
          </Grid>
        </Grid>
      </Paper>
    </LocalizationProvider>
  );
};
