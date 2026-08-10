import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'

import ClipTile from '@/components/ClipTile.vue'

const baseVideo = {
  name: 'My Clip',
  duration: 125,
  createdAt: '2026-01-15T00:00:00Z',
  author: 'alice',
  thumbnail: '/files/thumbnails/abc.jpg',
}

describe('ClipTile', () => {
  it('renders the video name, formatted duration, and author', () => {
    const wrapper = mount(ClipTile, { props: { video: baseVideo } })

    expect(wrapper.text()).toContain('My Clip')
    expect(wrapper.text()).toContain('2:05')
    expect(wrapper.text()).toContain('alice')
  })

  it('renders the thumbnail image when present', () => {
    const wrapper = mount(ClipTile, { props: { video: baseVideo } })

    const img = wrapper.find('img')
    expect(img.attributes('src')).toBe(baseVideo.thumbnail)
  })

  it('renders no image (just the background placeholder) when there is no thumbnail', () => {
    const wrapper = mount(ClipTile, { props: { video: { ...baseVideo, thumbnail: '' } } })

    expect(wrapper.find('img').exists()).toBe(false)
  })

  it('gives the same author consistent badge coloring', () => {
    const wrapperA = mount(ClipTile, { props: { video: baseVideo } })
    const wrapperB = mount(ClipTile, { props: { video: { ...baseVideo, name: 'Other Clip' } } })

    const colorA = wrapperA.findAll('div').find((d) => d.text() === 'alice').attributes('style')
    const colorB = wrapperB.findAll('div').find((d) => d.text() === 'alice').attributes('style')
    expect(colorA).toBe(colorB)
  })
})
