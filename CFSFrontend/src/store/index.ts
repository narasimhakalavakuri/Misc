import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../features/auth/authSlice';
import positionReducer from '../features/position/positionSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    position: positionReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
