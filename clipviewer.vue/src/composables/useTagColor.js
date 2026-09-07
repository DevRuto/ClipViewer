// Assigns each tag a consistent color (by name) from a fixed palette, so the same tag always
// reads as the same color across the app. Two variants per hue: an outline chip for the default
// look, and a solid chip for an "active/selected" state (e.g. the current filter in ClipsView).
const PALETTE = [
  {
    outline: 'border-red-300 bg-red-50 text-red-700 dark:border-red-800 dark:bg-red-900/20 dark:text-red-300',
    solid: 'border-red-600 bg-red-600 text-white dark:border-red-500 dark:bg-red-500',
  },
  {
    outline:
      'border-orange-300 bg-orange-50 text-orange-700 dark:border-orange-800 dark:bg-orange-900/20 dark:text-orange-300',
    solid: 'border-orange-600 bg-orange-600 text-white dark:border-orange-500 dark:bg-orange-500',
  },
  {
    outline: 'border-amber-300 bg-amber-50 text-amber-700 dark:border-amber-800 dark:bg-amber-900/20 dark:text-amber-300',
    solid: 'border-amber-600 bg-amber-600 text-white dark:border-amber-500 dark:bg-amber-500',
  },
  {
    outline: 'border-lime-300 bg-lime-50 text-lime-700 dark:border-lime-800 dark:bg-lime-900/20 dark:text-lime-300',
    solid: 'border-lime-600 bg-lime-600 text-white dark:border-lime-500 dark:bg-lime-500',
  },
  {
    outline:
      'border-emerald-300 bg-emerald-50 text-emerald-700 dark:border-emerald-800 dark:bg-emerald-900/20 dark:text-emerald-300',
    solid: 'border-emerald-600 bg-emerald-600 text-white dark:border-emerald-500 dark:bg-emerald-500',
  },
  {
    outline: 'border-teal-300 bg-teal-50 text-teal-700 dark:border-teal-800 dark:bg-teal-900/20 dark:text-teal-300',
    solid: 'border-teal-600 bg-teal-600 text-white dark:border-teal-500 dark:bg-teal-500',
  },
  {
    outline: 'border-cyan-300 bg-cyan-50 text-cyan-700 dark:border-cyan-800 dark:bg-cyan-900/20 dark:text-cyan-300',
    solid: 'border-cyan-600 bg-cyan-600 text-white dark:border-cyan-500 dark:bg-cyan-500',
  },
  {
    outline: 'border-blue-300 bg-blue-50 text-blue-700 dark:border-blue-800 dark:bg-blue-900/20 dark:text-blue-300',
    solid: 'border-blue-600 bg-blue-600 text-white dark:border-blue-500 dark:bg-blue-500',
  },
  {
    outline:
      'border-violet-300 bg-violet-50 text-violet-700 dark:border-violet-800 dark:bg-violet-900/20 dark:text-violet-300',
    solid: 'border-violet-600 bg-violet-600 text-white dark:border-violet-500 dark:bg-violet-500',
  },
  {
    outline:
      'border-fuchsia-300 bg-fuchsia-50 text-fuchsia-700 dark:border-fuchsia-800 dark:bg-fuchsia-900/20 dark:text-fuchsia-300',
    solid: 'border-fuchsia-600 bg-fuchsia-600 text-white dark:border-fuchsia-500 dark:bg-fuchsia-500',
  },
  {
    outline: 'border-pink-300 bg-pink-50 text-pink-700 dark:border-pink-800 dark:bg-pink-900/20 dark:text-pink-300',
    solid: 'border-pink-600 bg-pink-600 text-white dark:border-pink-500 dark:bg-pink-500',
  },
  {
    outline: 'border-rose-300 bg-rose-50 text-rose-700 dark:border-rose-800 dark:bg-rose-900/20 dark:text-rose-300',
    solid: 'border-rose-600 bg-rose-600 text-white dark:border-rose-500 dark:bg-rose-500',
  },
]

function paletteEntry(tag) {
  if (!tag) return PALETTE[0]
  let hash = 0
  for (let i = 0; i < tag.length; i++) {
    hash = (hash << 5) - hash + tag.charCodeAt(i)
    hash |= 0
  }
  return PALETTE[Math.abs(hash) % PALETTE.length]
}

export function useTagColor() {
  function tagColorClasses(tag) {
    return paletteEntry(tag).outline
  }

  function tagColorSolidClasses(tag) {
    return paletteEntry(tag).solid
  }

  return { tagColorClasses, tagColorSolidClasses }
}
