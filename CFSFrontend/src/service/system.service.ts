import { apiClient } from './api.client';
import { SystemControl, ApiResponse, Department } from 'common/types';

class SystemService {
  async getSystemControl(): Promise<SystemControl> {
    return apiClient.get<SystemControl>(`/system/control`);
  }

  async closeDay(department: string, verifyDate: string): Promise<ApiResponse<void>> {
    return apiClient.post<ApiResponse<void>>(`/system/close-day`, {
      department,
      verifyDate
    });
  }

  async reopenDay(department: string): Promise<ApiResponse<void>> {
    return apiClient.post<ApiResponse<void>>(`/system/reopen-day/${department}`);
  }

  async isDepartmentClosed(department: string): Promise<boolean> {
    const response = await apiClient.get<{ isClosed: boolean }>(
      `/system/department-status/${department}`
    );
    return response.isClosed;
  }

  async getDepartmentList(): Promise<Department[]> {
    return apiClient.get<Department[]>(`/system/departments`);
  }
}

export const systemService = new SystemService();
