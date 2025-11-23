# 🎬 Director Simulator: Scene Builder  
**Game Design Document (GDD)**

Version: 1.0  
Author: Lai Vu  
Platform: PC (Windows, Mac), Android, iOS  
Release Date: TBD   
Engine: Unity (URP)  
Genre: Simulation / Sandbox / Creative Tool  
Development Stage: Prototype / Itchfunding Phase  

---

## 1. 🎯 High Concept
**Tagline:**  
> “Be the director of your own story — build, shoot, and share cinematic scenes.”

**Core Idea:**  
Players take the role of a film director in a virtual studio, where they can **build environments**, **direct actors**, **set cameras**, **record scenes**, and **tell stories** without needing filmmaking skills.  
The game blends the **freedom of The Sims** with the **creative control of a cinematic sandbox**.

---

## 2. 🧠 Vision & Design Goals
| Goal | Description |
|------|--------------|
| **Creative Freedom** | Allow players to construct any type of scene (comedy, sci-fi, romance, horror...) |
| **Intuitive Tools** | Provide a simple, visual interface for directing and building |
| **Shareability** | Let users export screenshots or video to share online |
| **Community Focus** | Encourage player-made content and modular add-ons |
| **Educational Use** | Suitable for creative learning and film direction training |

---

## 3. 🎮 Core Gameplay Loop
1. **Build Scene** – Choose environment, props, characters, lights  
2. **Direct Actors** – Set animations, dialogue, poses  
3. **Set Camera** – Adjust position, lens, focus, movement  
4. **Record / Screenshot** – Capture cinematic output  
5. **Share & Iterate** – Export or improve for the next scene  

➡️ *Loop repeats as the player refines skills and unlocks assets.*

---

## 4. 🧩 Gameplay Systems

### 4.1 Scene Builder
- Grid-based object placement (snap to floor/walls)  
- Modular props library (walls, lights, cameras, furniture)  
- Lighting editor: intensity, color, falloff  
- Weather & FX: fog, rain, volumetric light  

### 4.2 Character System
- Customizable models (face, clothes, expression)  
- Pose and animation library  
- Timeline for action sequencing  
- Dialogue editor (text-to-speech planned for later phase)

### 4.3 Camera System
- Free camera & orbit mode  
- Preset angles (wide, close-up, dolly)  
- Keyframed camera motion paths  
- Cinematic filters and depth of field options  

### 4.4 Export & Sharing
- Screenshot export (PNG/JPG)  
- Video recording (MP4)  
- Online gallery integration (future update)

---

## 5. 🧰 Progression & Meta Systems
| System | Description |
|---------|--------------|
| **Unlockables** | Earn new props, FX, and lighting tools through creative milestones |
| **Achievements** | Example: “First Scene Recorded”, “10 Actors Directed” |
| **Save Projects** | Multiple scene slots |
| **Director Rank** | Level up based on number and quality of projects |

---

## 6. 💡 Creative Features (Differentiators)
| Feature | Description |
|----------|--------------|
| **Scene-as-Canvas** | A visual storytelling sandbox, not a traditional builder |
| **Realistic Cinematic Tools** | Camera & light simulation close to real film setup |
| **Workshop Integration** | User-created content and mod support |
| **Playable Scenes** | Scenes act as short playable cutscenes |

---

## 7. 🖥️ User Interface / UX
**Layout Concept:**
- Left Panel → Asset Library (props, actors, lights)  
- Center → 3D Scene View  
- Right Panel → Properties (transform, material, animation)  
- Bottom → Timeline for actions, camera, FX  
- Top Bar → Record, playback, export, lighting presets  

*UX Goal:* “As intuitive as The Sims, as powerful as Unreal Sequencer.”

---

## 8. 🧑‍💻 Technical Overview
| Category | Implementation |
|-----------|----------------|
| **Engine** | Unity (URP) |
| **Save System** | JSON-based scene data |
| **Camera Capture** | RenderTexture or Timeline Recorder |
| **Lighting System** | URP Light + Volumetric FX |
| **UI Framework** | Unity UI Toolkit / uGUI |
| **Asset System** | Modular ScriptableObjects |
| **Cloud Sync (Future)** | Firebase or Supabase backend |

---

## 9. 🎨 Art Direction
| Aspect | Description |
|---------|-------------|
| **Style** | Realistic-stylized hybrid |
| **Palette** | Neutral tones with cinematic lighting |
| **Reference Titles** | The Sims 4, Filmmaker Tycoon, Unreal Sequencer |
| **UI Design** | Flat, minimalistic, dark background with neon accents |
| **Camera FX** | Depth of field, lens flare, chromatic aberration |

---

## 10. 🎵 Audio Design
- **Ambient Soundtrack:** soft cinematic loops to aid focus  
- **SFX:** clicks, prop placement, camera shutter, lighting hum  
- **Voice:** placeholder voice or AI-generated (later phase)  
- **Music Packs:** optional DLC (romance / thriller / sci-fi themes)

---

## 11. 🪙 Monetization Plan
| Type | Description |
|-------|-------------|
| **Base Game** | $15–25 Early Access version on Itch.io |
| **Expansion Packs** | Paid DLCs (props, camera, character packs) |
| **Workshop Marketplace** | User-generated asset sharing (10–15% fee) |
| **Education License** | School/creative classroom edition |
| **Collabs** | Sponsored “Scene Challenges” with creators/brands |

---

## 12. 📈 Target Audience
| Group | Motivation |
|--------|-------------|
| 🎥 Filmmakers | Want to visualize stories virtually |
| 🎮 Sandbox Players | Fans of The Sims, Garry’s Mod, or Movie Tycoon |
| 🧑‍🏫 Educators | Use for teaching directing, lighting, or film theory |
| 📱 Content Creators | Quick cinematic clip creation for YouTube/TikTok |

---

## 13. 🗺️ Roadmap
| Phase | Timeframe | Key Features |
|--------|------------|---------------|
| **Prototype** | Now | Scene building + basic camera system |
| **Alpha** | Q2 2026 | Character animations, lighting tools |
| **Beta** | Q4 2026 | Export video, workshop integration |
| **Full Release** | 2027 | Marketplace + multiplayer scene sharing |

---

## 14. 💬 Team & Roles
| Role | Member | Responsibility |
|-------|----------|----------------|
| **Game Director** | Lai Vu | Vision, GDD, funding, coordination |
| **Programmer(s)** | TBD | Core systems, camera, UI, saving |
| **3D Artist** | TBD | Props, characters, environments |
| **UI/UX Designer** | TBD | Interface design |
| **Sound Designer** | Freelance | Music & SFX |
| **Community Manager** | TBD | Devlogs, social channels, player feedback |

---

## 15. 📢 Pitch Summary
> “Director Simulator: Scene Builder is a cinematic sandbox where anyone can build and direct film scenes. It empowers storytellers to create, record, and share their imagination.”

**Funding Goal:** $15,000 (via Itchfunding)  
**Objective:** Complete Alpha build within 6 months.  

---

## 🧾 Notes
- This GDD can evolve into a **TDD (Technical Design Document)** for programmers.  
- Future add-ons can include VR/AR support, multiplayer co-directing, and workshop monetization.  
- Suitable for showcasing on **Itch.io, Kickstarter, or Steam Greenlight**.

---
