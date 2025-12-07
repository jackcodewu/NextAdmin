# 贡献指南

感谢您对 NextAdmin 项目的关注！我们欢迎任何形式的贡献。

## 如何贡献

### 报告 Bug

如果您发现了 Bug，请在 GitHub Issues 中创建一个新的 Issue，并包含以下信息：

- 问题描述
- 复现步骤
- 期望行为
- 实际行为
- 环境信息（操作系统、.NET 版本、MongoDB 版本等）
- 相关截图或错误日志

### 提交功能建议

如果您有新功能的想法，请在 GitHub Issues 中创建一个 Feature Request，并描述：

- 功能描述
- 使用场景
- 为什么需要这个功能
- 可能的实现方案（可选）

### 提交代码

1. **Fork 仓库**
   
   点击右上角的 "Fork" 按钮，将项目 Fork 到您的账户下。

2. **克隆仓库**
   
   ```bash
   git clone https://github.com/YOUR_USERNAME/NextAdmin.git
   cd NextAdmin
   ```

3. **创建分支**
   
   ```bash
   git checkout -b feature/your-feature-name
   # 或
   git checkout -b fix/your-bug-fix
   ```

4. **开发和测试**
   
   - 遵循项目的代码规范
   - 编写清晰的提交信息
   - 确保所有测试通过
   - 添加必要的单元测试

5. **提交代码**
   
   ```bash
   git add .
   git commit -m "feat: 添加新功能描述"
   # 或
   git commit -m "fix: 修复某个问题描述"
   ```

6. **推送到远程**
   
   ```bash
   git push origin feature/your-feature-name
   ```

7. **创建 Pull Request**
   
   在 GitHub 上创建 Pull Request，并清楚地描述您的更改。

## 代码规范

### C# 编码规范

- 遵循 [Microsoft C# 编码约定](https://docs.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 个空格缩进（不使用 Tab）
- 类名使用 PascalCase（如 `UserService`）
- 方法名使用 PascalCase（如 `GetUserById`）
- 私有字段使用 camelCase，并以下划线开头（如 `_userRepository`）
- 常量使用 PascalCase（如 `MaxRetryCount`）

### 提交信息规范

使用 [约定式提交](https://www.conventionalcommits.org/zh-hans/) 格式：

```
<类型>(<范围>): <描述>

[可选的正文]

[可选的脚注]
```

**类型：**
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式调整（不影响功能）
- `refactor`: 重构（既不是新功能也不是 Bug 修复）
- `perf`: 性能优化
- `test`: 添加或修改测试
- `chore`: 构建过程或辅助工具的变动

**示例：**
```
feat(auth): 添加 JWT 刷新令牌功能
fix(tenant): 修复租户查询过滤条件错误
docs(readme): 更新安装步骤说明
```

## DDD 架构规范

### 分层职责

- **API 层**: 仅处理 HTTP 请求和响应，不包含业务逻辑
- **Application 层**: 应用服务、DTO 转换、事务协调
- **Domain 层**: 领域模型、业务规则、领域事件
- **Infrastructure 层**: 数据访问、外部服务集成

### 实体设计原则

1. 实体必须继承 `AggregateRoot` 或 `BaseEntity`
2. 使用私有 setter 保护数据完整性
3. 通过公共方法修改状态（如 `UpdateInfo()`, `Enable()`, `Disable()`）
4. 领域事件在聚合根中触发

### 仓储模式

- 仓储接口定义在 Domain 层
- 仓储实现在 Infrastructure 层
- 优先使用动态生成的仓储
- 仅在有特殊查询需求时手动实现

### 应用服务

- 使用 `AppService<>` 泛型基类
- 依赖仓储接口而非实现
- 使用 AutoMapper 进行 DTO 转换
- 事务管理在应用服务层

## 测试

### 单元测试

- 使用 xUnit 框架
- 测试类命名：`{被测试类}Tests`
- 测试方法命名：`{方法名}_{场景}_{期望结果}`

示例：
```csharp
[Fact]
public async Task GetAsync_ExistingId_ReturnsEntity()
{
    // Arrange
    var id = "507f1f77bcf86cd799439011";
    
    // Act
    var result = await _service.GetAsync(id);
    
    // Assert
    Assert.NotNull(result);
}
```

### 集成测试

- 使用 WebApplicationFactory
- 使用测试容器（Testcontainers）模拟 MongoDB 和 Redis

## 文档

- 所有公共 API 必须有 XML 文档注释
- 复杂逻辑需要添加代码注释
- 更新相关 Markdown 文档（位于 `/mds` 目录）

## 审查流程

1. 自动化检查（CI/CD）
   - 代码编译
   - 单元测试
   - 代码覆盖率
   - 代码质量检查

2. 代码审查
   - 至少一位维护者审查
   - 解决所有审查意见
   - 通过所有检查后才能合并

## 许可证

通过提交贡献，您同意您的贡献将在 MIT 许可证下授权。

## 联系方式

如果您有任何问题，可以通过以下方式联系我们：

- 创建 GitHub Issue
- 加入讨论区
- 发送邮件至项目维护者

感谢您的贡献！
