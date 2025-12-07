using NextAdmin.Log;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextAdmin.Application.Services
{
    /// <summary>
    /// 数据库迁移服务
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public DatabaseMigrationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        /// <returns>迁移结果</returns>
        public async Task<bool> ExecuteMigrationsAsync()
        {
            try
            {
                LogHelper.Info("开始执行数据库迁移服务...");
                
                using var scope = _serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                
                // 直接调用迁移逻辑，避免跨项目依赖
                await ExecuteMigrationsAsync(database);
                
                LogHelper.Info("数据库迁移服务执行完成");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"执行数据库迁移服务时发生错误: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 检查是否需要执行迁移
        /// </summary>
        /// <returns>是否需要迁移</returns>
        public async Task<bool> NeedsMigrationAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                
                // 检查设备集合中是否有文档缺少新字段
                var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                
                // 检查是否有文档缺少 TotalPowerConsumption 字段
                var filter1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("TotalPowerConsumption")
                );
                var count1 = await collection.CountDocumentsAsync(filter1);
                
                // 检查是否有文档缺少 ParentDeviceId 字段
                var filter2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ParentDeviceId")
                );
                var count2 = await collection.CountDocumentsAsync(filter2);
                
                // 检查是否有文档缺少 IsMeter 字段
                var filter3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("IsMeter")
                );
                var count3 = await collection.CountDocumentsAsync(filter3);
                
                // 检查 RegisterInfos 中是否有缺少 ParameterType 字段的
                var filter4 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("RegisterInfos");
                var documents = await collection.Find(filter4).ToListAsync();
                int count4 = 0;
                
                foreach (var document in documents)
                {
                    if (document.Contains("RegisterInfos") && document["RegisterInfos"].IsBsonArray)
                    {
                        var registerInfosArray = document["RegisterInfos"].AsBsonArray;
                        foreach (var registerInfo in registerInfosArray)
                        {
                            if (!registerInfo.AsBsonDocument.Contains("ParameterType"))
                            {
                                count4++;
                                break; // 只要有一个缺少就足够了
                            }
                        }
                    }
                }

                // 检查设备数据集合中是否有文档缺少 ProjectId 字段
                var deviceDataCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceDatas");
                var filter5 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ProjectId")
                );
                var count5 = await deviceDataCollection.CountDocumentsAsync(filter5);
                
                bool needsMigration = count1 > 0 || count2 > 0 || count3 > 0 || count4 > 0 || count5 > 0;
                
                if (needsMigration)
                {
                    LogHelper.Info($"检测到需要迁移的文档：缺少TotalPowerConsumption字段的文档 {count1} 个，缺少ParentDeviceId字段的文档 {count2} 个，缺少IsMeter字段的文档 {count3} 个，缺少ParameterType字段的RegisterInfos {count4} 个，缺少ProjectId字段的设备数据 {count5} 个");
                }
                else
                {
                    LogHelper.Info("数据库迁移检查完成，无需执行迁移");
                }
                
                return needsMigration;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"检查数据库迁移需求时发生错误: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        /// <param name="database">MongoDB数据库实例</param>
        private static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            try
            {
                if (database != null)
                {
                    LogHelper.Info("开始执行数据库迁移...");
                    
                    var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                    
                    // 查找所有没有 TotalPowerConsumption 字段的文档
                    var filter1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("TotalPowerConsumption")
                    );
                    var update1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("TotalPowerConsumption", 0.0);
                    var result1 = await collection.UpdateManyAsync(filter1, update1);
                    LogHelper.Info($"迁移完成：为 {result1.ModifiedCount} 个设备文档添加了总耗电量字段，默认值为 0.0");

                    // 查找所有没有 ParentDeviceId 字段的文档
                    var filter2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ParentDeviceId")
                    );
                    var update2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("ParentDeviceId", MongoDB.Bson.ObjectId.Empty);
                    var result2 = await collection.UpdateManyAsync(filter2, update2);
                    LogHelper.Info($"迁移完成：为 {result2.ModifiedCount} 个设备文档添加了父设备ID字段，默认值为空");

                    // 查找所有没有 IsMeter 字段的文档
                    var filter3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("IsMeter")
                    );
                    var update3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("IsMeter", false);
                    var result3 = await collection.UpdateManyAsync(filter3, update3);
                    LogHelper.Info($"迁移完成：为 {result3.ModifiedCount} 个设备文档添加了是否电表字段，默认值为 false");

                    // 更新设备集合中 RegisterInfos 的 ParameterType 字段
                    await UpdateRegisterInfosParameterTypeAsync(collection);
                    
                    // 更新设备类型集合中 RegisterInfos 的 ParameterType 字段
                    var deviceTypeCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceTypes");
                    await UpdateDeviceTypeRegisterInfosParameterTypeAsync(deviceTypeCollection);
                    
                    // 为设备数据添加 ProjectId 字段
                    await AddProjectIdToDeviceDataAsync(database);
                    
                    LogHelper.Info("数据库迁移执行完成");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"执行数据库迁移时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新设备集合中 RegisterInfos 的 ParameterType 字段
        /// </summary>
        /// <param name="collection">设备集合</param>
        private static async Task UpdateRegisterInfosParameterTypeAsync(IMongoCollection<MongoDB.Bson.BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("开始更新设备集合中 RegisterInfos 的 ParameterType 字段...");
                
                // 查找所有包含 RegisterInfos 数组的文档
                var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("RegisterInfos");
                var documents = await collection.Find(filter).ToListAsync();
                
                int updatedCount = 0;
                foreach (var document in documents)
                {
                    if (document.Contains("RegisterInfos") && document["RegisterInfos"].IsBsonArray)
                    {
                        var registerInfosArray = document["RegisterInfos"].AsBsonArray;
                        bool hasChanges = false;
                        
                        for (int i = 0; i < registerInfosArray.Count; i++)
                        {
                            var registerInfo = registerInfosArray[i].AsBsonDocument;
                            
                            // 检查是否缺少 ParameterType 字段
                            if (!registerInfo.Contains("ParameterType"))
                            {
                                // 根据寄存器(点位)名称或其他属性推断 ParameterType
                                var parameterType = InferParameterTypeFromRegisterInfo(registerInfo);
                                registerInfo["ParameterType"] = parameterType;
                                hasChanges = true;
                            }
                        }
                        
                        if (hasChanges)
                        {
                            var updateFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
                            await collection.UpdateOneAsync(updateFilter, update);
                            updatedCount++;
                        }
                    }
                }
                
                LogHelper.Info($"设备集合迁移完成：更新了 {updatedCount} 个设备文档的 RegisterInfos ParameterType 字段");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新设备集合 RegisterInfos ParameterType 时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新设备类型集合中 RegisterInfos 的 ParameterType 字段
        /// </summary>
        /// <param name="collection">设备类型集合</param>
        private static async Task UpdateDeviceTypeRegisterInfosParameterTypeAsync(IMongoCollection<MongoDB.Bson.BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("开始更新设备类型集合中 RegisterInfos 的 ParameterType 字段...");
                
                // 查找所有包含 RegisterInfos 数组的文档
                var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("RegisterInfos");
                var documents = await collection.Find(filter).ToListAsync();
                
                int updatedCount = 0;
                foreach (var document in documents)
                {
                    if (document.Contains("RegisterInfos") && document["RegisterInfos"].IsBsonArray)
                    {
                        var registerInfosArray = document["RegisterInfos"].AsBsonArray;
                        bool hasChanges = false;
                        
                        for (int i = 0; i < registerInfosArray.Count; i++)
                        {
                            var registerInfo = registerInfosArray[i].AsBsonDocument;
                            
                            // 检查是否缺少 ParameterType 字段
                            if (!registerInfo.Contains("ParameterType"))
                            {
                                // 根据寄存器(点位)名称或其他属性推断 ParameterType
                                var parameterType = InferParameterTypeFromRegisterInfo(registerInfo);
                                registerInfo["ParameterType"] = parameterType;
                                hasChanges = true;
                            }
                        }
                        
                        if (hasChanges)
                        {
                            var updateFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
                            await collection.UpdateOneAsync(updateFilter, update);
                            updatedCount++;
                        }
                    }
                }
                
                LogHelper.Info($"设备类型集合迁移完成：更新了 {updatedCount} 个设备类型文档的 RegisterInfos ParameterType 字段");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新设备类型集合 RegisterInfos ParameterType 时发生错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据寄存器(点位)信息推断 ParameterType
        /// </summary>
        /// <param name="registerInfo">寄存器(点位)信息文档</param>
        /// <returns>推断的 ParameterType</returns>
        private static string InferParameterTypeFromRegisterInfo(MongoDB.Bson.BsonDocument registerInfo)
        {
            try
            {
                // 获取寄存器(点位)名称
                string name = registerInfo.Contains("Name") && !registerInfo["Name"].IsBsonNull ? registerInfo["Name"].AsString : "";
                string enName = registerInfo.Contains("EnName") && !registerInfo["EnName"].IsBsonNull ? registerInfo["EnName"].AsString : "";
                string unit = registerInfo.Contains("Unit") && !registerInfo["Unit"].IsBsonNull ? registerInfo["Unit"].AsString : "";
                
                // 转换为小写进行比较
                string nameLower = name.ToLower();
                string enNameLower = enName.ToLower();
                string unitLower = unit.ToLower();
                
                // 根据名称和单位推断参数类型
                if (nameLower.Contains("电流") || enNameLower.Contains("current") || unitLower.Contains("a"))
                {
                    return "Current";
                }
                else if (nameLower.Contains("电压") || enNameLower.Contains("voltage") || unitLower.Contains("v"))
                {
                    return "Voltage";
                }
                else if (nameLower.Contains("功率") || enNameLower.Contains("power") || unitLower.Contains("w") || unitLower.Contains("kw"))
                {
                    return "Power";
                }
                else if (nameLower.Contains("能耗") || nameLower.Contains("电能") || enNameLower.Contains("energy") || unitLower.Contains("kwh"))
                {
                    return "Energy";
                }
                else if (nameLower.Contains("功率因数") || enNameLower.Contains("powerfactor") || enNameLower.Contains("pf"))
                {
                    return "PowerFactor";
                }
                else if (nameLower.Contains("频率") || enNameLower.Contains("frequency") || unitLower.Contains("hz"))
                {
                    return "Frequency";
                }
                else if (nameLower.Contains("温度") || enNameLower.Contains("temperature") || unitLower.Contains("°c") || unitLower.Contains("c"))
                {
                    return "Temperature";
                }
                else if (nameLower.Contains("湿度") || enNameLower.Contains("humidity") || unitLower.Contains("%"))
                {
                    return "Humidity";
                }
                else if (nameLower.Contains("压力") || enNameLower.Contains("pressure") || unitLower.Contains("pa") || unitLower.Contains("bar"))
                {
                    return "Pressure";
                }
                else if (nameLower.Contains("流量") || enNameLower.Contains("flow") || unitLower.Contains("m³") || unitLower.Contains("l"))
                {
                    return "Flow";
                }
                else if (nameLower.Contains("转速") || enNameLower.Contains("speed") || unitLower.Contains("rpm"))
                {
                    return "Speed";
                }
                else if (nameLower.Contains("振动") || enNameLower.Contains("vibration"))
                {
                    return "Vibration";
                }
                else if (nameLower.Contains("位移") || enNameLower.Contains("displacement"))
                {
                    return "Displacement";
                }
                else if (nameLower.Contains("扭矩") || enNameLower.Contains("torque"))
                {
                    return "Torque";
                }
                else if (nameLower.Contains("液位") || enNameLower.Contains("level"))
                {
                    return "Level";
                }
                else if (nameLower.Contains("开关") || nameLower.Contains("状态") || enNameLower.Contains("status") || enNameLower.Contains("switch"))
                {
                    return "SwitchStatus";
                }
                else if (nameLower.Contains("运行") || enNameLower.Contains("running"))
                {
                    return "RunningStatus";
                }
                else if (nameLower.Contains("故障") || enNameLower.Contains("fault"))
                {
                    return "FaultStatus";
                }
                else if (nameLower.Contains("通信") || enNameLower.Contains("communication"))
                {
                    return "CommunicationStatus";
                }
                else if (nameLower.Contains("不平衡") || enNameLower.Contains("unbalance"))
                {
                    return "Unbalance";
                }
                
                // 默认返回 Other
                return "Other";
            }
            catch (Exception ex)
            {
                LogHelper.Error($"推断 ParameterType 时发生错误: {ex.Message}", ex);
                return "Other";
            }
        }

        /// <summary>
        /// 为设备数据添加 ProjectId 字段
        /// </summary>
        /// <param name="database">MongoDB数据库实例</param>
        private static async Task AddProjectIdToDeviceDataAsync(IMongoDatabase database)
        {
            try
            {
                LogHelper.Info("开始为设备数据添加 ProjectId 字段...");
                
                var deviceDataCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceDatas");
                var deviceCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                
                // 查找所有没有 ProjectId 字段的设备数据文档
                var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ProjectId")
                );
                
                var deviceDataDocuments = await deviceDataCollection.Find(filter).ToListAsync();
                LogHelper.Info($"找到 {deviceDataDocuments.Count} 个需要添加 ProjectId 字段的设备数据文档");
                
                int updatedCount = 0;
                int errorCount = 0;
                
                foreach (var deviceDataDoc in deviceDataDocuments)
                {
                    try
                    {
                        // 获取设备ID
                        if (!deviceDataDoc.Contains("DeviceId"))
                        {
                            LogHelper.Warn($"设备数据文档缺少 DeviceId 字段，跳过: {deviceDataDoc["_id"]}");
                            errorCount++;
                            continue;
                        }
                        
                        var deviceId = deviceDataDoc["DeviceId"].AsObjectId;
                        
                        // 根据设备ID查找对应的设备文档
                        var deviceFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", deviceId);
                        var deviceDoc = await deviceCollection.Find(deviceFilter).FirstOrDefaultAsync();
                        
                        if (deviceDoc == null)
                        {
                            LogHelper.Warn($"未找到设备ID为 {deviceId} 的设备文档，跳过");
                            errorCount++;
                            continue;
                        }
                        
                        // 获取设备的 ProjectId
                        ObjectId projectId = MongoDB.Bson.ObjectId.Empty;
                        if (deviceDoc.Contains("ProjectId"))
                        {
                            projectId = deviceDoc["ProjectId"].AsObjectId;
                        }
                        else
                        {
                            LogHelper.Warn($"设备文档 {deviceId} 缺少 ProjectId 字段，使用默认值");
                        }
                        
                        // 更新设备数据文档
                        var updateFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", deviceDataDoc["_id"]);
                        var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("ProjectId", projectId);
                        var result = await deviceDataCollection.UpdateOneAsync(updateFilter, update);
                        
                        if (result.ModifiedCount > 0)
                        {
                            updatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"更新设备数据文档时发生错误: {ex.Message}", ex);
                        errorCount++;
                    }
                }
                
                LogHelper.Info($"设备数据 ProjectId 字段迁移完成：成功更新 {updatedCount} 个文档，错误 {errorCount} 个");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"为设备数据添加 ProjectId 字段时发生错误: {ex.Message}", ex);
            }
        }
    }
}
