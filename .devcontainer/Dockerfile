FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

RUN set -eux; \
    rm -f /etc/apt/sources.list.d/yarn.list; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        ffmpeg \
        git-lfs \
        gnupg \
        jq \
        less \
        mediainfo \
        procps \
        ripgrep \
        sqlite3 \
        unzip \
        zip; \
    install -d -m 0755 /etc/apt/keyrings; \
    curl -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor -o /etc/apt/keyrings/yarn-archive-keyring.gpg; \
    chmod a+r /etc/apt/keyrings/yarn-archive-keyring.gpg; \
    echo "deb [signed-by=/etc/apt/keyrings/yarn-archive-keyring.gpg] https://dl.yarnpkg.com/debian/ stable main" > /etc/apt/sources.list.d/yarn.list; \
    apt-get update; \
    git lfs install --system; \
    rm -rf /var/lib/apt/lists/*

RUN set -eux; \
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh; \
    chmod +x /tmp/dotnet-install.sh; \
    /tmp/dotnet-install.sh --version 8.0.421 --install-dir /usr/share/dotnet; \
    rm -f /tmp/dotnet-install.sh; \
    dotnet --list-sdks
