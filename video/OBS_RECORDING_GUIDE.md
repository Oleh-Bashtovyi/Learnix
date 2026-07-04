# OBS Studio Recording Guide for Web Application Demos

This guide outlines the configuration process in OBS Studio optimized for capturing high-fidelity web application demonstrations. Following these parameters ensures crisp text rendering, fluid UI transitions, and optimal file size distribution.

---

## 1. Environment Isolation (Profiles & Scene Collections)

To prevent overwriting your daily streaming or recording configurations, isolate your technical and visual setups using Profiles and Scene Collections.

### Technical Profiles
* **UI Location:** Top Menu Bar `->` `Profile` `->` `New`
* **Action:** Name the profile (e.g., `Web_Demo_Config`) and complete or skip the auto-configuration wizard.
* **Justification:** Profiles store backend configurations, including encoders, resolutions, bitrates, and audio routing. Switching profiles instantly reloads these parameters without manual re-entry.

### Visual Scene Collections
* **UI Location:** Top Menu Bar `->` `Scene Collection` `->` `New`
* **Action:** Name the collection (e.g., `Web_Demo_Scenes`).
* **Justification:** Scene Collections store the layout, ordering, cropping, and filtering of your captured sources (e.g., windows, cameras, overlays). This creates a blank workspace dedicated exclusively to the application capture.

---

## 2. Video Canvas Configuration

Configure the baseline canvas dimensions to match native display layouts and prevent scaling artifacts.

* **UI Location:** `Settings` `->` `Video`

| Parameter | Configuration Value | Technical Justification |
| :--- | :--- | :--- |
| **Base (Canvas) Resolution** | `1920x1080` | Matches standard 1080p browser presentation sizes. |
| **Output (Scaled) Resolution** | `1920x1080` | Eliminates downstream scaling filters by matching the canvas resolution exactly. This prevents the interpolation and blurring of small typography and fine interface lines. |
| **Downscale Filter** | *Grayed out / Disabled* | Automatically disabled because the base and output resolutions match, avoiding computational overhead. |
| **Common FPS Values** | `60` | Provides smooth capture of rapid interface interactions, such as scrolling dynamic layouts, opening dropdown menus, and CSS transitions. |

---

## 3. Advanced Output & Encoding Parameters

Advanced mode unlocks precise quantization controls required for structural text preservation during recording.

* **UI Location:** `Settings` `->` `Output` `->` Change **Output Mode** dropdown from *Simple* to **Advanced** `->` Navigate to the **Recording** tab.

### Recording File Options
* **Type:** `Standard`
* **Recording Format:** `Matroska (.mkv)`
  * **Justification:** A container format resilient to data corruption. In the event of an OS crash, power failure, or resource depletion, the recording up to the exact moment of failure remains readable. Remux the final asset to `.mp4` post-recording using the native OBS tool (`File` `->` `Remux Recordings`).

### Encoder Settings
* **Video Encoder:** `NVIDIA NVENC H.264` (or `AMD HW H.264` / `Intel QuickSync H.264`)
  * **Justification:** Offloads the computational burden of video compression onto a dedicated hardware ASIC chip located on the GPU. This isolates encoding tasks, keeping the Central Processing Unit (CPU) fully available for running background Docker containers, local/remote database instances, application servers, and heavy browser instances.

### Encoder Fine-Tuning
* **Rate Control:** `CQP` (Constant Quantization Parameter)
  * **Justification:** Fixes the visual quality level of each frame rather than targeting a strict bit limit per second. Static web layouts require minimal bits to maintain structure, while fast actions like scrolling trigger immediate, on-demand bitrate expansion. This maximizes efficiency while guaranteeing uniform visual quality.
* **CQ Level:** `20` (or `22` for slightly smaller files)
  * **Justification:** Delivers mathematically and visually near-lossless compression. Text edges remain sharp and free of macroblocking or compression noise.
* **Keyframe Interval:** `2 s`
  * **Justification:** Forces the encoder to generate a complete reference frame every two seconds, ensuring reliable timeline scrubbing and indexing during post-editing or repository playback.
* **Preset:** `P5: Slow (Good Quality)`
  * **Justification:** Balances high-quality multipass encoding logic with minimal latency, optimal for H.264 hardware profiles.
* **Tuning:** `High Quality`
* **Multipass Mode:** `Two Passes (Quarter Resolution)`
* **Profile:** `high`

---

## 4. Visual Source Acquisition (Window Capture)

Capture specific software execution layers while excluding native operating system artifacts.

* **UI Location:** `Sources` Panel `->` Click `+` `->` Select **Window Capture**

| Parameter | Configuration Value | Technical Justification |
| :--- | :--- | :--- |
| **Window** | `[Select target browser window]` | Targets the explicit process execution handle of your active browser. |
| **Capture Method** | `Windows 10 (1903 and up)` | Leverages modern OS graphics capture APIs. This resolves the black-screen rendering issue common to browsers utilizing hardware-accelerated UI compositing. |
| **Window Match Priority** | `Match title, otherwise find window of same type` | Maintains source binding even if navigation within the web application modifies the active browser tab's title string. |
| **Capture Cursor** | `Enabled` | Ensures user interactions, click paths, hover states, and navigational intent are clearly visible to repository reviewers. |
| **Client Area** | `Enabled` | Automatically clips the native OS window frame borders from the frame buffer, maximizing real estate for the web application layout. |

### Operational Notes
1. **Full-Screen Focus:** Enter full-screen execution inside the browser by pressing `F11` before initiating capture. This completely removes the URL address bar, extension icons, and bookmarks from the recording plane.
2. **Manual Cropping:** If full-screen mode cannot be used, hold the `Alt` key while clicking and dragging the red bounding box edges in the OBS viewport to manually crop out unwanted interface rows.
