import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'

import ThemeSwitcher from '@/components/ThemeSwitcher.vue'

function mockMatchMedia(prefersDark) {
  window.matchMedia = vi.fn().mockImplementation((query) => ({
    matches: prefersDark,
    media: query,
    addListener: vi.fn(),
    removeListener: vi.fn(),
  }))
}

describe('ThemeSwitcher', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
    mockMatchMedia(false)
  })

  afterEach(() => {
    document.documentElement.classList.remove('dark')
  })

  it('defaults to light mode when there is no saved preference and the system prefers light', async () => {
    const wrapper = mount(ThemeSwitcher)
    await wrapper.vm.$nextTick()

    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem('theme')).toBe('light')
    expect(wrapper.attributes('title')).toBe('Switch to dark mode')
  })

  it('defaults to dark mode when the system prefers dark and there is no saved preference', async () => {
    mockMatchMedia(true)
    const wrapper = mount(ThemeSwitcher)
    await wrapper.vm.$nextTick()

    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(wrapper.attributes('title')).toBe('Switch to light mode')
  })

  it('honors a saved theme preference over the system preference', () => {
    localStorage.setItem('theme', 'dark')
    mockMatchMedia(false)
    mount(ThemeSwitcher)

    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('toggles the theme and persists the choice when clicked', async () => {
    const wrapper = mount(ThemeSwitcher)
    expect(document.documentElement.classList.contains('dark')).toBe(false)

    await wrapper.find('button').trigger('click')

    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('theme')).toBe('dark')

    await wrapper.find('button').trigger('click')

    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem('theme')).toBe('light')
  })
})
