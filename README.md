# NextAdmin - 通用后台管理框架

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![MongoDB](https://img.shields.io/badge/MongoDB-6.0%2B-green.svg)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-7.0%2B-red.svg)](https://redis.io/)

**基于 DDD（领域驱动设计）的通用企业级后台管理框架**

## 🎯 项目简介

本项目是一个**通用框架模板**，基于领域驱动设计（DDD）原则构建，采用清晰的分层架构和 SOLID 设计原则。所有具体业务逻辑已被移除，只保留了核心的基础设施和抽象，可以作为任何领域应用的起点。

## 🏗️ 技术栈

- **后端框架**: ASP.NET Core 9 Web API
- **数据库**: MongoDB
- **缓存**: Redis
- **消息中介**: MediatR（领域事件）
- **架构模式**: DDD + CQRS + Clean Architecture

## 📁 项目结构

```
NextAdmin/
├── src/
│   ├── API/                      # Web API 层（表示层）
│   ├── Application/              # 应用层（应用服务、DTOs）
│   ├── Core/                     # 核心层 ⭐
│   │   └── Domain/              # 领域层（实体、值对象、领域事件）
│   ├── Infrastructure/           # 基础设施层（数据访问、外部服务）
│   ├── Common/                   # 通用工具类
│   ├── Shared/                   # 共享类型
│   ├── KB0.Log/                  # 日志服务
│   └── KB0.Redis/                # Redis 服务
├── .env.example                  # 环境变量示例
├── LICENSE                       # MIT 许可证
├── CONTRIBUTING.md               # 贡献指南
└── README.md
```

## ⭐ 核心特性

### 1. 领域层 (Core/Domain)
- ✅ `BaseEntity` - 实体基类（ID、审计字段）
- ✅ `AggregateRoot` - 聚合根（领域事件、软删除）
- ✅ `ValueObject` - 值对象基类
- ✅ `DomainEventBase` - 领域事件基类
- ✅ `IBaseRepository` - 通用仓储接口

详细文档：[Core/README.md](src/Core/README.md) | [Domain/README.md](src/Core/Domain/README.md)

### 2. 分层架构
- **API 层**: RESTful API、控制器、中间件
- **Application 层**: 应用服务、DTO、映射
- **Domain 层**: 领域模型、业务规则、领域事件
- **Infrastructure 层**: 数据访问、外部服务集成

### 3. 设计模式
- **仓储模式**: 封装数据访问
- **中介者模式**: MediatR 处理领域事件
- **CQRS**: 命令查询职责分离
- **依赖注入**: 松耦合设计

## 🚀 快速开始

### 环境要求
- .NET 9 SDK
- MongoDB 6.0+
- Redis 7.0+
- Visual Studio 2022 或 VS Code

### 1. 配置环境变量

⚠️ **安全提示**：切勿将 `appsettings.json` 中的敏感信息提交到版本控制系统！

**推荐做法：**

1. 复制 `.env.example` 文件为 `.env`（已在 `.gitignore` 中）
2. 在 `.env` 中配置您的真实连接字符串和密钥
3. 或使用用户机密管理（User Secrets）：
   ```bash
   dotnet user-secrets init --project src/API
   dotnet user-secrets set "MongoDb:ConnectionString" "your_connection_string" --project src/API
   dotnet user-secrets set "Jwt:SecretKey" "your_secret_key" --project src/API
   ```

### 2. MongoDB 配置

1. 通过 MongoDB Compass 连接数据库
2. 创建管理员用户（**请使用强密码**）：
   ```javascript
   use admin
   db.createUser({
     user: "admin",
     pwd: "YOUR_STRONG_PASSWORD",  // ⚠️ 请修改为强密码
     roles: ["root"]
   })
   ```

3. 启用认证（编辑 `mongod.cfg`）：
   ```yaml
   security:
     authorization: enabled
   ```

4. 重启 MongoDB 服务

5. 更新连接字符串（使用环境变量或用户机密）：
   ```json
   "MongoDb": {
     "ConnectionString": "mongodb://admin:YOUR_PASSWORD@localhost:27017/NextAdmin?authSource=admin",
     "DatabaseName": "NextAdmin"
   }
   ```

### 3. 运行项目

```bash
# 还原 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行 API 项目
dotnet run --project src/API/NextAdmin.API.csproj
```

访问 Swagger UI：`https://localhost:5001/swagger`

**开发证书信任：**
```bash
dotnet dev-certs https --trust
```

## 🐳 Docker 快速部署

### 使用 Docker Compose 启动所有服务

```bash
# 复制环境变量文件
cp .env.example .env

# 编辑 .env 设置您的密码
# 启动服务（MongoDB + Redis）
docker-compose up -d

# 查看服务状态
docker-compose ps

# 停止服务
docker-compose down
```

### 仅启动数据库服务

```bash
# 启动 MongoDB 和 Redis
docker-compose up -d mongodb redis

# 本地运行 API
dotnet run --project src/API/NextAdmin.API.csproj
```

## 📚 文档

- [项目架构](src/Core/README.md)
- [领域层设计](src/Core/Domain/README.md)
- [动态生成机制](mds/TENANT_DYNAMIC_GENERATION_SUMMARY.md)
- [贡献指南](CONTRIBUTING.md)
- [开源检查清单](OPEN_SOURCE_CHECKLIST.md)

## 🤝 贡献

欢迎贡献！请先阅读 [贡献指南](CONTRIBUTING.md)。

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: 添加某个功能'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [MongoDB](https://www.mongodb.com/)
- [MediatR](https://github.com/jbogard/MediatR)
- [AutoMapper](https://github.com/AutoMapper/AutoMapper)

## 📧 联系方式

如有问题或建议，请创建 [Issue](https://github.com/YOUR_USERNAME/NextAdmin/issues)

---

⭐ 如果这个项目对您有帮助，请给它一个星标！
# NextAdmin
