// import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
// import { User } from '../../common/types';
// import { userService, LoginRequest, ChangePasswordRequest } from '../../service/user.service';

// interface AuthState {
//   user: User | null;
//   token: string | null;
//   isAuthenticated: boolean;
//   loading: boolean;
//   error: string | null;
// }

// const initialState: AuthState = {
//   user: null,
//   token: localStorage.getItem('authToken'),
//   isAuthenticated: false,
//   loading: false, // CHANGED: Was blocking rendering
//   error: null,
// };

// export const login = createAsyncThunk(
//   'auth/login',
//   async (credentials: LoginRequest, { rejectWithValue }) => {
//     try {
//       const response = await userService.login(credentials);
//       localStorage.setItem('authToken', response.token);
//       return response;
//     } catch (error: any) {
//       return rejectWithValue(error.response?.data?.message || 'Login failed');
//     }
//   }
// );

// // export const login = createAsyncThunk(
// //   'auth/login',
// //   async (credentials: LoginRequest, { rejectWithValue }) => {
// //     try {
// //       // TEMPORARY: Mock login for frontend testing
// //       // Remove this once backend is ready
// //       if (credentials.username === 'admin' && credentials.password === 'admin') {
// //         const mockResponse = {
// //           token: 'mock-jwt-token-12345',
// //           user: {
// //             id: 1,
// //             userid: 'admin',
// //             department: 'IT',
// //             accessMask: '7', // Full admin access
// //             fullName: 'System Administrator',
// //             email: 'admin@example.com'
// //           }
// //         };
// //         localStorage.setItem('authToken', mockResponse.token);
// //         return mockResponse;
// //       } else {
// //         return rejectWithValue('Invalid credentials. Use admin/admin for testing.');
// //       }
      
// //       // PRODUCTION CODE (uncomment when backend is ready):
// //       // const response = await userService.login(credentials);
// //       // localStorage.setItem('authToken', response.token);
// //       // return response;
// //     } catch (error: any) {
// //       return rejectWithValue(error.response?.data?.message || 'Login failed');
// //     }
// //   }
// // );


// export const getCurrentUser = createAsyncThunk(
//   'auth/getCurrentUser',
//   async (_, { rejectWithValue }) => {
//     try {
//       return await userService.getCurrentUser();
//     } catch (error: any) {
//       return rejectWithValue(error.response?.data?.message || 'Failed to fetch user');
//     }
//   }
// );

// export const changePassword = createAsyncThunk(
//   'auth/changePassword',
//   async (request: ChangePasswordRequest, { rejectWithValue }) => {
//     try {
//       return await userService.changePassword(request);
//     } catch (error: any) {
//       return rejectWithValue(error.response?.data?.message || 'Password change failed');
//     }
//   }
// );

// const authSlice = createSlice({
//   name: 'auth',
//   initialState,
//   reducers: {
//     logout: (state) => {
//       state.user = null;
//       state.token = null;
//       state.isAuthenticated = false;
//       localStorage.removeItem('authToken');
//     },
//     clearError: (state) => {
//       state.error = null;
//     },
//   },
//   extraReducers: (builder) => {
//     builder
//       .addCase(login.pending, (state) => {
//         state.loading = true;
//         state.error = null;
//       })
//       .addCase(login.fulfilled, (state, action) => {
//         state.loading = false;
//         state.user = action.payload.user;
//         state.token = action.payload.token;
//         state.isAuthenticated = true;
//       })
//       .addCase(login.rejected, (state, action) => {
//         state.loading = false;
//         state.error = action.payload as string;
//       })
//       .addCase(getCurrentUser.fulfilled, (state, action) => {
//         state.user = action.payload;
//         state.isAuthenticated = true;
//       })
//       .addCase(changePassword.fulfilled, (state) => {
//         state.error = null;
//       })
//       .addCase(changePassword.rejected, (state, action) => {
//         state.error = action.payload as string;
//       });
//   },
// });

// export const { logout, clearError } = authSlice.actions;
// export default authSlice.reducer;

import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { userService } from '../../service/user.service';
import { User } from '../../common/types';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  token: localStorage.getItem('authToken'),
  isAuthenticated: !!localStorage.getItem('authToken'),
  loading: false,
  error: null,
};

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: { username: string; password: string }, { rejectWithValue }) => {
    try {
      const response = await userService.login(credentials);
      localStorage.setItem('authToken', response.token);
      return response;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || 'Login failed');
    }
  }
);

export const getCurrentUser = createAsyncThunk(
  'auth/getCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      return await userService.getCurrentUser();
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.error || 'Failed to get user');
    }
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem('authToken');
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      // Login
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.loading = false;
        state.isAuthenticated = true;
        state.token = action.payload.token;
        state.user = action.payload.user;
        state.error = null;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
        state.isAuthenticated = false;
      })
      // Get current user
      .addCase(getCurrentUser.pending, (state) => {
        state.loading = true;
      })
      .addCase(getCurrentUser.fulfilled, (state, action) => {
        state.loading = false;
        state.user = action.payload;
        state.isAuthenticated = true;
      })
      .addCase(getCurrentUser.rejected, (state) => {
        state.loading = false;
        state.isAuthenticated = false;
        state.token = null;
        localStorage.removeItem('authToken');
      });
  },
});

export const { logout, setError } = authSlice.actions;
export default authSlice.reducer;
