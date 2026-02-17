# Node.js build stage
FROM node:24.12.0-alpine AS node-build
WORKDIR /src

# Copy package files for better layer caching
COPY ["clipviewer.vue/package.json", "clipviewer.vue/package-lock.json*", "clipviewer.vue/"]

# Install dependencies
WORKDIR /src/clipviewer.vue
RUN npm ci

COPY "clipviewer.vue/" .
RUN npm run build

# .NET build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ClipViewer.API/ClipViewer.API.csproj", "ClipViewer.API/"]
COPY ["ClipViewer.Data/ClipViewer.Data.csproj", "ClipViewer.Data/"]
COPY ["ClipViewer.UnitTests/ClipViewer.UnitTests.csproj", "ClipViewer.UnitTests/"]
RUN dotnet restore "ClipViewer.API/ClipViewer.API.csproj"

# Copy everything else and build
COPY . .

WORKDIR "/src/ClipViewer.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y libgssapi-krb5-2

# Create the directory structure the API expects
RUN mkdir -p /clipviewer.vue/dist

# Copy the published app and Vue.js files
COPY --from=build /app/publish .
COPY --from=node-build /src/clipviewer.vue/dist /clipviewer.vue/dist

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Create necessary directories for uploads
RUN mkdir -p /app/output

# Expose the port the app runs on
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "ClipViewer.API.dll"]
