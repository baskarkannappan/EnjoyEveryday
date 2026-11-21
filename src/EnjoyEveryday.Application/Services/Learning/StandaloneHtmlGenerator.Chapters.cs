using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;

namespace EnjoyEveryday.Application.Services.Learning;

public static partial class StandaloneHtmlGenerator
{
    public static string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Wonder Experience";
        var t = title.Trim();
        // Remove repeated prefixes if compounded
        while (t.StartsWith("The Playful World of The Playful World of", StringComparison.OrdinalIgnoreCase) ||
               t.StartsWith("The Playful World of The Playful World Of", StringComparison.OrdinalIgnoreCase))
        {
            t = "The Playful World of " + t.Substring("The Playful World of The Playful World of".Length).Trim();
        }
        return t;
    }

    public static string CleanDescription(string? desc, string cleanTitle)
    {
        if (string.IsNullOrWhiteSpace(desc))
            return "Children embark on an imaginative, playful quest turning tactile curiosity into creative storytelling, hands-on construction, and joyful collaboration.";
        var d = desc.Trim();
        d = Regex.Replace(d, @"the playful world of the playful world of", "the playful world of", RegexOptions.IgnoreCase);
        return d;
    }

    public static List<StoryChapter> BuildChapters(Experience? exp, ExperienceDna dna)
    {
        var chapters = new List<StoryChapter>();
        var cleanTitle = CleanTitle(exp?.Title);
        var mission = !string.IsNullOrWhiteSpace(exp?.Description) ? CleanDescription(exp.Description, cleanTitle) : "Discovering new wonders through playful hands and curious eyes.";
        var challenge = !string.IsNullOrWhiteSpace(dna.Challenge) ? dna.Challenge : "Explore materials, test ideas, and discover something surprising together.";
        // Clean double prefixes in challenge
        challenge = Regex.Replace(challenge, @"our the playful world of", "our playful world of", RegexOptions.IgnoreCase);
        var create = !string.IsNullOrWhiteSpace(dna.Create) ? dna.Create : "Children engage hands-on with organic shapes, sensory textures, and spontaneous creation.";
        var magic = !string.IsNullOrWhiteSpace(dna.MagicMoment) ? dna.MagicMoment : "A sudden flash of excitement when materials combine in an unexpected, beautiful way.";
        var accomplishment = !string.IsNullOrWhiteSpace(dna.Accomplishment) ? dna.Accomplishment : "Children beam with pride as they showcase their creation to friends.";
        var reflection = !string.IsNullOrWhiteSpace(dna.Reflection) ? dna.Reflection : "Gathering back in a gentle circle to share what we learned and felt today.";

        if (dna.ExecutionGuide?.Phases != null && dna.ExecutionGuide.Phases.Count >= 3)
        {
            var phases = dna.ExecutionGuide.Phases;
            int count = phases.Count;

            for (int i = 0; i < count; i++)
            {
                var p = phases[i];
                string sceneKey;
                string icon;
                string narrative;
                string childStory;
                string teacherScript;
                string wonderPrompt;

                var pChildren = !string.IsNullOrWhiteSpace(p.ChildrenDo) ? p.ChildrenDo.Trim().TrimEnd('.') + "." : "";
                var pTeacher = !string.IsNullOrWhiteSpace(p.TeacherDoes) ? p.TeacherDoes.Trim().TrimEnd('.') + "." : "";
                var pName = !string.IsNullOrWhiteSpace(p.Name) ? p.Name.Trim() : $"Step {i + 1}";
                var isAdventureOrQuest = cleanTitle.IndexOf("quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("rescue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("adventure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("explore", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("sea", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("fish", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         cleanTitle.IndexOf("reef", StringComparison.OrdinalIgnoreCase) >= 0;

                if (i == 0)
                {
                    sceneKey = "gathering";
                    icon = "🌿";
                    childStory = $"Welcome, curious explorers! Today our classroom is waiting like an open canvas for an exciting adventure all about {cleanTitle.ToLowerInvariant()}. Reach out with your hands, feel the textures, and let's imagine what wonderful secrets we can discover together!";
                    narrative = !string.IsNullOrWhiteSpace(pTeacher)
                        ? $"Welcome the children warmly into {pName}. {pTeacher} Introduce the materials and invite curious hands and eyes to make their first discoveries."
                        : $"Welcome the children warmly into {pName}. Introduce materials and invite open exploration with gentle sensory curiosity.";

                    teacherScript = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.TeacherIntroduction)
                        ? dna.ExecutionGuide.TeacherIntroduction
                        : (!string.IsNullOrWhiteSpace(pTeacher) && pTeacher.Contains("\"")
                            ? pTeacher
                            : $"\"Welcome, explorers! Look at all the wonderful colors we see today. What do your eyes and fingers notice?\"");

                    wonderPrompt = !string.IsNullOrWhiteSpace(challenge)
                        ? $"Can you guess what exciting quest is waiting for us today? {challenge}"
                        : "What curious secrets do you think are waiting to be discovered?";
                }
                else if (i == count - 1)
                {
                    sceneKey = "reflection";
                    icon = "🌟";
                    childStory = $"Step softly, little artists! Look at the wonderful world our hands created together across the whole room. Each colorful mark tells a special story, and our classroom is shining with creativity and pride!";
                    narrative = "Gather the children together in a gentle circle for reflection. Guide a collaborative gallery walk where each child can share the story behind their marks, celebrating how everyone's creativity transformed the room.";

                    if (!string.IsNullOrWhiteSpace(pTeacher) && pTeacher.Contains("\""))
                    {
                        teacherScript = pTeacher;
                    }
                    else if (!string.IsNullOrWhiteSpace(reflection) && reflection.Contains("?"))
                    {
                        teacherScript = $"\"Let's celebrate our wonderful teamwork! {reflection}\"";
                    }
                    else
                    {
                        teacherScript = $"\"Look at what we accomplished together! Who would like to share their favorite discovery from today?\"";
                    }

                    wonderPrompt = !string.IsNullOrWhiteSpace(reflection)
                        ? reflection
                        : (!string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What was your favorite part of today's adventure?");
                }
                else if (count == 3) // middle phase for a 3-step experience
                {
                    sceneKey = isAdventureOrQuest ? "challenge" : "explore";
                    icon = isAdventureOrQuest ? "🔍" : "🎨";
                    childStory = $"Swish, glide, and tap! Little fingers create swirling patterns, rainbow paths, and friendly shapes. Friends share their materials, smiling and pointing with joy as our giant shared masterpiece comes alive!";
                    narrative = "Children dive into creative hands-on exploration. Circulate to encourage cooperative sharing, observe fine-motor grips, and offer descriptive, encouraging prompts.";

                    if (!string.IsNullOrWhiteSpace(pTeacher) && pTeacher.Contains("\""))
                    {
                        teacherScript = pTeacher;
                    }
                    else if (!string.IsNullOrWhiteSpace(challenge))
                    {
                        teacherScript = $"\"{challenge.TrimEnd('?')}. Take your time, look closely, and let's see what you find!\"";
                    }
                    else
                    {
                        teacherScript = $"\"Look at what's happening right before our eyes! Tell me about what you are discovering.\"";
                    }

                    wonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask)
                        ? dna.ExecutionGuide.Ask
                        : (!string.IsNullOrWhiteSpace(challenge) ? challenge : "What clues are you using to make your discoveries?");
                }
                else if (i == 1 && count >= 4)
                {
                    sceneKey = "challenge";
                    icon = "🔍";
                    childStory = $"Can we work together on today's quest? {challenge}. Let's test our ideas and see what surprises we can build!";
                    narrative = $"Pose the gentle wonder challenge: {challenge}. There are no right or wrong answers—only open invitations to explore, hypothesize, and build.";

                    teacherScript = !string.IsNullOrWhiteSpace(challenge)
                        ? $"\"I wonder what will happen if we take on this challenge together? {challenge}\""
                        : $"\"I wonder what secret discoveries we can make if we work together? What happens if we try this?\"";

                    wonderPrompt = challenge;
                }
                else if (i == 2 && count >= 4)
                {
                    sceneKey = "explore";
                    icon = "🎨";
                    childStory = $"Hands busy, minds soaring! Watch how colors and textures mix and transform in front of us. Every idea is a wonderful adventure!";
                    narrative = "Deep creative flow takes over. Children explore, sort, and collaborate with focused, joyful energy.";

                    teacherScript = $"\"You are making such exciting discoveries! Tell me about what you found.\"";
                    wonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What does your discovery tell us?";
                }
                else
                {
                    sceneKey = "magic";
                    icon = "✨";
                    childStory = "Look at that magical surprise! Our ideas have connected into something brand new and delightful that makes everyone smile!";
                    narrative = "A breakthrough moment of discovery. Encourage children to share what surprised them and celebrate their self-guided breakthroughs.";
                    teacherScript = $"\"You discovered that all on your own! How did that happen? Can you show our friends?\"";
                    wonderPrompt = "Did you see that? What made it happen? Can we do it again?";
                }

                chapters.Add(new StoryChapter
                {
                    Number = i + 1,
                    Title = string.IsNullOrWhiteSpace(p.Name) ? $"Step {i + 1}" : p.Name,
                    PhaseTag = i == 0 ? "Step 1: Setup & Invitation" : i == count - 1 ? $"Step {i + 1}: Reflection & Sharing" : $"Step {i + 1}: Hands-On Action",
                    Duration = p.DurationMinutes > 0 ? $"{p.DurationMinutes} min" : "5 min",
                    Narrative = narrative,
                    ChildStory = childStory,
                    ChildrenAction = string.IsNullOrWhiteSpace(p.ChildrenDo) ? "Children explore with open hands and focused curiosity." : p.ChildrenDo,
                    TeacherSpark = string.IsNullOrWhiteSpace(p.TeacherDoes) ? "Observe and offer open-ended support when invited." : p.TeacherDoes,
                    WonderPrompt = wonderPrompt,
                    SvgSceneKey = sceneKey,
                    TeacherScript = teacherScript,
                    StepIcon = icon
                });
            }
        }
        else
        {
            chapters.Add(new StoryChapter
            {
                Number = 1,
                Title = "The Gathering & Setup Invitation",
                PhaseTag = "Step 1: Setup & Invitation",
                Duration = "5 min",
                Narrative = "Prepare the low circle table or exploration rug. Place tactile elements within reach. Children gather closely to inspect the materials with open curiosity.",
                ChildStory = $"Welcome, curious explorers! Today an exciting mystery is waiting for us all about {cleanTitle.ToLowerInvariant()}. Come feel the textures and take a gentle explorer breath together.",
                ChildrenAction = "Children gather around the sensory rug, feeling the smooth textures and whispering guesses about today's mystery.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.TeacherIntroduction) ? dna.ExecutionGuide.TeacherIntroduction : "Present the mystery basket with calm excitement. Ask: 'What curious secrets are hiding inside?'",
                WonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What does this feel like in your hands? Have you ever seen this before?",
                SvgSceneKey = "gathering",
                TeacherScript = "\"Come gather close, little explorers! What do your fingers notice when you touch these materials?\"",
                StepIcon = "🌿"
            });

            chapters.Add(new StoryChapter
            {
                Number = 2,
                Title = "The Wonder Challenge & Inquiry",
                PhaseTag = "Step 2: The Inquiry",
                Duration = "5 min",
                Narrative = $"Pose the gentle challenge: {challenge}. There are no right or wrong answers—only invitations to experiment, hypothesize, and build.",
                ChildStory = $"Can we work as a team to solve our mission? {challenge}. Let's test our ideas and see what surprises we can build!",
                ChildrenAction = "Children examine their tools, turn them over in their fingers, and whisper ideas to their neighbor with joyful energy.",
                TeacherSpark = "Frame the challenge as an adventure. Encourage each child that every idea is valuable and worth trying.",
                WonderPrompt = "How do you think we can make this work? What if we try it together?",
                SvgSceneKey = "challenge",
                TeacherScript = "\"I wonder what will happen if we test these ideas together? What does your curiosity tell you?\"",
                StepIcon = "🔍"
            });

            chapters.Add(new StoryChapter
            {
                Number = 3,
                Title = "Active Hands-On Play & Construction",
                PhaseTag = "Step 3: Deep Flow",
                Duration = "10 min",
                Narrative = $"The room fills with the happy hum of active creation. {create}. Little hands mold, balance, test, and arrange materials into brand new worlds.",
                ChildStory = $"Swish, pat, and build! Little hands create swirling paths and towers. Friends share supplies, smiling as our shared world takes shape!",
                ChildrenAction = "Deeply engaged in flow state: sorting colors, building structures, testing balance, and sharing supplies kindly.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Watch) ? dna.ExecutionGuide.Watch : "Observe problem-solving moments. Notice who collaborates and who finds a peaceful solo focus.",
                WonderPrompt = "Look what happens when these two come together! What story is your creation telling?",
                SvgSceneKey = "explore",
                TeacherScript = "\"Look at that balance! Tell me what you're imagining as your hands build this.\"",
                StepIcon = "🎨"
            });

            chapters.Add(new StoryChapter
            {
                Number = 4,
                Title = "The Magic Moment & Discovery",
                PhaseTag = "Step 4: The Discovery",
                Duration = "5 min",
                Narrative = $"Encourage children to share their breakthrough: {magic}. A shared ripple of delight touches every heart in the room.",
                ChildStory = "Look at that magical surprise! Our ideas have connected into something brand new that makes everyone smile with wonder!",
                ChildrenAction = "Eyes widen with excitement, pointing out their breakthrough to friends with wide, luminous smiles.",
                TeacherSpark = "Celebrate the discovery immediately! Say: 'You discovered that all on your own! How did that happen?'",
                WonderPrompt = "Did you see that? What made it happen? Can we do it again?",
                SvgSceneKey = "magic",
                TeacherScript = "\"You discovered that all on your own! Look at that wonderful breakthrough!\"",
                StepIcon = "✨"
            });

            chapters.Add(new StoryChapter
            {
                Number = 5,
                Title = "Pride, Reflection & Story Sharing",
                PhaseTag = "Step 5: Reflection",
                Duration = "5 min",
                Narrative = $"{accomplishment}. Each child holds up their triumph with gentle pride. {reflection}. The memories of today will stay warmly in our hearts.",
                ChildStory = "Look at what we accomplished together! Step softly and admire our creations—our whole room is glowing with pride and creative wonder.",
                ChildrenAction = "Children proudly present their work, explain their creative decisions, and help tidy up their sanctuary.",
                TeacherSpark = "Capture the moment with a warm photo or learning note. Guide a closing gratitude breath for our journey together.",
                WonderPrompt = "What was your favorite part of today's adventure? Who would you like to share this story with?",
                SvgSceneKey = "reflection",
                TeacherScript = "\"Let's celebrate each of our creations. What was your favorite surprise today?\"",
                StepIcon = "🌟"
            });
        }

        return chapters;
    }

}
