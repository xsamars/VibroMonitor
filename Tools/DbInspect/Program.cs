using Npgsql;
using System.Text.Json;

var cs = "Host=localhost;Port=5432;Database=vibromonitor;Username=postgres;Password=Rozh123srv";
using var conn = new NpgsqlConnection(cs);
conn.Open();

var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'equipmentpoints' ORDER BY ordinal_position;";
using var reader = cmd.ExecuteReader();
var list = new System.Collections.Generic.List<object>();
while (reader.Read())
{
    list.Add(new { Column = reader.GetString(0), Type = reader.GetString(1) });
}
Console.WriteLine(JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
