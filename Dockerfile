# Node.js build stage
FROM node:20 AS node-build
WORKDIR /src

# Copy package files for better layer caching
COPY ["clipviewer.vue/package.json", "clipviewer.vue/package-lock.json*", "clipviewer.vue/"]

# Install dependencies and build Vue.js app
WORKDIR /src/clipviewer.vue
RUN npm ci && npm run build

# .NET build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ClipViewer.API/ClipViewer.API.csproj", "ClipViewer.API/"]
COPY ["ClipViewer.UnitTests/ClipViewer.UnitTests.csproj", "ClipViewer.UnitTests/"]
RUN dotnet restore "ClipViewer.API/ClipViewer.API.csproj"

# Copy everything else and build
COPY . .

# Create the directory structure the API expects
RUN mkdir -p /src/clipviewer.vue/dist

# Copy Vue.js build output to the expected location
COPY --from=node-build /src/clipviewer.vue/dist /src/clipviewer.vue/dist

WORKDIR "/src/ClipViewer.API"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Create the directory structure the API expects
RUN mkdir -p /app/clipviewer.vue/dist

# Copy the published app and Vue.js files
COPY --from=build /app/publish .
COPY --from=build /src/clipviewer.vue/dist /app/clipviewer.vue/dist

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Create necessary directories for uploads
RUN mkdir -p /app/output /app/Thumbnails /app/HLS

# Expose the port the app runs on
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "ClipViewer.API.dll"]
