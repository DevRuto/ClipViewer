FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ClipViewer.Worker/ClipViewer.Worker.csproj", "ClipViewer.Worker/"]
RUN dotnet restore "ClipViewer.Worker/ClipViewer.Worker.csproj"
COPY . .
WORKDIR "/src/ClipViewer.Worker"
RUN dotnet build "./ClipViewer.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClipViewer.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y libgssapi-krb5-2 ffmpeg

WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/output
ENTRYPOINT ["dotnet", "ClipViewer.Worker.dll"]
