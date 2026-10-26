FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CoffeeNChill.Functions.csproj ./
RUN dotnet restore CoffeeNChill.Functions.csproj

COPY . ./
RUN dotnet publish CoffeeNChill.Functions.csproj \
    --configuration Release \
    --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS runtime
WORKDIR /home/site/wwwroot

COPY --from=build /home/site/wwwroot ./

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true