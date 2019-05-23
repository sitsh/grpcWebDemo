FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 9090

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src/grpcWebDemo
COPY ["grpcWebDemo.csproj", "/src/grpcNetCoreServer"]
RUN dotnet restore "/src/grpcNetCoreServer/grpcWebDemo.csproj"
COPY . .
WORKDIR "/src/grpcNetCoreServer"
RUN dotnet build "grpcWebDemo.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "grpcWebDemo.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "grpcWebDemo.dll"]