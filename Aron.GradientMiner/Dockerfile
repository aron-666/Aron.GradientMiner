# �ϥθ��p����¦�M��
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Aron.GradientMiner.csproj .
RUN dotnet restore "Aron.GradientMiner.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet publish "Aron.GradientMiner.csproj" -c $BUILD_CONFIGURATION -o /app/publish --self-contained -r linux-x64

# �ϥθ��p���B��ɰ�¦�M��
FROM debian:bullseye-20240722-slim AS final
EXPOSE 50003/tcp
ENV ASPNETCORE_HTTP_PORTS 50003
WORKDIR /app

RUN apt-get update -y
RUN apt-get install -y wget


RUN apt-get install -y --no-install-recommends \
    curl \
    net-tools \
    iputils-ping \
    ca-certificates \
    chromium \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
RUN chmod +x Aron.GradientMiner

ENTRYPOINT ["./Aron.GradientMiner"]
