# ============================================================================
# FIAP Cloud Games - Microsserviço de Payments
# Dockerfile Otimizado para Kubernetes - Fase 4
# ============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

RUN mkdir -p FIAP.MicroService.Payments \
             FIAP.MicroService.Payments.Application \
             FIAP.MicroService.Payments.Data \
             FIAP.MicroService.Payments.Domain \
             FIAP.MicroService.Payments.InfraEstrutura
			 
# Copiar apenas arquivos de projeto primeiro (melhor cache de layers)
COPY src/FIAP.MicroService.Payments/FIAP.MicroService.Payments.API.csproj FIAP.MicroService.Payments/FIAP.MicroService.Payments.API.csproj
COPY src/FIAP.MicroService.Payments.Application/FIAP.MicroService.Payments.Application.csproj FIAP.MicroService.Payments.Application/FIAP.MicroService.Payments.Application.csproj
COPY src/FIAP.MicroService.Payments.Data/FIAP.MicroService.Payments.Data.csproj FIAP.MicroService.Payments.Data/FIAP.MicroService.Payments.Data.csproj
COPY src/FIAP.MicroService.Payments.Domain/FIAP.MicroService.Payments.Domain.csproj FIAP.MicroService.Payments.Domain/FIAP.MicroService.Payments.Domain.csproj
COPY src/FIAP.MicroService.Payments.InfraEstrutura/FIAP.MicroService.Payments.InfraEstrutura.csproj FIAP.MicroService.Payments.InfraEstrutura/FIAP.MicroService.Payments.InfraEstrutura.csproj

# Restore de dependências
RUN dotnet restore ./FIAP.MicroService.Payments/FIAP.MicroService.Payments.API.csproj

# Copiar código fonte
COPY src .

# Build da aplicação
WORKDIR /src/FIAP.MicroService.Payments
RUN dotnet build FIAP.MicroService.Payments.API.csproj -c Release -o /app/build

# -----------------------------------------------------------------------------
# Stage 2: Publish
# -----------------------------------------------------------------------------
FROM build AS publish

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: Runtime (Imagem Final)
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

LABEL maintainer="Grupo 118 - FIAP Tech Challenge" \
      version="4.0" \
      description="FCG Payments API - Microsserviço de Pagamentos"

# Instalar dependências necessárias em Alpine
RUN apk add --no-cache icu-libs libc6-compat libgcc libstdc++

# Criar usuário non-root
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -D -s /bin/sh appuser

WORKDIR /app

# Copiar binários publicados
COPY --from=publish /app/publish .

# Ajustar permissões

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FIAP.MicroService.Payments.API.dll"]
