
// import { apiClient } from './api.client';
// import { User, ApiResponse } from 'common/types';

// export interface LoginRequest {
//   username: string;
//   password: string;
// }

// export interface ChangePasswordRequest {
//   oldPassword: string;
//   newPassword: string;
// }

// export interface CreateUserRequest {
//   userid: string;
//   department: string;
//   accessMask: string;
//   fullName?: string;
//   email?: string;
// }

// class UserService {
//   private readonly basePath = '/users';

//   async login(request: LoginRequest): Promise<{ token: string; user: User }> {
//     return apiClient.post<{ token: string; user: User }>(`${this.basePath}/login`, request);
//   }

//   async getCurrentUser(): Promise<User> {
//     return apiClient.get<User>(`${this.basePath}/me`);
//   }

//   async changePassword(request: ChangePasswordRequest): Promise<ApiResponse<void>> {
//     return apiClient.post<ApiResponse<void>>(`${this.basePath}/change-password`, request);
//   }

//   async getAllUsers(): Promise<User[]> {
//     return apiClient.get<User[]>(`${this.basePath}`);
//   }

//   async createUser(request: CreateUserRequest): Promise<User> {
//     return apiClient.post<User>(`${this.basePath}`, request);
//   }

//   async updateUser(id: number, request: Partial<CreateUserRequest>): Promise<User> {
//     return apiClient.put<User>(`${this.basePath}/${id}`, request);
//   }

//   async deleteUser(id: number): Promise<ApiResponse<void>> {
//     return apiClient.delete<ApiResponse<void>>(`${this.basePath}/${id}`);
//   }
// }

// export const userService = new UserService();


import { apiClient } from './api.client';
import { User, ApiResponse } from '../common/types';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
}

export interface CreateUserRequest {
  userid: string;
  department: string;
  accessMask: string;
  fullName?: string;
  email?: string;
}

class UserService {
  private readonly basePath = '/auth';

  async login(request: LoginRequest): Promise<{ token: string; user: User }> {
    return apiClient.post<{ token: string; user: User }>(`${this.basePath}/login`, request);
  }

  async getCurrentUser(): Promise<User> {
    return apiClient.get<User>(`${this.basePath}/me`);
  }

  async changePassword(request: ChangePasswordRequest): Promise<ApiResponse<void>> {
    return apiClient.post<ApiResponse<void>>('/users/change-password', request);
  }

  async getAllUsers(): Promise<User[]> {
    const response = await apiClient.get<any>('/users');
    return response.data || response;
  }

  async getUserByUserid(userid: string): Promise<User> {
    return apiClient.get<User>(`/users/${userid}`);
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    return apiClient.post<User>('/users', request);
  }

  async updateUser(id: number, request: Partial<CreateUserRequest>): Promise<User> {
    return apiClient.put<User>(`/users/${id}`, request);
  }

  async deleteUser(id: number): Promise<ApiResponse<void>> {
    return apiClient.delete<ApiResponse<void>>(`/users/${id}`);
  }
}

export const userService = new UserService();
