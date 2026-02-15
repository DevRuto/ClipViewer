import { describe, expect, it } from 'vitest'

import { formatDuration, parseTimeToSeconds } from '@/composables/useDuration.js'

describe('useDuration', () => {
  describe('formatDuration', () => {
    it('returns 0:00 for falsy input', () => {
      expect(formatDuration(null)).toBe('0:00')
      expect(formatDuration(undefined)).toBe('0:00')
      expect(formatDuration(0)).toBe('0:00')
    })

    it('formats seconds as m:ss', () => {
      expect(formatDuration(5)).toBe('0:05')
      expect(formatDuration(65)).toBe('1:05')
      expect(formatDuration(601)).toBe('10:01')
    })

    it('formats seconds as h:mm:ss when >= 1 hour', () => {
      expect(formatDuration(3600)).toBe('1:00:00')
      expect(formatDuration(3661)).toBe('1:01:01')
    })

    it('formats dot-separated time strings like 00:00:20.2240000', () => {
      expect(formatDuration('00:00:20.2240000')).toBe('0:20')
      expect(formatDuration('01:02:03.000')).toBe('1:02:03')
    })

    it('returns 0:00 on invalid string format', () => {
      expect(formatDuration('not-a-time')).toBe('0:00')
      expect(formatDuration('12:xx')).toBe('0:00')
    })
  })

  describe('parseTimeToSeconds', () => {
    it('returns 0 for falsy', () => {
      expect(parseTimeToSeconds('')).toBe(0)
      expect(parseTimeToSeconds(null)).toBe(0)
    })

    it('parses mm:ss', () => {
      expect(parseTimeToSeconds('01:05')).toBe(65)
      expect(parseTimeToSeconds('10:00')).toBe(600)
    })

    it('parses hh:mm:ss', () => {
      expect(parseTimeToSeconds('01:00:00')).toBe(3600)
      expect(parseTimeToSeconds('01:01:01')).toBe(3661)
    })

    it('returns 0 for unsupported formats', () => {
      expect(parseTimeToSeconds('5')).toBe(0)
      expect(parseTimeToSeconds('1:2:3:4')).toBe(0)
    })
  })
})
