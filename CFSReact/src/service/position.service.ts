
import { apiClient } from './api.client';
import {
  PositionEntry,
  CreatePositionRequest,
  ApiResponse,
  PaginatedResponse,
} from '../common/types';

// ❌ DELETE the CreatePositionRequest interface from here - use the one from common/types!
// ❌ DELETE: export interface CreatePositionRequest { ... }

class PositionService {
  private readonly basePath = '/positions';

  async createPosition(request: CreatePositionRequest): Promise<PositionEntry> {
    return apiClient.post<PositionEntry>(this.basePath, request);
  }

  async getPendingApprovals(
    department: string,
    date: string
  ): Promise<PositionEntry[]> {
    const response = await apiClient.get<any>(
      `${this.basePath}/pending-approvals?department=${department}&date=${date}`
    );
    return response.items || response.data || [];
  }

  async getIncompleteEntries(department: string): Promise<PositionEntry[]> {
    const response = await apiClient.get<any>(
      `${this.basePath}/incomplete?department=${department}`
    );
    return response.items || response.data || [];
  }

  async getCorrectionEntries(department: string): Promise<PositionEntry[]> {
    const response = await apiClient.get<any>(
      `${this.basePath}/corrections?department=${department}`
    );
    return response.items || response.data || [];
  }

  async getStatistics(department: string): Promise<any> {
    return apiClient.get<any>(`${this.basePath}/statistics?department=${department}`);
  }

  async checkoutEntry(uid: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${uid}/checkout`);
  }

  async checkinEntry(uid: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${uid}/checkin`);
  }

  async approveEntry(uid: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${uid}/approve`);
  }

  async rejectEntry(uid: string, reason: string): Promise<void> {
    await apiClient.post<void>(`${this.basePath}/${uid}/reject`, { reason });
  }

  async deleteEntry(uid: string): Promise<void> {
    await apiClient.delete<void>(`${this.basePath}/${uid}`);
  }

  async getEntryByUid(uid: string): Promise<PositionEntry> {
    const response = await apiClient.get<any>(`${this.basePath}/${uid}`);
    return response.data || response;
  }

  async getAllEntries(
    department: string,
    page: number = 0,
    size: number = 20
  ): Promise<PaginatedResponse<PositionEntry>> {
    return apiClient.get<PaginatedResponse<PositionEntry>>(
      `${this.basePath}?department=${department}&page=${page}&size=${size}`
    );
  }
}

export const positionService = new PositionService();
