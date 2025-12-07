namespace NextAdmin.Common.Constants
{
    public static class SystemConstants
    {
        public const string DefaultConnectionString = "mongodb://localhost:27017";
        public const string DatabaseName = "NextAdmin";
        
        public static class Vehicle
        {
            public const string StatusAvailable = "Available";
            public const string StatusBusy = "Busy";
            public const string StatusCharging = "Charging";
            public const string StatusMaintenance = "Maintenance";
            public const string StatusError = "Error";
            
            public const string TypeAGV = "AGV";
            public const string TypeForklift = "Forklift";
            public const string TypeTugger = "Tugger";
        }
        
        public static class Task
        {
            public const string StatusPending = "Pending";
            public const string StatusInProgress = "InProgress";
            public const string StatusCompleted = "Completed";
            public const string StatusFailed = "Failed";
            public const string StatusCancelled = "Cancelled";
            
            public const string TypeTransport = "Transport";
            public const string TypeCharging = "Charging";
            public const string TypeMaintenance = "Maintenance";
        }
        
        public static class Path
        {
            public const string StatusAvailable = "Available";
            public const string StatusOccupied = "Occupied";
            public const string StatusBlocked = "Blocked";
            
            public const string TypeNormal = "Normal";
            public const string TypeEmergency = "Emergency";
            public const string TypeCharging = "Charging";
        }
        
        public static class Track
        {
            public const string StatusAvailable = "Available";
            public const string StatusOccupied = "Occupied";
            public const string StatusMaintenance = "Maintenance";
            
            public const string TypeNormal = "Normal";
            public const string TypeCharging = "Charging";
            public const string TypeEmergency = "Emergency";
        }
        
        public static class ChargingStation
        {
            public const string StatusAvailable = "Available";
            public const string StatusOccupied = "Occupied";
            public const string StatusMaintenance = "Maintenance";
            public const string StatusError = "Error";
        }
        
        public static class TrackMap
        {
            public const string StatusActive = "Active";
            public const string StatusInactive = "Inactive";
            public const string StatusMaintenance = "Maintenance";
        }
        
        public static class ErrorCodes
        {
            public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
            public const string TaskNotFound = "TASK_NOT_FOUND";
            public const string PathNotFound = "PATH_NOT_FOUND";
            public const string TrackNotFound = "TRACK_NOT_FOUND";
            public const string ChargingStationNotFound = "CHARGING_STATION_NOT_FOUND";
            public const string IndoorMapNotFound = "INDOOR_MAP_NOT_FOUND";
            public const string InvalidOperation = "INVALID_OPERATION";
            public const string SystemError = "SYSTEM_ERROR";
        }
    }
} 
