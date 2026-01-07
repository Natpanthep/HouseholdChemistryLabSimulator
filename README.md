# 🧪 Household Chemistry Lab (Unity)

**Household Chemistry Lab** is an interactive **2D educational game** built with **Unity**, allowing players to perform simple chemistry experiments by mixing household ingredients.

Players discover reactions, unlock a recipe book, build combo streaks, and complete all experiments to reach **100% progress**.

---

## 🎮 Gameplay Overview

- Drag household ingredient bottles into a **Beaker**
- Combine **2 ingredients** to trigger chemical reactions

### ✅ Successful Reactions
- Show reaction result, icon, and fun facts
- Unlock entries in the **Recipe Book**
- Increase **Combo Score** *(only for new reactions)*

### ❌ Failed Reactions
- Break the combo
- Show **“Failed”** feedback

🎯 **Goal:** Discover all reactions and reach **100% completion**.

---

## ✨ Key Features

### 🧪 Perform Experiments
- Drag & drop ingredients into the beaker
- Real-time reaction detection via `ReactionDatabase`
- Visual effects (particles, color changes)
- Sound effects for success, failure, and interactions

### 📖 Recipe Book
- Automatically records discovered reactions
- Displays:
  - Product name
  - Reaction display name
  - Fun fact
  - Reaction icon
- Persistent progress using `PlayerPrefs`

### 🔢 Combo System
- Combo increases **only** when a **new reaction** is discovered
- Repeating an already-known reaction does **not** increase combo
- Failed reactions reset the combo
- Highest combo is preserved until **Trash / Reset logic** is applied

### 🔄 Reset & Trash Controls

**Reset Button**
- Clears the beaker and visuals
- Brings ingredients back
- **Does not** reset combo

**Trash Button**
- Clears all loose ingredients
- Clears beaker contents
- Resets combo

### 📊 Progress System
- Circular progress indicator shows completion percentage
- Progress reaches **100%** when all reactions are discovered
- Triggers a **Congratulations screen** with sound

---

## 🏠 Main Menu
- Play Game
- Settings (Volume, Fullscreen, Quality)
- How To Play
- Credits
- Quit

---

## ⚙️ Settings
- Master Volume control
- Fullscreen modes:
  - Windowed
  - Borderless
  - Exclusive Fullscreen
- Quality levels (resolution & clarity)
- Settings saved between sessions

---

## 🧩 System Architecture

### Core Systems
- **Beaker** – Handles ingredient input, reactions, combos, FX/SFX
- **ReactionDatabase** – Defines valid ingredient combinations
- **RecipeBookManager** – Tracks discovered reactions & progress
- **Ingredient / Draggable2D** – Ingredient behavior
- **SettingsPanel** – Audio, fullscreen, quality control
- **MainMenu** – Navigation & UI flow

### Design Pattern
- Event-driven interaction
- Data-driven reactions via **ScriptableObjects**
- UI separation from gameplay logic

---

## 🛠️ Tech Stack
- **Engine:** Unity (2D)
- **Language:** C#
- **UI:** Unity UI + TextMeshPro
- **Data:** ScriptableObjects
- **Persistence:** PlayerPrefs
- **Render Pipeline:** Built-in Render Pipeline

---

## 📂 Project Structure (Simplified)

```text
Assets/
├─ Scripts/
│  ├─ Gameplay/
│  │  ├─ Beaker.cs
│  │  ├─ Ingredient.cs
│  │  ├─ Draggable2D.cs
│  ├─ Systems/
│  │  ├─ RecipeBookManager.cs
│  │  ├─ ReactionDatabase.cs
│  ├─ UI/
│  │  ├─ RecipeRowUI.cs
│  │  ├─ SettingsPanel.cs
│  │  ├─ MainMenu.cs
├─ ScriptableObjects/
│  ├─ IngredientSO
│  ├─ ReactionDefinition
├─ Scenes/
│  ├─ MainMenu
│  ├─ Main (Lab)
```

---

## 🧑‍💻 Developer
- **Developed by:** natpanthep
- **Project Type:** Educational Game / Game-Based Learning
- **Purpose:** Learning, Portfolio, Academic Use

---

## 📜 License

This project is intended for **educational and personal use**.
Assets and third-party resources are credited where applicable.

