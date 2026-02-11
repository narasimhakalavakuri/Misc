
// ===== USER TYPES =====
export interface User {
  id: number;
  userid: string;
  department: string;
  accessMask: string;
  fullName?: string;
  email?: string;
}

// ===== ACCESS LEVEL ENUM =====
export enum AccessLevel {
  QUERY = 0,
  INPUT = 1,
  CHECK = 2,
  ADMIN = 3,
  USER_ADMIN = 4,
  REPORT = 5,
  SYSTEM_CONTROL = 6,
}

// ===== POSITION ENTRY TYPES =====
export enum EntryStatus {
  PENDING = 'PENDING',
  APPROVED = 'APPROVED',
  REJECTED = 'REJECTED',
  INCOMPLETE = 'INCOMPLETE',
  CORRECTION = 'CORRECTION',
  POSTED = 'POSTED',
}

export interface CreatePositionRequest {
  entryDate: string;
  valueDate: string;
  department: string;
  transactionType: string;
  reference: string;
  theirReference: string;
  inwardCurrency: string;
  inwardAmount: number;
  outwardCurrency: string;
  outwardAmount: number;
  exchangeRate: number;
  calcOperator: string;
  inwardAccount: string;
  outwardAccount: string;
  isFeExchange?: boolean;
}

export interface PositionEntry extends CreatePositionRequest {
  id?: number;
  uid: string;
  status: EntryStatus;
  checkedOutBy?: string;
  approvedDate?: string;
  createdBy: string;
  createdDate: string;
  modifiedBy?: string;
  modifiedDate?: string;
}

// ===== LOOKUP TYPES =====
export interface Currency {
  code: string;
  longName: string;
  decimals: number;
}

export interface CustomerAccount {
  accountNo: string;
  customerName: string;
  abbreviatedName?: string;
}

export interface Department {
  code: string;
  description: string;
  isClosed: boolean;
  closedDate?: string;
}

// ===== SYSTEM CONTROL TYPES =====
export interface SystemControl {
  currentDate: string;
  isClosed: boolean;
  departments: Department[];
}

// ===== API RESPONSE TYPES =====
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string;
}

export interface PaginatedResponse<T> {
  content: T[];
  totalElements: number;
  totalPages: number;
  pageNumber: number;
  pageSize: number;
}
