// import axios, { AxiosInstance, AxiosError, AxiosRequestConfig } from 'axios';
// import { API_BASE_URL } from 'constants/app.constants';

// class ApiClient {
//   private instance: AxiosInstance;

//   constructor() {
//     this.instance = axios.create({
//       baseURL: API_BASE_URL,
//       timeout: 30000,
//       headers: {
//         'Content-Type': 'application/json',
//       },
//     });

//     this.setupInterceptors();
//   }

//   private setupInterceptors(): void {
//     this.instance.interceptors.request.use(
//       (config) => {
//         const token = localStorage.getItem('authToken');
//         if (token) {
//           config.headers.Authorization = `Bearer ${token}`;
//         }
//         return config;
//       },
//       (error) => Promise.reject(error)
//     );

//     this.instance.interceptors.response.use(
//       (response) => response,
//       (error: AxiosError) => {
//         if (error.response?.status === 401) {
//           localStorage.removeItem('authToken');
//           window.location.href = '/login';
//         }
//         return Promise.reject(error);
//       }
//     );
//   }

//   async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
//     const response = await this.instance.get<T>(url, config);
//     return response.data;
//   }

//   async post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
//     const response = await this.instance.post<T>(url, data, config);
//     return response.data;
//   }

//   async put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
//     const response = await this.instance.put<T>(url, data, config);
//     return response.data;
//   }

//   async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
//     const response = await this.instance.delete<T>(url, config);
//     return response.data;
//   }
// }

// export const apiClient = new ApiClient();
import axios, { AxiosInstance, AxiosError, AxiosRequestConfig } from 'axios';

class ApiClient {
  private instance: AxiosInstance;

  constructor() {
    this.instance = axios.create({
      baseURL: 'http://localhost:8080/v1', // Backend URL
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    this.instance.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('authToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.instance.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.put<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.delete<T>(url, config);
    return response.data;
  }
}

export const apiClient = new ApiClient();