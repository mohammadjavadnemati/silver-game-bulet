FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Silver.Engine/ Silver.Engine/
COPY Silver.Api/ Silver.Api/
RUN dotnet publish Silver.Api/Silver.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Silver.Api.dll"]