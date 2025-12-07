<script setup lang="ts">
interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

const { data: weatherForecasts, error, status } = await useFetch<WeatherForecast[]>('/api/weatherforecast');

console.log('Status:', status.value);
console.log('Data:', weatherForecasts.value);
console.log('Error:', error.value);
</script>

<template>
  <div>
    <p>Status: {{ status }}</p>
    <p v-if="error" style="color: red;">Error: {{ error.message }}</p>
    <div v-if="weatherForecasts">
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Summary</th>
            <th>T (°C)</th>
            <th>T (°F)</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in weatherForecasts" :key="item.date">
            <td>{{ item.date }}</td>
            <td>{{ item.summary }}</td>
            <td>{{ item.temperatureC }}</td>
            <td>{{ item.temperatureF }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <p v-else>No data loaded</p>
  </div>
</template>
