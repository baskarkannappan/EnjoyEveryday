```javascript
/* =========================================================
   ENJOYEVERYDAY WIREFRAME
   Prototype only — data lives in JavaScript.
   ========================================================= */


/* =========================================================
   TENANTS
   ========================================================= */

const tenants = {

    "little-stars": {

        name: "Little Stars Daycare",
        location: "Toronto · 3 branches",

        classroom: "Bluebirds",
        children: 18,
        age: "3–4",

        teacher: "Priya",

        environment: "Indoor",
        weather: "Rainy",

        materials: [
            "Blocks",
            "Paper",
            "Crayons"
        ],

        interests: [
            "Animals",
            "Building",
            "Movement"
        ]

    },

    "sunshine-garden": {

        name: "Sunshine Garden Preschool",
        location: "Toronto · 2 branches",

        classroom: "Butterflies",
        children: 14,
        age: "4–5",

        teacher: "Daniel",

        environment: "Outdoor",
        weather: "Sunny",

        materials: [
            "Paint",
            "Garden",
            "Music"
        ],

        interests: [
            "Nature",
            "Painting",
            "Music"
        ]

    }

};


let currentTenantId = "little-stars";


/* =========================================================
   EXPERIENCES
   ========================================================= */

const experiences = [

    {
        id: "city",
        title: "Our Little City",
        type: "Create",
        age: "3–4",
        duration: "25 min",
        environment: "Indoor",
        creator: "Priya",
        source: "created",
        status: "Tenant Approved",
        usage: 12,
        version: "v3",
        loved: 10,
        description:
            "Children build a city together and discover what makes a community."
    },

    {
        id: "tiny",
        title: "Tiny Discoveries",
        type: "Explore",
        age: "3–4",
        duration: "15 min",
        environment: "Indoor",
        creator: "EnjoyEveryday",
        source: "platform",
        status: "Platform Approved",
        usage: 19,
        version: "v2",
        loved: 16,
        description:
            "Look closely at tiny things hiding in the classroom."
    },

    {
        id: "frog",
        title: "Frog Rescue",
        type: "Together",
        age: "3–4",
        duration: "20 min",
        environment: "Indoor",
        creator: "Experience AI",
        source: "ai",
        status: "Experimental",
        usage: 8,
        version: "v2",
        loved: 6,
        description:
            "A cooperative rescue mission involving movement, planning and imagination."
    },

    {
        id: "animals",
        title: "Animal Adventure",
        type: "Move",
        age: "3–4",
        duration: "10 min",
        environment: "Indoor",
        creator: "Priya",
        source: "created",
        status: "Tenant Approved",
        usage: 9,
        version: "v1",
        loved: 8,
        description:
            "Move, sound and imagine like different animals."
    },

    {
        id: "painting",
        title: "Giant Painting",
        type: "Create",
        age: "4–5",
        duration: "30 min",
        environment: "Outdoor",
        creator: "Daniel",
        source: "created",
        status: "Tenant Approved",
        usage: 6,
        version: "v2",
        loved: 5,
        description:
            "A large shared painting where every child contributes."
    },

    {
        id: "nature",
        title: "Nature Detectives",
        type: "Explore",
        age: "4–5",
        duration: "20 min",
        environment: "Outdoor",
        creator: "EnjoyEveryday",
        source: "platform",
        status: "Platform Approved",
        usage: 15,
        version: "v3",
        loved: 12,
        description:
            "Children investigate what they can find, hear and notice outdoors."
    },

    {
        id: "seed",
        title: "The Little Seed",
        type: "Story",
        age: "3–5",
        duration: "10 min",
        environment: "Indoor",
        creator: "EnjoyEveryday",
        source: "platform",
        status: "Platform Approved",
        usage: 21,
        version: "v4",
        loved: 18,
        description:
            "A quiet story about growing, waiting and discovering."
    }

];


let libraryFilter = "all";


/* =========================================================
   TENANT SWITCHING
   ========================================================= */

function switchTenant(id) {

    currentTenantId = id;

    const tenant = tenants[id];

    document.getElementById("tenantLocation").textContent =
        tenant.location;

    updatePageContext();

    renderExperiences();

    renderChildren();

    renderClassrooms();

    renderTeachers();

    showToast(
        `Switched to ${tenant.name}`
    );
}


function updatePageContext() {

    const tenant = tenants[currentTenantId];

    document.getElementById("breadcrumb").textContent =
        `${tenant.name} / ${getCurrentPageName()}`;

}


/* =========================================================
   NAVIGATION
   ========================================================= */

function showPage(page, button = null) {

    document.querySelectorAll(".page")
        .forEach(p => p.classList.remove("active-page"));

    const target =
        document.getElementById(`page-${page}`);

    if (target) {
        target.classList.add("active-page");
    }

    document.querySelectorAll(".nav-item")
        .forEach(item => item.classList.remove("active"));

    if (button) {
        button.classList.add("active");
    }

    const names = {
        today: "Today",
        experiences: "Experiences",
        planner: "Monthly Planner",
        harvest: "Harvest & Insights",
        children: "Children",
        classrooms: "Classrooms",
        teachers: "Teachers",
        journey: "Child Journey",
        stories: "Parent Stories",
        management: "Management",
        settings: "Settings"
    };

    document.getElementById("pageTitle").textContent =
        page === "today"
            ? `Good Morning, ${tenants[currentTenantId].teacher}`
            : names[page];

    document.getElementById("breadcrumb").textContent =
        `${tenants[currentTenantId].name} / ${names[page]}`;

    if (page === "planner") {
        renderCalendar();
    }
}


function getCurrentPageName() {

    const active =
        document.querySelector(".page.active-page");

    if (!active) return "Today";

    return active.id
        .replace("page-", "")
        .replace("-", " ");

}


/* =========================================================
   EXPERIENCE LIBRARY
   ========================================================= */

function renderExperiences() {

    const grid =
        document.getElementById("experienceGrid");

    if (!grid) return;

    let list = [...experiences];

    if (libraryFilter !== "all") {

        list = list.filter(x =>
            x.source === libraryFilter
        );

    }

    grid.innerHTML = list.map(experience => {

        return `

        <article class="experience-card">

            <div class="card-top">

                <span class="type-chip">
                    ${experience.type}
                </span>

                <span class="status-chip">
                    ${experience.status}
                </span>

            </div>

            <h3>
                ${experience.title}
            </h3>

            <p class="description">
                ${experience.description}
            </p>

            <div class="card-meta">
                <span>${experience.age}</span>
                <span>${experience.duration}</span>
                <span>${experience.environment}</span>
            </div>

            <div class="card-bottom">

                <div class="usage">
                    ${experience.usage} uses ·
                    ${experience.loved} ♥
                    · ${experience.version}
                </div>

                <button
                    class="card-action"
                    onclick="openExperience('${experience.id}')"
                >
                    View →
                </button>

            </div>

        </article>

        `;

    }).join("");

}


function filterLibrary(filter, button) {

    libraryFilter = filter;

    document.querySelectorAll(".library-tab")
        .forEach(tab =>
            tab.classList.remove("active")
        );

    button.classList.add("active");

    renderExperiences();
}


function searchExperiences() {

    const query =
        document.getElementById("experienceSearch")
            .value
            .toLowerCase();

    const cards =
        document.querySelectorAll(".experience-card");

    cards.forEach(card => {

        card.style.display =
            card.textContent
                .toLowerCase()
                .includes(query)
                ? ""
                : "none";

    });

}


/* =========================================================
   EXPERIENCE DETAIL
   ========================================================= */

function openExperience(id) {

    const experience =
        experiences.find(x => x.id === id);

    if (!experience) return;

    document.getElementById("modalExperienceTitle")
        .textContent = experience.title;

    document.getElementById("modalMission")
        .textContent =
        experience.description;

    document.getElementById("experienceModal")
        .classList.add("open");

}


function startExperience(id) {

    const experience =
        experiences.find(x => x.id === id);

    if (!experience) return;

    showToast(
        `${experience.title} started for ${tenants[currentTenantId].classroom}`
    );

}


/* =========================================================
   CREATE EXPERIENCE
   ========================================================= */

function openCreateExperience() {

    document.getElementById("createModal")
        .classList.add("open");

}


function saveExperience(mode) {

    const name =
        document.getElementById("newExperienceName")
            .value
            .trim();

    if (!name) {

        showToast("Give your experience a name first.");

        return;
    }

    const newExperience = {

        id: "custom-" + Date.now(),

        title: name,

        type: "Explore",

        age: tenants[currentTenantId].age,

        duration: "20 min",

        environment: tenants[currentTenantId].environment,

        creator: tenants[currentTenantId].teacher,

        source: "created",

        status:
            mode === "draft"
                ? "Draft"
                : "Tenant Approved",

        usage: 0,

        version: "v1",

        loved: 0,

        description:
            "A new experience created by your daycare."

    };

    experiences.unshift(newExperience);

    closeModal("createModal");

    renderExperiences();

    showToast(
        mode === "draft"
            ? "Experience saved as draft."
            : "Experience added to My Experiences."
    );

}


/* =========================================================
   MONTHLY CALENDAR
   ========================================================= */

const calendarExperiences = {

    1: [
        ["Discover Our Classroom", "Explore", false]
    ],

    2: [
        ["Build Our Neighborhood", "Create", true]
    ],

    3: [
        ["Build Something Together", "Together", false]
    ],

    8: [
        ["Tiny Discoveries", "Explore", false]
    ],

    9: [
        ["Build a Bridge", "Create", false]
    ],

    10: [
        ["Our Own Sounds", "Create", true]
    ],

    15: [
        ["Nature Detectives", "Explore", false]
    ],

    16: [
        ["Giant Painting", "Create", false]
    ],

    17: [
        ["Frog Rescue", "Together", false]
    ],

    22: [
        ["Our Big World", "Explore", false]
    ],

    23: [
        ["Build a Community", "Together", true]
    ],

    24: [
        ["Children Choose", "Explore", false]
    ]

};


function renderCalendar() {

    const grid =
        document.getElementById("calendarGrid");

    if (!grid) return;

    grid.innerHTML = "";

    const days = 30;

    for (let day = 1; day <= days; day++) {

        const cell =
            document.createElement("div");

        cell.className = "calendar-day";

        const today =
            day === 21;

        cell.innerHTML = `

            <div class="
                calendar-date
                ${today ? "today" : ""}
            ">
                ${day}
            </div>

        `;

        const dayExperiences =
            calendarExperiences[day] || [];

        dayExperiences.forEach(item => {

            const experience =
                document.createElement("div");

            experience.className =
                "calendar-experience " +
                (item[2] ? "suggested" : "");

            experience.innerHTML = `

                <strong>
                    ${item[0]}
                </strong>

                <span>
                    ${item[1]}
                    · ${item[2] ? "Suggested" : "Scheduled"}
                </span>

            `;

            experience.onclick = () => {

                showToast(
                    `${item[0]} selected`
                );

            };

            cell.appendChild(experience);

        });

        grid.appendChild(cell);

    }

}


function generateMonth() {

    showToast(
        "AI is preparing a September experience plan..."
    );

    setTimeout(() => {

        showToast(
            "AI suggestion ready — review before publishing."
        );

    }, 1200);

}


function publishMonth() {

    showToast(
        "September plan published for teachers."
    );

}


function previousMonth() {

    showToast("Previous month selected.");

}


function nextMonth() {

    showToast("Next month selected.");

}


/* =========================================================
   HARVEST
   ========================================================= */

function openInsight(id) {

    const messages = {

        city:
            "Children stayed longer when the bridge challenge came first. Teachers added animals in 7 of 12 uses.",

        frog:
            "The experience worked especially well when children had to cooperate rather than take turns.",

        painting:
            "Large shared surfaces produced more conversation and collaboration than individual painting."
    };

    showToast(
        messages[id] ||
        "New experience insight discovered."
    );

}


/* =========================================================
   CHILDREN
   ========================================================= */

function renderChildren() {

    const tenant =
        tenants[currentTenantId];

    const names =
        currentTenantId === "little-stars"
            ? ["Aarav", "Mia", "Noah", "Sofia", "Leo", "Anaya"]
            : ["Emma", "Oliver", "Liam", "Amelia", "Lucas", "Maya"];

    const grid =
        document.getElementById("childrenGrid");

    if (!grid) return;

    grid.innerHTML =
        names.map(name => {

            return `

                <div class="person-card">

                    <div class="person-top">

                        <div class="person-avatar">
                            ${name.charAt(0)}
                        </div>

                        <div>
                            <h3>${name}</h3>
                            <p>
                                Age ${tenant.age}
                                · ${tenant.classroom}
                            </p>
                        </div>

                    </div>

                    <div class="person-interest">

                        <strong>Recent interest</strong>

                        <p>
                            ${
                                tenant.interests[
                                    names.indexOf(name) %
                                    tenant.interests.length
                                ]
                            }
                        </p>

                    </div>

                </div>

            `;

        }).join("");

}


/* =========================================================
   CLASSROOMS
   ========================================================= */

function renderClassrooms() {

    const tenant =
        tenants[currentTenantId];

    const grid =
        document.getElementById("classroomGrid");

    if (!grid) return;

    grid.innerHTML = `

        <div class="classroom-card">

            <div class="classroom-icon">▣</div>

            <h3>${tenant.classroom}</h3>

            <p>
                ${tenant.children} children
                · Ages ${tenant.age}
            </p>

            <p>
                Teacher: ${tenant.teacher}
            </p>

            <p>
                Environment: ${tenant.environment}
            </p>

        </div>

        <div class="classroom-card">

            <div class="classroom-icon">◇</div>

            <h3>Available materials</h3>

            <p>
                ${tenant.materials.join(" · ")}
            </p>

        </div>

        <div class="classroom-card">

            <div class="classroom-icon">✦</div>

            <h3>Current interests</h3>

            <p>
                ${tenant.interests.join(" · ")}
            </p>

        </div>

    `;

}


/* =========================================================
   TEACHERS
   ========================================================= */

function renderTeachers() {

    const tenant =
        tenants[currentTenantId];

    const grid =
        document.getElementById("teacherGrid");

    if (!grid) return;

    grid.innerHTML = `

        <div class="teacher-card">

            <div class="teacher-top">

                <div class="person-avatar">
                    ${tenant.teacher.charAt(0)}
                </div>

                <div>
                    <h3>${tenant.teacher}</h3>
                    <p>Experience Facilitator</p>
                </div>

            </div>

            <div class="person-interest">
                <strong>Today's support</strong>

                <p>
                    4 experiences planned.
                    Keep room for child-led exploration.
                </p>
            </div>

        </div>

    `;

}


/* =========================================================
   FEELING
   ========================================================= */

function selectFeeling(button) {

    document.querySelectorAll(".feeling")
        .forEach(x => x.classList.remove("active"));

    button.classList.add("active");

    showToast(
        `Classroom feeling: ${button.innerText}`
    );

}


/* =========================================================
   AI
   ========================================================= */

function openAI() {

    document.getElementById("aiModal")
        .classList.add("open");

}


function aiPrompt(prompt) {

    const input =
        document.getElementById("aiInput");

    input.value = prompt;

    sendAI();

}


function sendAI() {

    const input =
        document.getElementById("aiInput");

    const chat =
        document.getElementById("aiChat");

    const text =
        input.value.trim();

    if (!text) return;

    chat.innerHTML += `

        <div class="ai-user-message">
            ${escapeHtml(text)}
        </div>

    `;

    let response =
        "Based on your classroom context, I would suggest a simple experience with choice, movement and a small challenge. You decide whether it fits the children right now.";

    if (text.toLowerCase().includes("restless")) {

        response =
            "Try a 7-minute animal movement mission. Let children choose an animal, move across the room, and then invent a new animal together.";

    }

    if (text.toLowerCase().includes("animal")) {

        response =
            "Follow the animal interest. Tomorrow you could explore animal footprints, build an animal habitat, or create a rescue mission using the blocks you already have.";

    }

    if (text.toLowerCase().includes("low-prep")) {

        response =
            "Try 'Something Is Hiding'. Hide three ordinary classroom objects and let children become detectives. No preparation beyond choosing the objects.";

    }

    if (text.toLowerCase().includes("week")) {

        response =
            "I would build the week around Explore → Create → Together, while leaving one day open for children to choose where their curiosity goes.";

    }

    setTimeout(() => {

        chat.innerHTML += `

            <div class="ai-message">
                ${response}
            </div>

        `;

        chat.scrollTop =
            chat.scrollHeight;

    }, 350);

    input.value = "";

}


/* =========================================================
   MODALS
   ========================================================= */

function closeModal(id) {

    document.getElementById(id)
        .classList.remove("open");

}


document.querySelectorAll(".modal-overlay")
    .forEach(overlay => {

        overlay.addEventListener(
            "click",
            event => {

                if (event.target === overlay) {

                    overlay.classList.remove("open");

                }

            }
        );

    });


/* =========================================================
   NOTIFICATIONS
   ========================================================= */

function openNotifications() {

    showToast(
        "2 new experience insights are waiting."
    );

}


/* =========================================================
   TOAST
   ========================================================= */

let toastTimer;

function showToast(message) {

    const toast =
        document.getElementById("toast");

    toast.textContent = message;

    toast.classList.add("show");

    clearTimeout(toastTimer);

    toastTimer =
        setTimeout(() => {

            toast.classList.remove("show");

        }, 2500);

}


/* =========================================================
   SECURITY — PROTOTYPE HTML ESCAPING
   ========================================================= */

function escapeHtml(value) {

    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");

}


/* =========================================================
   INITIALIZE
   ========================================================= */

document.addEventListener("DOMContentLoaded", () => {

    renderExperiences();

    renderChildren();

    renderClassrooms();

    renderTeachers();

    renderCalendar();

});
```
 
 