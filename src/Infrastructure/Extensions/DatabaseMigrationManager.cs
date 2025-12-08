using MongoDB.Driver;
using MongoDB.Bson;
using NextAdmin.Log;

namespace NextAdmin.Infrastructure.Extensions
{
    /// <summary>
    /// Database migration manager
    /// </summary>
    public static class DatabaseMigrationManager
    {
        /// <summary>
        /// Execute database migrations
        /// </summary>
        /// <param name="database">MongoDB database instance</param>
        public static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            try
            {
                if (database != null)
                {
                    LogHelper.Info("Starting database migration...");
                    
                    var collection = database.GetCollection<BsonDocument>("devices");
                    
                    // Find all documents without the TotalPowerConsumption field
                    var filter1 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("TotalPowerConsumption")
                    );
                    var update1 = Builders<BsonDocument>.Update.Set("TotalPowerConsumption", 0.0);
                    var result1 = await collection.UpdateManyAsync(filter1, update1);
                    LogHelper.Info($"Migration complete: Added TotalPowerConsumption field to {result1.ModifiedCount} device documents with default value 0.0");

                    // Find all documents without the ParentDeviceId field
                    var filter2 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("ParentDeviceId")
                    );
                    var update2 = Builders<BsonDocument>.Update.Set("ParentDeviceId", ObjectId.Empty);
                    var result2 = await collection.UpdateManyAsync(filter2, update2);
                    LogHelper.Info($"Migration complete: Added ParentDeviceId field to {result2.ModifiedCount} device documents with default value empty");

                    // Find all documents without the IsMeter field
                    var filter3 = Builders<BsonDocument>.Filter.Not(
                        Builders<BsonDocument>.Filter.Exists("IsMeter")
                    );
                    var update3 = Builders<BsonDocument>.Update.Set("IsMeter", false);
                    var result3 = await collection.UpdateManyAsync(filter3, update3);
                    LogHelper.Info($"Migration complete: Added IsMeter field to {result3.ModifiedCount} device documents with default value false");

                    // Update ParameterType field in device collection RegisterInfos
                    await UpdateRegisterInfosParameterTypeAsync(collection);
                    
                    // Update ParameterType field in device type collection RegisterInfos
                    var deviceTypeCollection = database.GetCollection<BsonDocument>("deviceTypes");
                    await UpdateDeviceTypeRegisterInfosParameterTypeAsync(deviceTypeCollection);
                    
