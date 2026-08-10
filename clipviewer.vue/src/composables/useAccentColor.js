import { ref } from 'vue'

export const ACCENT_COLORS = [
  { value: 'blue', label: 'Blue', swatch: '#2563eb' },
  { value: 'violet', label: 'Violet', swatch: '#8b5cf6' },
  { value: 'green', label: 'Green', swatch: '#16a34a' },
  { value: 'orange', label: 'Orange', swatch: '#ea580c' },
  { value: 'rose', label: 'Rose', swatch: '#e11d48' },
  { value: 'neutral', label: 'Neutral', swatch: '#525252' },
]

const accent = ref('blue')

function applyAccent() {
  if (accent.value === 'blue') {
    delete document.documentElement.dataset.accent
  } else {
    document.documentElement.dataset.accent = accent.value
  }
}

export function useAccentColor() {
  function setAccent(value) {
    accent.value = value
    localStorage.setItem('accent', value)
    applyAccent()
  }

  function initAccent() {
    const saved = localStorage.getItem('accent')
    accent.value = ACCENT_COLORS.some((c) => c.value === saved) ? saved : 'blue'
    applyAccent()
  }

  return {
    accent,
    setAccent,
    initAccent,
  }
}
