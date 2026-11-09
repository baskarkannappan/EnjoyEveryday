namespace EnjoyEveryday.Domain.Entities.Learning;

public class StoryChapter
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string PhaseTag { get; set; } = "";
    public string Duration { get; set; } = "5 min";
    public string Narrative { get; set; } = "";
    public string ChildStory { get; set; } = "";
    public string ChildrenAction { get; set; } = "";
    public string TeacherSpark { get; set; } = "";
    public string WonderPrompt { get; set; } = "";
    public string SvgSceneKey { get; set; } = "gathering";
    public string TeacherScript { get; set; } = "";
    public string StepIcon { get; set; } = "🌿";
}
