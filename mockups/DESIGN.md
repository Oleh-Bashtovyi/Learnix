---
name: Cognitive Clarity
colors:
  surface: '#f7f9fb'
  surface-dim: '#d8dadc'
  surface-bright: '#f7f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef0'
  surface-container-high: '#e6e8ea'
  surface-container-highest: '#e0e3e5'
  on-surface: '#191c1e'
  on-surface-variant: '#424754'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#727785'
  outline-variant: '#c2c6d6'
  surface-tint: '#005ac2'
  primary: '#0058be'
  on-primary: '#ffffff'
  primary-container: '#2170e4'
  on-primary-container: '#fefcff'
  inverse-primary: '#adc6ff'
  secondary: '#712ae2'
  on-secondary: '#ffffff'
  secondary-container: '#8a4cfc'
  on-secondary-container: '#fffbff'
  tertiary: '#924700'
  on-tertiary: '#ffffff'
  tertiary-container: '#b75b00'
  on-tertiary-container: '#fffbff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d8e2ff'
  primary-fixed-dim: '#adc6ff'
  on-primary-fixed: '#001a42'
  on-primary-fixed-variant: '#004395'
  secondary-fixed: '#eaddff'
  secondary-fixed-dim: '#d2bbff'
  on-secondary-fixed: '#25005a'
  on-secondary-fixed-variant: '#5a00c6'
  tertiary-fixed: '#ffdcc6'
  tertiary-fixed-dim: '#ffb786'
  on-tertiary-fixed: '#311400'
  on-tertiary-fixed-variant: '#723600'
  background: '#f7f9fb'
  on-background: '#191c1e'
  surface-variant: '#e0e3e5'
typography:
  h1:
    fontFamily: Plus Jakarta Sans
    fontSize: 48px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.02em
  h2:
    fontFamily: Plus Jakarta Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: '1.3'
    letterSpacing: -0.01em
  h3:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.4'
  body-lg:
    fontFamily: Work Sans
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Work Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.6'
  label-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '600'
    lineHeight: '1'
    letterSpacing: 0.05em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
---

## Brand & Style

This design system is built on the philosophy of **Modern Corporate minimalism with a high-fidelity finish**. It targets lifelong learners and students who require a focused environment that feels both intellectually serious and technologically advanced. 

The aesthetic leverages "Soft UI" principles—combining the structured reliability of professional SaaS platforms with the fluid, approachable energy of modern consumer tech. The interface should feel breathable and intentional, using generous whitespace to reduce cognitive load during complex learning sessions. The emotional response is one of clarity, momentum, and confidence.

## Colors

The palette is anchored by a vibrant **Electric Blue** for primary actions, symbolizing intelligence and trust. The **Sleek Purple** serves as a strategic accent color, specifically reserved for "premium" interactions and AI-driven features (like the AI Tutor), creating a distinct visual lane for cognitive assistance.

The background palette utilizes a cool-toned Slate scale. The base layer is nearly white (#f8fafc) to maintain a "paper-like" cleanliness, while cards and surface elements use pure white (#ffffff) to pop against the background. Text uses a deep Navy Slate (#0f172a) rather than pure black to maintain a high-fidelity, polished feel.

## Typography

This design system utilizes a dual-font strategy. **Plus Jakarta Sans** is used for headings and labels; its slightly rounded, geometric terminals provide a friendly and optimistic tone. For body copy, **Work Sans** (serving as a high-performance alternative to DM Sans) is used to ensure maximum readability during long-form reading, providing a neutral and grounded structure.

Visual hierarchy is achieved through significant scale shifts between headings and body text. Labels use a slightly tighter tracking and semi-bold weight to distinguish them from narrative content.

## Layout & Spacing

The system employs a **12-column fluid grid** for desktop, transitioning to a single-column layout for mobile. A strict 8px spacing power-of-two scale ensures mathematical harmony across all components.

Layouts should favor "Center-Out" composition for focused learning tasks (like quiz interfaces) and "Left-Heavy" sidebar navigation for the main dashboard. Generous padding within cards (minimum 24px) is required to maintain the clean, high-fidelity aesthetic.

## Elevation & Depth

This design system utilizes **Ambient Shadows** to create a sense of tactile layering. Shadows are never pure gray; they are subtly tinted with the primary blue (#3b82f6) at very low opacities (3-5%) to ensure they feel integrated with the background.

- **Level 1 (Default Cards):** 0px 4px 20px rgba(59, 130, 246, 0.05).
- **Level 2 (Hover/Active):** 0px 12px 32px rgba(59, 130, 246, 0.1).
- **Level 3 (Modals/Overlays):** 0px 24px 48px rgba(15, 23, 42, 0.12).

Depth is further enhanced by subtle 1px borders in a lighter shade than the background (Slate-200) to define edges even when shadows are minimal.

## Shapes

The shape language is defined as **Rounded**, avoiding both the sharpness of traditional corporate UI and the "bubbly" feel of children's educational tools. 

Standard components (buttons, inputs) use a 0.5rem (8px) radius. Larger layout containers and cards use a 1rem (16px) radius to soften the overall visual footprint of the page. This creates a modern, sophisticated silhouette that feels contemporary and approachable.

## Components

### Buttons
Primary buttons use a solid gradient of the primary blue or secondary purple with white text. They feature a subtle 1px "inner glow" border on the top edge to provide a high-fidelity, tactile look.

### Cards
Cards are the primary content vehicle. They must have a white background, Level 1 elevation, and a 1px Slate-100 border. For AI Tutor features, cards should utilize a subtle purple-to-blue linear border (2px) to denote "Intelligence."

### Input Fields
Inputs use a soft Slate-50 background with a 1px border. On focus, the border transitions to Primary Blue with a 3px soft outer glow (the blue color at 10% opacity).

### AI Tutor Module
The AI Tutor interface should be distinguished by a "Glassmorphic" panel—using a backdrop-blur (12px) and a semi-transparent purple tint. This visually separates human-generated content from AI-generated insights.

### Progress Indicators
Use thick (8px) rounded bars. Completed segments should use a gradient from Blue to Purple to indicate growth and achievement.