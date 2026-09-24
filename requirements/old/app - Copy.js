// ============================================================
// ENJOYEVERYDAY
// MULTI-TENANT WIREFRAME
// ============================================================


// ------------------------------------------------------------
// TENANT DATA
// ------------------------------------------------------------

const tenants = {

    littleStars: {

        id: "littleStars",

        name: "Little Stars Daycare",

        icon: "🌱",

        branch: "Main Branch",

        branches: 3,

        childrenCount: 126,

        classroomsCount: 6,

        teachersCount: 14,

        currentClassroom: "Bluebirds",

        ageGroup: "3–4 years",

        classroomChildren: 18,

        weather: "Rainy",

        environment: "Indoor",

        energy: "Energetic",

        teacher: "Priya",

        teacherRole: "Lead Teacher",

        materials: [
            "Blocks",
            "Paper",
            "Crayons",
            "Cardboard"
        ],

        spaces: [
            "Main Classroom",
            "Reading Corner",
            "Creative Table"
        ],


        todayExperiences: [

            {
                id: "city",
                title: "Our Little City",
                type: "Create",
                duration: "25 min",
                description:
                    "Build a city together and discover what a city needs.",
                magic:
                    "The children decide what belongs in their city."
            },

            {
                id: "bug",
                title: "Tiny Bug Hunt",
                type: "Explore",
                duration: "15 min",
                description:
                    "Look closely for tiny creatures hiding around the classroom.",
                magic:
                    "Children discover something they didn't expect."
            },

            {
                id: "frog",
                title: "Frog Rescue",
                type: "Imagine",
                duration: "20 min",
                description:
                    "Work together to help a tiny frog reach the pond.",
                magic:
                    "The children invent their own rescue plan."
            }

        ],


        experiences: [

            {
                id: "city",
                title: "Our Little City",
                category: "Create",
                duration: "25 min",
                age: "3–4",
                materials: "Blocks, paper, crayons",
                description:
                    "Build a city together.",
                challenge:
                    "Can we build a bridge that connects two parts of our city?",
                together:
                    "Build one part of the city with a friend.",
                joy:
                    "The whole city suddenly becomes connected."
            },

            {
                id: "bug",
                title: "Tiny Bug Hunt",
                category: "Explore",
                duration: "15 min",
                age: "3–4",
                materials: "Magnifying glasses",
                description:
                    "Discover tiny things hiding around the classroom.",
                challenge:
                    "Can you find something smaller than your finger?",
                together:
                    "Look together with a friend.",
                joy:
                    "Someone discovers something nobody else noticed."
            },

            {
                id: "frog",
                title: "Frog Rescue",
                category: "Imagine",
                duration: "20 min",
                age: "3–4",
                materials: "Paper, blocks",
                description:
                    "Help an imaginary frog get home.",
                challenge:
                    "How can we cross the river?",
                together:
                    "Build the rescue path together.",
                joy:
                    "The children invent a completely new rescue idea."
            }

        ],


        children: [

            {
                name: "Aarav",
                age: 3,
                interest: "Animals",
                accomplishment: "Asked a friend for help"
            },

            {
                name: "Mia",
                age: 4,
                interest: "Building",
                accomplishment: "Built a tall tower independently"
            },

            {
                name: "Noah",
                age: 3,
                interest: "Drawing",
                accomplishment: "Created a story using pictures"
            },

            {
                name: "Sofia",
                age: 4,
                interest: "Movement",
                accomplishment: "Invented a new movement game"
            }

        ],


        classrooms: [

            {
                name: "Bluebirds",
                age: "3–4 years",
                children: 18,
                teacher: "Priya",
                energy: "Energetic"
            },

            {
                name: "Butterflies",
                age: "3–4 years",
                children: 16,
                teacher: "Meena",
                energy: "Calm"
            },

            {
                name: "Little Foxes",
                age: "4–5 years",
                children: 20,
                teacher: "Daniel",
                energy: "Curious"
            }

        ],


        teachers: [

            {
                name: "Priya",
                role: "Lead Teacher",
                classroom: "Bluebirds"
            },

            {
                name: "Meena",
                role: "Lead Teacher",
                classroom: "Butterflies"
            },

            {
                name: "Daniel",
                role: "Teacher",
                classroom: "Little Foxes"
            }

        ],


        journey: {

            child: "Aarav",

            events: [

                {
                    icon: "🌱",
                    title: "Explored",
                    text: "Looked closely at a tiny bug."
                },

                {
                    icon: "🎨",
                    title: "Created",
                    text: "Created his own bug garden."
                },

                {
                    icon: "🤝",
                    title: "Connected",
                    text: "Worked together with Rahul."
                },

                {
                    icon: "⭐",
                    title: "Accomplished",
                    text: "Asked a friend for help."
                }

            ]

        },


        stories: [

            {
                child: "Aarav",
                title: "A little discovery",
                text:
                    "Aarav was watching from a distance. Then he suddenly came closer and said, \"I found one!\""
            },

            {
                child: "Mia",
                title: "The tower",
                text:
                    "Mia kept rebuilding her tower after it fell. She finally stood back and said, \"I did it!\""
            }

        ],


        aiContext: [

            "18 children",

            "Age 3–4",

            "Indoor",

            "Rainy day",

            "Blocks",

            "Paper",

            "Crayons",

            "Energetic classroom"

        ]

    },


    // --------------------------------------------------------
    // TENANT 2
    // --------------------------------------------------------

    sunshineGarden: {

        id: "sunshineGarden",

        name: "Sunshine Garden Preschool",

        icon: "🌻",

        branch: "Garden Campus",

        branches: 2,

        childrenCount: 72,

        classroomsCount: 4,

        teachersCount: 9,

        currentClassroom: "Butterflies",

        ageGroup: "4–5 years",

        classroomChildren: 14,

        weather: "Sunny",

        environment: "Outdoor",

        energy: "Curious",

        teacher: "Daniel",

        teacherRole: "Lead Teacher",

        materials: [
            "Leaves",
            "Magnifying glasses",
            "Buckets",
            "Paint"
        ],

        spaces: [
            "Garden",
            "Outdoor Studio",
            "Tree Corner"
        ],


        todayExperiences: [

            {
                id: "garden",
                title: "Garden Detectives",
                type: "Explore",
                duration: "20 min",
                description:
                    "Discover unusual things hiding in the garden.",
                magic:
                    "The children find something nobody expected."
            },

            {
                id: "painting",
                title: "Giant Garden Painting",
                type: "Create",
                duration: "30 min",
                description:
                    "Create one giant painting together.",
                magic:
                    "Everyone contributes one small part."
            },

            {
                id: "orchestra",
                title: "Nature Orchestra",
                type: "Music",
                duration: "15 min",
                description:
                    "Turn sounds from nature into music.",
                magic:
                    "The children create a rhythm using things around them."
            }

        ],


        experiences: [

            {
                id: "garden",
                title: "Garden Detectives",
                category: "Explore",
                duration: "20 min",
                age: "4–5",
                materials: "Leaves, magnifying glasses, buckets",
                description:
                    "Discover unusual things hiding in the garden.",
                challenge:
                    "Can you find something with an interesting shape?",
                together:
                    "Compare discoveries with a friend.",
                joy:
                    "The children discover something unexpected."
            },

            {
                id: "painting",
                title: "Giant Garden Painting",
                category: "Create",
                duration: "30 min",
                age: "4–5",
                materials: "Paint, large paper",
                description:
                    "Create one giant painting together.",
                challenge:
                    "How can our small drawings become one big picture?",
                together:
                    "Add something to another child's artwork.",
                joy:
                    "The painting suddenly becomes a shared world."
            },

            {
                id: "orchestra",
                title: "Nature Orchestra",
                category: "Music",
                duration: "15 min",
                age: "4–5",
                materials: "Leaves, sticks, stones",
                description:
                    "Create music using sounds from nature.",
                challenge:
                    "Can we make three different sounds?",
                together:
                    "Create one rhythm together.",
                joy:
                    "Everyone joins the rhythm."
            }

        ],


        children: [

            {
                name: "Emma",
                age: 4,
                interest: "Nature",
                accomplishment: "Explained her discovery to a friend"
            },

            {
                name: "Liam",
                age: 5,
                interest: "Music",
                accomplishment: "Created a new rhythm"
            },

            {
                name: "Olivia",
                age: 4,
                interest: "Art",
                accomplishment: "Added to a friend's painting"
            },

            {
                name: "Ethan",
                age: 5,
                interest: "Animals",
                accomplishment: "Led a garden discovery"
            }

        ],


        classrooms: [

            {
                name: "Butterflies",
                age: "4–5 years",
                children: 14,
                teacher: "Daniel",
                energy: "Curious"
            },

            {
                name: "Robins",
                age: "3–4 years",
                children: 16,
                teacher: "Sarah",
                energy: "Energetic"
            },

            {
                name: "Sunbeams",
                age: "4–5 years",
                children: 15,
                teacher: "Emily",
                energy: "Calm"
            }

        ],


        teachers: [

            {
                name: "Daniel",
                role: "Lead Teacher",
                classroom: "Butterflies"
            },

            {
                name: "Sarah",
                role: "Teacher",
                classroom: "Robins"
            },

            {
                name: "Emily",
                role: "Teacher",
                classroom: "Sunbeams"
            }

        ],


        journey: {

            child: "Emma",

            events: [

                {
                    icon: "🔎",
                    title: "Discovered",
                    text: "Found an unusual leaf in the garden."
                },

                {
                    icon: "🌱",
                    title: "Explored",
                    text: "Compared different leaves."
                },

                {
                    icon: "🤝",
                    title: "Connected",
                    text: "Explained her discovery to a friend."
                },

                {
                    icon: "⭐",
                    title: "Accomplished",
                    text: "Led a small garden discovery."
                }

            ]

        },


        stories: [

            {
                child: "Emma",
                title: "The unusual leaf",
                text:
                    "Emma found a leaf nobody else had noticed. She carried it carefully to the group and explained why she thought it was special."
            },

            {
                child: "Liam",
                title: "The garden orchestra",
                text:
                    "Liam started tapping two sticks together. Soon three other children joined him and the garden became a little orchestra."
            }

        ],


        aiContext: [

            "14 children",

            "Age 4–5",

            "Outdoor",

            "Sunny day",

            "Garden",

            "Leaves",

            "Magnifying glasses",

            "Curious classroom"

        ]

    }

};


