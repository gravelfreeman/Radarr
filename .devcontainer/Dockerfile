FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

ARG CHROME_FOR_TESTING_VERSION=134.0.6998.165

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
        libasound2 \
        libatk-bridge2.0-0 \
        libatk1.0-0 \
        libcups2 \
        libdrm2 \
        libgbm1 \
        libglib2.0-0 \
        libgtk-3-0 \
        mediainfo \
        libnspr4 \
        libnss3 \
        procps \
        ripgrep \
        sqlite3 \
        libu2f-udev \
        libvulkan1 \
        libx11-xcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
        libxshmfence1 \
        unzip \
        xdg-utils \
        zip; \
    curl -fsSL "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_FOR_TESTING_VERSION}/linux64/chrome-linux64.zip" -o /tmp/chrome-linux64.zip; \
    rm -rf /opt/chrome-for-testing; \
    mkdir -p /opt/chrome-for-testing; \
    unzip -q /tmp/chrome-linux64.zip -d /opt/chrome-for-testing; \
    ln -sf /opt/chrome-for-testing/chrome-linux64/chrome /usr/local/bin/google-chrome; \
    ln -sf /opt/chrome-for-testing/chrome-linux64/chrome /usr/local/bin/chromium; \
    ln -sf /opt/chrome-for-testing/chrome-linux64/chrome /usr/local/bin/chrome; \
    rm -f /tmp/chrome-linux64.zip; \
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
