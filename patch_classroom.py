import os

base_dir = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay'
razor_path = os.path.join(base_dir, r'src\EnjoyEveryday.Daycare.Web\Components\Pages\Attendance\ClassroomAttendance.razor')

with open(razor_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace dynamic usages
content = content.replace("private IEnumerable<dynamic> Children = new List<dynamic>();", "private IEnumerable<PersonDto> Children = new List<PersonDto>();")
content = content.replace("private IEnumerable<dynamic> Teachers = new List<dynamic>();", "private IEnumerable<PersonDto> Teachers = new List<PersonDto>();")

# Replace QueryAsync types
content = content.replace("await conn.QueryAsync<dynamic>(\"SELECT id as Id", "await conn.QueryAsync<PersonDto>(\"SELECT id as Id")
content = content.replace("await conn.QueryAsync<dynamic>(@\"\n                SELECT u.id as Id", "await conn.QueryAsync<PersonDto>(@\"\n                SELECT u.id as Id")

# Add PersonDto class at the end
if "public class PersonDto" not in content:
    content = content.replace("}", "}\n\n    public class PersonDto\n    {\n        public Guid Id { get; set; }\n        public string FirstName { get; set; } = \"\";\n        public string LastName { get; set; } = \"\";\n    }\n}")

with open(razor_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("ClassroomAttendance patched for strongly typed query results.")