// ------------------------------------------------------------
// CURRENT STATE
// ------------------------------------------------------------

let currentTenantId = "littleStars";

let currentPage = "today";

let currentTenant = tenants[currentTenantId];


// ------------------------------------------------------------
// INITIALIZE
// ------------------------------------------------------------

document.addEventListener("DOMContentLoaded", () => {

    setupNavigation();

    renderTenant();

    renderPage();

});


// ------------------------------------------------------------
// NAVIGATION
// ------------------------------------------------------------

function setupNavigation() {

    document.querySelectorAll(".nav-item").forEach(button => {

        button.addEventListener("click", () => {

            document
                .querySelectorAll(".nav-item")
                .forEach(item =>
                    item.classList.remove("active")
                );

            button.classList.add("active");

            currentPage = button.dataset.page;

            renderPage();

        });

    });

}


// ------------------------------------------------------------
// TENANT SWITCHER
// ------------------------------------------------------------

function openTenantSwitcher() {

    const modal =
        document.getElementById("tenantModal");

    renderTenantList();

    modal.classList.remove("hidden");

}


function closeTenantSwitcher() {

    document
        .getElementById("tenantModal")
        .classList.add("hidden");

}


function renderTenantList() {

    const container =
        document.getElementById("tenantList");

    container.innerHTML = "";

    Object.values(tenants).forEach(tenant => {

        const selected =
            tenant.id === currentTenantId;

        const item =
            document.createElement("div");

        item.className =
            "tenant-option " +
            (selected ? "selected" : "");

        item.innerHTML = `

            <div class="tenant-option-icon">
                ${tenant.icon}
            </div>

            <div class="tenant-option-info">

                <div class="tenant-option-name">
                    ${tenant.name}
                </div>

                <div class="tenant-option-details">
                    ${tenant.branches} branches
                    ·
                    ${tenant.childrenCount} children
                </div>

            </div>

            <div class="tenant-check">
                ${selected ? "✓" : ""}
            </div>

        `;

        item.onclick = () => {

            switchTenant(tenant.id);

        };

        container.appendChild(item);

    });

}


