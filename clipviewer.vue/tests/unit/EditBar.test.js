import { describe, expect, it } from 'vitest'
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
})
