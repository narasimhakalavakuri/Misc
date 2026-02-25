
// Application info
export const APP_VERSION = '1.4.0';
export const APP_NAME = 'Position Reporting Entry System';

// Date formats
export const DATE_FORMAT = 'yyyy-MM-dd';
export const DISPLAY_DATE_FORMAT = 'dd MMM yyyy';  // ✅ Changed from DISPLAYDATEFORMAT
export const DATETIME_FORMAT = 'yyyy-MM-dd HH:mm:ss';

// Pagination
export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;

// API Configuration
export const API_BASE_URL = 'http://localhost:8080';
export const API_VERSION = '/v1';

// Permission/Access Levels
export const PERMISSION_LABELS: Record<number, string> = {
  0: 'Query',
  1: 'Input',
  2: 'Check',
  3: 'Admin',
  4: 'User Admin',
  5: 'Report',
  6: 'System Control',
};

// Entry Status Colors
export const STATUS_COLORS: Record<string, string> = {
  PENDING: '#FFA726',
  APPROVED: '#66BB6A',
  REJECTED: '#EF5350',
  INCOMPLETE: '#FFEE58',
  CORRECTION: '#42A5F5',
  POSTED: '#26C6DA',
};

// Transaction Types
export const TRANSACTION_TYPES = [
  'FXSPOT',
  'FXFORWARD',
  'FXSWAP',
  'NOSTROTRANSFER',
  'INTERBANK',
];

// Entry Status Values
export const ENTRY_STATUS = {
  PENDING: 'PENDING',
  APPROVED: 'APPROVED',
  REJECTED: 'REJECTED',
  INCOMPLETE: 'INCOMPLETE',
  CORRECTION: 'CORRECTION',
  POSTED: 'POSTED',
};
