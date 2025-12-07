# Aspire 13 + Nuxt 4 + .NET 10: The Complete Setup Guide

_A practical guide to orchestrating a Nuxt frontend with a .NET backend using Aspire's polyglot capabilities_

---

Aspire 13 dropped with first-class JavaScript support, but most examples show React or basic Node apps. If you're a .NET developer who prefers Vue/Nuxt, the documentation is sparse. This guide fills that gap.

We'll build a working setup with:

- **Aspire 13** for orchestration
- **Nuxt 4** frontend with server routes
- **.NET 10** WebAPI backend
- **pnpm** as the package manager

Along the way, I'll share the bugs and quirks I discovered so you don't have to.

## The Architecture

```
AspireWithNuxt/
├── AppHost/          ← Aspire orchestrator
├── WebApi/           ← .NET 10 API
└── WebApp/           ← Nuxt 4 frontend
```

Aspire starts both projects, injects environment variables for service discovery, and provides a dashboard to monitor everything.

## Why Not Use Nuxt's Proxy?

If you've tried configuring `routeRules` or `nitro.devProxy` in Nuxt 4, you've probably hit issues. The proxy configuration is unreliable—SSR requests hang, 404s appear randomly, and debugging is painful.

The solution: **use Nuxt server routes instead**. They work with SSR, they're explicit, and they're debuggable.

```typescript
// server/api/weatherforecast.ts
export default defineEventHandler(async (): Promise<any> => {
  const apiBase = process.env.API_BASE_URL ?? 'http://localhost:5230';
  return await $fetch(`${apiBase}/api/weatherforecast`);
});
```

Aspire injects `API_BASE_URL` pointing to your .NET API. The server route picks it up and forwards requests. Clean.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- pnpm (`npm install -g pnpm`)
- Aspire CLI (`curl -sSL https://aspire.dev/install.sh | bash`)

## Step 1: Create the .NET WebAPI

```bash
mkdir AspireWithNuxt && cd AspireWithNuxt
dotnet new webapi -o WebApi
```

The default weather forecast API is perfect for testing.

## Step 2: Create the Nuxt App

```bash
npx nuxi init WebApp
cd WebApp
pnpm install
pnpm add -D @types/node
```

### Configure the dev script

**Critical:** pnpm has a quirk where the `--` separator creates directories instead of passing arguments. Bake the flags directly into `package.json`:

```json
{
  "scripts": {
    "dev": "nuxt dev --host 0.0.0.0 --port 4000",
    "build": "nuxt build",
    "generate": "nuxt generate",
    "preview": "nuxt preview",
    "postinstall": "nuxt prepare"
  }
}
```

### Create the server route

```typescript
// server/api/weatherforecast.ts
export default defineEventHandler(async (): Promise<any> => {
  const apiBase = process.env.API_BASE_URL ?? 'http://localhost:5230';
  return await $fetch(`${apiBase}/api/weatherforecast`);
});
```

### Create a component to display data

```vue
<!-- components/WeatherForecasts.vue -->
<script setup lang="ts">
interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

const { data, error, status } = await useFetch<WeatherForecast[]>('/api/weatherforecast');
</script>

<template>
  <div>
    <p>Status: {{ status }}</p>
    <p v-if="error" style="color: red;">Error: {{ error.message }}</p>
    <table v-if="data?.length">
      <thead>
        <tr>
          <th>Date</th>
          <th>Summary</th>
          <th>°C</th>
          <th>°F</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in data" :key="item.date">
          <td>{{ item.date }}</td>
          <td>{{ item.summary }}</td>
          <td>{{ item.temperatureC }}</td>
          <td>{{ item.temperatureF }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
```

### Update app.vue

```vue
<!-- app.vue -->
<template>
  <div>
    <h1>Weather Forecasts</h1>
    <WeatherForecasts />
  </div>
</template>
```

## Step 3: Create the Aspire AppHost

```bash
cd ..
dotnet new aspire-apphost -o AppHost
dotnet sln add AppHost/AppHost.csproj
dotnet add AppHost/AppHost.csproj reference WebApi/WebApi.csproj
```

### Add the JavaScript hosting package

```bash
cd AppHost
dotnet add package Aspire.Hosting.JavaScript
```

### Configure Program.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.WebApi>("WebApi");

var webApp = builder.AddJavaScriptApp("WebApp", "../WebApp")
    .WithPnpm()
    .WithRunScript("dev")
    .WithHttpEndpoint(port: 4000, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));

builder.Build().Run();
```

**Key details:**

- `WithPnpm()` — tells Aspire to use pnpm instead of npm
- `WithRunScript("dev")` — runs your package.json "dev" script
- `WithHttpEndpoint(port: 4000, isProxied: false)` — the `isProxied: false` is crucial; without it, Aspire waits for service discovery confirmation that Nuxt never sends, and you get errors like "information about the port to expose the service is missing"
- `WithEnvironment("API_BASE_URL", ...)` — injects the .NET API URL for your server routes

## Step 4: Run It

```bash
cd AppHost
dotnet run
```

Open the Aspire dashboard (URL shown in console). You should see:

| Name   | State   | URLs                   |
| ------ | ------- | ---------------------- |
| WebApp | Running | http://localhost:4000  |
| WebApi | Running | https://localhost:7298 |

Click the WebApp URL. Your Nuxt app loads, calls the server route, which calls the .NET API, and displays weather data.

## Gotchas I Discovered

### 1. pnpm's `--` separator creates directories

```bash
# ❌ Creates a folder named "--host"
pnpm run dev -- --host 0.0.0.0

# ✅ Works correctly
pnpm dev --host 0.0.0.0
```

Solution: Put flags directly in your package.json script.

### 2. Nuxt ignores the PORT environment variable

Unlike many Node apps, Nuxt's dev server doesn't read `PORT`. You must pass `--port` as a CLI argument.

### 3. The `isProxied: false` flag

Without this, Aspire's DCP (Distributed Control Plane) repeatedly logs:

```
Could not create Endpoint object(s) ...
"error": "information about the port to expose the service is missing"
```

The app still works, but the dashboard won't show the URL. Adding `isProxied: false` tells Aspire to trust your port configuration without waiting for confirmation.

### 4. Nuxt 4's proxy is broken

Don't waste time on `routeRules` proxy or `nitro.devProxy`. They have known issues with SSR. Server routes are more reliable.

## Production Considerations

For production, you'll want to:

1. **Build Nuxt for production:**

   ```bash
   pnpm build
   ```

2. **Use environment-specific API URLs:**

   ```typescript
   const apiBase =
     process.env.API_BASE_URL ??
     (process.dev ? 'http://localhost:5230' : 'https://api.yoursite.com');
   ```

3. **Consider hybrid rendering:**
   ```typescript
   // nuxt.config.ts
   export default defineNuxtConfig({
     routeRules: {
       '/': { prerender: true }, // Static landing page
       '/app/**': { ssr: false }, // SPA for interactive features
     },
   });
   ```

## Why This Stack?

- **.NET backend** — Enterprise-grade, great for complex business logic, Azure integration, SignalR
- **Nuxt frontend** — SSR for SEO, file-based routing, excellent DX
- **Aspire orchestration** — One command starts everything, automatic service discovery, unified dashboard

The combination is underrepresented in tutorials (most .NET content pushes Blazor), but it's a pragmatic choice for teams with .NET backend expertise who prefer Vue's developer experience.

## Repository

Full source code: [GitHub link]

---

_Built with Aspire 13, Nuxt 4.2.1, .NET 10, and pnpm on Windows 11._
