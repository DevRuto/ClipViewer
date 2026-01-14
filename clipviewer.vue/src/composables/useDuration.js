export function formatDuration(duration) {
  if (!duration) return '0:00'

  // Handle integer seconds
  if (typeof duration === 'number') {
    const totalSeconds = Math.floor(duration)
    const hours = Math.floor(totalSeconds / 3600)
    const minutes = Math.floor((totalSeconds % 3600) / 60)
    const seconds = totalSeconds % 60

    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
    } else {
      return `${minutes}:${seconds.toString().padStart(2, '0')}`
    }
  }

  // Handle format like "00:00:20.2240000"
  const parts = duration.split('.')
  const timePart = parts[0]

  // Split into hours, minutes, seconds
  const timeComponents = timePart.split(':').map(Number)

  let hours = 0
  let minutes = 0
  let seconds = 0

  if (timeComponents.length === 3) {
    ;[hours, minutes, seconds] = timeComponents
  } else if (timeComponents.length === 2) {
    ;[minutes, seconds] = timeComponents
  } else if (timeComponents.length === 1) {
    seconds = timeComponents[0]
  }

  // Format based on duration
  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
  } else {
    return `${minutes}:${seconds.toString().padStart(2, '0')}`
  }
}
