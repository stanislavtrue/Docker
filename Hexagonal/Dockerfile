FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /core

COPY . .
RUN dotnet publish adapters/Http/http.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app .

ENV PORT=8081
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "http.dll"]
