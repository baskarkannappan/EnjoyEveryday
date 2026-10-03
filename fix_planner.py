import os

path = r'c:\MyDrive\ProjectDrive\EnjoyEveryDay\src\EnjoyEveryday.Admin.Web\Components\Pages\MonthlyPlanner.razor'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the GetSchedulesForDay call in the loop
old_loop_init = '''                    var iterDay = day;
                    var currentDate = new DateOnly(_currentDate.Year, _currentDate.Month, iterDay);
                    var dailySchedules = GetSchedulesForDay(currentDate, "Morning").ToList();'''

new_loop_init = '''                    var iterDay = day;
                    var currentDate = new DateOnly(_currentDate.Year, _currentDate.Month, iterDay);
                    var dailySchedules = _schedules.Where(s => s.ScheduledDate == currentDate).OrderBy(s => s.TimeOfDay).ToList();'''

content = content.replace(old_loop_init, new_loop_init)

# Replace the display logic to show TimeOfDay instead of schedule.Status (or alongside it)
old_schedule_display = '''                                    <span>
                                        @GetExperienceType(experience) &middot; @schedule.Status
                                    </span>'''

new_schedule_display = '''                                    <span>
                                        @GetExperienceType(experience) &middot; @schedule.TimeOfDay
                                    </span>'''

content = content.replace(old_schedule_display, new_schedule_display)

# Update OpenScheduleDialog to default to Morning but it doesn't matter since they can pick in the modal
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
