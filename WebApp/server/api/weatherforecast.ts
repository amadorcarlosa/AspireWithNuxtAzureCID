
// ...existing code...
export default defineEventHandler(async (): Promise<any> => {
  const apiBase: string = process.env?.API_BASE_URL ?? 'http://localhost:5230';
  return await $fetch(`${apiBase}/api/weatherforecast`);
});
// ...existing code...