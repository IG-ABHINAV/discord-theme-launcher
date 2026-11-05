import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  workers: 2,
  reporter: 'list',
  use: { viewport: { width: 1280, height: 800 }, colorScheme: 'dark' }
});