function switchTenant(tenantId) {

    if (!tenants[tenantId]) {
        return;
    }

    currentTenantId = tenantId;

    currentTenant =
        tenants[currentTenantId];

    closeTenantSwitcher();

    renderTenant();

    renderPage();

}


// ------------------------------------------------------------
// TENANT HEADER
// ------------------------------------------------------------

function renderTenant() {

    document.getElementById(
        "tenantIcon"
    ).textContent =
        currentTenant.icon;

    document.getElementById(
        "sidebarTenantName"
    ).textContent =
        currentTenant.name;

    document.getElementById(
        "sidebarTenantBranch"
    ).textContent =
        currentTenant.branch;

    document.getElementById(
        "breadcrumbTenant"
    ).textContent =
        currentTenant.name;

}


// ------------------------------------------------------------
// PAGE ROUTER
// ------------------------------------------------------------

function renderPage() {

    const pageNames = {

        today: "Today",

        experiences: "Experience Explorer",

        ai: "Ask AI",

        children: "Children",

        classrooms: "Classrooms",

        teachers: "Teachers",

        journey: "Child Journey",

        stories: "Parent Stories",

        management: "Management",

        platform: "Platform",

        settings: "Settings"

    };

    document.getElementById(
        "breadcrumbPage"
    ).textContent =
        pageNames[currentPage] || "Today";


    const content =
        document.getElementById("appContent");


    switch (currentPage) {

        case "today":
            content.innerHTML =
                renderToday();
            break;

        case "experiences":
            content.innerHTML =
                renderExperiences();
            break;

        case "ai":
            content.innerHTML =
                renderAI();
            break;

        case "children":
            content.innerHTML =
                renderChildren();
            break;

        case "classrooms":
            content.innerHTML =
                renderClassrooms();
            break;

        case "teachers":
            content.innerHTML =
                renderTeachers();
            break;

        case "journey":
            content.innerHTML =
                renderJourney();
            break;

        case "stories":
            content.innerHTML =
                renderStories();
            break;

        case "management":
            content.innerHTML =
                renderManagement();
            break;

        case "platform":
            content.innerHTML =
                renderPlatform();
            break;

        case "settings":
            content.innerHTML =
                renderSettings();
            break;

    }

}


