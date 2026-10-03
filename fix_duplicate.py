import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\Experiences.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace duplicated declarations
# We'll just remove one instance if it's duplicated.
if content.count('private bool _isViewModalOpen = false;') > 1:
    content = content.replace('private bool _isViewModalOpen = false;', '', 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
