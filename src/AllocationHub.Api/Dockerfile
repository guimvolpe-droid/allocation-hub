# Multi-stage build for the AllocationHub API. Build context is the REPO ROOT (see docker-compose.yml),
# so the whole solution — needed to restore/test — is available.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching) — copy the manifests the solution needs.
COPY AllocationHub.sln Directory.Build.props ./
COPY src/AllocationHub.Core/AllocationHub.Core.csproj src/AllocationHub.Core/
COPY src/AllocationHub.Infrastructure/AllocationHub.Infrastructure.csproj src/AllocationHub.Infrastructure/
COPY src/AllocationHub.Api/AllocationHub.Api.csproj src/AllocationHub.Api/
COPY tests/AllocationHub.Tests/AllocationHub.Tests.csproj tests/AllocationHub.Tests/
RUN dotnet restore

# Copy the rest and build. The tests run in the image build so a broken commit can't produce an image.
COPY . .
RUN dotnet test tests/AllocationHub.Tests/AllocationHub.Tests.csproj -c Release --no-restore
RUN dotnet publish src/AllocationHub.Api/AllocationHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# Bind all interfaces inside the container; the host maps it. Invariant globalization matches the build.
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
EXPOSE 8080
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "AllocationHub.Api.dll"]
