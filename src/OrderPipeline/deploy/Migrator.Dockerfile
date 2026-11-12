# Applies every EF Core migration once, then exits. The API services wait for it with
# `condition: service_completed_successfully`, so they never start against an empty
# schema. Migrating from a separate step rather than from service startup keeps the
# services free of a concurrency hazard they would have in any multi-replica deployment.
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

# dotnet ef is not part of the SDK image, so it has to be installed. The version is
# pinned to the EF Core packages in Directory.Packages.props. A tool newer than the
# runtime emits migrations that runtime cannot read, which fails at apply time rather
# than at build time. Installed before the source is copied so the layer stays cached.
RUN dotnet tool install --global dotnet-ef --version 8.0.11
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY . .

# Each database belongs to one service, so each has its own DbContext to update.
CMD ["sh", "-c", "set -e \
 && dotnet ef database update --project services/OrderPipeline.Api \
 && dotnet ef database update --project services/Inventory.Api \
 && dotnet ef database update --project services/Payment.Api \
 && echo 'All migrations applied.'"]
