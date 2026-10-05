using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ExperienceDna
{
    public int AgeMin { get; set; } = 3;
    public int AgeMax { get; set; } = 5;
    public int GroupSizeMin { get; set; } = 4;
    public int GroupSizeMax { get; set; } = 10;
    public string Environment { get; set; } = "Indoor / Outdoor";
    
    public string Challenge { get; set; } = string.Empty;
    public string Together { get; set; } = string.Empty;
    public string Create { get; set; } = string.Empty;
    public string ChildChoice { get; set; } = string.Empty;
    public string MagicMoment { get; set; } = string.Empty;
    public string Accomplishment { get; set; } = string.Empty;
    public string Reflection { get; set; } = string.Empty;
    public string TeacherGuidance { get; set; } = string.Empty;

    public List<string> Materials { get; set; } = new();
}
