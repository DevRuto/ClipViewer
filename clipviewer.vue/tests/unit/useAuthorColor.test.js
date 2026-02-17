import { describe, expect, it } from 'vitest'

import { useAuthorColor } from '@/composables/useAuthorColor.js'

describe('useAuthorColor', () => {
  it('returns a deterministic color for the same string', () => {
    const { stringToColor } = useAuthorColor()
    expect(stringToColor('alice')).toBe(stringToColor('alice'))
    expect(stringToColor('bob')).toBe(stringToColor('bob'))
  })

  it('returns a valid hex color', () => {
    const { stringToColor } = useAuthorColor()
    const color = stringToColor('alice')
    expect(color).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('returns default color for empty input', () => {
    const { stringToColor } = useAuthorColor()
    expect(stringToColor('')).toBe('#3B82F6')
    expect(stringToColor(null)).toBe('#3B82F6')
  })

  it('returns black text for light background and white text for dark background', () => {
    const { getContrastColor } = useAuthorColor()
    expect(getContrastColor('#ffffff')).toBe('#000000')
    expect(getContrastColor('#000000')).toBe('#FFFFFF')
  })
})
