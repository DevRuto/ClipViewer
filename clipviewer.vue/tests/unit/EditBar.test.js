import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'

import EditBar from '@/components/EditBar.vue'

// The bar's drag math is all relative to trackRef's own bounding box, which jsdom reports as
// all-zero by default - stub a fixed 200px-wide box so ratioFromClientX resolves clientX to a
// real position along the (0, videoDuration) range instead of always clamping to 0.
function stubTrackWidth(wrapper, width = 200) {
  const track = wrapper.find('[role="slider"]').element.parentElement
  track.getBoundingClientRect = () => ({ left: 0, width, top: 0, height: 56, right: width, bottom: 56 })
  return track
}

function pointer(el, type, clientX) {
  return el.element.dispatchEvent(new MouseEvent(type, { clientX, button: 0, bubbles: true, cancelable: true }))
}

// jsdom doesn't actually decode video or implement canvas, so generateFilmstrip's offscreen
// <video>/<canvas> pair is stubbed here to drive its loadedmetadata -> seek-per-tile -> drawImage
// loop deterministically: setting currentTime synchronously fires a 'seeked' event back, and
// getContext/toDataURL are faked since jsdom has no canvas backend to call through to.
function stubFilmstripCapture() {
  const realCreateElement = document.createElement.bind(document)
  vi.spyOn(document, 'createElement').mockImplementation((tag) => {
    if (tag === 'video') {
      const el = realCreateElement('video')
      Object.defineProperty(el, 'duration', { value: 10, configurable: true })
      Object.defineProperty(el, 'videoWidth', { value: 16, configurable: true })
      Object.defineProperty(el, 'videoHeight', { value: 9, configurable: true })
      Object.defineProperty(el, 'currentTime', {
        get: () => 0,
        set: () => {
          queueMicrotask(() => el.dispatchEvent(new Event('seeked')))
        },
        configurable: true,
      })
      queueMicrotask(() => el.dispatchEvent(new Event('loadedmetadata')))
      return el
    }
    if (tag === 'canvas') {
      const el = realCreateElement('canvas')
      el.getContext = () => ({ drawImage: () => {} })
      el.toDataURL = () => 'data:image/jpeg;base64,fake'
      return el
    }
    return realCreateElement(tag)
  })
}

describe('EditBar', () => {
  it('on mount, defaults end time to the full video duration and emits a valid range', async () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 120 },
    })
    await wrapper.vm.$nextTick()

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted).toBeTruthy()
    expect(emitted[emitted.length - 1]).toEqual([{ startTime: 0, endTime: 120 }])
    expect(wrapper.text()).toContain('Selected 2:00 of 2:00')
    expect(wrapper.text()).toContain('Start 0:00')
    expect(wrapper.text()).toContain('End 2:00')
  })

  it('shows filmstrip loading progress and hides it once every tile is captured', async () => {
    stubFilmstripCapture()
    const wrapper = mount(EditBar, {
      props: { videoDuration: 10, videoUrl: 'blob:mock-video' },
    })

    expect(wrapper.text()).toContain('Loading preview 0%')

    // Once every tile is captured, filmstripProgress hits 100 and the v-if that gates the
    // overlay flips in the same render pass - so "100%" is never actually painted, only the
    // overlay's disappearance is observable.
    await vi.waitFor(() => {
      expect(wrapper.text()).not.toContain('Loading preview')
    })
    vi.restoreAllMocks()
  })

  it('adopts the full duration once it arrives, even if Edit Mode was toggled on before the video finished loading', async () => {
    // videoDuration starts at 0 until the preview video's metadata loads - mirrors mounting
    // EditBar while that's still in flight.
    const wrapper = mount(EditBar, {
      props: { videoDuration: 0 },
    })
    await wrapper.vm.$nextTick()
    expect(wrapper.emitted('timestamps-change').at(-1)).toEqual([{ startTime: 0, endTime: 0 }])

    await wrapper.setProps({ videoDuration: 120 })

    expect(wrapper.emitted('timestamps-change').at(-1)).toEqual([{ startTime: 0, endTime: 120 }])
    expect(wrapper.text()).toContain('Start 0:00')
    expect(wrapper.text()).toContain('End 2:00')
  })

  it('there is no way to type a timestamp - trimming is drag-only', () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 120 },
    })

    expect(wrapper.findAll('input').length).toBe(0)
    expect(wrapper.findAll('button').length).toBe(0)
  })

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

    expect(wrapper.text()).toContain('Start 0:25')
    expect(startHandle.attributes('aria-valuenow')).toBe('25')
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

    expect(wrapper.text()).toContain('Start 1:39') // clamped to end (100s) minus the 0.5s min gap
  })

  it('dragging the end handle updates the end time and seeks the preview', async () => {
    const videoPlayerRef = { currentTime: 0, goToTime: vi.fn() }
    const wrapper = mount(EditBar, {
      props: { videoDuration: 100, videoPlayerRef },
    })
    stubTrackWidth(wrapper)

    const endHandle = wrapper.find('[aria-label="End time"]')
    endHandle.element.setPointerCapture = vi.fn()
    endHandle.element.releasePointerCapture = vi.fn()
    await pointer(endHandle, 'pointerdown', 200)
    await pointer(endHandle, 'pointermove', 150) // 150/200 = 75% of 100s = 75s
    await pointer(endHandle, 'pointerup', 150)

    expect(wrapper.text()).toContain('End 1:15')
    expect(videoPlayerRef.goToTime).toHaveBeenCalledWith(75)

    const emitted = wrapper.emitted('timestamps-change')
    expect(emitted[emitted.length - 1]).toEqual([{ startTime: 0, endTime: 75 }])
  })

  it('dragging the selection moves both handles together', async () => {
    const wrapper = mount(EditBar, {
      props: { videoDuration: 100 },
    })
    stubTrackWidth(wrapper)

    // Put the range at [10, 30] first by dragging the handles into place.
    const startHandle = wrapper.find('[aria-label="Start time"]')
    startHandle.element.setPointerCapture = vi.fn()
    await pointer(startHandle, 'pointerdown', 0)
    await pointer(startHandle, 'pointermove', 20) // 20/200 = 10% of 100s = 10s
    await pointer(startHandle, 'pointerup', 20)

    const endHandle = wrapper.find('[aria-label="End time"]')
    endHandle.element.setPointerCapture = vi.fn()
    await pointer(endHandle, 'pointerdown', 200)
    await pointer(endHandle, 'pointermove', 60) // 60/200 = 30% of 100s = 30s
    await pointer(endHandle, 'pointerup', 60)

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
