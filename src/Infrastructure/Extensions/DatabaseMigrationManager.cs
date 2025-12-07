using MongoDB.Driver;
using MongoDB.Bson;
using NextAdmin.Log;

namespace NextAdmin.Infrastructure.Extensions
{
    /// <summary>
    /// 数据库迁移管理器
    /// </summary>
    public static class DatabaseMigrationManager
    {
        /// <summary>
        /// 执行数据库迁移
        /// </summary>
        /// <param name="database">MongoDB数据库实例</param>
        public static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            try
            {
                if (database != null)
                {
                    LogHelper.Info("开始执行数据库迁移...");
                    
                    var collection = database.GetCollection<BsonDocument>("devices");
                    
                    // 查找所有没有 TotalPowerConsumption 字段的文档
                    var filter1 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("TotalPowerConsumption")
                    );
                    var update1 = Builders<BsonDocument>.Update.Set("TotalPowerConsumption", 0.0);
                    var result1 = await collection.UpdateManyAsync(filter1, update1);
                    LogHelper.Info($"迁移完成：为 {result1.ModifiedCount} 个设备文档添加了总耗电量字段，默认值为 0.0");

                    // 查找所有没有 ParentDeviceId 字段的文档
                    var filter2 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("ParentDeviceId")
                    );
                    var update2 = Builders<BsonDocument>.Update.Set("ParentDeviceId", ObjectId.Empty);
                    var result2 = await collection.UpdateManyAsync(filter2, update2);
                    LogHelper.Info($"迁移完成：为 {result2.ModifiedCount} 个设备文档添加了父设备ID字段，默认值为空");

                    // 查找所有没有 IsMeter 字段的文档
                    var filter3 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("IsMeter")
                    );
                    var update3 = Builders<BsonDocument>.Update.Set("IsMeter", false);
                    var result3 = await collection.UpdateManyAsync(filter3, update3);
                    LogHelper.Info($"迁移完成：为 {result3.ModifiedCount} 个设备文档添加了是否电表字段，默认值为 false");

                    // 更新设备集合中 RegisterInfos 的 ParameterType 字段
                    await UpdateRegisterInfosParameterTypeAsync(collection);
                    
                    // 更新设备类型集合中 RegisterInfos 的 ParameterType 字段
                    var deviceTypeCollection = database.GetCollection<BsonDocument>("deviceTypes");
                    await UpdateDeviceTypeRegisterInfosParameterTypeAsync(deviceTypeCollection);
                    
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
        private static async Task UpdateRegisterInfosParameterTypeAsync(IMongoCollection<BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("开始更新设备集合中 RegisterInfos 的 ParameterType 字段...");
                
                // 查找所有包含 RegisterInfos 数组的文档
                var filter = Builders<BsonDocument>.Filter.Exists("RegisterInfos");
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
                            var updateFilter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = Builders<BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
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
        private static async Task UpdateDeviceTypeRegisterInfosParameterTypeAsync(IMongoCollection<BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("开始更新设备类型集合中 RegisterInfos 的 ParameterType 字段...");
                
                // 查找所有包含 RegisterInfos 数组的文档
                var filter = Builders<BsonDocument>.Filter.Exists("RegisterInfos");
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
                            var updateFilter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = Builders<BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
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
        private static string InferParameterTypeFromRegisterInfo(BsonDocument registerInfo)
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
    }
}
