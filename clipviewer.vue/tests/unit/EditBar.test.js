import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'

import EditBar from '@/components/EditBar.vue'

describe('EditBar', () => {
  it('on mount, defaults end time to the full video duration and emits a valid range', () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 120 },
    })

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted).toBeTruthy()
    expect(emitted[emitted.length - 1]).toEqual([{ startTime: 0, endTime: 120 }])
    expect(wrapper.text()).toContain('Video duration: 2:00')
  })

  it('emits null and shows an error when start time is after end time', async () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 60 },
    })

    const inputs = wrapper.findAll('input[type="text"]')
    await inputs[0].setValue('0:50')
    await inputs[1].setValue('0:10')

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted[emitted.length - 1]).toEqual([null])
    expect(wrapper.text()).toContain('Invalid timestamps')
  })

  it('emits null when the end time exceeds the video duration', async () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 30 },
    })

    const inputs = wrapper.findAll('input[type="text"]')
    await inputs[0].setValue('0:00')
    await inputs[1].setValue('0:45')

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted[emitted.length - 1]).toEqual([null])
  })

  it('emits a valid range when both timestamps are within bounds', async () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 100 },
    })

    const inputs = wrapper.findAll('input[type="text"]')
    await inputs[0].setValue('0:10')
    await inputs[1].setValue('0:40')

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted[emitted.length - 1]).toEqual([{ startTime: 10, endTime: 40 }])
  })

  it('setStartTime uses the current time of the video player ref', async () => {
    const videoPlayerRef = { currentTime: 15 }
    const wrapper = mount(EditBar, {
      props: { videoDuration: 100, videoPlayerRef },
    })

    wrapper.vm.setStartTime()
    await wrapper.vm.$nextTick()

    const inputs = wrapper.findAll('input[type="text"]')
    expect(inputs[0].element.value).toBe('0:15')
  })

  it('setStartTime does nothing when there is no video player ref', () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 100, videoPlayerRef: null },
    })

    expect(() => wrapper.vm.setStartTime()).not.toThrow()
    const inputs = wrapper.findAll('input[type="text"]')
    expect(inputs[0].element.value).toBe('0:00')
  })

  describe('trim bar dragging', () => {
    // The bar's drag math is all relative to trackRef's own bounding box, which jsdom reports
    // as all-zero by default - stub a fixed 200px-wide box so ratioFromClientX resolves clientX
    // to a real position along the (0, videoDuration) range instead of always clamping to 0.
    function stubTrackWidth(wrapper, width = 200) {
      const track = wrapper.find('[role="slider"]').element.parentElement
      track.getBoundingClientRect = () => ({ left: 0, width, top: 0, height: 56, right: width, bottom: 56 })
      return track
    }

    function pointer(el, type, clientX) {
      return el.element.dispatchEvent(
        new MouseEvent(type, { clientX, button: 0, bubbles: true, cancelable: true }),
      )
    }

    it('dragging the start handle updates the start time and seeks the preview', async () => {
      const videoPlayerRef = { currentTime: 0, goToTime: vi.fn() }
      const wrapper = mount(EditBar, {
        props: { videoDuration: 100, videoPlayerRef },
      })
      stubTrackWidth(wrapper)

      const startHandle = wrapper.find('[aria-label="Start time"]')
      startHandle.element.setPointerCapture = vi.fn()
      startHandle.element.releasePointerCapture = vi.fn()
      await pointer(startHandle, 'pointerdown', 0)
      await pointer(startHandle, 'pointermove', 50) // 50/200 = 25% of 100s = 25s
      await pointer(startHandle, 'pointerup', 50)

      const inputs = wrapper.findAll('input[type="text"]')
      expect(inputs[0].element.value).toBe('0:25')
      expect(videoPlayerRef.goToTime).toHaveBeenCalledWith(25)

      const emitted = wrapper.emitted('timestamps-change')
      expect(emitted[emitted.length - 1]).toEqual([{ startTime: 25, endTime: 100 }])
    })

    it('dragging the start handle past the end handle stops short of it', async () => {
      const wrapper = mount(EditBar, {
        props: { videoDuration: 100 },
      })
      stubTrackWidth(wrapper)

      const startHandle = wrapper.find('[aria-label="Start time"]')
      startHandle.element.setPointerCapture = vi.fn()
      startHandle.element.releasePointerCapture = vi.fn()
      await pointer(startHandle, 'pointerdown', 0)
      await pointer(startHandle, 'pointermove', 200) // would be 100s, past the end handle at 100s
      await pointer(startHandle, 'pointerup', 200)

      const inputs = wrapper.findAll('input[type="text"]')
      expect(inputs[0].element.value).toBe('1:39') // clamped to end (100s) minus the 0.5s min gap
    })

    it('dragging the selection moves both handles together', async () => {
      const wrapper = mount(EditBar, {
        props: { videoDuration: 100 },
      })
      const inputs = wrapper.findAll('input[type="text"]')
      await inputs[0].setValue('0:10')
      await inputs[1].setValue('0:30')
      stubTrackWidth(wrapper)

      const selection = wrapper.find('[data-testid="trim-selection"]')
      selection.element.setPointerCapture = vi.fn()
      selection.element.releasePointerCapture = vi.fn()
      await pointer(selection, 'pointerdown', 20) // grabs right at the selection's start edge (10s)
      await pointer(selection, 'pointermove', 60) // 60/200 = 30% of 100s = 30s -> new start, same 20s width
      await pointer(selection, 'pointerup', 60)

      const emitted = wrapper.emitted('timestamps-change')
      expect(emitted[emitted.length - 1]).toEqual([{ startTime: 30, endTime: 50 }])
    })
  })
})
