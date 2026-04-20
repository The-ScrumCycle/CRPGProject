# 🌍 Shattered World: Verge

![Unity](https://img.shields.io/badge/Made_with-Unity-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/Language-C%23-blue?style=for-the-badge&logo=c-sharp)
![Status](https://img.shields.io/badge/Status-Release-brightgreen?style=for-the-badge)

**Shattered World: Verge** is a tactical RPG that blends expansive open-world exploration with deep, deterministic hex-grid combat. Inspired by classic RPG design, the game trusts players to chart their own course, discover hidden islands, and survive grueling boss encounters with minimal hand-holding. 

---

## 📸 Media
> **Note:** Gameplay trailer and screenshots coming soon!
> 
> *[Placeholder: Main Menu Image]*
> *[Placeholder: Open World Exploration]*
> *[Placeholder: Tactical Combat Arena]*

---

## ⚔️ Key Features

### 🗺️ Unforgiving Open World
Explore a vast, hand-crafted world filled with hostile encounters, hidden islands, and formidable bosses. We designed the world to reward curiosity and bravery, relying on environmental storytelling, scripted sequences, and rich NPC interactions rather than waypoint markers.

### 🗣️ Dynamic Dialogue Trees
Engage with the inhabitants of the Verge through an extensive, branching dialogue system. Your choices and classic RPG player stats actively unlock (or lock) specific conversational paths, shaping your journey and interactions.

### ♟️ Deterministic Hex-Grid Combat
Transition seamlessly from real-time exploration into highly tactical, turn-based combat arenas. 
* **Telegraphed AI:** Inspired by modern tactical masterpieces like *Into the Breach*, enemies generate an `ActionIntent` a turn in advance. Danger zones and attack vectors are visually rendered, turning combat into a lethal puzzle of strategic counter-play.
* **Physics & Displacement:** Control the battlefield using precise knockbacks, grapples, and wall-collision damage.

### 💾 Persistent World State
A robust Save/Load architecture that accurately serializes and deserializes party stats, inventory, enemy encounter states, and complex dialogue tree progression across sessions.

---

## 🛠️ Technical Architecture

Under the hood, **Shattered World: Verge** is built on a highly decoupled, scalable architecture designed for a smooth development pipeline:

* **Engine:** Unity (C#)
* **Combat Systems:** Built on a custom `CombatManager` and `ActionResolver` that strictly validate moves, abilities, and physics prior to execution. 
* **Dynamic UI & MVC Pattern:** Custom UI managers read world-space coordinates to dynamically scale Action Bars, Roster Cards, and floating health/damage previews. The UI automatically magnetizes and adjusts as units are defeated or take damage.
* **Data-Driven Spawning:** Encounter and unit configurations are driven by scriptable objects and case-insensitive tag-to-prefab factories, ensuring designers can rapidly build encounters without touching code.

---

## 👥 The Team (The Scrum Cycle)

* **Carlos** - *Team Leader + Lead Open World Dev*
* **Raphael** - *Lead Combat Engine Dev*
* **Jonah**
* **Patrick**
* **Biswas**
* **Hao**
* **Alex**

---
*© 2026 The Scrum Cycle. All Rights Reserved.*
