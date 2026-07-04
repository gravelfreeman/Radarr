# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0 AS build

ARG TARGETARCH
ARG RADARR_VERSION
ARG PACKAGE_VERSION
ARG PACKAGE_AUTHOR
ARG PACKAGE_BRANCH=develop
ARG PACKAGE_UPDATE_MESSAGE="Updates are published from this fork's GitHub Actions pipeline."

ENV DEBIAN_FRONTEND=noninteractive

RUN set -eux; \
    rm -f /etc/apt/sources.list.d/yarn.list; \
    apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates \
      curl \
      ffmpeg \
      git \
      gnupg \
      jq \
      sqlite3; \
    install -d -m 0755 /etc/apt/keyrings; \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg; \
    chmod a+r /etc/apt/keyrings/nodesource.gpg; \
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" > /etc/apt/sources.list.d/nodesource.list; \
    apt-get update; \
    apt-get install -y --no-install-recommends nodejs; \
    npm install --global yarn@1.22.19; \
    node --version; \
    npm --version; \
    yarn --version; \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

COPY . .

RUN case "${TARGETARCH}" in \
      amd64) export RID="linux-x64" ;; \
      arm64) export RID="linux-arm64" ;; \
      arm) export RID="linux-arm" ;; \
      *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    export RADARRVERSION="${RADARR_VERSION}" && \
    ./build.sh --backend --frontend --packages -r "${RID}" -f net8.0

RUN case "${TARGETARCH}" in \
      amd64) export RID="linux-x64" ;; \
      arm64) export RID="linux-arm64" ;; \
      arm) export RID="linux-arm" ;; \
      *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac && \
    mkdir -p /out/bin && \
    cp -a "_artifacts/${RID}/net8.0/Radarr/." /out/bin/ && \
    cat > /out/package_info <<EOF
PackageVersion=${PACKAGE_VERSION}
PackageAuthor=${PACKAGE_AUTHOR}
PackageGlobalMessage=This image is built from the ${PACKAGE_AUTHOR} fork.
UpdateMethod=Docker
UpdateMethodMessage=${PACKAGE_UPDATE_MESSAGE}
Branch=${PACKAGE_BRANCH}
ReleaseVersion=${RADARR_VERSION}
EOF

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim

ARG RADARR_UID=1000
ARG RADARR_GID=1000

ENV DEBIAN_FRONTEND=noninteractive \
    HOME=/tmp

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates \
      libsqlite3-0 \
      sqlite3 \
      tzdata && \
    rm -rf /var/lib/apt/lists/*

RUN groupadd --gid "${RADARR_GID}" radarr && \
    useradd --uid "${RADARR_UID}" --gid "${RADARR_GID}" --create-home --home-dir /home/radarr --shell /usr/sbin/nologin radarr

WORKDIR /app

COPY --from=build /out/package_info /app/package_info
COPY --from=build /out/bin /app/bin

RUN mkdir -p /config /media && \
    chown -R radarr:radarr /app /config /media /home/radarr

EXPOSE 7878

VOLUME ["/config", "/media"]

USER radarr

ENTRYPOINT ["/app/bin/Radarr"]
CMD ["-nobrowser", "-data=/config"]
