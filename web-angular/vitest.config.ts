import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Evita procesos hijos adicionales en Windows/OneDrive, donde pueden expirar
    // antes de inicializarse. La suite actual es pequeña y no requiere paralelismo.
    pool: 'threads',
    minWorkers: 1,
    maxWorkers: 1,
    poolOptions: {
      threads: {
        singleThread: true,
      },
    },
  },
});
