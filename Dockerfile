# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY melanki.trippeltrumf.sln ./
COPY src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj src/melanki.trippeltrumf.service/
COPY tests/melanki.trippeltrumf.service.tests/melanki.trippeltrumf.service.tests.csproj tests/melanki.trippeltrumf.service.tests/

RUN dotnet restore melanki.trippeltrumf.sln

COPY . .
RUN dotnet publish src/melanki.trippeltrumf.service/melanki.trippeltrumf.service.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o /app/publish

FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-noble AS runtime
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish/ ./

ENTRYPOINT ["./melanki.trippeltrumf.service"]