// ------------------------------------------------------------
// TODAY
// ------------------------------------------------------------

function renderToday() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    ${currentTenant.icon}
                    ${currentTenant.branch}
                </div>

                <h1>
                    Good morning, ${currentTenant.teacher}
                </h1>

                <p>
                    Let's make today something worth remembering.
                </p>

            </div>

            <div class="date-card">

                <div class="date-day">
                    ${new Date().toLocaleDateString(
                        "en-US",
                        { weekday: "long" }
                    )}
                </div>

                <div class="date-number">
                    ${new Date().getDate()}
                </div>

            </div>

        </div>


        <div class="context-strip">

            <div>
                <span>Children</span>
                <strong>
                    ${currentTenant.classroomChildren}
                </strong>
            </div>

            <div>
                <span>Classroom</span>
                <strong>
                    ${currentTenant.currentClassroom}
                </strong>
            </div>

            <div>
                <span>Age</span>
                <strong>
                    ${currentTenant.ageGroup}
                </strong>
            </div>

            <div>
                <span>Environment</span>
                <strong>
                    ${currentTenant.environment}
                </strong>
            </div>

            <div>
                <span>Weather</span>
                <strong>
                    ${currentTenant.weather}
                </strong>
            </div>

            <div>
                <span>Energy</span>
                <strong>
                    ${currentTenant.energy}
                </strong>
            </div>

        </div>


        <div class="section-heading">

            <div>

                <h2>
                    Today's possibilities
                </h2>

                <p>
                    You don't have to do everything.
                    Follow the children.
                </p>

            </div>

            <button
                class="secondary-button"
                onclick="openPage('experiences')">

                Explore all

            </button>

        </div>


        <div class="experience-grid">

            ${currentTenant.todayExperiences
                .map(exp => experienceCard(exp))
                .join("")}

        </div>


        <div class="two-column">

            <div class="ai-card">

                <div class="ai-card-top">

                    <div class="ai-symbol">
                        ✦
                    </div>

                    <div>

                        <div class="card-eyebrow">
                            EXPERIENCE INTELLIGENCE
                        </div>

                        <h2>
                            A thought for today
                        </h2>

                    </div>

                </div>


                <p>

                    Based on your classroom, environment
                    and recent experiences, consider giving
                    children something they can build together.

                </p>


                <button
                    class="primary-button"
                    onclick="openPage('ai')">

                    Explore idea

                </button>

            </div>


            <div class="surprise-card">

                <div class="card-eyebrow">
                    TODAY'S SURPRISE
                </div>

                <div class="surprise-icon">
                    ✨
                </div>

                <h2>
                    Something strange is hiding
                    in the classroom.
                </h2>

                <p>
                    Let the children discover what it is.
                </p>

                <button class="ghost-button">
                    Reveal later
                </button>

            </div>

        </div>

    `;

}


// ------------------------------------------------------------
// EXPERIENCE CARD
// ------------------------------------------------------------

function experienceCard(exp) {

    return `

        <div
            class="experience-card"
            onclick="openExperience('${exp.id}')">

            <div class="experience-card-top">

                <span class="experience-type">
                    ${exp.type || exp.category}
                </span>

                <span class="duration">
                    ${exp.duration}
                </span>

            </div>

            <h3>
                ${exp.title}
            </h3>

            <p>
                ${exp.description}
            </p>

            <div class="experience-footer">

                <span>
                    ✨ ${exp.magic || exp.joy}
                </span>

            </div>

        </div>

    `;

}


// ------------------------------------------------------------
// EXPERIENCE EXPLORER
// ------------------------------------------------------------

function renderExperiences() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    EXPERIENCE LIBRARY
                </div>

                <h1>
                    Find something worth experiencing.
                </h1>

                <p>
                    Designed for ${currentTenant.name}.
                </p>

            </div>

        </div>


        <div class="filter-row">

            <button class="filter active">
                All
            </button>

            <button class="filter">
                Explore
            </button>

            <button class="filter">
                Create
            </button>

            <button class="filter">
                Move
            </button>

            <button class="filter">
                Imagine
            </button>

            <button class="filter">
                Together
            </button>

        </div>


        <div class="experience-grid">

            ${currentTenant.experiences
                .map(exp => experienceCard(exp))
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// AI
// ------------------------------------------------------------

function renderAI() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    EXPERIENCE INTELLIGENCE
                </div>

                <h1>
                    What should we experience today?
                </h1>

                <p>
                    AI looks at the classroom context
                    and suggests possibilities.
                </p>

            </div>

        </div>


        <div class="ai-layout">

            <div class="ai-chat">

                <div class="chat-message assistant">

                    <div class="chat-avatar">
                        ✦
                    </div>

                    <div>

                        <strong>
                            Experience AI
                        </strong>

                        <p>
                            I am looking at today's
                            classroom context.
                        </p>

                        <div class="context-pills">

                            ${currentTenant.aiContext
                                .map(x =>
                                    `<span>${x}</span>`
                                )
                                .join("")}

                        </div>

                        <p>
                            What would you like to explore?
                        </p>

                    </div>

                </div>


                <div class="suggestion-row">

                    <button onclick="askAI('quick')">
                        Something with no preparation
                    </button>

                    <button onclick="askAI('energy')">
                        Children have lots of energy
                    </button>

                    <button onclick="askAI('curiosity')">
                        Follow yesterday's curiosity
                    </button>

                </div>


                <div id="aiResponse"></div>


                <div class="chat-input">

                    <input
                        id="aiInput"
                        placeholder="Tell me what's happening..." />

                    <button onclick="sendAI()">
                        →
                    </button>

                </div>

            </div>


            <div class="ai-context-panel">

                <div class="card-eyebrow">
                    CURRENT CONTEXT
                </div>

                <h3>
                    ${currentTenant.currentClassroom}
                </h3>

                <div class="context-list">

                    <div>
                        <span>Children</span>
                        <strong>
                            ${currentTenant.classroomChildren}
                        </strong>
                    </div>

                    <div>
                        <span>Age</span>
                        <strong>
                            ${currentTenant.ageGroup}
                        </strong>
                    </div>

                    <div>
                        <span>Environment</span>
                        <strong>
                            ${currentTenant.environment}
                        </strong>
                    </div>

                    <div>
                        <span>Weather</span>
                        <strong>
                            ${currentTenant.weather}
                        </strong>
                    </div>

                    <div>
                        <span>Energy</span>
                        <strong>
                            ${currentTenant.energy}
                        </strong>
                    </div>

                </div>


                <div class="materials">

                    <div class="card-eyebrow">
                        AVAILABLE MATERIALS
                    </div>

                    ${currentTenant.materials
                        .map(m =>
                            `<span>${m}</span>`
                        )
                        .join("")}

                </div>

            </div>

        </div>

    `;

}


