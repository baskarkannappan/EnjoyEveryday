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
    public static string GetSvgForKey(string key)
    {
        return key switch
        {
            "gathering" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""gSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#fdfbf7""/>
      <stop offset=""60%"" stop-color=""#f4efe5""/>
      <stop offset=""100%"" stop-color=""#e9f0eb""/>
    </linearGradient>
    <radialGradient id=""gSun"" cx=""400"" cy=""90"" r=""220"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0%"" stop-color=""#faeecf"" stop-opacity=""0.7""/>
      <stop offset=""60%"" stop-color=""#faeecf"" stop-opacity=""0.2""/>
      <stop offset=""100%"" stop-color=""#faeecf"" stop-opacity=""0""/>
    </radialGradient>
    <linearGradient id=""gWood"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""#dcc9a8""/>
      <stop offset=""50%"" stop-color=""#ebd8ba""/>
      <stop offset=""100%"" stop-color=""#d5bf9b""/>
    </linearGradient>
  </defs>

  <!-- Background Wall -->
  <rect width=""800"" height=""360"" fill=""url(#gSky)""/>
  <circle cx=""400"" cy=""90"" r=""220"" fill=""url(#gSun)""/>

  <!-- Grand Arched Studio Window -->
  <path d=""M 300 240 L 300 120 C 300 65, 500 65, 500 120 L 500 240 Z"" fill=""#ffffff"" stroke=""#d9e2db"" stroke-width=""6""/>
  <line x1=""400"" y1=""72"" x2=""400"" y2=""240"" stroke=""#d9e2db"" stroke-width=""3""/>
  <line x1=""300"" y1=""150"" x2=""500"" y2=""150"" stroke=""#d9e2db"" stroke-width=""3""/>
  <polygon points=""310,240 220,330 580,330 490,240"" fill=""rgba(255,255,255,0.45)""/>

  <!-- Scandinavian Floor & Horizon -->
  <line x1=""0"" y1=""255"" x2=""800"" y2=""255"" stroke=""#e0e6e1"" stroke-width=""2""/>
  <line x1=""90"" y1=""255"" x2=""30"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""240"" y1=""255"" x2=""190"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""560"" y1=""255"" x2=""610"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""710"" y1=""255"" x2=""770"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>

  <!-- Large Braided Woven Circle Rug with Fringe -->
  <ellipse cx=""400"" cy=""285"" rx=""270"" ry=""56"" fill=""#e6eee9"" stroke=""#557c6c"" stroke-width=""3.5"" stroke-dasharray=""7 5""/>
  <ellipse cx=""400"" cy=""285"" rx=""220"" ry=""44"" fill=""#f5efe3"" stroke=""#d9caa8"" stroke-width=""2""/>

  <!-- Low Montessori Circle Table -->
  <ellipse cx=""400"" cy=""278"" rx=""100"" ry=""22"" fill=""url(#gWood)"" stroke=""#b89e72"" stroke-width=""2""/>
  <ellipse cx=""400"" cy=""275"" rx=""90"" ry=""18"" fill=""#f7eee0""/>

  <!-- Wicker Curiosity Basket overflowing with tactile items -->
  <path d=""M 370 250 L 430 250 L 424 274 L 376 274 Z"" fill=""#a87d55"" stroke=""#8c603a"" stroke-width=""2""/>
  <ellipse cx=""400"" cy=""250"" rx=""30"" ry=""7"" fill=""#c99c72""/>
  <!-- Basket Items: Pinecones, river stones, magnifying glass -->
  <ellipse cx=""390"" cy=""247"" rx=""9"" ry=""6"" fill=""#6b4928""/>
  <ellipse cx=""410"" cy=""246"" rx=""8"" ry=""5"" fill=""#8c9e94""/>
  <circle cx=""400"" cy=""243"" r=""11"" stroke=""#df9b3a"" stroke-width=""3"" fill=""rgba(240,248,244,0.7)""/>
  <line x1=""407"" y1=""250"" x2=""422"" y2=""262"" stroke=""#8c603a"" stroke-width=""4"" stroke-linecap=""round""/>

  <!-- Explorer Floor Cushions around table -->
  <ellipse cx=""260"" cy=""288"" rx=""42"" ry=""18"" fill=""#f4d89a"" stroke=""#df9b3a"" stroke-width=""2.5""/>
  <ellipse cx=""540"" cy=""288"" rx=""42"" ry=""18"" fill=""#f2cdc3"" stroke=""#d9826c"" stroke-width=""2.5""/>
  <ellipse cx=""400"" cy=""312"" rx=""48"" ry=""20"" fill=""#d5e5db"" stroke=""#557c6c"" stroke-width=""2.5""/>

  <!-- Potted Fiddle Leaf Tree (Right) -->
  <path d=""M 650 250 L 685 250 L 680 286 L 655 286 Z"" fill=""#d9826c""/>
  <path d=""M 667 250 Q 655 180 670 140"" stroke=""#31523c"" stroke-width=""4.5"" fill=""none""/>
  <ellipse cx=""662"" cy=""170"" rx=""24"" ry=""13"" fill=""#557c6c"" transform=""rotate(-25 662 170)""/>
  <ellipse cx=""682"" cy=""145"" rx=""26"" ry=""15"" fill=""#31523c"" transform=""rotate(20 682 145)""/>
  <ellipse cx=""665"" cy=""125"" rx=""22"" ry=""12"" fill=""#78998a""/>

  <!-- Hanging Potted Eucalyptus (Left) -->
  <line x1=""140"" y1=""0"" x2=""140"" y2=""85"" stroke=""#7c877f"" stroke-width=""2""/>
  <path d=""M 125 85 L 155 85 L 150 110 L 130 110 Z"" fill=""#ab9c76""/>
  <path d=""M 140 85 Q 110 120 120 150 Q 135 125 140 85"" fill=""#557c6c""/>
  <path d=""M 140 85 Q 170 115 160 155 Q 145 130 140 85"" fill=""#31523c""/>
  <path d=""M 140 85 Q 140 135 145 165 Q 150 135 140 85"" fill=""#78998a""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 1 · SENSORY INVITATION SETUP</text>

  <!-- Sparkles of curiosity -->
  <polygon points=""400,210 404,220 415,222 406,228 408,238 400,232 392,238 394,228 385,222 396,220"" fill=""#e9b65e""/>
  <circle cx=""438"" cy=""218"" r=""3.5"" fill=""#e9b65e""/>
  <circle cx=""362"" cy=""222"" r=""3"" fill=""#e9b65e""/>
</svg>",

            "challenge" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""cSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#faf8f3""/>
      <stop offset=""100%"" stop-color=""#eff5f1""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#cSky)""/>
  <circle cx=""400"" cy=""180"" r=""160"" fill=""#edf4f0""/>
  <circle cx=""400"" cy=""180"" r=""110"" fill=""#ffffff""/>

  <!-- Large Montessori Inquiry Workbench -->
  <rect x=""180"" y=""215"" width=""440"" height=""26"" rx=""8"" fill=""#ecd9ba"" stroke=""#ab9c76"" stroke-width=""2.5""/>
  <rect x=""220"" y=""241"" width=""24"" height=""85"" rx=""4"" fill=""#ab9c76""/>
  <rect x=""556"" y=""241"" width=""24"" height=""85"" rx=""4"" fill=""#ab9c76""/>
  <line x1=""180"" y1=""326"" x2=""620"" y2=""326"" stroke=""#e0e6e1"" stroke-width=""3""/>

  <!-- Centerpiece: Mystery Inquiry Box Glowing with Amber Light -->
  <rect x=""330"" y=""135"" width=""140"" height=""80"" rx=""10"" fill=""#557c6c""/>
  <rect x=""322"" y=""128"" width=""156"" height=""18"" rx=""6"" fill=""#31523c""/>
  <!-- Golden Ribbon & Bow -->
  <rect x=""392"" y=""128"" width=""16"" height=""87"" fill=""#e9b65e""/>
  <ellipse cx=""388"" cy=""120"" rx=""12"" ry=""7"" fill=""#e9b65e"" transform=""rotate(-20 388 120)""/>
  <ellipse cx=""412"" cy=""120"" rx=""12"" ry=""7"" fill=""#e9b65e"" transform=""rotate(20 412 120)""/>
  <circle cx=""400"" cy=""122"" r=""5"" fill=""#df9b3a""/>

  <!-- Giant Detailed Magnifying Glass with Glass Shine -->
  <circle cx=""260"" cy=""175"" r=""38"" stroke=""#df9b3a"" stroke-width=""8"" fill=""rgba(235, 246, 240, 0.75)""/>
  <line x1=""288"" y1=""203"" x2=""325"" y2=""238"" stroke=""#a87d55"" stroke-width=""10"" stroke-linecap=""round""/>
  <path d=""M 240 160 Q 260 148 280 160"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" fill=""none""/>

  <!-- Nature specimen jars with leaves and acorns -->
  <rect x=""495"" y=""165"" width=""36"" height=""50"" rx=""6"" fill=""rgba(240,248,244,0.85)"" stroke=""#78998a"" stroke-width=""2""/>
  <rect x=""492"" y=""160"" width=""42"" height=""8"" rx=""3"" fill=""#8c603a""/>
  <path d=""M 513 180 Q 520 195 513 205 Q 506 195 513 180 Z"" fill=""#557c6c""/>

  <rect x=""545"" y=""175"" width=""32"" height=""40"" rx=""6"" fill=""rgba(240,248,244,0.85)"" stroke=""#78998a"" stroke-width=""2""/>
  <rect x=""542"" y=""170"" width=""38"" height=""8"" rx=""3"" fill=""#8c603a""/>
  <circle cx=""561"" cy=""195"" r=""9"" fill=""#df9b3a""/>

  <!-- Wonder Question & Spark Constellations -->
  <text x=""225"" y=""120"" font-size=""44"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#557c6c"">?</text>
  <text x=""550"" y=""115"" font-size=""38"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#df9b3a"">?</text>
  <polygon points=""300,90 304,98 312,100 305,106 307,114 300,109 293,114 295,106 288,100 296,98"" fill=""#e9b65e""/>
  <polygon points=""510,80 513,87 520,89 514,94 516,101 510,96 504,101 506,94 500,89 507,87"" fill=""#557c6c""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 2 · THE WONDER INQUIRY</text>
</svg>",

            "explore" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""eSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#f6f8f5""/>
      <stop offset=""100%"" stop-color=""#eef4ee""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#eSky)""/>
  <ellipse cx=""400"" cy=""190"" rx=""320"" ry=""120"" fill=""#ffffff"" stroke=""#e1e9e3"" stroke-width=""2.5""/>

  <!-- Studio Craft Table Surface -->
  <rect x=""140"" y=""225"" width=""520"" height=""24"" rx=""8"" fill=""#eedfc4"" stroke=""#b89e72"" stroke-width=""2""/>
  <rect x=""180"" y=""249"" width=""22"" height=""75"" fill=""#b89e72""/>
  <rect x=""598"" y=""249"" width=""22"" height=""75"" fill=""#b89e72""/>

  <!-- Multi-Colored Montessori Building Blocks & Arches (Center-Left) -->
  <rect x=""260"" y=""195"" width=""65"" height=""30"" rx=""4"" fill=""#557c6c""/>
  <rect x=""335"" y=""195"" width=""65"" height=""30"" rx=""4"" fill=""#e9b65e""/>
  <rect x=""285"" y=""162"" width=""90"" height=""32"" rx=""4"" fill=""#d9826c""/>
  <!-- Arch Block -->
  <path d=""M 305 162 A 25 25 0 0 1 355 162 Z"" fill=""#ffffff""/>
  <polygon points=""330,110 285,160 375,160"" fill=""#31523c""/>
  <circle cx=""330"" cy=""95"" r=""12"" fill=""#df9b3a""/>

  <!-- Sculpting Clay Pot with Sprout (Center-Right) -->
  <ellipse cx=""475"" cy=""210"" rx=""36"" ry=""15"" fill=""#c9775f""/>
  <path d=""M 439 210 Q 439 235 475 235 Q 511 235 511 210 Z"" fill=""#b3654f""/>
  <line x1=""475"" y1=""210"" x2=""485"" y2=""145"" stroke=""#855e39"" stroke-width=""5"" stroke-linecap=""round""/>
  <circle cx=""485"" cy=""140"" r=""8"" fill=""#557c6c""/>
  <circle cx=""495"" cy=""152"" r=""7"" fill=""#78998a""/>
  <circle cx=""470"" cy=""160"" r=""6"" fill=""#31523c""/>

  <!-- Watercolor Artist Palette with Daubs (Far Left) -->
  <path d=""M 175 210 C 175 185 235 185 235 210 C 235 235 175 235 175 210 Z"" fill=""#faf5ec"" stroke=""#ab9c76"" stroke-width=""2""/>
  <circle cx=""195"" cy=""202"" r=""6"" fill=""#557c6c""/>
  <circle cx=""215"" cy=""200"" r=""6"" fill=""#df9b3a""/>
  <circle cx=""205"" cy=""216"" r=""6"" fill=""#d9826c""/>
  <circle cx=""188"" cy=""216"" r=""5"" fill=""#31523c""/>

  <!-- Textured Craft Paper & Animal Silhouette (Far Right) -->
  <rect x=""535"" y=""190"" width=""60"" height=""35"" rx=""4"" fill=""#f4d89a"" stroke=""#df9b3a"" stroke-width=""1.5""/>
  <polygon points=""550,210 565,195 580,210"" fill=""#557c6c""/>
  <!-- Craft sticks -->
  <line x1=""530"" y1=""225"" x2=""560"" y2=""225"" stroke=""#ab9c76"" stroke-width=""4"" stroke-linecap=""round""/>
  <line x1=""565"" y1=""225"" x2=""595"" y2=""225"" stroke=""#ab9c76"" stroke-width=""4"" stroke-linecap=""round""/>

  <!-- Children Handprints Motif in background -->
  <path d=""M 195 130 C 190 120 185 122 187 135 C 185 118 177 120 181 138 C 175 122 167 126 173 142 C 170 132 160 136 167 150 C 173 162 193 165 200 155 Z"" fill=""#f4d89a"" opacity=""0.7""/>
  <path d=""M 610 130 C 615 120 620 122 618 135 C 620 118 628 120 624 138 C 630 122 638 126 632 142 C 635 132 645 136 638 150 C 632 162 612 165 605 155 Z"" fill=""#e5eee8"" stroke=""#557c6c"" stroke-width=""1.5""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 3 · ACTIVE HANDS-ON FLOW</text>
</svg>",

            "magic" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <radialGradient id=""mGlow"" cx=""50%"" cy=""50%"" r=""50%"">
      <stop offset=""0%"" stop-color=""#fff9e6""/>
      <stop offset=""60%"" stop-color=""#eef7f2""/>
      <stop offset=""100%"" stop-color=""#f8faf8""/>
    </radialGradient>
    <linearGradient id=""mRainbow"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""rgba(217, 130, 108, 0.75)""/>
      <stop offset=""35%"" stop-color=""rgba(233, 182, 94, 0.75)""/>
      <stop offset=""70%"" stop-color=""rgba(85, 124, 108, 0.75)""/>
      <stop offset=""100%"" stop-color=""rgba(62, 98, 83, 0.75)""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#mGlow)""/>

  <!-- Luminous Starburst Rings -->
  <circle cx=""400"" cy=""175"" r=""130"" stroke=""#e6eee9"" stroke-width=""3.5"" stroke-dasharray=""7 7""/>
  <circle cx=""400"" cy=""175"" r=""90"" stroke=""#f4d89a"" stroke-width=""2.5"" stroke-dasharray=""5 5""/>

  <!-- Optical Refracting Crystal Prism -->
  <polygon points=""400,105 340,210 460,210"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""4.5""/>
  <polygon points=""400,105 400,210 460,210"" fill=""rgba(230,238,233,0.55)""/>

  <!-- Radiant Soft Rainbow Beam -->
  <polygon points=""400,160 620,90 660,125"" fill=""url(#mRainbow)""/>

  <!-- Giant Golden Wonder Sparkle -->
  <polygon points=""400,70 410,92 432,96 415,112 420,134 400,122 380,134 385,112 368,96 390,92"" fill=""#e9b65e""/>
  <circle cx=""400"" cy=""102"" r=""5"" fill=""white""/>

  <!-- Dancing Celebration Stars -->
  <polygon points=""240,125 244,134 253,136 245,142 248,151 240,145 232,151 235,142 227,136 236,134"" fill=""#df9b3a""/>
  <polygon points=""560,200 564,209 573,211 565,217 568,226 560,220 552,226 555,217 547,211 556,209"" fill=""#557c6c""/>
  <polygon points=""290,230 293,237 300,238 294,243 296,250 290,245 284,250 286,243 280,238 287,237"" fill=""#d9826c""/>

  <!-- Pedestal of Discovery -->
  <ellipse cx=""400"" cy=""250"" rx=""110"" ry=""18"" fill=""#e2ebe4"" stroke=""#557c6c"" stroke-width=""2.5""/>
  <text x=""400"" y=""295"" text-anchor=""middle"" font-size=""16"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#31523c"" letter-spacing=""2"">A MOMENT OF PURE DISCOVERY</text>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 4 · THE DISCOVERY SPARK</text>
</svg>",

            "reef" or "ocean" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""seaBg"" x1=""0%"" y1=""0%"" x2=""0%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#e6f4f1""/>
      <stop offset=""50%"" stop-color=""#ccebe5""/>
      <stop offset=""100%"" stop-color=""#a2dad0""/>
    </linearGradient>
  </defs>
  <rect width=""800"" height=""360"" fill=""url(#seaBg)""/>
  <!-- Gentle sea ripples -->
  <path d=""M0 120 Q 200 90 400 120 T 800 120"" stroke=""rgba(255,255,255,0.7)"" stroke-width=""5"" fill=""none""/>
  <path d=""M0 160 Q 200 130 400 160 T 800 160"" stroke=""rgba(255,255,255,0.5)"" stroke-width=""3"" fill=""none""/>
  <!-- Coral seafloor -->
  <path d=""M0 310 Q 250 280 500 310 T 800 300 L 800 360 L 0 360 Z"" fill=""#ebdcc5""/>
  <!-- Coral fans & sea anemones -->
  <path d=""M 120 310 C 100 240, 160 210, 140 310"" stroke=""#d9826c"" stroke-width=""14"" stroke-linecap=""round""/>
  <path d=""M 150 310 C 140 230, 200 220, 170 310"" stroke=""#e9b65e"" stroke-width=""10"" stroke-linecap=""round""/>
  <path d=""M 650 310 C 630 220, 710 200, 680 310"" stroke=""#557c6c"" stroke-width=""14"" stroke-linecap=""round""/>
  <!-- Colorful Fish Friends Swimming -->
  <ellipse cx=""320"" cy=""180"" rx=""24"" ry=""14"" fill=""#e9b65e""/>
  <polygon points=""340,180 358,168 358,192"" fill=""#df9b3a""/>
  <circle cx=""310"" cy=""177"" r=""3"" fill=""#26332d""/>

  <ellipse cx=""480"" cy=""140"" rx=""26"" ry=""15"" fill=""#d9826c""/>
  <polygon points=""502,140 522,128 522,152"" fill=""#c96e57""/>
  <circle cx=""470"" cy=""137"" r=""3"" fill=""#ffffff""/>

  <ellipse cx=""240"" cy=""220"" rx=""20"" ry=""11"" fill=""#557c6c""/>
  <polygon points=""258,220 274,210 274,230"" fill=""#31523c""/>
  <circle cx=""232"" cy=""218"" r=""2.5"" fill=""#ffffff""/>

  <!-- Bubbles -->
  <circle cx=""330"" cy=""140"" r=""7"" stroke=""white"" stroke-width=""2"" fill=""rgba(255,255,255,0.4)""/>
  <circle cx=""345"" cy=""115"" r=""5"" stroke=""white"" stroke-width=""1.5"" fill=""rgba(255,255,255,0.4)""/>
  <circle cx=""490"" cy=""100"" r=""6"" stroke=""white"" stroke-width=""2"" fill=""rgba(255,255,255,0.4)""/>
  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""320"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">OCEAN EXPLORATION REEF BLUEPRINT</text>
</svg>",

            "reflection" or _ => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""rSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#fdfbf7""/>
      <stop offset=""100%"" stop-color=""#f0f6f2""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#rSky)""/>

  <!-- Festive Natural Linen Bunting Garland Swagged across studio -->
  <path d=""M 100 45 Q 250 90 400 55 Q 550 90 700 45"" stroke=""#7c877f"" stroke-width=""2.5"" fill=""none""/>
  <!-- Pennants -->
  <polygon points=""160,60 185,65 172,98"" fill=""#557c6c""/>
  <polygon points=""235,72 260,72 248,108"" fill=""#e9b65e""/>
  <polygon points=""315,68 340,64 328,102"" fill=""#d9826c""/>
  <polygon points=""435,64 460,68 448,102"" fill=""#31523c""/>
  <polygon points=""515,72 540,72 528,108"" fill=""#df9b3a""/>
  <polygon points=""595,62 620,56 608,94"" fill=""#78998a""/>

  <!-- Proud Gallery Exhibition Display Shelf -->
  <rect x=""140"" y=""205"" width=""520"" height=""18"" rx=""5"" fill=""#ab9c76"" stroke=""#8c603a"" stroke-width=""2""/>
  
  <!-- Masterpiece Sculptures Proudly Displayed on Shelf -->
  <!-- 1. Wooden Arch Tower -->
  <rect x=""200"" y=""155"" width=""42"" height=""50"" rx=""6"" fill=""#557c6c""/>
  <circle cx=""221"" cy=""142"" r=""14"" fill=""#e9b65e""/>
  
  <!-- 2. Clay Pot & Leaf -->
  <polygon points=""400"" y=""125"" points=""400,125 430,203 370,203"" fill=""#d9826c""/>
  <circle cx=""400"" cy=""160"" r=""16"" fill=""#ffffff""/>
  <path d=""M 394 157 C 394 152 400 151 400 155 C 400 151 406 152 406 157 C 406 163 400 166 400 166 C 400 166 394 163 394 157 Z"" fill=""#d9826c""/>

  <!-- 3. Animal Collage & Sticks -->
  <rect x=""560"" y=""162"" width=""52"" height=""43"" rx=""6"" fill=""#31523c""/>
  <ellipse cx=""586"" cy=""150"" rx=""18"" ry=""10"" fill=""#f4d89a""/>

  <!-- Little Stars of Accomplishment -->
  <polygon points=""170,115 174,123 182,124 175,130 178,138 170,133 162,138 165,130 158,124 166,123"" fill=""#e9b65e""/>
  <polygon points=""630,115 634,123 642,124 635,130 638,138 630,133 622,138 625,130 618,124 626,123"" fill=""#e9b65e""/>
  <polygon points=""400,85 404,95 415,97 407,105 409,116 400,110 391,116 393,105 385,97 396,95"" fill=""#e9b65e""/>

  <!-- Closing Reflection Circle Rug -->
  <ellipse cx=""400"" cy=""285"" rx=""290"" ry=""48"" fill=""#eef5f1"" stroke=""#557c6c"" stroke-width=""2.5""/>
  <text x=""400"" y=""292"" text-anchor=""middle"" font-size=""16"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.5"">Stories Shared · Little Hands, Big Minds · Memories Made</text>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Roboto, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">FINAL PHASE · CIRCLE SHARING &amp; REFLECTION</text>
</svg>"
        };
    }
}
