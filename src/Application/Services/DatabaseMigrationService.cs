using NextAdmin.Log;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextAdmin.Application.Services
{
    /// <summary>
    /// Database Migration Service
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        public DatabaseMigrationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Execute database migrations
        /// </summary>
        /// <returns>Migration result</returns>
        public async Task<bool> ExecuteMigrationsAsync()
        {
            try
            {
                LogHelper.Info("Starting database migration service...");
                
                using var scope = _serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                
                // Directly call migration logic to avoid cross-project dependencies
                await ExecuteMigrationsAsync(database);
                
                LogHelper.Info("Database migration service completed");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while executing database migration service: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Check if migration is needed
        /// </summary>
        /// <returns>Whether migration is needed</returns>
        public async Task<bool> NeedsMigrationAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                
                // Check if documents in device collection are missing new fields
                var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                
                // Check if any documents are missing the TotalPowerConsumption field
                var filter1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("TotalPowerConsumption")
                );
                var count1 = await collection.CountDocumentsAsync(filter1);
                
                // Check if any documents are missing the ParentDeviceId field
                var filter2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ParentDeviceId")
                );
                var count2 = await collection.CountDocumentsAsync(filter2);
                
                // Check if any documents are missing the IsMeter field
                var filter3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("IsMeter")
                );
                var count3 = await collection.CountDocumentsAsync(filter3);
                
                // Check if any RegisterInfos are missing the ParameterType field
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
                                break; // One missing is enough
                            }
                        }
                    }
                }

                // Check if any documents in device data collection are missing ProjectId field
                var deviceDataCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceDatas");
                var filter5 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ProjectId")
                );
                var count5 = await deviceDataCollection.CountDocumentsAsync(filter5);
                
                bool needsMigration = count1 > 0 || count2 > 0 || count3 > 0 || count4 > 0 || count5 > 0;
                
                if (needsMigration)
                {
                    LogHelper.Info($"Documents requiring migration detected: {count1} documents missing TotalPowerConsumption field, {count2} documents missing ParentDeviceId field, {count3} documents missing IsMeter field, {count4} RegisterInfos missing ParameterType field, {count5} device data missing ProjectId field");
                }
                else
                {
                    LogHelper.Info("Database migration check completed, no migration needed");
                }
                
                return needsMigration;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while checking database migration requirements: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Execute database migrations
        /// </summary>
        /// <param name="database">MongoDB database instance</param>
        private static async Task ExecuteMigrationsAsync(IMongoDatabase database)
        {
            try
            {
                if (database != null)
                {
                    LogHelper.Info("Starting database migration...");
                    
                    var collection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                    
                    // Find all documents without TotalPowerConsumption field
                    var filter1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("TotalPowerConsumption")
                    );
                    var update1 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("TotalPowerConsumption", 0.0);
                    var result1 = await collection.UpdateManyAsync(filter1, update1);
                    LogHelper.Info($"Migration completed: Added TotalPowerConsumption field to {result1.ModifiedCount} device documents with default value 0.0");

                    // Find all documents without ParentDeviceId field
                    var filter2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ParentDeviceId")
                    );
                    var update2 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("ParentDeviceId", MongoDB.Bson.ObjectId.Empty);
                    var result2 = await collection.UpdateManyAsync(filter2, update2);
                    LogHelper.Info($"Migration completed: Added ParentDeviceId field to {result2.ModifiedCount} device documents with default value empty");

                    // Find all documents without IsMeter field
                    var filter3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                        MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("IsMeter")
                    );
                    var update3 = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("IsMeter", false);
                    var result3 = await collection.UpdateManyAsync(filter3, update3);
                    LogHelper.Info($"Migration completed: Added IsMeter field to {result3.ModifiedCount} device documents with default value false");

                    // Update ParameterType field in RegisterInfos for device collection
                    await UpdateRegisterInfosParameterTypeAsync(collection);
                    
                    // Update ParameterType field in RegisterInfos for device type collection
                    var deviceTypeCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceTypes");
                    await UpdateDeviceTypeRegisterInfosParameterTypeAsync(deviceTypeCollection);
                    
                    // Add ProjectId field to device data
                    await AddProjectIdToDeviceDataAsync(database);
                    
                    LogHelper.Info("Database migration execution completed");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred during database migration execution: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update ParameterType field in RegisterInfos for device collection
        /// </summary>
        /// <param name="collection">Device collection</param>
        private static async Task UpdateRegisterInfosParameterTypeAsync(IMongoCollection<MongoDB.Bson.BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("Starting to update ParameterType field in RegisterInfos for device collection...");
                
                // Find all documents containing RegisterInfos array
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
                            var updateFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
                            await collection.UpdateOneAsync(updateFilter, update);
                            updatedCount++;
                        }
                    }
                }
                
                LogHelper.Info($"Device collection migration completed: Updated RegisterInfos ParameterType field for {updatedCount} device documents");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while updating RegisterInfos ParameterType for device collection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update ParameterType field in RegisterInfos for device type collection
        /// </summary>
        /// <param name="collection">Device type collection</param>
        private static async Task UpdateDeviceTypeRegisterInfosParameterTypeAsync(IMongoCollection<MongoDB.Bson.BsonDocument> collection)
        {
            try
            {
                LogHelper.Info("Starting to update ParameterType field in RegisterInfos for device type collection...");
                
                // Find all documents containing RegisterInfos array
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
                            var updateFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", document["_id"]);
                            var update = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Update.Set("RegisterInfos", registerInfosArray);
                            await collection.UpdateOneAsync(updateFilter, update);
                            updatedCount++;
                        }
                    }
                }
                LogHelper.Info($"Device type collection migration completed: Updated RegisterInfos ParameterType field for {updatedCount} device type documents");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while updating RegisterInfos ParameterType for device type collection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Infer ParameterType based on register (point) information
        /// </summary>
        /// <param name="registerInfo">Register (point) information document</param>
        /// <returns>Inferred ParameterType</returns>
        private static string InferParameterTypeFromRegisterInfo(MongoDB.Bson.BsonDocument registerInfo)
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
                else if (nameLower.Contains("energy") || nameLower.Contains("consumption") || enNameLower.Contains("energy") || unitLower.Contains("kwh"))
                {
                    return "Energy";
                }
                else if (nameLower.Contains("powerfactor") || enNameLower.Contains("powerfactor") || enNameLower.Contains("pf"))
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
                else if (nameLower.Contains("speed") || enNameLower.Contains("speed") || unitLower.Contains("rpm"))
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

        /// <summary>
        /// Add ProjectId field to device data
        /// </summary>
        /// <param name="database">MongoDB database instance</param>
        private static async Task AddProjectIdToDeviceDataAsync(IMongoDatabase database)
        {
            try
            {
                LogHelper.Info("Starting to add ProjectId field to device data...");
                
                var deviceDataCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("deviceDatas");
                var deviceCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("devices");
                
                // Find all device data documents without ProjectId field
                var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Not(
                    MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Exists("ProjectId")
                );
                
                var deviceDataDocuments = await deviceDataCollection.Find(filter).ToListAsync();
                LogHelper.Info($"Found {deviceDataDocuments.Count} device data documents that need ProjectId field added");
                
                int updatedCount = 0;
                int errorCount = 0;
                
                foreach (var deviceDataDoc in deviceDataDocuments)
                {
                    try
                    {
                        // Get device ID
                        if (!deviceDataDoc.Contains("DeviceId"))
                        {
                            LogHelper.Warn($"Device data document missing DeviceId field, skipping: {deviceDataDoc["_id"]}");
                            errorCount++;
                            continue;
                        }
                        
                        var deviceId = deviceDataDoc["DeviceId"].AsObjectId;
                        
                        // Find corresponding device document by device ID
                        var deviceFilter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", deviceId);
                        var deviceDoc = await deviceCollection.Find(deviceFilter).FirstOrDefaultAsync();
                        
                        if (deviceDoc == null)
                        {
                            LogHelper.Warn($"Device document with ID {deviceId} not found, skipping");
                            errorCount++;
                            continue;
                        }
                        
                        // Get device's ProjectId
                        ObjectId projectId = MongoDB.Bson.ObjectId.Empty;
                        if (deviceDoc.Contains("ProjectId"))
                        {
                            projectId = deviceDoc["ProjectId"].AsObjectId;
                        }
                        else
                        {
                            LogHelper.Warn($"Device document {deviceId} missing ProjectId field, using default value");
                        }
                        
                        // Update device data document
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
                        LogHelper.Error($"Error occurred while updating device data document: {ex.Message}", ex);
                        errorCount++;
                    }
                }
                
                LogHelper.Info($"Device data ProjectId field migration completed: Successfully updated {updatedCount} documents, {errorCount} errors");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error occurred while adding ProjectId field to device data: {ex.Message}", ex);
            }
        }
    }
}
