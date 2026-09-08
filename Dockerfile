# syntax=docker/dockerfile:1.7

FROM node:20.11.1-alpine AS frontend

WORKDIR /src

COPY package.json yarn.lock tsconfig.json .yarnrc ./
RUN corepack enable && \
    corepack prepare yarn@1.22.19 --activate && \
    yarn install --frozen-lockfile --network-timeout 120000

COPY frontend ./frontend
RUN yarn build --env production

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend

ARG TARGETARCH

WORKDIR /src

COPY src ./src
COPY Logo ./Logo

RUN dotnet restore src/NzbDrone.Console/Radarr.Console.csproj \
    && dotnet restore src/NzbDrone.Mono/Radarr.Mono.csproj

RUN case "${TARGETARCH}" in \
        amd64) rid=linux-musl-x64 ;; \
        arm64) rid=linux-musl-arm64 ;; \
        arm) rid=linux-musl-arm ;; \
        *) echo "Unsupported target architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && dotnet publish src/NzbDrone.Console/Radarr.Console.csproj \
        --configuration Release \
        --framework net8.0 \
        --runtime "${rid}" \
        --self-contained true \
        -p:RunAnalyzers=false \
        -p:TreatWarningsAsErrors=false \
        --output /out \
    && dotnet publish src/NzbDrone.Mono/Radarr.Mono.csproj \
        --configuration Release \
        --framework net8.0 \
        --runtime "${rid}" \
        --self-contained true \
        -p:RunAnalyzers=false \
        -p:TreatWarningsAsErrors=false \
        --output /mono-out \
    && cp /mono-out/Radarr.Mono.dll /out/Radarr.Mono.dll \
    && cp /mono-out/Mono.Posix.NETStandard.dll /out/Mono.Posix.NETStandard.dll \
    && cp /mono-out/libMonoPosixHelper.so /out/libMonoPosixHelper.so

FROM docker.io/library/alpine:3.24

ARG RADARR_VERSION
ARG PACKAGE_VERSION
ARG PACKAGE_AUTHOR
ARG PACKAGE_BRANCH=main
ARG PACKAGE_UPDATE_MESSAGE="Update this container by pulling a newer ghcr.io image from this fork."

ENV DOTNET_EnableDiagnostics=0 \
    HOME=/tmp

USER root
WORKDIR /app

RUN apk add --no-cache \
        bash \
        ca-certificates \
        catatonit \
        coreutils \
        curl \
        icu-libs \
        jq \
        libintl \
        nano \
        sqlite-libs \
        tzdata \
    && mkdir -p /app/bin /config

COPY --from=backend /out /app/bin
COPY --from=frontend /src/_output/UI /app/bin/UI

RUN printf "PackageVersion=%s\nPackageAuthor=%s\nPackageGlobalMessage=This image is built from the %s fork.\nUpdateMethod=Docker\nUpdateMethodMessage=%s\nBranch=%s\nReleaseVersion=%s\n" \
        "${PACKAGE_VERSION}" "${PACKAGE_AUTHOR}" "${PACKAGE_AUTHOR}" "${PACKAGE_UPDATE_MESSAGE}" "${PACKAGE_BRANCH}" "${RADARR_VERSION}" > /app/package_info \
    && chown -R root:root /app \
    && chmod -R 755 /app \
    && chown -R nobody:nogroup /config \
    && rm -rf /tmp/* /app/bin/Radarr.Update

USER nobody:nogroup
WORKDIR /config
VOLUME ["/config"]

EXPOSE 7878

ENTRYPOINT ["/usr/bin/catatonit", "--", "/app/bin/Radarr"]
CMD ["--nobrowser", "--data=/config"]
