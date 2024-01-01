# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./DynDNSv2/DynDNSv2.csproj .
RUN dotnet restore -a $TARGETARCH

# copy and publish app and libraries
COPY ./DynDNSv2/. .
RUN dotnet publish --no-restore -a $TARGETARCH -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./DynDNSv2"]