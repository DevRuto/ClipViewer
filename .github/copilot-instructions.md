# ClipViewer AI Coding Instructions

## Project Overview

ClipViewer is a Vue 3 single-page application built with Vite, designed for clipboard viewing and management. It uses Vue Router for navigation, Tailwind CSS for styling, and follows modern ES module patterns.

## Architecture & Key Components

### Framework Stack

- **Vue 3**: Composition API (via `<script setup>`) for component logic
- **Vue Router 4**: Client-side routing configured in [main.js](../src/main.js)
- **Vite 7**: Build tool and dev server with hot module replacement
- **Tailwind CSS 4**: Utility-first CSS via `@tailwindcss/vite` plugin

### Current State

- Minimal scaffolding: Only [App.vue](../src/App.vue) exists as the root component
- Router configured with single route: `/` → App component
- CSS entry point: [main.css](../src/main.css) imports Tailwind directives

### Module Path Alias

The Vite config defines `@` alias pointing to `src/` directory. Use it for imports:

```javascript
import MyComponent from '@/components/MyComponent.vue'
```

## Development Workflows

### Startup

```bash
npm install          # Install dependencies
npm run dev          # Start Vite dev server (HMR enabled)
```

Dev server runs at `http://localhost:5173` by default (check terminal for actual port).

### Build & Preview

```bash
npm run build        # Production build to /dist
npm run preview      # Serve built dist locally
```

### Code Quality

```bash
npm run lint         # ESLint with auto-fix and cache
npm run format       # Prettier formatting for src/
```

## Project Conventions

### Component Structure

Use single-file components (`.vue`) with `<script setup>` syntax:

- **Template**: Single root element for proper Vue 3 reactivity
- **Script**: Use `<script setup>` instead of `setup()` option
- **Style**: Scoped styles with `<style scoped>` to avoid conflicts

### Naming Conventions

- Components: PascalCase file names (e.g., `ClipboardViewer.vue`)
- Props/functions: camelCase
- CSS classes: Use Tailwind utility classes; avoid custom classes unless unavoidable
- Route names: lowercase-with-hyphens paths (e.g., `/clipboard-viewer`)

### Styling Approach

- **Primary**: Tailwind CSS utility classes in templates
- **Avoid**: Custom CSS files except for non-Tailwind features
- **Scoped styles**: Only use `<style scoped>` for component-specific logic that Tailwind can't handle

## Linting & Code Standards

### ESLint Rules

Configuration in [eslint.config.js](../eslint.config.js) enforces:

- Vue 3 essential rules (no template syntax errors)
- JavaScript recommended rules
- Prettier formatting integration (auto-formats on lint)
- Global ignores: `/dist`, `/dist-ssr`, `/coverage`

Run `npm run lint` before commits. ESLint uses caching for faster subsequent runs.

### Dependencies

All dependencies locked in `package.json`. Node engine requirement: `^20.19.0 || >=22.12.0`

## Integration Points

### Browser APIs

- Vue DevTools available for debugging (configured in Vite)
- Tailwind DevTools may be helpful for responsive design testing

### Entry Point

[index.html](../index.html) is the Vite entry point; [main.js](../src/main.js) initializes Vue app with Router.

## Common Tasks

**Add a new route**: Edit [main.js](../src/main.js) `routes` array, create component in `src/`

**Create a component**: Add `.vue` file with `<script setup>`, template, and scoped style

**Update styling**: Prefer Tailwind classes; add custom styles only to `<style scoped>` blocks

**Debug**: Use Vue DevTools browser extension; Vite shows HMR errors in console
