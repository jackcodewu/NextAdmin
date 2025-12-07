# NextAdmin.Core - 通用领域层框架

本项目是一个基于 DDD（领域驱动设计）原则构建的通用领域层框架，提供了开箱即用的基础设施和抽象，可用于快速构建任何领域的应用程序。

## 🎯 项目概述

这是一个**通用框架**，已经移除了所有具体业务领域的实体和逻辑，只保留了 DDD 所需的核心基础类和接口。你可以基于此框架快速开发自己的领域模型。

## 📁 项目结构

```
Core/
├── Domain/                          # 领域层
│   ├── Entities/                    # 实体
│   │   ├── BaseEntity.cs           # 实体基类
│   │   └── AggregateRoot.cs        # 聚合根基类
│   ├── ValueObjects/                # 值对象
│   │   └── ValueObject.cs          # 值对象基类
│   ├── Events/                      # 领域事件
│   │   └── DomainEventBase.cs      # 领域事件基类
│   ├── Interfaces/                  # 接口定义
│   │   └── Repositories/
│   │       └── IBaseRepository.cs  # 通用仓储接口
│   ├── Extensions/                  # 扩展
│   │   └── MongoCollectionAttribute.cs
│   └── README.md                    # 详细使用文档
├── Common/                          # 通用类
├── Extensions/                      # 扩展方法
└── Interfaces/                      # 核心接口
```

## ✨ 核心特性

### 1. **实体管理**
- ✅ `BaseEntity` - 提供 ID、审计字段、相等性比较
- ✅ `AggregateRoot` - 支持领域事件、启用/禁用、软删除

### 2. **值对象支持**
- ✅ `ValueObject` - 基于值的相等性比较
- ✅ 不可变性支持

### 3. **领域事件**
- ✅ `DomainEventBase<TEntity>` - 泛型领域事件基类
- ✅ 内置事件类型：Added, Updated, Removed, Custom
- ✅ 集成 MediatR 进行事件发布/订阅

### 4. **仓储模式**
- ✅ `IBaseRepository<TEntity>` - 通用仓储接口
- ✅ 完整的 CRUD 操作
- ✅ 分页查询、条件查询、投影查询
- ✅ 内置缓存支持

### 5. **持久化**
- ✅ MongoDB 支持（可扩展到其他数据库）
- ✅ MongoCollection 特性标记

## 🚀 快速开始

### 步骤 1: 定义实体

```csharp
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Extensions;

[MongoCollection("products")]
public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public string Category { get; private set; }

    public Product(string name, decimal price, string category)
    {
        Name = name;
        Price = price;
        Category = category;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than zero");

        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(this, newPrice));
    }
}
```

### 步骤 2: 定义值对象

```csharp
using NextAdmin.Core.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 步骤 3: 定义领域事件

```csharp
using NextAdmin.Core.Domain.Events;

public class ProductPriceChangedEvent : DomainEventBase<Product>
{
    public decimal NewPrice { get; }

    public ProductPriceChangedEvent(Product product, decimal newPrice) 
        : base(product, DomainEventType.Updated)
    {
        NewPrice = newPrice;
    }
}
```

### 步骤 4: 定义仓储接口

```csharp
using NextAdmin.Core.Domain.Interfaces.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<List<Product>> GetByCategoryAsync(string category);
    Task<List<Product>> GetLowStockProductsAsync(int threshold);
}
```

### 步骤 5: 实现仓储（在基础设施层）

```csharp
public class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _collection;

    // 实现 IBaseRepository<Product> 的所有方法
    // 以及 IProductRepository 的特定方法

    public async Task<List<Product>> GetByCategoryAsync(string category)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Category, category);
        return await FindAsync(filter);
    }
}
```

### 步骤 6: 使用领域服务

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly IMediator _mediator;

    public ProductService(IProductRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Product> CreateProductAsync(string name, decimal price, string category)
    {
        var product = new Product(name, price, category);
        
        await _repository.AddAsync(product);

        // 发布领域事件
        foreach (var domainEvent in product.DomainEvents)
        {
            await _mediator.Publish(domainEvent);
        }

        product.ClearDomainEvents();

        return product;
    }
}
```

## 📚 设计模式

### DDD 战术模式
- **实体 (Entity)**: 具有唯一标识的对象
- **值对象 (Value Object)**: 通过属性值定义的对象
- **聚合根 (Aggregate Root)**: 聚合的入口，维护一致性边界
- **领域事件 (Domain Event)**: 领域中发生的重要事件
- **仓储 (Repository)**: 封装数据访问逻辑

### SOLID 原则
- ✅ **单一职责原则**: 每个类只负责一项职责
- ✅ **开闭原则**: 对扩展开放，对修改关闭
- ✅ **里氏替换原则**: 子类可以替换父类
- ✅ **接口隔离原则**: 客户端不应依赖它不需要的接口
- ✅ **依赖倒置原则**: 依赖抽象而非具体实现

## 🔧 技术栈

- **.NET 6/7/8** - 现代 C# 语言特性
- **MongoDB.Bson** - MongoDB 数据类型支持
- **MediatR** - 领域事件的中介者模式实现

## 📖 详细文档

请查看 [Domain/README.md](Domain/README.md) 获取更详细的使用指南，包括：
- 完整的 API 文档
- 最佳实践
- 代码示例
- 扩展指南

## 🎓 学习资源

### DDD 相关
- Eric Evans - Domain-Driven Design: Tackling Complexity in the Heart of Software
- Vaughn Vernon - Implementing Domain-Driven Design

### 设计模式
- Martin Fowler - Patterns of Enterprise Application Architecture
- Clean Architecture by Robert C. Martin

## 💡 最佳实践

### ✅ DO（推荐）
- ✅ 将业务逻辑放在实体和聚合根中
- ✅ 使用值对象封装业务概念
- ✅ 通过领域事件解耦不同聚合
- ✅ 保持聚合根的边界清晰
- ✅ 使用仓储接口而非直接访问数据库

### ❌ DON'T（避免）
- ❌ 在实体中直接调用外部服务
- ❌ 暴露可变的集合属性
- ❌ 使用贫血模型（只有 getter/setter 的实体）
- ❌ 在领域层引用基础设施层
- ❌ 忽略业务规则验证

## 🔄 版本历史

### v2.0.0 (2025-11-02)
- 🎉 重构为通用框架
- 🗑️ 移除所有业务特定实体和逻辑
- ✨ 优化仓储接口，移除业务特定参数
- 📝 添加完整的文档和示例
- 🔧 简化领域事件类型

### v1.x.x
- 原业务特定版本（已弃用）

## 🤝 贡献

欢迎贡献代码、报告问题或提出改进建议！

## 📄 许可证

[添加你的许可证信息]

## 📞 联系方式

如有问题或建议，欢迎联系：
- [添加联系方式]

---

**享受使用这个通用 DDD 框架构建你的领域模型吧！** 🚀
