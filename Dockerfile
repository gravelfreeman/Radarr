# syntax=docker/dockerfile:1.7

FROM node:20.11.1-bookworm-slim AS frontend-build

WORKDIR /src

COPY . .

RUN yarn --version && \
    yarn install --frozen-lockfile --network-timeout 120000 && \
    yarn build

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

ARG TARGETARCH
ARG RADARR_VERSION
ARG PACKAGE_VERSION
ARG PACKAGE_AUTHOR
ARG PACKAGE_BRANCH=develop
ARG PACKAGE_UPDATE_MESSAGE="Updates are published from this fork's GitHub Actions pipeline."

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates \
      curl \
      ffmpeg \
      git \
      jq \
      sqlite3 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

COPY . .

RUN case "${TARGETARCH}" in \
      amd64) export RID="linux-musl-x64" ;; \
      arm64) export RID="linux-musl-arm64" ;; \
      arm) export RID="linux-musl-arm" ;; \
      *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    export RADARRVERSION="${RADARR_VERSION}" && \
    ./build.sh --backend -r "${RID}" -f net8.0

COPY --from=frontend-build /src/_output/UI /src/_output/UI

RUN case "${TARGETARCH}" in \
      amd64) export RID="linux-musl-x64" ;; \
      arm64) export RID="linux-musl-arm64" ;; \
      arm) export RID="linux-musl-arm" ;; \
      *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    export RADARRVERSION="${RADARR_VERSION}" && \
    ./build.sh --packages -r "${RID}" -f net8.0 && \
    mkdir -p /out/bin && \
    cp -a "_artifacts/${RID}/net8.0/Radarr/." /out/bin/ && \
    rm -rf /out/bin/Radarr.Update && \
    cat > /out/package_info <<EOF
PackageVersion=${PACKAGE_VERSION}
PackageAuthor=${PACKAGE_AUTHOR}
PackageGlobalMessage=This image is built from the ${PACKAGE_AUTHOR} fork.
UpdateMethod=Docker
UpdateMethodMessage=${PACKAGE_UPDATE_MESSAGE}
Branch=${PACKAGE_BRANCH}
ReleaseVersion=${RADARR_VERSION}
EOF

FROM docker.io/library/alpine:3.24

ENV DOTNET_EnableDiagnostics=0 \
    HOME=/tmp

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
      tzdata && \
    mkdir -p /app/bin /config

COPY --from=build /out/package_info /app/package_info
COPY --from=build /out/bin /app/bin

RUN chmod -R 755 /app && \
    chown -R root:root /app && \
    chown -R nobody:nogroup /config

EXPOSE 7878

VOLUME ["/config"]

USER nobody:nogroup
WORKDIR /config

ENTRYPOINT ["/usr/bin/catatonit", "--", "/app/bin/Radarr"]
CMD ["--nobrowser", "--data=/config"]
