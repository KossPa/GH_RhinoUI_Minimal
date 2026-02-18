# GH_RhinoUI_Minimal

A lightweight Grasshopper C# script that replaces cluttered canvas sliders with a clean floating control panel inside Rhino. Supports saving and loading parameter presets as JSON files.

![Rhino 6](https://img.shields.io/badge/Rhino-6-red) ![Grasshopper](https://img.shields.io/badge/Grasshopper-C%23-green)

---

## Features

- Floating Eto panel that controls GH sliders in real time
- Min, max and decimal places are read directly from your GH sliders — no hardcoding
- Save and load parameter presets as `.json` files
- Easily scalable — adding a new parameter is one line of code

---

## Requirements

- Rhino 6
- Grasshopper
- `Eto.dll` and `Newtonsoft.Json.dll` (both ship with Rhino 6)

---

## Setup

1. Open the `.gh` file in Grasshopper
2. Make sure your Number Sliders have unique nicknames (right-click slider → Edit)
3. In the C# component, find `PARAMS` at the top of the Additional Code section and list your slider nicknames:

```csharp
static List<ParamDef> PARAMS = new List<ParamDef>
{
    new ParamDef("FloorHeight"),
    new ParamDef("NumFloors"),
    new ParamDef("FacadeGridX"),
};
```

4. Toggle the Boolean Toggle on the canvas to open the panel

---

## Adding a New Parameter

1. Add a Number Slider to the GH canvas and give it a unique nickname
2. Add one line to `PARAMS`:

```csharp
new ParamDef("YourSliderNickname"),
```

3. Toggle the panel off and on again — the new row appears automatically

---

## Presets

- **Save Preset** — saves current slider values to a `.json` file in `~/GH_Presets/`
- **Load Preset** — loads a `.json` file and applies values to all matching sliders
- **Sync from GH** — pulls current GH slider values into the panel if they get out of sync

Example preset:

```json
{
  "FloorHeight": 3.20,
  "NumFloors": 8.0,
  "FacadeGridX": 1.50
}
```

---