function askAI(type) {

    const response =
        document.getElementById("aiResponse");

    if (!response) return;

    let text = "";

    if (type === "quick") {

        text =
            currentTenant.environment === "Outdoor"

                ? "Try a 10-minute discovery walk. Ask children to find one thing that looks unusual."

                : "Try 'Build Something That Shouldn't Work.' Give children blocks and ask them to invent something strange.";

    }

    else if (type === "energy") {

        text =
            "Try an experience with movement followed by creation. Let the children move first, then turn what they discovered into something.";

    }

    else {

        text =
            currentTenant.id === "sunshineGarden"

                ? "Yesterday the children were exploring leaves. Today, turn that curiosity into a garden detective mission."

                : "Yesterday the children were building. Today, let them decide what their city needs next.";

    }


    response.innerHTML = `

        <div class="chat-message assistant">

            <div class="chat-avatar">
                ✦
            </div>

            <div>

                <strong>
                    Here's a possibility
                </strong>

                <p>
                    ${text}
                </p>

                <button
                    class="primary-button small"
                    onclick="openExperience('${currentTenant.todayExperiences[0].id}')">

                    Turn into experience

                </button>

            </div>

        </div>

    `;

}


function sendAI() {

    const input =
        document.getElementById("aiInput");

    if (!input || !input.value.trim()) {
        return;
    }

    const response =
        document.getElementById("aiResponse");

    response.innerHTML = `

        <div class="chat-message user">

            <div>

                <p>
                    ${escapeHtml(input.value)}
                </p>

            </div>

        </div>

        <div class="chat-message assistant">

            <div class="chat-avatar">
                ✦
            </div>

            <div>

                <strong>
                    Experience AI
                </strong>

                <p>
                    Based on ${currentTenant.name},
                    your ${currentTenant.currentClassroom}
                    classroom and today's environment,
                    I would turn that into a simple
                    child-led experience.
                </p>

            </div>

        </div>

    `;

    input.value = "";

}


