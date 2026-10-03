import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\test.csx'
with open(path, 'w', encoding='utf-8') as f:
    f.write('''
using System;
using System.IO;
using Npgsql;

var connStr = "Host=localhost;Database=enjoy_everyday;Username=postgres;Password=postgres";
// Let's just run a quick dapper-like query with Npgsql directly.
using var conn = new NpgsqlConnection(connStr);
conn.Open();

using var cmd = new NpgsqlCommand("SELECT id, name FROM classrooms", conn);
using var reader = cmd.ExecuteReader();
Console.WriteLine("CLASSROOMS:");
while (reader.Read()) {
    Console.WriteLine(reader.GetGuid(0) + " - " + reader.GetString(1));
}
reader.Close();

using var cmd2 = new NpgsqlCommand("SELECT id, experience_id, classroom_id, scheduled_date, time_of_day FROM experience_schedules", conn);
using var reader2 = cmd2.ExecuteReader();
Console.WriteLine("SCHEDULES:");
while (reader2.Read()) {
    Console.WriteLine(reader2.GetGuid(0) + " - Exp: " + reader2.GetGuid(1) + " - Class: " + reader2.GetGuid(2) + " - Date: " + reader2.GetDateTime(3).ToString("yyyy-MM-dd") + " - " + reader2.GetString(4));
}
reader2.Close();
''')
