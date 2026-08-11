import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import VideoSeekBar from '@/components/video-player/VideoSeekBar.vue'

function stubTrackRect(wrapper, { left = 0, width = 200 } = {}) {
  const track = wrapper.find('[role="slider"]').element
  vi.spyOn(track, 'getBoundingClientRect').mockReturnValue({ left, width, right: left + width, top: 0, bottom: 0, height: 0 })
  return track
}

describe('VideoSeekBar', () => {
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
})
