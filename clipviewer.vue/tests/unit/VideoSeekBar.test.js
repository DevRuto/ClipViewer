import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import VideoSeekBar from '@/components/video-player/VideoSeekBar.vue'
import { formatDuration } from '@/composables/useDuration.js'

function stubTrackRect(wrapper, { left = 0, width = 200 } = {}) {
  const track = wrapper.find('[role="slider"]').element
  vi.spyOn(track, 'getBoundingClientRect').mockReturnValue({ left, width, right: left + width, top: 0, bottom: 0, height: 0 })
  return track
}

describe('VideoSeekBar', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders progress/buffered widths proportional to duration', () => {
    const wrapper = mount(VideoSeekBar, { props: { currentTime: 30, duration: 100, bufferedEnd: 60 } })
    const bars = wrapper.findAll('.rounded-full.bg-primary, .rounded-full.bg-white\\/40')
    // played bar (bg-primary) and buffered bar (bg-white/40) each get an inline width %
    const played = wrapper.find('.bg-primary')
    const buffered = wrapper.find('.bg-white\\/40')
    expect(played.attributes('style')).toContain('width: 30%')
    expect(buffered.attributes('style')).toContain('width: 60%')
    expect(bars.length).toBeGreaterThan(0)
  })

  it('emits seek at the pointer ratio on pointerdown, and while dragging', async () => {
    const wrapper = mount(VideoSeekBar, { props: { currentTime: 0, duration: 100, bufferedEnd: 0 } })
    const track = stubTrackRect(wrapper, { left: 0, width: 200 })

    track.dispatchEvent(new PointerEvent('pointerdown', { clientX: 50, pointerId: 1, button: 0, bubbles: true }))
    await wrapper.vm.$nextTick()
    expect(wrapper.emitted('seek')[0][0]).toBeCloseTo(25)
    expect(wrapper.emitted('scrub-start')).toHaveLength(1)

    track.dispatchEvent(new PointerEvent('pointermove', { clientX: 100, pointerId: 1, bubbles: true }))
    expect(wrapper.emitted('seek')[1][0]).toBeCloseTo(50)

    track.dispatchEvent(new PointerEvent('pointerup', { clientX: 100, pointerId: 1, bubbles: true }))
    expect(wrapper.emitted('scrub-end')).toHaveLength(1)
  })

  it('does not emit seek while merely hovering (no pointerdown)', () => {
    const wrapper = mount(VideoSeekBar, { props: { currentTime: 0, duration: 100, bufferedEnd: 0 } })
    const track = stubTrackRect(wrapper, { left: 0, width: 200 })

    track.dispatchEvent(new PointerEvent('pointermove', { clientX: 100, pointerId: 1, bubbles: true }))
    expect(wrapper.emitted('seek')).toBeUndefined()
  })

  it('ignores pointerdown when duration is unknown', () => {
    const wrapper = mount(VideoSeekBar, { props: { currentTime: 0, duration: 0, bufferedEnd: 0 } })
    const track = stubTrackRect(wrapper)

    track.dispatchEvent(new PointerEvent('pointerdown', { clientX: 50, pointerId: 1, button: 0, bubbles: true }))
    expect(wrapper.emitted('seek')).toBeUndefined()
    expect(wrapper.emitted('scrub-start')).toBeUndefined()
  })

  it('shows a scrub preview thumbnail cropped from the sprite manifest while hovering', async () => {
    const manifest = { image: 'vid-sprite.jpg', interval: 5, tileWidth: 160, tileHeight: 90, columns: 7, count: 41 }
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(manifest) }),
    )

    const wrapper = mount(VideoSeekBar, {
      props: { currentTime: 0, duration: 200, bufferedEnd: 0, scrubSprite: '/files/thumbnails/vid-sprite.json' },
    })
    await flushPromises()

    const track = stubTrackRect(wrapper, { left: 0, width: 200 })
    // hoverRatio 0.5 -> hoverTime 100s -> tile index 20 -> col 6, row 2 (columns=7)
    track.dispatchEvent(new PointerEvent('pointermove', { clientX: 100, pointerId: 1, bubbles: true }))
    await wrapper.vm.$nextTick()

    expect(fetch).toHaveBeenCalledWith('/files/thumbnails/vid-sprite.json')
    const preview = wrapper.find('.overflow-hidden.rounded.border-white\\/20')
    expect(preview.exists()).toBe(true)
    const style = preview.attributes('style')
    expect(style).toContain('width: 160px')
    expect(style).toContain('height: 90px')
    expect(style).toContain('background-image: url("/files/thumbnails/vid-sprite.jpg")')
    expect(style).toContain('background-position: -960px -180px')
  })

  it('renders no preview thumbnail when the clip has no scrub sprite', () => {
    const wrapper = mount(VideoSeekBar, { props: { currentTime: 0, duration: 100, bufferedEnd: 0 } })
    const track = stubTrackRect(wrapper, { left: 0, width: 200 })

    track.dispatchEvent(new PointerEvent('pointermove', { clientX: 100, pointerId: 1, bubbles: true }))
    expect(wrapper.find('.overflow-hidden.rounded.border-white\\/20').exists()).toBe(false)
  })

  it('ignores a failed manifest fetch and still shows the time-only tooltip', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))

    const wrapper = mount(VideoSeekBar, {
      props: { currentTime: 0, duration: 100, bufferedEnd: 0, scrubSprite: '/files/thumbnails/vid-sprite.json' },
    })
    await flushPromises()

    const track = stubTrackRect(wrapper, { left: 0, width: 200 })
    track.dispatchEvent(new PointerEvent('pointermove', { clientX: 100, pointerId: 1, bubbles: true }))
    await wrapper.vm.$nextTick()

    expect(wrapper.find('.overflow-hidden.rounded.border-white\\/20').exists()).toBe(false)
    expect(wrapper.text()).toContain(formatDuration(50))
  })
})
