// Shared style for icon buttons in the player control bar. Kept as a plain string (rather than
// scoped CSS) since it's reused across sibling components whose `<style scoped>` blocks can't see
// each other.
// `pointer-coarse:p-2.5` grows the tap target on touch devices (30px -> ~38px) without affecting
// the compact mouse-driven layout, where the extra padding would just waste space.
export const playerButtonClass =
  'inline-flex items-center justify-center rounded-full p-1.5 pointer-coarse:p-2.5 text-white/90 transition-colors hover:bg-white/15 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-white/70 disabled:pointer-events-none disabled:opacity-40'
