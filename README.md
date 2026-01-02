# MRLGlitch_Template (Unity / Meta Quest)

A Unity project template for a short Mixed Reality (MR) experience on **Meta Quest** using **Passthrough**, a **Timeline-driven sequence** (≈5 minutes), and multiple animated avatars.

This repo is meant to be a starting point for building and iterating on a scene where:
- a 3D environment (“Space”) is placed in the scene,
- several animated avatars (01_Anim, 02_Anim, …) appear at specific times,
- a soundtrack is synced with the experience,
- Passthrough remains enabled for MR testing.

---

## What’s inside

- **Meta XR / Passthrough setup** (MR template scene)
- **TimelineRoot** with a Timeline asset (sequence control)
- **5 avatars** (01_Anim … 05_Anim) ready to be triggered by Timeline
- **Soundtrack** track on Timeline
- **Space** environment imported as FBX (e.g. `00_Basement.fbx`)
- A few practical fixes documented (offscreen animation culling, transform offsets, etc.)

---

## Requirements

- **Unity 6** (tested with `Unity 6.0 (6000.x)` — check `ProjectSettings/ProjectVersion.txt`)
- **Meta XR SDK / Meta XR Tools** installed in the project
- Android build support installed in Unity Hub
- A **Meta Quest** device (Quest 3 used during testing)

> If you open the project and Unity asks to update packages, accept only if you’re comfortable updating the whole template.