                    LogHelper.Info("Database migration execution completed");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred during database migration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update ParameterType field in device collection RegisterInfos
        /// </summary>
        /// <param name="collection">Device collection</param>
        private static async Task UpdateRegisterInfosParameterTypeAsync(IMongoCollection<BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("Starting to update ParameterType field in device collection RegisterInfos...");
                
                // Find all documents containing RegisterInfos array
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
                            
                            // Check if ParameterType field is missing
                            if (!registerInfo.Contains("ParameterType"))
                            {
                                // Infer ParameterType based on register (point) name or other attributes
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
                
                LogHelper.Info($"Device collection migration complete: Updated RegisterInfos ParameterType field for {updatedCount} device documents");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while updating device collection RegisterInfos ParameterType: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update ParameterType field in device type collection RegisterInfos
        /// </summary>
        /// <param name="collection">Device type collection</param>
        private static async Task UpdateDeviceTypeRegisterInfosParameterTypeAsync(IMongoCollection<BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("Starting to update ParameterType field in device type collection RegisterInfos...");
                
                // Find all documents containing RegisterInfos array
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
                            
                            // Check if ParameterType field is missing
                            if (!registerInfo.Contains("ParameterType"))
                            {
                                // Infer ParameterType based on register (point) name or other attributes
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
                
                LogHelper.Info($"Device type collection migration complete: Updated RegisterInfos ParameterType field for {updatedCount} device type documents");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while updating device type collection RegisterInfos ParameterType: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Infer ParameterType based on register (point) information
        /// </summary>
        /// <param name="registerInfo">Register (point) information document</param>
        /// <returns>Inferred ParameterType</returns>
        private static string InferParameterTypeFromRegisterInfo(BsonDocument registerInfo)
        {
            try
            {
                // Get register (point) name
                string name = registerInfo.Contains("Name") && !registerInfo["Name"].IsBsonNull ? registerInfo["Name"].AsString : "";
                string enName = registerInfo.Contains("EnName") && !registerInfo["EnName"].IsBsonNull ? registerInfo["EnName"].AsString : "";
                string unit = registerInfo.Contains("Unit") && !registerInfo["Unit"].IsBsonNull ? registerInfo["Unit"].AsString : "";
                
                // Convert to lowercase for comparison
                string nameLower = name.ToLower();
                string enNameLower = enName.ToLower();
                string unitLower = unit.ToLower();
                
                // Infer parameter type based on name and unit
                if (nameLower.Contains("current") || enNameLower.Contains("current") || unitLower.Contains("a"))
                {
                    return "Current";
                }
                else if (nameLower.Contains("voltage") || enNameLower.Contains("voltage") || unitLower.Contains("v"))
                {
                    return "Voltage";
                }
                else if (nameLower.Contains("power") || enNameLower.Contains("power") || unitLower.Contains("w") || unitLower.Contains("kw"))
                {
                    return "Power";
                }
                else if (nameLower.Contains("energy consumption") || nameLower.Contains("electric energy") || enNameLower.Contains("energy") || unitLower.Contains("kwh"))
                {
                    return "Energy";
                }
                else if (nameLower.Contains("power factor") || enNameLower.Contains("powerfactor") || enNameLower.Contains("pf"))
                {
                    return "PowerFactor";
                }
                else if (nameLower.Contains("frequency") || enNameLower.Contains("frequency") || unitLower.Contains("hz"))
                {
                    return "Frequency";
                }
                else if (nameLower.Contains("temperature") || enNameLower.Contains("temperature") || unitLower.Contains("°c") || unitLower.Contains("c"))
                {
                    return "Temperature";
                }
                else if (nameLower.Contains("humidity") || enNameLower.Contains("humidity") || unitLower.Contains("%"))
                {
                    return "Humidity";
                }
                else if (nameLower.Contains("pressure") || enNameLower.Contains("pressure") || unitLower.Contains("pa") || unitLower.Contains("bar"))
                {
                    return "Pressure";
                }
                else if (nameLower.Contains("flow") || enNameLower.Contains("flow") || unitLower.Contains("m³") || unitLower.Contains("l"))
                {
                    return "Flow";
                }
                else if (nameLower.Contains("rotation speed") || enNameLower.Contains("speed") || unitLower.Contains("rpm"))
                {
                    return "Speed";
                }
                else if (nameLower.Contains("vibration") || enNameLower.Contains("vibration"))
                {
                    return "Vibration";
                }
                else if (nameLower.Contains("displacement") || enNameLower.Contains("displacement"))
                {
                    return "Displacement";
                }
                else if (nameLower.Contains("torque") || enNameLower.Contains("torque"))
                {
                    return "Torque";
                }
                else if (nameLower.Contains("level") || enNameLower.Contains("level"))
                {
                    return "Level";
                }
                else if (nameLower.Contains("switch") || nameLower.Contains("status") || enNameLower.Contains("status") || enNameLower.Contains("switch"))
                {
                    return "SwitchStatus";
                }
                else if (nameLower.Contains("running") || enNameLower.Contains("running"))
                {
                    return "RunningStatus";
                }
                else if (nameLower.Contains("fault") || enNameLower.Contains("fault"))
                {
                    return "FaultStatus";
                }
                else if (nameLower.Contains("communication") || enNameLower.Contains("communication"))
                {
                    return "CommunicationStatus";
                }
                else if (nameLower.Contains("unbalance") || enNameLower.Contains("unbalance"))
                {
                    return "Unbalance";
                }
                
                // Default return Other
                return "Other";
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while inferring ParameterType: {ex.Message}", ex);
                return "Other";
            }
        }
    }
}
