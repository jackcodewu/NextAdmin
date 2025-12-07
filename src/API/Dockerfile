# 多阶段构建 Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制解决方案和项目文件
COPY ["NextAdmin.sln", "./"]
COPY ["src/API/NextAdmin.API.csproj", "src/API/"]
COPY ["src/Application/NextAdmin.Application.csproj", "src/Application/"]
COPY ["src/Core/NextAdmin.Core.csproj", "src/Core/"]
COPY ["src/Infrastructure/NextAdmin.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Common/NextAdmin.Common.csproj", "src/Common/"]
COPY ["src/Shared/NextAdmin.Shared.csproj", "src/Shared/"]
COPY ["src/KB0.Log/NextAdmin.Log.csproj", "src/KB0.Log/"]
COPY ["src/KB0.Redis/NextAdmin.Redis.csproj", "src/KB0.Redis/"]

# 还原依赖
RUN dotnet restore "src/API/NextAdmin.API.csproj"

# 复制所有源代码
COPY . .

# 构建和发布
WORKDIR "/src/src/API"
RUN dotnet build "NextAdmin.API.csproj" -c Release -o /app/build
RUN dotnet publish "NextAdmin.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 创建非 root 用户
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app

# 复制发布的应用
COPY --from=build --chown=appuser:appuser /app/publish .

# 切换到非 root 用户
USER appuser

ENTRYPOINT ["dotnet", "NextAdmin.API.dll"]