// ------------------------------------------------------------
// CHILDREN
// ------------------------------------------------------------

function renderChildren() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    ${currentTenant.name}
                </div>

                <h1>
                    Children
                </h1>

                <p>
                    See children through their experiences,
                    not through scores.
                </p>

            </div>

            <button class="primary-button">
                + Add child
            </button>

        </div>


        <div class="people-grid">

            ${currentTenant.children
                .map(child => `

                    <div class="person-card">

                        <div class="person-avatar">
                            ${child.name.charAt(0)}
                        </div>

                        <div class="person-info">

                            <h3>
                                ${child.name}
                            </h3>

                            <p>
                                ${child.age} years
                            </p>

                        </div>

                        <div class="interest">
                            Interested in
                            <strong>
                                ${child.interest}
                            </strong>
                        </div>

                        <div class="accomplishment">
                            ⭐ ${child.accomplishment}
                        </div>

                    </div>

                `)
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// CLASSROOMS
// ------------------------------------------------------------

function renderClassrooms() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    ${currentTenant.name}
                </div>

                <h1>
                    Classrooms
                </h1>

                <p>
                    Every classroom has its own rhythm.
                </p>

            </div>

        </div>


        <div class="classroom-grid">

            ${currentTenant.classrooms
                .map(room => `

                    <div class="classroom-card">

                        <div class="classroom-icon">
                            ▦
                        </div>

                        <div>

                            <h3>
                                ${room.name}
                            </h3>

                            <p>
                                ${room.age}
                            </p>

                        </div>

                        <div class="classroom-meta">

                            <span>
                                ${room.children} children
                            </span>

                            <span>
                                ${room.teacher}
                            </span>

                            <span>
                                ${room.energy}
                            </span>

                        </div>

                    </div>

                `)
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// TEACHERS
// ------------------------------------------------------------

function renderTeachers() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    PEOPLE
                </div>

                <h1>
                    Teachers
                </h1>

                <p>
                    Help teachers spend more time with children.
                </p>

            </div>

        </div>


        <div class="people-grid">

            ${currentTenant.teachers
                .map(teacher => `

                    <div class="person-card">

                        <div class="person-avatar">
                            ${teacher.name.charAt(0)}
                        </div>

                        <div class="person-info">

                            <h3>
                                ${teacher.name}
                            </h3>

                            <p>
                                ${teacher.role}
                            </p>

                        </div>

                        <div class="teacher-classroom">

                            Classroom

                            <strong>
                                ${teacher.classroom}
                            </strong>

                        </div>

                    </div>

                `)
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// JOURNEY
// ------------------------------------------------------------

function renderJourney() {

    const journey =
        currentTenant.journey;

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    CHILD JOURNEY
                </div>

                <h1>
                    ${journey.child}'s little world
                </h1>

                <p>
                    A collection of experiences,
                    discoveries and accomplishments.
                </p>

            </div>

        </div>


        <div class="journey-card">

            <div class="journey-intro">

                <div class="journey-avatar">
                    ${journey.child.charAt(0)}
                </div>

                <div>

                    <h2>
                        A journey made of moments
                    </h2>

                    <p>
                        Not a score. Not a ranking.
                        Just things worth remembering.
                    </p>

                </div>

            </div>


            <div class="journey-line">

                ${journey.events
                    .map(event => `

                        <div class="journey-event">

                            <div class="journey-event-icon">
                                ${event.icon}
                            </div>

                            <div>

                                <div class="journey-event-title">
                                    ${event.title}
                                </div>

                                <div class="journey-event-text">
                                    ${event.text}
                                </div>

                            </div>

                        </div>

                    `)
                    .join("")}

            </div>

        </div>

    `;

}


// ------------------------------------------------------------
// STORIES
// ------------------------------------------------------------

function renderStories() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    PARENT STORIES
                </div>

                <h1>
                    A window into the day
                </h1>

                <p>
                    Parents should feel the day,
                    not receive a report card.
                </p>

            </div>

        </div>


        <div class="story-grid">

            ${currentTenant.stories
                .map(story => `

                    <div class="story-card">

                        <div class="story-heart">
                            ♡
                        </div>

                        <div class="story-child">
                            ${story.child}
                        </div>

                        <h2>
                            ${story.title}
                        </h2>

                        <p>
                            ${story.text}
                        </p>

                        <div class="story-footer">
                            From today's experience
                        </div>

                    </div>

                `)
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// MANAGEMENT
// ------------------------------------------------------------

function renderManagement() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    MANAGEMENT
                </div>

                <h1>
                    ${currentTenant.name}
                </h1>

                <p>
                    A calm view of what is happening.
                </p>

            </div>

        </div>


        <div class="stat-grid">

            <div class="stat-card">

                <span>
                    Children
                </span>

                <strong>
                    ${currentTenant.childrenCount}
                </strong>

                <small>
                    across ${currentTenant.branches} branches
                </small>

            </div>


            <div class="stat-card">

                <span>
                    Classrooms
                </span>

                <strong>
                    ${currentTenant.classroomsCount}
                </strong>

                <small>
                    active classrooms
                </small>

            </div>


            <div class="stat-card">

                <span>
                    Teachers
                </span>

                <strong>
                    ${currentTenant.teachersCount}
                </strong>

                <small>
                    teaching team
                </small>

            </div>


            <div class="stat-card">

                <span>
                    Experiences
                </span>

                <strong>
                    ${currentTenant.experiences.length}
                </strong>

                <small>
                    currently available
                </small>

            </div>

        </div>


        <div class="management-panel">

            <div>

                <div class="card-eyebrow">
                    THIS CLASSROOM
                </div>

                <h2>
                    ${currentTenant.currentClassroom}
                </h2>

                <p>
                    ${currentTenant.classroomChildren}
                    children ·
                    ${currentTenant.ageGroup}
                    ·
                    ${currentTenant.energy}
                </p>

            </div>

            <div class="management-status">
                ● Experience ready
            </div>

        </div>

    `;

}


// ------------------------------------------------------------
// PLATFORM
// ------------------------------------------------------------

function renderPlatform() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    PLATFORM
                </div>

                <h1>
                    All daycare tenants
                </h1>

                <p>
                    Platform-level view.
                    Tenant data remains separated.
                </p>

            </div>

        </div>


        <div class="platform-notice">

            <div class="platform-icon">
                ◈
            </div>

            <div>

                <strong>
                    Platform context
                </strong>

                <p>
                    This view belongs to the platform,
                    not to an individual daycare.
                </p>

            </div>

        </div>


        <div class="tenant-platform-grid">

            ${Object.values(tenants)
                .map(tenant => `

                    <div class="platform-tenant-card">

                        <div class="platform-tenant-top">

                            <div class="large-tenant-icon">
                                ${tenant.icon}
                            </div>

                            <div>

                                <h2>
                                    ${tenant.name}
                                </h2>

                                <p>
                                    ${tenant.branches}
                                    branches
                                </p>

                            </div>

                        </div>


                        <div class="platform-metrics">

                            <div>
                                <strong>
                                    ${tenant.childrenCount}
                                </strong>
                                <span>
                                    Children
                                </span>
                            </div>

                            <div>
                                <strong>
                                    ${tenant.classroomsCount}
                                </strong>
                                <span>
                                    Classrooms
                                </span>
                            </div>

                            <div>
                                <strong>
                                    ${tenant.teachersCount}
                                </strong>
                                <span>
                                    Teachers
                                </span>
                            </div>

                        </div>


                        <button
                            class="secondary-button"
                            onclick="switchTenant('${tenant.id}')">

                            Open tenant

                        </button>

                    </div>

                `)
                .join("")}

        </div>

    `;

}


// ------------------------------------------------------------
// SETTINGS
// ------------------------------------------------------------

function renderSettings() {

    return `

        <div class="page-heading">

            <div>

                <div class="eyebrow">
                    TENANT SETTINGS
                </div>

                <h1>
                    ${currentTenant.name}
                </h1>

                <p>
                    Configure how the daycare works.
                </p>

            </div>

        </div>


        <div class="settings-grid">

            <div class="settings-card">

                <div class="settings-icon">
                    🏫
                </div>

                <h3>
                    Organization
                </h3>

                <p>
                    Branches, classrooms and school identity.
                </p>

            </div>


            <div class="settings-card">

                <div class="settings-icon">
                    ✦
                </div>

                <h3>
                    Experience preferences
                </h3>

                <p>
                    Duration, materials and preferred experiences.
                </p>

            </div>


            <div class="settings-card">

                <div class="settings-icon">
                    🛡
                </div>

                <h3>
                    Safety
                </h3>

                <p>
                    School policies and experience restrictions.
                </p>

            </div>


            <div class="settings-card">

                <div class="settings-icon">
                    🌎
                </div>

                <h3>
                    Language & culture
                </h3>

                <p>
                    Local language and cultural context.
                </p>

            </div>

        </div>

    `;

}


// ------------------------------------------------------------
// EXPERIENCE DETAIL
// ------------------------------------------------------------

function openExperience(experienceId) {

    const experience =
        currentTenant.experiences.find(
            x => x.id === experienceId
        );

    if (!experience) return;


    document.getElementById(
        "experienceDetail"
    ).innerHTML = `

        <div class="experience-detail">

            <div class="experience-detail-type">
                ${experience.category}
            </div>

            <h1>
                ${experience.title}
            </h1>

            <p class="experience-description">
                ${experience.description}
            </p>


            <div class="detail-grid">

                <div>
                    <span>
                        AGE
                    </span>

                    <strong>
                        ${experience.age}
                    </strong>
                </div>

                <div>
                    <span>
                        TIME
                    </span>

                    <strong>
                        ${experience.duration}
                    </strong>
                </div>

                <div>
                    <span>
                        MATERIALS
                    </span>

                    <strong>
                        ${experience.materials}
                    </strong>
                </div>

            </div>


            <div class="detail-section">

                <div class="detail-label">
                    TODAY'S MISSION
                </div>

                <p>
                    ${experience.description}
                </p>

            </div>


            <div class="detail-section">

                <div class="detail-label">
                    DISCOVER
                </div>

                <p>
                    ${experience.challenge}
                </p>

            </div>


            <div class="detail-section">

                <div class="detail-label">
                    TOGETHER
                </div>

                <p>
                    ${experience.together}
                </p>

            </div>


            <div class="joy-box">

                <div>
                    ✨
                </div>

                <div>

                    <strong>
                        Joy Moment
                    </strong>

                    <p>
                        ${experience.joy}
                    </p>

                </div>

            </div>


            <div class="experience-actions">

                <button
                    class="primary-button"
                    onclick="startExperience('${experience.id}')">

                    Start experience

                </button>

                <button
                    class="secondary-button"
                    onclick="closeExperience()">

                    Save for later

                </button>

            </div>

        </div>

    `;


    document
        .getElementById("experienceModal")
        .classList.remove("hidden");

}


function closeExperience() {

    document
        .getElementById("experienceModal")
        .classList.add("hidden");

}


function startExperience(id) {

    closeExperience();

    alert(
        `Experience started for ${currentTenant.name}.\n\n` +
        `Classroom: ${currentTenant.currentClassroom}\n` +
        `Children: ${currentTenant.classroomChildren}`
    );

}


// ------------------------------------------------------------
// PAGE NAVIGATION
// ------------------------------------------------------------

function openPage(page) {

    currentPage = page;

    document
        .querySelectorAll(".nav-item")
        .forEach(item => {

            item.classList.toggle(
                "active",
                item.dataset.page === page
            );

        });

    renderPage();

}


// ------------------------------------------------------------
// UTILITIES
// ------------------------------------------------------------

function escapeHtml(text) {

    const div =
        document.createElement("div");

    div.textContent = text;

    return div.innerHTML;

}


// ------------------------------------------------------------
// CLOSE MODALS WHEN CLICKING OUTSIDE
// ------------------------------------------------------------

document.addEventListener("click", event => {

    if (
        event.target.classList.contains(
            "modal-overlay"
        )
    ) {

        event.target.classList.add("hidden");

    }

});