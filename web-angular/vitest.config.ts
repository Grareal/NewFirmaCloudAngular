import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Un solo worker evita bloqueos de procesos en Windows/OneDrive y mantiene
    // suficiente paralelismo para esta suite pequeña.
    pool: 'threads',
    minWorkers: 1,
    maxWorkers: 1
  }
});
