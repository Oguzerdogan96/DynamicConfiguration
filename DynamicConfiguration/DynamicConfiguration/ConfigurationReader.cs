using Microsoft.Extensions.Caching.Memory;
using System.Data.SqlClient;

namespace DynamicConfiguration
{
    public class ConfigurationReader
    {
        private readonly string _applicationName;
        private readonly string _connectionString;
        private readonly int _refreshTimerIntervalInMs;
        private readonly Timer _timer;
        private readonly IMemoryCache _cache;
        private readonly object _lock = new();
        private readonly Dictionary<int, ConfigEntry> _entries = new();

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            _applicationName = applicationName;
            _connectionString = connectionString;
            _refreshTimerIntervalInMs = refreshTimerIntervalInMs;
            _cache = new MemoryCache(new MemoryCacheOptions());
            LoadConfigurationsAsync().Wait();
            _timer = new Timer(async _ => await LoadConfigurationsAsync(), null, refreshTimerIntervalInMs, refreshTimerIntervalInMs);
        }

        public async Task LoadConfigurationsAsync()
        {
            lock (_lock)
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    connection.Open();

                    var command = new SqlCommand(@"
                        SELECT Id, Name, Type, Value, IsActive, ApplicationName 
                        FROM ConfigurationSettings 
                        WHERE IsActive = 1 AND ApplicationName = @ApplicationName", connection);
                    command.Parameters.AddWithValue("@ApplicationName", _applicationName);

                    using var reader = command.ExecuteReader();
                    _entries.Clear();

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string type = reader.GetString(2);
                        string value = reader.GetString(3);
                        bool isActive = reader.GetBoolean(4);
                        string applicationName = reader.GetString(5);

                        var config = new ConfigEntry
                        {
                            Id = id,
                            Name = name,
                            Type = type,
                            Value = value,
                            IsActive = isActive,
                            ApplicationName = applicationName
                        };

                        _entries[id] = config;
                        _cache.Set(id.ToString(), config);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ConfigurationReader] Hata: {ex.Message}");
                }
            }
        }

        public class ConfigEntry
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public bool IsActive { get; set; }
            public string ApplicationName { get; set; }
        }

        public bool GetValue<T>(string key, out T value)
        {
            value = default;
            if (_cache.TryGetValue(key, out ConfigEntry entry))
            {
                try
                {
                    value = (T)Convert.ChangeType(entry.Value, typeof(T));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<bool> UpdateValueAsync(int id, string newName, string newType, string newValue, bool newIsActive, string newApplicationName)
        {
            lock (_lock)
            {
                if (!_entries.ContainsKey(id))
                    return false;

                var entry = _entries[id];

                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    connection.Open();

                    var command = new SqlCommand(@"
                        UPDATE ConfigurationSettings
                        SET Name = @Name ,Type = @Type, Value = @Value ,IsActive=@IsActive ,ApplicationName=@ApplicationName
                        WHERE Id = @Id", connection);

                    command.Parameters.AddWithValue("@Name", newName);
                    command.Parameters.AddWithValue("@Type", newType);
                    command.Parameters.AddWithValue("@Value", newValue);
                    command.Parameters.AddWithValue("@IsActive", newIsActive);
                    command.Parameters.AddWithValue("@ApplicationName", newApplicationName);
                    command.Parameters.AddWithValue("@Id", id);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                        return false;

                    entry.Name = newName;
                    entry.Type = newType;
                    entry.Value = newValue;
                    entry.IsActive = newIsActive;
                    entry.ApplicationName = newApplicationName;
                    _entries[id] = entry;
                    _cache.Set(id.ToString(), entry);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UpdateValue] Hata: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            lock (_lock)
            {
                if (!_entries.ContainsKey(id))
                    return false;

                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    connection.Open();

                    var command = new SqlCommand(@"
                        UPDATE ConfigurationSettings
                        SET IsActive = 0
                        WHERE Id = @Id", connection);

                    command.Parameters.AddWithValue("@Id", id);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                        return false;

                    _entries.Remove(id);
                    _cache.Remove(id.ToString());

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Delete] Hata: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<bool> InsertValueAsync(string name, string type, string value, bool isActive, string applicationName)
        {
            lock (_lock)
            {
                try
                {
                    using var connection = new SqlConnection(_connectionString);
                    connection.Open();

                    var command = new SqlCommand(@"
                        INSERT INTO ConfigurationSettings (Name, Type, Value, IsActive, ApplicationName)
                        VALUES (@Name, @Type, @Value, @IsActive, @ApplicationName);
                        SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Type", type);
                    command.Parameters.AddWithValue("@Value", value);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.Parameters.AddWithValue("@ApplicationName", applicationName);

                    var insertedIdObj = command.ExecuteScalar();
                    if (insertedIdObj == null || insertedIdObj == DBNull.Value)
                        return false;

                    int insertedId = Convert.ToInt32(insertedIdObj);

                    var entry = new ConfigEntry
                    {
                        Id = insertedId,
                        Name = name,
                        Type = type,
                        Value = value,
                        IsActive = isActive,
                        ApplicationName = applicationName
                    };

                    _entries[insertedId] = entry;
                    _cache.Set(insertedId.ToString(), entry);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[InsertValue] Hata: {ex.Message}");
                    return false;
                }
            }
        }

        public Task<Dictionary<int, object>> GetAllAsync()
        {
            lock (_lock)
            {
                var result = _entries.ToDictionary(
                    pair => pair.Key,
                    pair => new
                    {
                        Id = pair.Value.Id,
                        Name = pair.Value.Name,
                        Type = pair.Value.Type,
                        Value = pair.Value.Value,
                        IsActive = pair.Value.IsActive,
                        ApplicationName = pair.Value.ApplicationName
                    } as object
                );

                return Task.FromResult(result);
            }
        }
    }
}
