
// import { defineConfig } from 'vite';
// import react from '@vitejs/plugin-react';
// import path from 'path';

// export default defineConfig({
//   plugins: [react()],
//   server: { 
//     port: 8081,
//     proxy: {
//       '/api': {
//         target: 'http://localhost:8080',
//         changeOrigin: true
//       }
//     }
//   },
//   resolve: {
//     alias: {
//       src: path.resolve(__dirname, './src'),
//       components: path.resolve(__dirname, './src/components'),
//       constants: path.resolve(__dirname, './src/constants'),
//       features: path.resolve(__dirname, './src/features'),
//       service: path.resolve(__dirname, './src/service'),
//       translations: path.resolve(__dirname, './src/translations'),
//       utils: path.resolve(__dirname, './src/utils'),
//       logger: path.resolve(__dirname, './src/logger'),
//       common: path.resolve(__dirname, './src/common'),
//       assets: path.resolve(__dirname, './src/assets'),
//     },
//   },
// });



import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  server: { 
    host: '0.0.0.0',
    port: 8081,
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true
      }
    }
  },
  resolve: {
    alias: {
      src: path.resolve(__dirname, './src'),
      components: path.resolve(__dirname, './src/components'),
      constants: path.resolve(__dirname, './src/constants'),
      features: path.resolve(__dirname, './src/features'),
      service: path.resolve(__dirname, './src/service'),
      translations: path.resolve(__dirname, './src/translations'),
      utils: path.resolve(__dirname, './src/utils'),
      logger: path.resolve(__dirname, './src/logger'),
      common: path.resolve(__dirname, './src/common'),
      assets: path.resolve(__dirname, './src/assets'),
    },
  },
});