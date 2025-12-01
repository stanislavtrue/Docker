FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN useradd -m nonroot
USER nonroot

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Presentation.dll"]
