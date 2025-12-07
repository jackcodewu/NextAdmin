# 领域层通用框架

本领域层框架基于领域驱动设计（DDD）原则，提供了一套通用的基础类和接口，可用于构建任何业务领域的应用程序。

## 框架结构

### 1. 实体 (Entities)

#### BaseEntity
所有实体的基类，提供以下功能：
- **ID管理**：使用 MongoDB ObjectId 作为主键
- **审计字段**：创建人、更新人、创建时间、更新时间
- **相等性比较**：基于 ID 的相等性比较和哈希码计算

**使用示例：**
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    
    public Product() : base()
    {
    }
}
```

#### AggregateRoot
聚合根基类，继承自 BaseEntity，添加了：
- **领域事件管理**：支持添加、移除和清除领域事件
- **启用/禁用**：IsEnabled 属性控制实体状态
- **软删除**：IsDeleted 属性支持软删除模式

**使用示例：**
```csharp
public class Order : AggregateRoot
{
    public string OrderNumber { get; set; }
    public List<OrderItem> Items { get; set; }
    
    public void AddItem(OrderItem item)
    {
        Items.Add(item);
        AddDomainEvent(new OrderItemAddedEvent(this, item));
    }
    
    public void Cancel()
    {
        SetDeleted();
        AddDomainEvent(new OrderCancelledEvent(this));
    }
}
```

### 2. 值对象 (ValueObjects)

#### ValueObject
值对象基类，提供：
- **值相等性**：基于属性值的相等性比较
- **不可变性支持**：通过 Clone 方法支持浅拷贝

**使用示例：**
```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }
    
    public Address(string street, string city, string zipCode)
    {
        Street = street;
        City = city;
        ZipCode = zipCode;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

### 3. 领域事件 (Events)

#### DomainEventBase<TEntity>
抽象的领域事件基类，支持泛型实体类型：
- **事件元数据**：事件ID、发生时间、版本号
- **事件类型**：Added、Updated、Removed、Custom
- **实体引用**：关联的聚合根实体

**事件类型：**
- `Added` - 实体创建事件
- `Updated` - 实体更新事件
- `Removed` - 实体删除事件
- `Custom` - 自定义事件

**使用示例：**
```csharp
public class OrderCreatedEvent : DomainEventBase<Order>
{
    public OrderCreatedEvent(Order order) 
        : base(order, DomainEventType.Added)
    {
    }
}

public class OrderItemAddedEvent : DomainEventBase<Order>
{
    public OrderItem AddedItem { get; }
    
    public OrderItemAddedEvent(Order order, OrderItem item) 
        : base(order, DomainEventType.Custom)
    {
        AddedItem = item;
    }
}
```

### 4. 仓储接口 (Interfaces/Repositories)

#### IBaseRepository<TEntity>
通用仓储接口，提供基本的 CRUD 操作：

**方法列表：**
- `GetByIdAsync` - 根据ID获取实体
- `GetAllAsync` - 获取所有实体
- `AddAsync` - 添加新实体
- `UpdateAsync` - 更新实体
- `DeleteAsync` - 删除实体
- `ExistsAsync` - 检查实体是否存在
- `CountAsync` - 统计实体数量
- `FindAsync` - 根据条件查询
- `GetPagedAsync` - 分页查询

**使用示例：**
```csharp
public interface IProductRepository : IBaseRepository<Product>
{
    Task<List<Product>> GetByCategoryAsync(string category);
    Task<List<Product>> GetLowStockProductsAsync(int threshold);
}

public class ProductRepository : IProductRepository
{
    // 实现 IBaseRepository<Product> 的所有方法
    // 以及 IProductRepository 的特定方法
}
```

### 5. 扩展 (Extensions)

#### MongoCollectionAttribute
用于标记实体对应的 MongoDB 集合名称：

**使用示例：**
```csharp
[MongoCollection("products")]
public class Product : AggregateRoot
{
    // 实体定义
}
```

## 设计原则

### 1. 领域驱动设计 (DDD)
- **实体 (Entity)**：具有唯一标识的对象
- **值对象 (Value Object)**：无标识，通过值来区分的对象
- **聚合根 (Aggregate Root)**：聚合的入口点，维护聚合的一致性边界
- **领域事件 (Domain Event)**：领域中发生的重要业务事件

### 2. SOLID 原则
- **单一职责**：每个类只负责一个职责
- **开闭原则**：对扩展开放，对修改关闭
- **依赖倒置**：依赖抽象而非具体实现

### 3. 持久化无关
- 框架使用 MongoDB，但设计上保持对持久化技术的独立性
- 可以通过实现 IBaseRepository 接口来支持其他数据库

## 最佳实践

### 1. 实体设计
```csharp
// ✓ 好的实践
public class Customer : AggregateRoot
{
    private readonly List<Order> _orders = new();
    
    public string Name { get; private set; }
    public IReadOnlyList<Order> Orders => _orders.AsReadOnly();
    
    public void PlaceOrder(Order order)
    {
        // 业务逻辑验证
        if (order == null) throw new ArgumentNullException(nameof(order));
        
        _orders.Add(order);
        AddDomainEvent(new OrderPlacedEvent(this, order));
    }
}

// ✗ 避免的实践
public class Customer : AggregateRoot
{
    public string Name { get; set; } // 公开的 setter
    public List<Order> Orders { get; set; } // 可变集合暴露
}
```

### 2. 值对象不可变性
```csharp
// ✓ 好的实践
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(Amount + other.Amount, Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 3. 领域事件使用
```csharp
// 在聚合根中触发事件
public class Order : AggregateRoot
{
    public void Confirm()
    {
        // 业务逻辑
        Status = OrderStatus.Confirmed;
        
        // 触发领域事件
        AddDomainEvent(new OrderConfirmedEvent(this));
    }
}

// 事件处理器
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IEmailService _emailService;
    
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationEmail(notification.Entity);
    }
}
```

### 4. 仓储使用
```csharp
// 在应用服务中使用仓储
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order(dto.CustomerId, dto.Items);
        
        await _orderRepository.AddAsync(order);
        
        // 发布领域事件
        foreach (var domainEvent in order.DomainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
        
        order.ClearDomainEvents();
        
        return order;
    }
}
```

## 扩展指南

### 添加新的实体
1. 确定是实体还是值对象
2. 如果需要独立的 ID，继承 `BaseEntity` 或 `AggregateRoot`
3. 如果是聚合根，使用 `AggregateRoot` 并添加领域事件
4. 添加 `[MongoCollection]` 特性指定集合名称

### 添加新的仓储
1. 创建继承自 `IBaseRepository<TEntity>` 的接口
2. 添加特定的查询方法
3. 在基础设施层实现该接口

### 添加新的领域事件
1. 继承 `DomainEventBase<TEntity>`
2. 在构造函数中指定事件类型
3. 添加事件特定的属性
4. 创建对应的事件处理器实现 `INotificationHandler`

## 依赖项

- **MongoDB.Bson** - MongoDB 数据类型支持
- **MediatR** - 领域事件的发布/订阅机制

## 总结

本框架提供了一套完整的 DDD 基础设施，使开发者可以专注于业务逻辑的实现，而无需关心基础设施的细节。通过遵循框架的设计原则和最佳实践，可以构建出高质量、可维护的领域模型。
